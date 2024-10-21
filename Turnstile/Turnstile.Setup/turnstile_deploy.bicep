@minLength(5)
@maxLength(13)
@description('''
Deployment name __must__:
- be globally unique;
- contain only alphanumeric characters (a-z, 0-9);
- be at least 5 characters long;
- be less than 13 characters long
''')
param deploymentName string = take(uniqueString(resourceGroup().id), 13)

param turnstileVersion string

@allowed([
  'D1'    // Shared
  'F1'    // Free
  'B1'    // Basic
  'B2'
  'B3'
  'S1'    // Standard (S1 is Default)
  'S2'
  'S3'
  'P1'    // Premium v1
  'P2'
  'P3'
  'P1V2'  // Premium v2
  'P2V2'
  'P3V2'
  'I1'    // Isolated (ASE)
  'I2'
  'I3'
  'Y1'    // Consumption/Dynamic (supported only for headless/API-only deployments)
])
@description('''
Note: Y1 (consumption/dynamic) is supported __only__ for headless/API-only deployments. 
Default is S1 (Standard).
''')
param appServicePlanSku string = 'B1'

param apiAadClientId string = ''
param userWebAppAadClientId string = ''
param adminWebAppAadClientId string = ''
param adminWebAppAadDomain string = ''
param aadTenantId string = ''

@description('''
In headless mode, __only__ the API and its supporting resources are deployed. 
The web apps are not deployed in headless mode.
''')
param headless bool = false

@description('''
By default, Turnstile's Cosmos account is created in serverless mode 
to control costs. Setting this flag will cause the account to be 
created in provisioned throughput mode.
''')
param useCosmosProvisionedThroughput bool = false

param location string = resourceGroup().location

var deploymentType = 'standard-v1'

var cleanDeploymentName = toLower(deploymentName)
var cosmosDbAccountName = 'turn-cosmos-${cleanDeploymentName}'
var cosmosDbName = 'turnstiledb'
var cosmosContainerName = 'turnstilecontainer'
var uniqueDeploymentName = toLower(uniqueString(resourceGroup().id, deployment().name, cleanDeploymentName))
var storageAccountName = take('turnstor${uniqueDeploymentName}', 24)
var configStorageContainerName = 'turn-configuration'
var configStorageBlobName = 'publisher_config.json'
var seatResultCacheStorageContainerName = 'seat-results'
var eventStoreContainerName = 'event-store'
var logAnalyticsName = 'turn-logs-${cleanDeploymentName}'
var appInsightsName = 'turn-insights-${cleanDeploymentName}'
var appServiceAlwaysOn = appServicePlanSku != 'Y1'
var appServicePlanName = 'turn-plan-${cleanDeploymentName}'
var eventGridTopicName = 'turn-events-${cleanDeploymentName}'
var eventGridConnectionName = 'turn-events-connect-${cleanDeploymentName}'
var eventGridConnectionDisplayName = 'Turnstile SaaS Events'
var apiAppName = 'turn-services-${cleanDeploymentName}'
var apiAuthScope = '${apiAadClientId}/.default'
var adminWebAppName = 'turn-admin-${cleanDeploymentName}'
var userWebAppName = 'turn-web-${cleanDeploymentName}'
var midName = 'turn-integration-id-${cleanDeploymentName}'

var cosmosCapabilities = useCosmosProvisionedThroughput ? [] : [ { name: 'EnableServerless' } ]

resource mid 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' = {
  name: midName
  location: location
}

resource eventGridConnection 'Microsoft.Web/connections@2016-06-01' = {
  name: eventGridConnectionName
  location: location
  kind: 'V1'
  properties: {
    customParameterValues: {}
    displayName: eventGridConnectionDisplayName
    parameterValueType: 'Alternative'
    api: {
      id: '${subscription().id}/providers/Microsoft.Web/locations/${eventGridTopic.location}/managedApis/azureeventgrid'
    }
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'Standalone'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    workspaceCapping: {
      dailyQuotaGb: -1
    }
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    RetentionInDays: 90
    WorkspaceResourceId: logAnalytics.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2021-11-15-preview' = {
  name: cosmosDbAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    capabilities: cosmosCapabilities
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
      }
    ]
  }
}

