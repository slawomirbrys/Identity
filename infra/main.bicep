metadata description = 'Main Bicep file for the infra module'

targetScope = 'subscription'

@description('Resource group name')
param resourceGroupName string = 'brys-identity-weu'

resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: 'West Europe'
}
