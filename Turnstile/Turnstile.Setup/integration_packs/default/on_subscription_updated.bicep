param deploymentName string

param location string = resourceGroup().location

// For subscribing to this Turnstile deployment's event grid topic...

param eventGridConnectionId string
param eventGridConnectionName string
param eventGridTopicId string
param managedIdId string

var name = 'turn-on-subscription-updated-${deploymentName}'
var eventType = 'subscription_updated'
var triggerName = 'When_a_new_subscription_is_updated'

var actionNames = {
  parseEventInfo: 'Parse_event_information'
  parseSubscriptionInfo: 'Parse_subscription_information'
  parseMoreSubscriptionInfo: 'Parse_more_subscription_information'
  parseSeatingConfig: 'Parse_seating_configuration'
  yourIntegrationLogic: 'Add_your_integration_logic_here'
}

resource workflow 'Microsoft.Logic/workflows@2019-05-01' = {
  name: name
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdId}': { }
    }
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
                topic: eventGridTopicId
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
            connectionId: eventGridConnectionId
            connectionName: eventGridConnectionName
            connectionProperties: {
              authentication: {
                identity: managedIdId
                type: 'ManagedServiceIdentity'
              }
            }
            id: '${subscription().id}/providers/Microsoft.Web/locations/${location}/managedApis/azureeventgrid'
          }
        }
      }
    }
  }
}