resource cosmosSqlDb 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2021-11-15-preview' = {
  parent: cosmosDbAccount
  name: cosmosDbName
  properties: {
    resource: {
      id: cosmosDbName
    }
  }
}

resource cosmosContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2021-11-15-preview' = {
  parent: cosmosSqlDb
  name: cosmosContainerName
  properties: {
    resource: {
      id: cosmosContainerName
      partitionKey: {
        paths: [
          '/partition_id'
        ]
        kind: 'Hash'
      }
      defaultTtl: -1
    }
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-08-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
  }
}

resource configStorageContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-08-01' = {
  name: '${storageAccount.name}/default/${configStorageContainerName}'
}

resource eventStoreContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-08-01' = {
  name: '${storageAccount.name}/default/${eventStoreContainerName}'
}

resource seatResultCacheContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-08-01' = {
  name: '${storageAccount.name}/default/${seatResultCacheStorageContainerName}'
}

resource eventGridTopic 'Microsoft.EventGrid/topics@2021-12-01' = {
  name: eventGridTopicName
  location: location
  properties: {
    inputSchema: 'EventGridSchema'
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2021-03-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: appServicePlanSku
  }
  properties: {}
}

resource apiApp 'Microsoft.Web/sites@2021-03-01' = {
  name: apiAppName
  identity: {
    type: 'SystemAssigned'
  }
  location: location
  kind: 'functionapp'
  properties: {
    reserved: false
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      alwaysOn: appServiceAlwaysOn
      appSettings: [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: 'InstrumentationKey=${appInsights.properties.InstrumentationKey}'
        }
        {
          name: 'AzureWebJobsDashboard'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'Turnstile_EventStorageConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'Turnstile_CosmosContainerId'
          value: cosmosContainerName
        }
        {
          name: 'Turnstile_CosmosDatabaseId'
          value: cosmosDbName
        }
        {
          name: 'Turnstile_CosmosEndpointUrl'
          value: cosmosDbAccount.properties.documentEndpoint
        }
        {
          name: 'Turnstile_EventGridTopicEndpointUrl'
          value: eventGridTopic.properties.endpoint
        }
        {
          name: 'Turnstile_SeatResultCache_StorageAccountName'
          value: storageAccount.name
        }
        {
          name: 'Turnstile_SeatResultCache_StorageContainerName'
          value: seatResultCacheStorageContainerName
        }
        {
          name: 'Turnstile_PublisherConfig_StorageAccountName'
          value: storageAccount.name
        }
        {
          name: 'Turnstile_PublisherConfig_StorageBlobName'
          value: configStorageBlobName
        }
        {
          name: 'Turnstile_PublisherConfig_StorageContainerName'
          value: configStorageContainerName
        }
        {
          name: 'Turnstile_ApiBaseUrl'
          value: 'https://${apiAppName}.azurewebsites.net'
        }
        {
          name: 'Turnstile_ApiAuthAudience'
          value: apiAadClientId
        }
        {
          name: 'Turnstile_ApiAuthScope'
          value: apiAuthScope
        }
        {
          name: 'Turnstile_ApiAuthTenantId'
          value: aadTenantId
        }
        {
          name: 'Turnstile_AadClientId'
          value: apiAadClientId
        }
        {
          name: 'Turnstile_PublisherTenantId'
          value: aadTenantId
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED'
          value: '1'
        }
      ]
    }
  }
}

resource userWebApp 'Microsoft.Web/sites@2021-03-01' = if (!headless) {
  name: userWebAppName
  identity: {
    type: 'SystemAssigned'
  }
  location: location
  kind: 'app'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: 'InstrumentationKey=${appInsights.properties.InstrumentationKey}'
        }
        {
          name: 'Turnstile_DeploymentName'
          value: deploymentName
        }
        {
          name: 'Turnstile_SeatResultCache_StorageConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'Turnstile_SeatResultCache_StorageAccountName'
          value: storageAccount.name
        }
        {
          name: 'Turnstile_SeatResultCache_StorageContainerName'
          value: seatResultCacheStorageContainerName
        }
        {
          name: 'Turnstile_ApiBaseUrl'
          value: 'https://${apiAppName}.azurewebsites.net'
        }
        {
          name: 'Turnstile_ApiAuthScope'
          value: apiAuthScope
        }
        {
          name: 'Turnstile_AadClientId'
          value: userWebAppAadClientId
        }
        {
          name: 'Turnstile_PublisherTenantId'
          value: aadTenantId
        } 
      ]
    }
  }
}

