@minLength(3)
@maxLength(13)
@description('''
Deployment name __must__:
- be globally unique;
- contain only alphanumeric characters (a-z, 0-9);
- be at least 3 characters long;
- be less than 13 characters long
''')
param deploymentName string = take(uniqueString(resourceGroup().id), 13)

@description('The name of the role (by default, Azure Active Directory role) that publisher turnstile administrators must belong to')
param publisherAdminRoleName string = 'turnstile_admins'

@description('The name of the role (by default, Azure Active Directory role) that subscriber tenant admins must belong to')
param subscriberTenantAdminRoleName string = 'subscriber_tenant_admins'

@description('The publisher\'s Azure Active Directory tenant ID')
param publisherTenantId string

param location string = resourceGroup().location

var cleanDeploymentName = toLower(deploymentName)
var cosmosDbAccountName = 'turn-cosmos-${cleanDeploymentName}'
var cosmosDbName = 'turnstiledb'
var cosmosContainerName = 'turnstilecontainer'
var storageAccountName = take('turnstor${cleanDeploymentName}', 24)
var configStorageContainerName = 'configuration'
var configStorageBlobName = 'publisher_config.json'
var appInsightsName = 'turn-insights-${cleanDeploymentName}'
var appServicePlanName = 'turn-plan-${cleanDeploymentName}'
var eventGridTopicName = 'turn-events-${cleanDeploymentName}'
var apiAppName = 'turn-services-${cleanDeploymentName}'
var webAppName = 'turn-web-${cleanDeploymentName}'

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
          value: '~3'
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

// This is a nasty yet necessary hack. [webApp] is created after [apiApp] because 
// [webApp] references the [apiApp] access key and base URL through it's appsettings. 
// However, there's some "sneak operation" that's eventually consistent following the successful
// deployment of [apiApp] that isn't always complete by the time that [apiApp] deployment 
// is complete (OK status per the ARM API) and causes the listKeys operation inside [webApp] appsettings
// to fail. For that reason, we wait an additional 2 minutes following [apiApp] deployment
// to deploy the [webApp] giving the eventually consistent operation time to complete before
// we try to reference the function keys.

// This appears to work fine and was inspired by this Microsoft escalation engineer tecnical community article -- 
// https://techcommunity.microsoft.com/t5/azure-database-support-blog/add-wait-operation-to-arm-template-deployment/ba-p/2915342

resource waitForApiApp 'Microsoft.Resources/deploymentScripts@2020-10-01' = {
  name: 'wait-${cleanDeploymentName}'
  location: location
  kind: 'AzurePowerShell'
  dependsOn: [
    apiApp
  ]
  properties: {
    azPowerShellVersion: '6.4'
    timeout: 'PT1H'
    scriptContent: 'start-sleep -Seconds 120'
    cleanupPreference: 'Always'
    retentionInterval: 'PT1H'
  }
}  

resource webApp 'Microsoft.Web/sites@2021-03-01' = {
  name: webAppName
  location: location
  kind: 'app'
  dependsOn: [
    waitForApiApp
  ]
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'Turnstile_ApiAccessKey'
          value: listKeys('${apiApp.id}/host/default', apiApp.apiVersion).functionKeys.default
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
          name: 'Turnstile_PublisherTenantId'
          value: publisherTenantId
        }
        {
          name: 'Turnstile_SubscriberTenantAdminRoleName'
          value: subscriberTenantAdminRoleName
        }
      ]
    }
  }
}

