#!/bin/bash

run_core_api_tests() {
    api_base_url=$1
    api_key=$2

    # 🧪 Test #1 - create a new subscription

    subscription_json=$(cat ./models/core_api_tests/subscription.json)
    subscription_id=$(echo "$subscription_json" | jq -r ".subscription_id")
    tenant_id=$(echo "$subscription_json" | jq -r ".tenant_id")
    create_subscription_url="$api_base_url/saas/subscriptions/$subscription_id" 

    create_subscription_status_code=$(curl -s \
        -X POST \
        -H "Content-Type: application/json" \
        -H "x-functions-key: $api_key" \
        -d "$subscription_json" \
        -o /dev/null \
        -w "%{http_code}" \
        "$create_subscription_url")

    if [[ "$create_subscription_status_code" == "200" ]]; then
        echo "✔️   Subscription [$subscription_id] successfully created."

        # 🧪 Test #2 - Patch an existing subscription

        subscription_patch_json=$(cat ./models/core_api_tests/subscription_patch.json)
        patch_subscription_url="$api_base_url/saas/subscriptions/$subscription_id"

        patch_subscription_response=$(curl \
            -X PATCH \
            -H "Content-Type: application/json" \
            -H "x-functions-key: $api_key" \
            -d "$subscription_patch_json" \
            "$patch_subscription_url")

        # Now let's make sure that the patch was properly applied...

        subscription_projector="{subscription_id, subscription_name, plan_id, state, admin_role_name, user_role_name, admin_name, admin_email, total_seats, subscriber_country: .subscriber_info.country, source_subscription_id: .source_subscription.id, seating_strategy_name: .seating_config.seating_strategy_name, limited_overflow_seating: .seating_config.limited_overflow_seating_enabled, reservation_expiry: .seating_config.seat_reservation_expiry_in_days, seat_expiry: .seating_config.default_seat_expiry_in_days}"

        actual_subscription=$(echo "$patch_subscription_response" | jq "$subscription_projector")
        expected_subscription=$(echo "$subscription_patch_json" | jq "$subscription_projector")

        if [[ "$actual_subscription" == "$expected_subscription" ]]; then
            echo "✔️   Subscription [$subscription_id] successfully patched."

            # 🧪 Test #3 - Reserve a seat in the subscription

            reserve_seat_id=$(cat /proc/sys/kernel/random/uuid) # Generate a random seat ID
            reserve_json=$(cat ./models/core_api_tests/reserve_seat.json)
            reserve_email=$(echo "$reserve_json" | jq -r ".email")
            reserve_url="$api_base_url/saas/subscriptions/$subscription_id/seats/$reserve_seat_id/reserve"

            reserve_seat_response=$(curl \
                -X POST \
                -H "Content-Type: application/json" \
                -H "x-functions-key: $api_key" \
                -d "$reserve_json" \
                "$reserve_url")

            reserved_seat_id=$(echo "$reserve_seat_response" | jq -r ".seat_id")
            reserved_subscription_id=$(echo "$reserve_seat_response" | jq -r ".subscription_id")
            reserved_email=$(echo "$reserve_seat_response" | jq -r ".reservation.email")

            if [[ "$reserve_seat_id" == "$reserved_seat_id" && "$subscription_id" == "$reserved_subscription_id" && "$reserve_email" == "$reserved_email" ]]; then
                echo "✔️   Seat [$reserve_seat_id] successfully reserved for [$reserve_email]."

                # 🧪 Test #4 - Redeem a reserved seat

                redeem_seat_id="$reserve_seat_id"
                redeem_json=$(cat ./models/core_api_tests/redeem_seat.json)
                redeem_user_id=$(echo "$redeem_json" | jq -r ".user_id")
                redeem_url="$api_base_url/saas/subscriptions/$subscription_id/seats/$redeem_seat_id/redeem"

                redeem_seat_response=$(curl \
                    -X POST \
                    -H "Content-Type: application/json" \
                    -H "x-functions-key: $api_key" \
                    -d "$redeem_json" \
                    "$redeem_url")

                redeemed_seat_id=$(echo "$redeem_seat_response" | jq -r ".seat_id")
                redeemed_seat_type=$(echo "$redeem_seat_response" | jq -r ".seat_type")
                redeemed_seat_subscription_id=$(echo "$redeem_seat_response" | jq -r ".subscription_id")
                redeemed_seat_tenant_id=$(echo "$redeem_seat_response" | jq -r ".occupant.tenant_id")
                redeemed_seat_user_id=$(echo "$redeem_seat_response" | jq -r ".occupant.user_id")

                if [[ "$redeemed_seat_type" == "standard" && "$redeem_seat_id" == "$redeemed_seat_id" && "$subscription_id" == "$redeemed_seat_subscription_id" && "$tenant_id" == "$redeemed_seat_tenant_id" && "$redeem_user_id" == "$redeemed_seat_user_id" ]]; then
                    echo "✔️   Seat [$redeem_seat_id] successfully redeemed."

                    # 🧪 Test #5 - Request some "walk-up" seats

                    # This subscription has 5 total_seats. We've already occupied one of them during the seat reservation test
                    # so let's max this out and request 4 additional seats for "walk-up" users.

                    for i in {2..5}; do
                        request_seat_id=$(cat /proc/sys/kernel/random/uuid) # Generate a random seat ID
                        request_seat_json=$(cat "./models/core_api_tests/request_seat_$i.json")
                        request_seat_user_id=$(echo "$request_seat_json" | jq -r ".user_id")
                        request_seat_url="$api_base_url/saas/subscriptions/$subscription_id/seats/$request_seat_id/request"

                        request_seat_response=$(curl \
                            -X POST \
                            -H "Content-Type: application/json" \
                            -H "x-functions-key: $api_key" \
                            -d "$request_seat_json" \
                            "$request_seat_url")

                        provided_seat_id=$(echo "$request_seat_response" | jq -r ".seat_id")
                        provided_seat_type=$(echo "$request_seat_response" | jq -r ".seat_type")
                        provided_seat_subscription_id=$(echo "$request_seat_response" | jq -r ".subscription_id")
                        provided_seat_tenant_id=$(echo "$request_seat_response" | jq -r ".occupant.tenant_id")
                        provided_seat_user_id=$(echo "$request_seat_response" | jq -r ".occupant.user_id")

                        if [[ "$provided_seat_type" == "standard" && "$request_seat_id" == "$provided_seat_id" && "$subscription_id" == "$provided_seat_subscription_id" && "$tenant_id" == "$provided_seat_tenant_id" && "$request_seat_user_id" == "$provided_seat_user_id" ]]; then
                            echo "✔️   Seat [$i] ([$request_seat_id]) was successfully provided to user [$request_seat_user_id]."
                        else
                            echo "❌   Unable to request seat [$i] ([$request_seat_id])."
                            echo
                            echo "Request seat API response: $request_seat_response"
                            return 1
                        fi
                    done

                    # 🧪 Test #6 - Request a limited seat

                    # At this point, we've exhausted our supply of this subscription's total_seats. This subscription is configured to provide
                    # limited seats after the supply of standard seats has been exhausted (subscription.seating_config.limited_overflow_seating_enabled == true).

                    request_seat_id=$(cat /proc/sys/kernel/random/uuid) # Generate a random seat ID
                    request_seat_json=$(cat ./models/core_api_tests/request_limited_seat.json)
                    request_seat_user_id=$(echo "$request_seat_json" | jq -r ".user_id")
                    request_seat_url="$api_base_url/saas/subscriptions/$subscription_id/seats/$request_seat_id/request"

                    request_seat_response=$(curl \
                        -X POST \
                        -H "Content-Type: application/json" \
                        -H "x-functions-key: $api_key" \
                        -d "$request_seat_json" \
                        "$request_seat_url")

                    provided_seat_id=$(echo "$request_seat_response" | jq -r ".seat_id")
                    provided_seat_type=$(echo "$request_seat_response" | jq -r ".seat_type")
                    provided_seat_subscription_id=$(echo "$request_seat_response" | jq -r ".subscription_id")
                    provided_seat_tenant_id=$(echo "$request_seat_response" | jq -r ".occupant.tenant_id")
                    provided_seat_user_id=$(echo "$request_seat_response" | jq -r ".occupant.user_id")

                    if [[ "$provided_seat_type" == "limited" && "$request_seat_id" == "$provided_seat_id" && "$subscription_id" == "$provided_seat_subscription_id" && "$tenant_id" == "$provided_seat_tenant_id" && "$request_seat_user_id" == "$provided_seat_user_id" ]]; then
                        echo "✔️   Limited seat [$request_seat_id] was successfully provided to user [$request_seat_user_id]."

                        # 🧪 Test #7 - Check to see if a user has a seat

                        # When a user logs in to your SaaS app, you should call this API to see if that user has a seat. If they don't have a seat,
                        # redirect them to the main Turnstile endpoint. To keep things simple, we'll reuse the identity of the user that we just created
                        # a limited seat for. The seat should certainly be here since we just created it!

                        get_seat_url="$api_base_url/saas/subscriptions/$subscription_id/user-seat/$tenant_id/$request_seat_user_id"

                        get_seat_response=$(curl \
                            -X GET \
                            -H "Content-Type: application/json" \
                            -H "x-functions-key: $api_key" \
                            "$get_seat_url")

                        seat_id=$(echo "$get_seat_response" | jq -r ".seat_id")
                        seat_subscription_id=$(echo "$get_seat_response" | jq -r ".subscription_id")
                        seat_tenant_id=$(echo "$get_seat_response" | jq -r ".occupant.tenant_id")
                        seat_user_id=$(echo "$get_seat_response" | jq -r ".occupant.user_id")

                        if [[ "$subscription_id" == "$seat_subscription_id" && "$tenant_id" == "$seat_tenant_id" && "$request_seat_user_id" == "$seat_user_id" ]]; then
                            echo "✔️   User [$seat_user_id] is currently occupying seat [$seat_id]."

                            # 🧪 Test #8 - Release a user's seat

                            # Seats automatically expire on a scheduled based on the chosen seating strategy. If needed, however, tenant administrators
                            # can remove users from seats and cancel reservations.

                            release_seat_url="$api_base_url/saas/subscriptions/$subscription_id/seats/$seat_id"

                            release_seat_status_code=$(curl -s \
                                -X DELETE \
                                -H "Content-Type: application/json" \
                                -H "x-functions-key: $api_key" \
                                -o /dev/null \
                                -w "%{http_code}" \
                                "$release_seat_url")

                            if [[ "$release_seat_status_code" == "204" ]]; then
                                echo "✔️   Seat [$seat_id] successfully released."
                            else
                                echo "❌   Unable to release seat [$seat_id]. API returned status code [$release_seat_status_code]."
                                return 1
                            fi
                        else
                            echo "❌   Unable to get seat for user [$request_seat_user_id]."
                            echo
                            echo "Get seat API response: $get_seat_response"
                            return 1
                        fi
                    else
                        echo "❌   Unable to request limited seat [$request_seat_id]."
                        echo
                        echo "Request seat API response: $request_seat_response"
                        return 1
                    fi
                else
                    echo "❌   Unable to redeem seat [$redeem_seat_id]."
                    echo
                    echo "Redeem seat API response: $redeem_seat_response"
                    return 1
                fi
            else
                echo "❌   Unable to reserve seat [$reserve_seat_id] for [$reserve_email]."
                echo
                echo "Reserve seat API response: $reserve_seat_response"
                return 1
            fi
        else
            echo "❌   Unable to create subscription [$subscription_id]."
            echo
            echo "Expected patched subscription is..."
            echo "$expected_subscription"
            echo
            echo "while actual subscription is..."
            echo "$actual_subscription"
            return 1
        fi
    else
        echo "❌   Unable to create subscription [$subscription_id]. API returned status code [$create_subscription_status_code]."
        return 1
    fi  
}