resource adminWebApp 'Microsoft.Web/sites@2021-03-01' = if (!headless) {
  name: adminWebAppName
  identity: {
    type: 'SystemAssigned'
  }
  location: location
  kind: 'app'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: 'InstrumentationKey=${appInsights.properties.InstrumentationKey}'
        }
        {
          name: 'Turnstile_DeploymentName'
          value: deploymentName
        }
        {
          name: 'Turnstile_SeatResultCache_StorageConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'Turnstile_SeatResultCache_StorageAccountName'
          value: storageAccount.name
        }
        {
          name: 'Turnstile_SeatResultCache_StorageContainerName'
          value: seatResultCacheStorageContainerName
        }
        {
          name: 'Turnstile_ApiBaseUrl'
          value: 'https://${apiAppName}.azurewebsites.net'
        }
        {
          name: 'Turnstile_ApiAuthScope'
          value: apiAuthScope
        }
        {
          name: 'Turnstile_AadClientId'
          value: adminWebAppAadClientId
        }
        {
          name: 'Turnstile_AadDomain'
          value: adminWebAppAadDomain
        }
        {
          name: 'Turnstile_PublisherTenantId'
          value: aadTenantId
        } 
      ]
    }
  }
}

output deploymentName string = deploymentName
output apiAppName string = apiAppName
output storageAccountName string = storageAccount.name
output storageAccountId string = storageAccount.id
output managedIdId string = mid.id
output managedIdName string = mid.name
output eventGridConnectionId string = eventGridConnection.id
output eventGridConnectionName string = eventGridConnection.name
output eventGridTopicId string = eventGridTopic.id
output eventGridTopicName string = eventGridTopic.name

output userWebAppName string = headless ? '' : userWebApp.name
output userWebAppBaseUrl string = headless ? '' : 'https://${userWebApp.properties.defaultHostName}'

output adminWebAppName string = headless ? '' : adminWebApp.name
output adminWebAppBaseUrl string = headless ? '' : 'https://${adminWebApp.properties.defaultHostName}'

output cosmosAccountName string = cosmosDbAccount.name
output cosmosAccountId string = cosmosDbAccount.id

// So, here's the idea with deploymentTypes and deploymentProfiles. In Mona, admins can access supporting Azure
// resources provisioned during deployment using Azure portal deep links presented in the Mona UI. Customers
// love this as it reduces the Azure learning curve needed to support a Mona deployment. While this is the only
// supported Turnstile deployment method today, it's entirely plausible that customers in the future may
// choose to deploy it in different ways including on K8s. To bring this Azure operations UI capability to 
// the Turnstile UI _while_ supporting multiple future potential deployment methods, we publish a deploymentProfile
// to a shared blob storage location along with a deploymentType label. If the Tursntile UI knows the deploymentType, it
// can present a UI experience that allows users to support it. This leaves the door open for future deploymentTypes
// and corresponding UI enhancements.

output deploymentType string = deploymentType

output deploymentProfile object = { // This schema describes a standard-v1 deployment type.
  deploymentName: deploymentName
  deployedVersion: turnstileVersion
  isHeadless: headless
  azureDeploymentName: deployment().name
  azureSubscriptionId: subscription().subscriptionId
  azureResourceGroupName: resourceGroup().name
  azureRegion: location
  eventGridTopicName: eventGridTopicName
  aadTenantId: aadTenantId
  apps: {
    api: {
      isDeployed: true
      aadClientId: apiAadClientId
      name: apiAppName
      baseUrl: 'https://${apiAppName}.azurewebsites.net'
    }
    userWeb: {
      isDeployed: !headless
      aadClientId: userWebAppAadClientId
      name: headless ? '' : userWebAppName
      baseUrl: headless ? '' : 'https://${userWebApp.properties.defaultHostName}'
    }
    adminWeb: {
      isDeployed: !headless
      aadClientId: adminWebAppAadClientId
      name: headless ? '' : adminWebAppName
      baseUrl: headless ? '' : 'https://${adminWebApp.properties.defaultHostName}'
    }
  }
}
