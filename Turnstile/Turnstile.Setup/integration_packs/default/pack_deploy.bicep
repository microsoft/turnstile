param eventGridTopicLocation string = resourceGroup().location // Should always be the same as [packLocation]
param eventGridTopicResourceGroup string = resourceGroup().name
param eventGridTopicResourceName string

@minLength(5)
@maxLength(13)
param packName string

param aadClientId string
param aadTenantId string

@secure()
param aadClientSecret string

var packType = 'default'
var packVersion = 'experimental-1.0'
var loweredPackName = toLower(packName)
var connectionName = '${loweredPackName}_topic_connection'
var connectionDisplayName = '${packName} Integration Pack Event Grid Topic Connection'
var eventGridApiUrl = '${subscription().id}/providers/Microsoft.Web/locations/${eventGridTopicLocation}/managedApis/azureeventgrid'
var topicResourceId = resourceId(subscription().id, eventGridTopicResourceGroup, 'Microsoft.EventGrid/topics', eventGridTopicResourceName)

var workflowNames = {
  onLowSeatWarningLevelReached: 'turn-on-low-seating-${loweredPackName}'
  onNoSeatsAvaialble: 'turn-on-no-seats-${loweredPackName}'
  onSeatProvided: 'turn-on-seat-provided-${loweredPackName}'
  onSeatRedeemed: 'turn-on-seat-redeemed-${loweredPackName}'
  onSeatReleased: 'turn-on-seat-released-${loweredPackName}'
  onSeatReserved: 'turn-on-seat-reserved-${loweredPackName}'
  onSubscriptionCreated: 'turn-on-subscription-created-${loweredPackName}'
  onSubscriptionUpdated: 'turn-on-subscription-updated-${loweredPackName}'
}

var workflowEventTypes = {
  onLowSeatWarningLevelReached: 'seat_warning_level_reached'
  onNoSeatsAvailable: 'no_seats_available'
  onSeatProvided: 'seat_provided'
  onSeatRedeemed: 'seat_redeemed'
  onSeatReleased: 'seat_released'
  onSeatReserved: 'seat_reserved'
  onSubscriptionCreated: 'subscription_created'
  onSubscriptionUpdated: 'subscription_updated'
}

var workflowTriggerNames = {
  onLowSeatWarningLevelReached: 'On low seat warning level reached'
  onNoSeatsAvailable: 'On no more seats available'
  onSeatProvided: 'On seat provided'
  onSeatRedeemed: 'On reserved seat redeemed'
  onSeatReleased: 'On seat manually released'
  onSeatReserved: 'On seat reserved'
  onSubscriptionCreated: 'On subscription created'
  onSubscriptionUpdated: 'On subscription updated'
}

// This is boilerplate for pretty much every integration pack.
// At some point, we might consider breaking this resource out into its own module template
// that we can reference from the pack-specific template.

resource eventGridConnection 'Microsoft.Web/connections@2016-06-01' = {
  name: connectionName
  location: packLocation
  kind: 'V1'
  properties: {
    displayName: connectionDisplayName
    parameterValues: {
      'token:clientId': aadClientId
      'token:clientSecret': aadClientSecret
      'token:TenantId': aadTenantId
      'token:grantType': 'client_credentials'
    }
    api: {
      id: eventGridApiUrl
    }
  }
}

// Everything below here is specific to the pack.
// TODO: Think about how we version these integration packs.

resource onLowSeatWarningLevelReached 'Microsoft.Logic/workflows@2019-05-01' = {
  name: workflowNames.onLowSeatWarningLevelReached
  location: packLocation
  tags: {
    'event_type': workflowEventTypes.onLowSeatWarningLevelReached
    'pack_name': packName
    'pack_type': packType
    'pack_version': packVersion
  }
  properties: {
    state: 'Enabled'
    definition: {
      '$schema': 'https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#'
      contentVersion: '1.0.0.0'
      parameters: {
        '$connections': {
          defaultValue: {}
          type: 'Object'
        }
      }
      triggers: {
        '${workflowTriggerNames.onLowSeatWarningLevelReached}': {
          splitOn: '@triggerBody()'
          type: 'ApiConnectionWebhook'
          inputs: {
            body: {
              properties: {
                destination: {
                  endpointType: 'WebHook'
                  properties: {
                    endpointUrl: '@{listCallbackUrl()}'
                  }
                }
                filter: {
                  includedEventTypes: [
                    '${workflowEventTypes.onLowSeatWarningLevelReached}'
                  ]
                }
                topic: topicResourceId
              }
            }
            host: {
              connection: {
                name: '@parameters(\'$connections\')[\'eventGrid\'][\'connectionId\']'
              }
            }
            path: '${subscription().id}/providers/Microsoft.EventGrid.Topics/resource/eventSubscriptions'
            queries: {
              'x-ms-api-version': '2017-06-15-preview'
            }
          }
        }
      }
      actions: {
        'Add your integration logic here': {
          actions: {}
          runAfter: {}
          type: 'Scope'
        }
      }
    }
    parameters: {
      '$connections': {
        value: {
          eventGrid: {
            connectionId: eventGridConnection.id
            connectionName: connectionDisplayName
            id: eventGridApiUrl
          }
        }
      }
    }
  }
}
