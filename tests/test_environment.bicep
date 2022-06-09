param location string = resourceGroup().location
param deploymentName string

var cosmosDbAccountName = 'turn-cosmos-${deploymentName}'
var cosmosDbName = 'turnstiledb'
var cosmosContainerName = 'turnstilecontainer'
var storageAccountName = take('turnstor${deploymentName}', 24)
var configStorageContainerName = 'turn-configuration'
var configStorageBlobName = 'publisher_config.json'
var eventStoreContainerName = 'event-store'
var appInsightsName = 'turn-insights-${deploymentName}'
var appServicePlanName = 'turn-plan-${deploymentName}'
var eventGridTopicName = 'turn-events-${deploymentName}'
var apiAppName = 'turn-services-${deploymentName}'

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    IngestionMode: 'ApplicationInsights'
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

resource configStorageContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-08-01' = {
  name: '${storageAccount.name}/default/${configStorageContainerName}'
}

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
    name: 'S1'
    tier: 'Standard'
    family: 'S'
    size: 'S1'
  }
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
      alwaysOn: true
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
      ]
    }
  }
}

output apiAppId string = apiApp.id
output apiAppName string = apiAppName
output storageAccountName string = storageAccount.name
output storageAccountKey string = storageAccount.listKeys().keys[0].value
output topicId string = eventGridTopic.id