verify_events() {
    storage_account_name=$1
    storage_account_key=$2
    container_name="event-store"

    subscription_json=$(cat ./models/core_api_tests/subscription.json)
    subscription_id=$(echo "$subscription_json" | jq -r ".subscription_id") # Events are stored in blob storage by subscription ID to make these tests easier.

    # The subscription_created event is published when the subscription is created in test #1.

    subscription_created_count=$(az storage blob list \
        --container-name "$container_name" \
        --account-key "$storage_account_key" \
        --account-name "$storage_account_name" \
        --prefix "saas/subscriptions/$subscription_id/subscription_created" \
        | jq ". | length")

    if [[ $subscription_created_count == 1 ]]; then
        echo "✔️   1 subscription_created event published."
    else
        echo "❌   [$subscription_created_count] subscription_created event(s) published; expected 1."
        verify_failed=1
    fi

    # The subscription_updated event is published when the subscription is patched in test #2.

    subscription_updated_count=$(az storage blob list \
        --container-name "$container_name" \
        --account-key "$storage_account_key" \
        --account-name "$storage_account_name" \
        --prefix "saas/subscriptions/$subscription_id/subscription_updated" \
        | jq ". | length")

    if [[ $subscription_updated_count == 1 ]]; then
        echo "✔️   1 subscription_updated event published."
    else
        echo "❌   [$subscription_updated_count] subscription_updated event(s) published; expected 1."
        verify_failed=1
    fi

    # The subscription_reserved event is published when a seat is reserved in test #3.

    seat_reserved_count=$(az storage blob list \
        --container-name "$container_name" \
        --account-key "$storage_account_key" \
        --account-name "$storage_account_name" \
        --prefix "saas/subscriptions/$subscription_id/seat_reserved" \
        | jq ". | length")

    if [[ $seat_reserved_count == 1 ]]; then
        echo "✔️   1 seat_reserved event published."
    else
        echo "❌   [$seat_reserved_count] seat_reserved event(s) published; expected 1."
        verify_failed=1
    fi

    # The subscription_redeemed event is published when a reserved seat is redeemed in test #4.

    seat_redeemed_count=$(az storage blob list \
        --container-name "$container_name" \
        --account-key "$storage_account_key" \
        --account-name "$storage_account_name" \
        --prefix "saas/subscriptions/$subscription_id/seat_redeemed" \
        | jq ". | length")

    if [[ $seat_redeemed_count == 1 ]]; then
        echo "✔️   1 seat_redeemed event published."
    else
        echo "❌   [$seat_redeemed_count] seat_redeemed event(s) published; expected 1."
        verify_failed=1
    fi

    # The seat_provided event is published when a "walk-up" user is provided a seat in tests #5 (where we create 4 standard seats)
    # and #6 (where we create a single limited seat).

    seat_provided_count=$(az storage blob list \
        --container-name "$container_name" \
        --account-key "$storage_account_key" \
        --account-name "$storage_account_name" \
        --prefix "saas/subscriptions/$subscription_id/seat_provided" \
        | jq ". | length")

    if [[ $seat_provided_count == 5 ]]; then
        echo "✔️   5 seat_provided events published."
    else
        echo "❌   [$seat_provided_count] seat_provided event(s) published; expected 5."
        verify_failed=1
    fi

    # The seat_warning_level_reached event is published when a subscription's available seats falls below 25% of total_seats.
    # This level is reached during test #5.

    low_seat_warning_count=$(az storage blob list \
        --container-name "$container_name" \
        --account-key "$storage_account_key" \
        --account-name "$storage_account_name" \
        --prefix "saas/subscriptions/$subscription_id/seat_warning_level_reached" \
        | jq ". | length")

    if [[ $low_seat_warning_count == 1 ]]; then
        echo "✔️   1 seat_warning_level_reached event published."
    else
        echo "❌   [$low_seat_warning_count] seat_warning_level_reached event(s) published; expected 1."
        verify_failed=1
    fi

    # The no_seats_available event is published in test #5 when the last standard seat is provided. It is published
    # again in step #6 as, technically, there are still no standard seats available.

    no_seats_count=$(az storage blob list \
        --container-name "$container_name" \
        --account-key "$storage_account_key" \
        --account-name "$storage_account_name" \
        --prefix "saas/subscriptions/$subscription_id/no_seats_available" \
        | jq ". | length")

    if [[ $no_seats_count == 2 ]]; then
        echo "✔️   2 no_seats_available events published."
    else
        echo "❌   [$no_seats_count] no_seats_available event(s) published; expected 2."
        verify_failed=1
    fi

    # The seat_released event is published when a seat is manually released through the API. When a seat expires, however,
    # no seat_released event is currently published.

    seat_released_count=$(az storage blob list \
        --container-name "$container_name" \
        --account-key "$storage_account_key" \
        --account-name "$storage_account_name" \
        --prefix "saas/subscriptions/$subscription_id/seat_released" \
        | jq ". | length")

    if [[ $seat_released_count == 1 ]]; then
        echo "✔️   1 seat_released event published."
    else
        echo "❌   [$seat_released_count] seat_released event(s) published; expected 1."
        verify_failed=1
    fi

    [[ -n $verify_failed ]] && return 1;
}

api_base_url=$1
api_key=$2
storage_account_name=$3
storage_account_key=$4

echo "🧪   Running core API tests..."

run_core_api_tests "$api_base_url" "$api_key"

if [[ $? == 0 ]]; then
    sleep 30 # Wait for those events to drop...

    echo "✔️   Core API tests passed. Verifying events..."

    verify_events "$storage_account_name" "$storage_account_key"

    if [[ $? == 0 ]]; then
        echo "✔️   Core API events verified."
        exit 0
    else
        echo "❌   Core API event verification failed."
        exit 1
    fi
else
    echo "❌   Core API tests failed. Bypassing event verification..."
    exit 1
fi