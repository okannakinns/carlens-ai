param location string
param keyVaultName string
param enablePurgeProtection bool
param deploymentPrincipalId string
param registryName string
param apiIdentityName string
param webIdentityName string
param workerIdentityName string
param tags object

var acrPullRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '7f951dda-4ed3-4680-a7ca-43fe172d538d'
)
var keyVaultSecretsOfficerRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  'b86a8fe4-44ce-4948-aee5-eccb2c155cd7'
)
var keyVaultSecretsUserRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '4633458b-17de-408a-b874-0445c86b69e6'
)

resource registry 'Microsoft.ContainerRegistry/registries@2025-11-01' existing = {
  name: registryName
}

resource keyVault 'Microsoft.KeyVault/vaults@2025-05-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    accessPolicies: []
    enablePurgeProtection: enablePurgeProtection
    enableRbacAuthorization: true
    enableSoftDelete: true
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
    publicNetworkAccess: 'Enabled'
    sku: {
      family: 'A'
      name: 'standard'
    }
    softDeleteRetentionInDays: enablePurgeProtection ? 90 : 7
    tenantId: tenant().tenantId
  }
}

resource apiIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: apiIdentityName
  location: location
  tags: tags
}

resource webIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: webIdentityName
  location: location
  tags: tags
}

resource workerIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' = {
  name: workerIdentityName
  location: location
  tags: tags
}

resource deploymentPrincipalSecretsOfficer 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, deploymentPrincipalId, keyVaultSecretsOfficerRoleDefinitionId)
  scope: keyVault
  properties: {
    principalId: deploymentPrincipalId
    roleDefinitionId: keyVaultSecretsOfficerRoleDefinitionId
  }
}

resource apiSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, apiIdentity.id, keyVaultSecretsUserRoleDefinitionId)
  scope: keyVault
  properties: {
    principalId: apiIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: keyVaultSecretsUserRoleDefinitionId
  }
}

resource webSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, webIdentity.id, keyVaultSecretsUserRoleDefinitionId)
  scope: keyVault
  properties: {
    principalId: webIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: keyVaultSecretsUserRoleDefinitionId
  }
}

resource workerSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, workerIdentity.id, keyVaultSecretsUserRoleDefinitionId)
  scope: keyVault
  properties: {
    principalId: workerIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: keyVaultSecretsUserRoleDefinitionId
  }
}

resource apiRegistryPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, apiIdentity.id, acrPullRoleDefinitionId)
  scope: registry
  properties: {
    principalId: apiIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullRoleDefinitionId
  }
}

resource webRegistryPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, webIdentity.id, acrPullRoleDefinitionId)
  scope: registry
  properties: {
    principalId: webIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullRoleDefinitionId
  }
}

resource workerRegistryPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, workerIdentity.id, acrPullRoleDefinitionId)
  scope: registry
  properties: {
    principalId: workerIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullRoleDefinitionId
  }
}

output keyVaultId string = keyVault.id
output keyVaultName string = keyVault.name
output apiIdentityId string = apiIdentity.id
output webIdentityId string = webIdentity.id
output workerIdentityId string = workerIdentity.id
