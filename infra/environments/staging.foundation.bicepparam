using '../main.bicep'

param environmentName = 'staging'
param location = 'westeurope'
param networkAddressPrefix = '10.20.0.0/16'
param containerAppsSubnetPrefix = '10.20.0.0/23'
param privateEndpointsSubnetPrefix = '10.20.4.0/24'
param postgresqlAdministratorPassword = readEnvironmentVariable('CARLENS_POSTGRES_ADMIN_PASSWORD')
param postgresqlSkuName = 'Standard_B1ms'
param postgresqlSkuTier = 'Burstable'
param postgresqlStorageSizeGb = 32
param postgresqlBackupRetentionDays = 7
param postgresqlHighAvailabilityMode = 'Disabled'
param redisSkuName = 'Balanced_B0'
param redisHighAvailability = 'Enabled'
param containerRegistrySku = 'Basic'
param logRetentionDays = 30
param logDailyQuotaGb = 1
param containerAppsZoneRedundant = false
param keyVaultPurgeProtection = false
