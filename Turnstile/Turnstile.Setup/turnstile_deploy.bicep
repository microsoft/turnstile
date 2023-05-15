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
param appServicePlanSku string = 'S1'

param publisherAdminRoleName string = 'turnstile_admins'
param subscriberTenantAdminRoleName string = 'subscriber_tenant_admins'
param webAppAadClientId string = ''
param webAppAadTenantId string = ''

@description('''
In headless mode, __only__ the API and its supporting resources are deployed. 
The web app is not deployed in headless mode.
''')
param headless bool = false

param useCosmosProvisionedThroughput bool = false

@secure()
param webAppAadClientSecret string = ''

param location string = resourceGroup().location

var cleanDeploymentName = toLower(deploymentName)
var cosmosDbAccountName = 'turn-cosmos-${cleanDeploymentName}'
var cosmosDbName = 'turnstiledb'
var cosmosContainerName = 'turnstilecontainer'
var uniqueDeploymentName = toLower(uniqueString(resourceGroup().id, deployment().name, cleanDeploymentName))
var storageAccountName = take('turnstor${uniqueDeploymentName}', 24)
var configStorageContainerName = 'turn-configuration'
var configStorageBlobName = 'publisher_config.json'
var eventStoreContainerName = 'event-store'
var logAnalyticsName = 'turn-logs-${cleanDeploymentName}'
var appInsightsName = 'turn-insights-${cleanDeploymentName}'
var appServiceAlwaysOn = appServicePlanSku != 'Y1'
var appServicePlanName = 'turn-plan-${cleanDeploymentName}'
var eventGridTopicName = 'turn-events-${cleanDeploymentName}'
var eventGridConnectionName = 'turn-events-connect-${cleanDeploymentName}'
var eventGridConnectionDisplayName = 'Turnstile SaaS Events'
var apiAppName = 'turn-services-${cleanDeploymentName}'
var webAppName = 'turn-web-${cleanDeploymentName}'
var midName = 'turn-id-${cleanDeploymentName}'

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
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
      }
    ]
  }
}

resource cosmosSqlDb 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2021-11-15-preview' = {
  name: '${cosmosDbAccount.name}/${cosmosDbName}'
  properties: {
    resource: {
      id: cosmosDbName
    }
  }
}

resource cosmosContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2021-11-15-preview' = {
  name: '${cosmosSqlDb.name}/${cosmosContainerName}'
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

resource configStorageContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-08-01' = { parent: storageAccount, name: 'default/${configStorageContainerName}' }

resource eventStoreContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-08-01' = {
  name: '${storageAccount.name}/default/${eventStoreContainerName}'
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
  properties: { }
}

resource apiApp 'Microsoft.Web/sites@2021-03-01' = {
  name: apiAppName
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
          value: 'dotnet'
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
          name: 'Turnstile_CosmosAccessKey'
          value: cosmosDbAccount.listKeys().primaryMasterKey
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
          name: 'Turnstile_EventGridTopicAccessKey'
          value: eventGridTopic.listKeys().key1
        }
        {
          name: 'Turnstile_EventGridTopicEndpointUrl'
          value: eventGridTopic.properties.endpoint
        }
        {
          name: 'Turnstile_PublisherConfigStorageBlobName'
          value: configStorageBlobName
        }
        {
          name: 'Turnstile_PublisherConfigStorageConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'Turnstile_PublisherConfigStorageContainerName'
          value: configStorageContainerName
        }
        {
          name: 'Turnstile_ApiBaseUrl'
          value: 'https://${apiAppName}.azurewebsites.net'
        }
      ]
    }
  }
}

resource webApp 'Microsoft.Web/sites@2021-03-01' = if (!headless) {
  name: webAppName
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
          name: 'Turnstile_ApiBaseUrl'
          value: 'https://${apiAppName}.azurewebsites.net'
        }
        {
          name: 'Turnstile_PublisherAdminRoleName'
          value: publisherAdminRoleName
        }
        {
          name: 'Turnstile_AadClientId'
          value: webAppAadClientId
        }
        {
          name: 'Turnstile_AadClientSecret'
          value: webAppAadClientSecret
        }
        {
          name: 'Turnstile_PublisherTenantId'
          value: webAppAadTenantId
        }
        {
          name: 'Turnstile_SubscriberTenantAdminRoleName'
          value: subscriberTenantAdminRoleName
        }
      ]
    }
  }
}

output deploymentName string = deploymentName
output apiAppName string = apiAppName
output storageAccountName string = storageAccount.name
output storageAccountKey string = storageAccount.listKeys().keys[0].value
output managedIdId string = mid.id
output managedIdName string = mid.name
output eventGridConnectionId string = eventGridConnection.id
output eventGridConnectionName string = eventGridConnection.name
output eventGridTopicId string = eventGridTopic.id
output eventGridTopicName string = eventGridTopic.name

output webAppName string = headless ? '' : webApp.name
output webAppBaseUrl string = headless ? '' : 'https://${webApp.properties.defaultHostName}'

