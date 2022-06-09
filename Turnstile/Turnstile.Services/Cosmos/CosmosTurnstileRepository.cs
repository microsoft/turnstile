// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Net;
using Turnstile.Core.Constants;
using Turnstile.Core.Interfaces;
using Turnstile.Core.Models;

namespace Turnstile.Services.Cosmos
{
    public class CosmosTurnstileRepository : IDisposable, ITurnstileRepository
    {
        // TODO: We should probably improve our model validation here.
        //       Technically, all uses of this repository should be behind a web app
        //       or API but we never know how people are going to use a public class...

        private bool disposedValue;

        private readonly CosmosClient cosmosClient;
        private readonly CosmosConfiguration cosmosConfig;

        private const string allSubsPartitionKey = "subscriptions";

        public CosmosTurnstileRepository(CosmosConfiguration cosmosConfig)
        {
            ArgumentNullException.ThrowIfNull(cosmosConfig, nameof(cosmosConfig));

            this.cosmosConfig = cosmosConfig;

            cosmosClient = new CosmosClient(cosmosConfig.EndpointUrl, cosmosConfig.AccessKey);
        }

        public async Task<Subscription> CreateSubscription(Subscription subscription)
        {
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));

            var container = GetContainer();

            await container.CreateItemAsync(PutInEnvelope(subscription), new PartitionKey(allSubsPartitionKey));
            await container.CreateItemAsync(PutInEnvelope(new SeatingSummary(), subscription.SubscriptionId!), new PartitionKey(subscription.SubscriptionId!));

