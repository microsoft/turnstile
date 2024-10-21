param deploymentName string

param location string = resourceGroup().location

// For subscribing to this Turnstile deployment's event grid topic...

param eventGridConnectionId string
param eventGridConnectionName string
param eventGridTopicId string
param managedIdId string

var name = 'turn-on-seat-reserved-${deploymentName}'
var eventType = 'seat_reserved'
var triggerName = 'When_a_seat_is_reserved'

var actionNames = {
  parseEventInfo: 'Parse_event_information'
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
                  title: 'Event ID'
                }
                event_type: {
                  title: 'Event type'
                }
                event_version: {
                  title: 'Event version'
                }
                occurred_utc: {
                  title: 'Event occurred date/time (UTC)'
                }
                seat: {
                  properties: {
                    created_utc: {
                      title: 'Seat created date/time (UTC)'
                    }
                    expires_utc: {
                      title: 'Seat expires date/time (UTC)'
                    }
                    reservation: {
                      properties: {
                        email: {
                          title: 'Seat reserved for email'
                        }
                        invite_url: {
                          description: 'The URL that should be used to redeem the seat reservation'
                          title: 'Seat inivitation URL'
                        }
                        tenant_id: {
                          title: 'Seat reserved for tenant ID'
                        }
                        user_id: {
                          title: 'Seat reserved for user ID'
                        }
                      }
                      type: 'object'
                    }
                    seat_id: {
                      title: 'Seat ID'
                    }
                    seat_type: {
                      description: 'Either [standard] or [limited]'
                      title: 'Seat type'
                    }
                    seating_strategy_name: {
                      description: 'The seating strategy (either [first_come_first_served] or [monthly_active_user]) used to provide this seat'
                      title: 'Seating strategy name'
                    }
                  }
                  type: 'object'
                }
                subscription: {
                  properties: {
                    admin_email: {
                      description: 'Main subscription admin\'s (subscriber) email'
                      title: 'Subscription admin email'
                    }
                    admin_name: {
                      description: 'Main subscription admin\'s (subscriber) display name'
                      title: 'Subscription admin name'
                    }
                    admin_role_name: {
                      description: 'When configured, role that subscription admins (subscriber) belong to'
                      title: 'Subscription admin role name'
                    }
                    created_utc: {
                      title: 'Subscription created date/time (UTC)'
                    }
                    is_being_configured: {
                      title: 'Is subscription currently being configured'
                      type: 'boolean'
                    }
                    is_free_trial: {
                      title: 'Is free trial subscription'
                      type: 'boolean'
                    }
                    is_setup_complete: {
                      title: 'Is subscription setup complete'
                      type: 'boolean'
                    }
                    is_test_subscription: {
                      title: 'Is test subscription'
                      type: 'boolean'
                    }
                    management_urls: {
                      description: 'URLs that can be used by the subscriber to manage this subscription'
                      title: 'Subscription management URLs'
                    }
                    offer_id: {
                      title: 'Subscription offer ID'
                    }
                    plan_id: {
                      title: 'Subscription plan ID'
                    }
                    seating_config: {
                      properties: {
                        default_seat_expiry_in_days: {
                          description: 'in days; doesn\'t apply to [monthly_active_user] seats'
                          title: 'Subscription seat expiry'
                          type: 'integer'
                        }
                        limited_overflow_seating_enabled: {
                          description: 'If [true], this subscription will provide limited seating when standard seating has been exhausted'
                          title: 'Is subscription limited seating enabled'
                          type: 'boolean'
                        }
                        seat_reservation_expiry_in_days: {
                          description: 'in days; indicates how long a seat should remain reserved before it is automatically released'
                          title: 'Subscription seat reservation expiry'
                          type: 'integer'
                        }
                        seating_strategy_name: {
                          description: 'The strategy (either [first_come_first_served] or [monthly_active_user]) that this subscription uses to provide seats'
                          title: 'Subscription seating strategy name'
                        }
                      }
                      type: 'object'
                    }
                    source_subscription: {
                      description: 'Property bag for custom subscription-specific information'
                      title: 'Additional subscription info'
                    }
                    state: {
                      description: 'Supported states are [purchased], [active], [suspended], and [canceled]'
                      title: 'Subscription state'
                    }
                    state_last_updated_utc: {
                      title: 'Subscription state last updated date/time (UTC)'
                    }
                    subscriber_info: {
                      description: 'Property bag for custom subscriber-specific information'
                      title: 'Additional subscriber info'
                    }
                    subscription_id: {
                      title: 'Subscription ID'
                    }
                    subscription_name: {
                      title: 'Subscription name'
                    }
                    tenant_id: {
                      title: 'Subscription tenant ID'
                    }
                    tenant_name: {
                      title: 'Subscription tenant name'
                    }
                    total_seats: {
                      description: 'When configured for user-based seating, the total number of usable seats (inc. available, occupied, and reserved) in the subscription'
                      title: 'Total subscription seats'
                    }
                    user_role_name: {
                      description: 'When configured, role that subscription users (subscriber) must belong to'
                      title: 'Subscription user role name'
                    }
                  }
                  type: 'object'
                }
                subscription_seats: {
                  properties: {
                    limited_seat_count: {
                      description: 'Includes limited seats currently occupied'
                      title: 'Consumed limited seat count'
                      type: 'integer'
                    }
                    standard_seat_count: {
                      description: 'Includes standard seats currently occupied or reserved'
                      title: 'Consumed standard seat count'
                      type: 'integer'
                    }
                  }
                  type: 'object'
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
