param deploymentName string

param location string = resourceGroup().location

// For subscribing to this Mona deployment's event grid topic...

param eventGridConnectionName string = 'turn-events-connection-${deploymentName}'
param eventGridTopicName string = 'turn-events-${deploymentName}'

var name = 'turn-on-no-seats-available-${deploymentName}'
var eventType = 'no_seats_available'
var triggerName = 'When_no_more_seats_are_available'

var actionNames = {
  parseEventInfo: 'Parse_event_information'
  parseSubscriptionInfo: 'Parse_subscription_information'
  parseMoreSubscriptionInfo: 'Parse_more_subscription_information'
  parseSeatingConfig: 'Parse_seating_configuration'
  parseSeatingSummary: 'Parse_seating_summary'
  yourIntegrationLogic: 'Add_your_integration_logic_here'
}

resource eventGridConnection 'Microsoft.Web/connections@2016-06-01' existing = {
  name: eventGridConnectionName
}

resource eventGridTopic 'Microsoft.EventGrid/topics@2021-12-01' existing = {
  name: eventGridTopicName
}

resource workflow 'Microsoft.Logic/workflows@2019-05-01' = {
  name: name
  location: location
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
        '${triggerName}': {
          splitOn: '@triggerBody()'
          type: 'ApiConnectionWebhook'
          inputs: {
            body: {
              properties: {
                destination: {
                  endpointType: 'webhook'
                  properties: {
                    endpointUrl: '@{listCallbackUrl()}'
                  }
                }
                filter: {
                  includedEventTypes: [
                    eventType
                  ]
                }
                topic: eventGridTopic.id
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
        '${actionNames.parseEventInfo}': {
          inputs: {
            content: '@triggerBody()?[\'data\']'
            schema: {
              properties: {
                event_id: {
                  type: 'string'
                }
                event_type: {
                  type: 'string'
                }
                event_version: {
                  type: 'string'
                }
                occurred_utc: {
                  type: 'string'
                }
              }
              type: 'object'
            }
          }
          runAfter: {}
          type: 'ParseJson'
        }
        '${actionNames.parseSubscriptionInfo}': {
          inputs: {
            content: '@{triggerBody()?[\'data\']}.subscription'
            schema: {
              properties: {
                admin_role_name: {
                  type: 'string'
                }
                offer_id: {
                  type: 'string'
                }
                plan_id: {
                  type: 'string'
                }
                state: {
                  type: 'string'
                }
                subscription_id: {
                  type: 'string'
                }
                subscription_name: {
                  type: 'string'
                }
                tenant_id: {
                  type: 'string'
                }
                tenant_name: {
                  type: 'string'
                }
                user_role_name: {
                  type: 'string'
                }
              }
              type: 'object'
            }
          }
          runAfter: {}
          type: 'ParseJson'
        }
        '${actionNames.parseMoreSubscriptionInfo}': {
          inputs: {
            content: '@{triggerBody()?[\'data\']}.subscription'
            schema: {
              properties: {
                admin_email: {
                  type: 'string'
                }
                admin_name: {
                  type: 'string'
                }
                created_utc: {
                  type: 'string'
                }
                is_being_configured: {
                  type: 'boolean'
                }
                is_free_trial: {
                  type: 'boolean'
                }
                is_setup_complete: {
                  type: 'boolean'
                }
                is_test_subscription: {
                  type: 'boolean'
                }
                state_last_updated_utc: {
                  type: 'string'
                }
                total_seats: {
                  type: 'integer'
                }
              }
              type: 'object'
            }
          }
          runAfter: {}
          type: 'ParseJson'
        }
        '${actionNames.parseSeatingConfig}': {
          inputs: {
            content: '@{triggerBody()?[\'data\']}.subscription.seating_config'
            schema: {
              properties: {
                default_seat_expiry_in_days: {
                  type: 'integer'
                }
                limited_overflow_seating_enabled: {
                  type: 'boolean'
                }
                seat_reservation_expiry_in_days: {
                  type: 'integer'
                }
                seating_strategy_name: {
                  type: 'string'
                }
              }
              type: 'object'
            }
          }
          runAfter: {}
          type: 'ParseJson'
        }
        '${actionNames.parseSeatingSummary}': {
          inputs: {
            content: '@{triggerBody()?[\'data\']}.subscription_seats'
            schema: {
              properties: {
                standard_seat_count: {
                  type: 'integer'
                }
                limited_seat_count: {
                  type: 'integer'
                }
              }
              type: 'object'
            }
          }
          runAfter: {}
          type: 'ParseJson'
        }
        '${actionNames.yourIntegrationLogic}': {
          actions: {}
          runAfter: {
            '${actionNames.parseSubscriptionInfo}': [
              'Succeeded'
            ]
            '${actionNames.parseMoreSubscriptionInfo}': [
              'Succeeded'
            ]
            '${actionNames.parseSeatingConfig}': [
              'Succeeded'
            ]
            '${actionNames.parseSeatingSummary}': [
              'Succeeded'
            ]
            '${actionNames.parseEventInfo}': [
              'Succeeded'
            ]
          }
          type: 'Scope'
        }
      }
    }
    parameters: {
      '$connections': {
        value: {
          eventGrid: {
            connectionId: eventGridConnection.id
            connectionName: eventGridConnection.name
            id: '${subscription().id}/providers/Microsoft.Web/locations/${location}/managedApis/azureeventgrid'
          }
        }
      }
    }
  }
}
