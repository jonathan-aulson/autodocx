param name string
param location string
param storageAccountName string
param hostingPlanName string
param deploymentStorageContainerName string = 'function-deployments'
param functionAppRuntime string = 'dotnet-isolated'
param functionAppRuntimeVersion string = '8.0'
param maximumInstanceCount int = 40
param instanceMemoryMB int = 2048
param subnetId string

@secure()
param SQL_CONNECTION_STRING string
param AZURE_SERVICE_CLIENT_TENANT string
param AZURE_SERVICE_CLIENT_ID string


resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${name}-appinsights'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

resource flexFuncPlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: hostingPlanName
  location: location
  kind: 'functionapp'
  sku: {
    tier: 'FlexConsumption'
    name: 'FC1'
  }
  properties: {
    reserved: true
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
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

resource functionContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2022-09-01' = {
  name: '${storageAccount.name}/default/${deploymentStorageContainerName}'
  properties: {
    publicAccess: 'None'
  }
}

resource flexFuncApp 'Microsoft.Web/sites@2024-04-01' = {
  name: name
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: flexFuncPlan.id
    httpsOnly: true
    publicNetworkAccess: 'Enabled'
    virtualNetworkSubnetId: subnetId  
    siteConfig: {
      appSettings: [  {
        name: 'DEPLOYMENT_STORAGE_CONNECTION_STRING'
        value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
      }
      {
        name: 'SQL_CONNECTION_STRING'
        value: SQL_CONNECTION_STRING
      }
      {
        name: 'AZURE_SERVICE_CLIENT_TENANT'
        value: AZURE_SERVICE_CLIENT_TENANT
      }
      {
        name: 'AZURE_SERVICE_CLIENT_ID'
        value: AZURE_SERVICE_CLIENT_ID
      }
      {
        name: 'AzureWebJobsStorage'
        value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
      }
      {
        name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
        value: appInsights.properties.InstrumentationKey
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: appInsights.properties.ConnectionString
      }]
    }
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: 'https://${storageAccount.name}.blob.${environment().suffixes.storage}/${deploymentStorageContainerName}'
          authentication: {
            type: 'StorageAccountConnectionString'
            storageAccountConnectionStringName: 'DEPLOYMENT_STORAGE_CONNECTION_STRING'
          }
        }
      }
      scaleAndConcurrency: {
        maximumInstanceCount: maximumInstanceCount
        instanceMemoryMB: instanceMemoryMB
      }
      runtime: {
        name: functionAppRuntime
        version: functionAppRuntimeVersion
      }
    }
  }
}


