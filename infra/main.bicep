targetScope = 'subscription'

@description('Deployment environment represented by this resource group.')
@allowed([
  'staging'
  'production'
])
param environmentName string

@description('Primary Azure region for the environment.')
param location string = 'westeurope'

@description('Resource group that owns the environment.')
param resourceGroupName string = 'rg-carlens-${environmentName}'

@description('Virtual network address space.')
param networkAddressPrefix string

@description('Subnet delegated to the Container Apps environment.')
param containerAppsSubnetPrefix string

@description('Subnet reserved for private endpoints.')
param privateEndpointsSubnetPrefix string

@description('PostgreSQL administrator login. The password is supplied separately as a secure parameter.')
param postgresqlAdministratorLogin string = 'carlensadmin'

@secure()
@minLength(16)
@description('PostgreSQL administrator password. Supply it from a protected deployment environment variable.')
param postgresqlAdministratorPassword string

@description('PostgreSQL Flexible Server compute SKU.')
param postgresqlSkuName string

@allowed([
  'Burstable'
  'GeneralPurpose'
  'MemoryOptimized'
])
@description('PostgreSQL Flexible Server compute tier.')
param postgresqlSkuTier string

@minValue(32)
@description('PostgreSQL storage allocation in GiB.')
param postgresqlStorageSizeGb int

@minValue(7)
@maxValue(35)
@description('PostgreSQL backup retention period.')
param postgresqlBackupRetentionDays int

@allowed([
  'Disabled'
  'SameZone'
  'ZoneRedundant'
])
@description('PostgreSQL high availability mode.')
param postgresqlHighAvailabilityMode string

@description('Azure Managed Redis SKU.')
param redisSkuName string

@allowed([
  'Disabled'
  'Enabled'
])
@description('Whether Azure Managed Redis uses a replicated high-availability topology.')
param redisHighAvailability string

@allowed([
  'Basic'
  'Standard'
  'Premium'
])
@description('Azure Container Registry SKU.')
param containerRegistrySku string

@minValue(30)
@maxValue(730)
@description('Log Analytics retention period.')
param logRetentionDays int

@minValue(1)
@maxValue(100)
@description('Daily Log Analytics ingestion cap in GiB.')
param logDailyQuotaGb int

@description('Whether the Container Apps environment spans availability zones.')
param containerAppsZoneRedundant bool

@description('Whether Key Vault purge protection is enabled.')
param keyVaultPurgeProtection bool

@description('Additional resource tags.')
param tags object = {}

var environmentShortName = environmentName == 'production' ? 'prod' : 'stg'
var uniqueSuffix = substring(uniqueString(subscription().subscriptionId, environmentName), 0, 8)
var resourceNames = {
  applicationInsights: 'appi-carlens-${environmentShortName}'
  apiIdentity: 'id-carlens-api-${environmentShortName}'
  containerAppsEnvironment: 'cae-carlens-${environmentShortName}'
  containerRegistry: 'carlens${environmentShortName}${uniqueSuffix}'
  keyVault: 'kv-carlens-${environmentShortName}-${uniqueSuffix}'
  logAnalytics: 'log-carlens-${environmentShortName}'
  postgresql: 'psql-carlens-${environmentShortName}-${uniqueSuffix}'
  redis: 'redis-carlens-${environmentShortName}-${uniqueSuffix}'
  virtualNetwork: 'vnet-carlens-${environmentShortName}'
  webIdentity: 'id-carlens-web-${environmentShortName}'
  workerIdentity: 'id-carlens-worker-${environmentShortName}'
}
var commonTags = union(
  {
    application: 'Carlens AI'
    environment: environmentName
    managedBy: 'Bicep'
    repository: 'okannakinns/carlens-ai'
  },
  tags
)

resource environmentResourceGroup 'Microsoft.Resources/resourceGroups@2025-04-01' = {
  name: resourceGroupName
  location: location
  tags: commonTags
}

module network './modules/network.bicep' = {
  name: 'network-${environmentName}'
  scope: environmentResourceGroup
  params: {
    location: location
    virtualNetworkName: resourceNames.virtualNetwork
    networkAddressPrefix: networkAddressPrefix
    containerAppsSubnetPrefix: containerAppsSubnetPrefix
    privateEndpointsSubnetPrefix: privateEndpointsSubnetPrefix
    tags: commonTags
  }
}

module observability './modules/observability.bicep' = {
  name: 'observability-${environmentName}'
  scope: environmentResourceGroup
  params: {
    location: location
    logAnalyticsWorkspaceName: resourceNames.logAnalytics
    applicationInsightsName: resourceNames.applicationInsights
    logRetentionDays: logRetentionDays
    logDailyQuotaGb: logDailyQuotaGb
    tags: commonTags
  }
}

module registry './modules/registry.bicep' = {
  name: 'registry-${environmentName}'
  scope: environmentResourceGroup
  params: {
    location: location
    registryName: resourceNames.containerRegistry
    registrySku: containerRegistrySku
    tags: commonTags
  }
}

module security './modules/security.bicep' = {
  name: 'security-${environmentName}'
  scope: environmentResourceGroup
  params: {
    location: location
    keyVaultName: resourceNames.keyVault
    enablePurgeProtection: keyVaultPurgeProtection
    deploymentPrincipalId: deployer().objectId
    registryName: resourceNames.containerRegistry
    apiIdentityName: resourceNames.apiIdentity
    webIdentityName: resourceNames.webIdentity
    workerIdentityName: resourceNames.workerIdentity
    tags: commonTags
  }
  dependsOn: [
    registry
  ]
}

module data './modules/data.bicep' = {
  name: 'data-${environmentName}'
  scope: environmentResourceGroup
  params: {
    location: location
    virtualNetworkId: network.outputs.virtualNetworkId
    privateEndpointsSubnetId: network.outputs.privateEndpointsSubnetId
    postgresqlServerName: resourceNames.postgresql
    postgresqlDatabaseName: 'carlensai'
    postgresqlAdministratorLogin: postgresqlAdministratorLogin
    postgresqlAdministratorPassword: postgresqlAdministratorPassword
    postgresqlSkuName: postgresqlSkuName
    postgresqlSkuTier: postgresqlSkuTier
    postgresqlStorageSizeGb: postgresqlStorageSizeGb
    postgresqlBackupRetentionDays: postgresqlBackupRetentionDays
    postgresqlHighAvailabilityMode: postgresqlHighAvailabilityMode
    redisName: resourceNames.redis
    redisSkuName: redisSkuName
    redisHighAvailability: redisHighAvailability
    tags: commonTags
  }
}

module containerAppsEnvironment './modules/container-environment.bicep' = {
  name: 'container-environment-${environmentName}'
  scope: environmentResourceGroup
  params: {
    location: location
    environmentName: resourceNames.containerAppsEnvironment
    infrastructureSubnetId: network.outputs.containerAppsSubnetId
    logAnalyticsWorkspaceId: observability.outputs.logAnalyticsWorkspaceId
    zoneRedundant: containerAppsZoneRedundant
    tags: commonTags
  }
}

output resourceGroupName string = environmentResourceGroup.name
output containerAppsEnvironmentName string = resourceNames.containerAppsEnvironment
output containerRegistryName string = resourceNames.containerRegistry
output keyVaultName string = resourceNames.keyVault
output postgresqlServerName string = resourceNames.postgresql
output postgresqlDatabaseName string = data.outputs.postgresqlDatabaseName
output redisName string = resourceNames.redis
output apiIdentityName string = resourceNames.apiIdentity
output webIdentityName string = resourceNames.webIdentity
output workerIdentityName string = resourceNames.workerIdentity
