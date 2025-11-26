param location string = resourceGroup().location
param functionName string
param storageAccountName string
param hostingPlanName string
param vnetName string
param subnetName string

@secure()
param SQL_CONNECTION_STRING string
param AZURE_SERVICE_CLIENT_ID string
param AZURE_SERVICE_CLIENT_TENANT string

module network 'modules/network.bicep' = {
  name: 'networkReference'
  scope: resourceGroup()
  params: {
    vnetName: vnetName
    subnetName: subnetName
  }
}

module functionAppModule './modules/functionApp.bicep' = {
  name: 'deployFunctionApp'
  params: {
    name: functionName
    location: location
    storageAccountName: storageAccountName
    hostingPlanName: hostingPlanName
    deploymentStorageContainerName: 'function-deployments'
    subnetId: network.outputs.subnetId
    SQL_CONNECTION_STRING: SQL_CONNECTION_STRING
    AZURE_SERVICE_CLIENT_ID: AZURE_SERVICE_CLIENT_ID
    AZURE_SERVICE_CLIENT_TENANT: AZURE_SERVICE_CLIENT_TENANT

  }
}


output referencedSubnetId string = network.outputs.subnetId
output referencedVnetId string = network.outputs.vnetId