            return subscription;
        }

        public async Task<Subscription> ReplaceSubscription(Subscription subscription)
        {
            ArgumentNullException.ThrowIfNull(subscription, nameof(subscription));

            var container = GetContainer();
            var subEnvelope = PutInEnvelope(subscription);

            await container.ReplaceItemAsync(subEnvelope, subEnvelope.Id, new PartitionKey(allSubsPartitionKey));

            return subscription;
        }

        public async Task<IList<Subscription>> GetSubscriptions(
            string? state = null, string? offerId = null, string? planId = null, string? tenantId = null)
        {
            var container = GetContainer();
            var subscriptions = new List<Subscription>();

            var queryable = container.GetItemLinqQueryable<CosmosEnvelope<Subscription>>(
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(allSubsPartitionKey) })
                .AsQueryable();

            if (state != null)
            {
                queryable = queryable.Where(e => e.Data!.State == state);
            }

            if (offerId != null)
            {
                queryable = queryable.Where(e => e.Data!.OfferId == offerId);
            }

            if (planId != null)
            {
                queryable = queryable.Where(e => e.Data!.PlanId == planId);
            }

            if (tenantId != null)
            {
                queryable = queryable.Where(e => e.Data!.TenantId == tenantId);
            }

            using (var feedIterator = queryable.ToFeedIterator())
            {
                while (feedIterator.HasMoreResults)
                {
                    var feedResponse = await feedIterator.ReadNextAsync();

                    subscriptions.AddRange(feedResponse.Select(e => e.Data!));
                }
            }

            return subscriptions;
        }

        public async Task<Subscription?> GetSubscription(string subscriptionId)
        {
            ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));

            var container = GetContainer();

            try
            {
                var cosmosResponse = await container.ReadItemAsync<CosmosEnvelope<Subscription>>(
                    subscriptionId, new PartitionKey(allSubsPartitionKey));

                return cosmosResponse.Resource!.Data;
            }
            catch (CosmosException ex)
            {
                // It's kind of silly that Cosmos throws a 404 exception when no document is found
                // but it seems like this is a topic of some debate with very valid reasons on 
                // either side >> https://github.com/Azure/azure-cosmos-dotnet-v3/issues/122

                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<IList<Seat>> GetSeats(string subscriptionId, string? byUserId = null, string? byEmail = null)
        {
            ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));

            var seats = new List<Seat>();

            var queryDefinition = new QueryDefinition(
                "SELECT * FROM s WHERE s.data_type = 'Seat' " +
                "AND (IS_NULL(s.data.expires_utc) OR s.data.expires_utc > GetCurrentDateTime()) " +
                (string.IsNullOrEmpty(byUserId) ? string.Empty : "AND (s.data.occupant.user_id = @userId OR s.data.reservation.user_id = @userId) ") +
                (string.IsNullOrEmpty(byEmail) ? string.Empty : "AND (s.data.occupant.email = @userEmail OR s.data.reservation.email = @userEmail)"));

            if (!string.IsNullOrEmpty(byUserId))
            {
                queryDefinition = queryDefinition.WithParameter("@userId", byUserId);
            }

            if (!string.IsNullOrEmpty(byEmail))
            {
                queryDefinition = queryDefinition.WithParameter("@userEmail", byEmail);
            }

            var container = GetContainer();
            var queryOptions = new QueryRequestOptions { PartitionKey = new PartitionKey(subscriptionId) };

            using (var iterator = container.GetItemQueryIterator<CosmosEnvelope<Seat>>(queryDefinition, requestOptions: queryOptions))
            {
                while (iterator.HasMoreResults)
                {
                    var resultSet = await iterator.ReadNextAsync();

                    seats.AddRange(resultSet.Select(e => e.Data!));
                }
            }

            return seats;
        }

        public async Task<Seat?> GetSeat(string seatId, string subscriptionId)
        {
            ArgumentNullException.ThrowIfNull(seatId, nameof(seatId));
            ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));

            var container = GetContainer();

            try
            {
                var itemResponse = await container.ReadItemAsync<CosmosEnvelope<Seat>>(
                    partitionKey: new PartitionKey(subscriptionId),
                    id: seatId);

                // There's a chance that there are expired seats in Cosmos that have not yet
                // been picked up by Cosmos TTL. Since this is real $$$ we're talking about, an
                // expired seat may as well not be a seat at all...

                if (itemResponse.Resource.Data!.ExpirationDateTimeUtc <= DateTime.UtcNow)
                {
                    return null;
                }
                else
                {
                    return itemResponse.Resource.Data;
                }
            }
            catch (CosmosException ex)
            {
                // It's kind of silly that Cosmos throws a 404 exception when no document is found
                // but it seems like this is a topic of some debate with very valid reasons on 
                // either side >> https://github.com/Azure/azure-cosmos-dotnet-v3/issues/122

                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public async Task<SeatCreationContext> CreateSeat(Seat seat, Subscription subscription)
        {
            ArgumentNullException.ThrowIfNull(seat, nameof(seat));

            while (true)
            {
                var actualSeatSummary = await GetActualSeatSummary(subscription.SubscriptionId!);
                var seatSummaryEnvelope = await GetSeatSummaryEnvelope(subscription.SubscriptionId!);
                var currentSeatSummary = seatSummaryEnvelope.Data!;

                currentSeatSummary.StandardSeatCount = actualSeatSummary.StandardSeatCount;
                currentSeatSummary.LimitedSeatCount = actualSeatSummary.LimitedSeatCount;

                if (seat.SeatType == SeatTypes.Standard)
                {
                    if (subscription.TotalSeats != null && 
                        subscription.TotalSeats <= actualSeatSummary.StandardSeatCount)
                    {
                        return new SeatCreationContext
                        {
                            IsSeatCreated = false,
                            SeatingSummary = currentSeatSummary
                        };
                    }

                    currentSeatSummary.StandardSeatCount++;
                }
                else
                {
                    currentSeatSummary.LimitedSeatCount++;
                }

                seatSummaryEnvelope.Data = currentSeatSummary;

                var container = GetContainer();
                var seatEnvelope = PutInEnvelope(seat);

                var txnBatchResponse = await container.CreateTransactionalBatch(new PartitionKey(subscription.SubscriptionId))
                    .ReplaceItem(seatSummaryEnvelope.Id, seatSummaryEnvelope, new TransactionalBatchItemRequestOptions { IfMatchEtag = seatSummaryEnvelope.Etag })
                    .CreateItem(seatEnvelope)
                    .ExecuteAsync();

                using (txnBatchResponse)
                {
                    if (txnBatchResponse.IsSuccessStatusCode)
                    {
                        return new SeatCreationContext
                        {
                            CreatedSeat = seat,
                            IsSeatCreated = true,
                            SeatingSummary = currentSeatSummary
                        };
                    }
                }
            }
        }

        public async Task<Seat> ReplaceSeat(Seat seat)
        {
            ArgumentNullException.ThrowIfNull(seat, nameof(seat));

            var container = GetContainer();
            var seatEnvelope = PutInEnvelope(seat);

            await container.ReplaceItemAsync(seatEnvelope, seatEnvelope.Id, new PartitionKey(seat.SubscriptionId));

            return seat;
        }

        public async Task DeleteSeat(string seatId, string subscriptionId)
        {
            ArgumentNullException.ThrowIfNull(seatId, nameof(seatId));
            ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));

            var container = GetContainer();

            try
            {
                await container.DeleteItemAsync<CosmosEnvelope<Seat>>(seatId, new PartitionKey(subscriptionId));
            }
            catch (CosmosException ex)
            {
                if (ex.StatusCode != HttpStatusCode.NotFound) // If the seat wasn't found, it's not _really_ a problem...
                {
                    throw;
                }
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (cosmosClient != null)
                    {
                        cosmosClient.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        private Container GetContainer(Database database) => database.GetContainer(cosmosConfig.ContainerId);

        private Container GetContainer() => GetContainer(GetDatabase());

        private Database GetDatabase() => cosmosClient.GetDatabase(cosmosConfig.DatabaseId);

        private async Task<CosmosEnvelope<SeatingSummary>> GetSeatSummaryEnvelope(string subscriptionId)
        {
            ArgumentNullException.ThrowIfNull(subscriptionId, nameof(subscriptionId));

            var container = GetContainer();

            try
            {
                var itemResponse = await container.ReadItemAsync<CosmosEnvelope<SeatingSummary>>(
                    partitionKey: new PartitionKey(subscriptionId),
                    id: GetSubscriptionSeatSummaryId(subscriptionId));

                return itemResponse.Resource!;
            }
            catch (CosmosException ex)
            {
                // It's kind of silly that Cosmos throws a 404 exception when no document is found
                // but it seems like this is a topic of some debate with very valid reasons on 
                // either side >> https://github.com/Azure/azure-cosmos-dotnet-v3/issues/122

                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new Exception($"Subscription [{subscriptionId}] seat counts not found.");
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task<SeatingSummary> GetActualSeatSummary(string subscriptionId)
        {
            var seatSummary = new SeatingSummary();

            var queryDefinition = new QueryDefinition(
                "SELECT COUNT(1) AS seat_count, e.data.seat_type FROM e " +
                "WHERE e.data_type = 'Seat' " +
                "AND (IS_NULL(e.data.expires_utc) OR e.data.expires_utc > GetCurrentDateTime()) " +
                "GROUP BY e.data.seat_type");

            var container = GetContainer();
            var queryOptions = new QueryRequestOptions { PartitionKey = new PartitionKey(subscriptionId) };

            using (var iterator = container.GetItemQueryIterator<CosmosSeatCount>(queryDefinition, requestOptions: queryOptions))
            {
                while (iterator.HasMoreResults)
                {
                    var resultSet = await iterator.ReadNextAsync();

                    foreach (var seatCt in resultSet)
                    {
                        if (seatCt.SeatType == SeatTypes.Standard)
                        {
                            seatSummary.StandardSeatCount = seatCt.SeatCount;
                        }
                        else
                        {
                            seatSummary.LimitedSeatCount = seatCt.SeatCount;
                        }
                    }
                }
            }

            return seatSummary;
        }

        private string GetSubscriptionSeatSummaryId(string subscriptionId) =>
            $"{subscriptionId}___seat_summary";

        private CosmosEnvelope<SeatingSummary> PutInEnvelope(SeatingSummary seatSummary, string subscriptionId) =>
            new CosmosEnvelope<SeatingSummary>
            {
                Data = seatSummary,
                DataType = nameof(SeatingSummary),
                Id = GetSubscriptionSeatSummaryId(subscriptionId),
                PartitionId = subscriptionId
            };

        private CosmosEnvelope<Seat> PutInEnvelope(Seat seat)
        {
            var seatEnvelope = new CosmosEnvelope<Seat>
            {
                Data = seat,
                DataType = nameof(Seat),
                Id = seat.SeatId,
                PartitionId = seat.SubscriptionId
            };

            if (seat.ExpirationDateTimeUtc.GetValueOrDefault(DateTime.UtcNow) > DateTime.UtcNow)
            {
                // It turns out that Cosmos has a really cool time-to-live (TTL) feature!
                // Since a seat really only exists if someone is sitting in it, setting the TTL on it
                // should automatically delete it when it expires.

                seatEnvelope.TimeToLive = (int)seat.ExpirationDateTimeUtc!.Value.Subtract(DateTime.UtcNow).TotalSeconds;
            }

            return seatEnvelope;
        }

        private CosmosEnvelope<Subscription> PutInEnvelope(Subscription subscription) =>
            new CosmosEnvelope<Subscription>
            {
                Data = subscription,
                DataType = nameof(Subscription),
                Id = subscription.SubscriptionId,
                PartitionId = allSubsPartitionKey
            };
    }
}
