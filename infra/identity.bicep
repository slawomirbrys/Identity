metadata description = 'Identity module'

targetScope = 'resourceGroup'

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-06-01' = {
  name: 'brysidentityweu'
  location: resourceGroup().location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
}