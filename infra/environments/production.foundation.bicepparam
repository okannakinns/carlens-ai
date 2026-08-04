using '../main.bicep'

param environmentName = 'production'
param location = 'westeurope'
param networkAddressPrefix = '10.30.0.0/16'
param containerAppsSubnetPrefix = '10.30.0.0/23'
param privateEndpointsSubnetPrefix = '10.30.4.0/24'
param postgresqlAdministratorPassword = readEnvironmentVariable('CARLENS_POSTGRES_ADMIN_PASSWORD')
param postgresqlSkuName = 'Standard_D2ds_v5'
param postgresqlSkuTier = 'GeneralPurpose'
param postgresqlStorageSizeGb = 64
param postgresqlBackupRetentionDays = 14
param postgresqlHighAvailabilityMode = 'ZoneRedundant'
param redisSkuName = 'Balanced_B1'
param redisHighAvailability = 'Enabled'
param containerRegistrySku = 'Standard'
param logRetentionDays = 90
param logDailyQuotaGb = 5
param containerAppsZoneRedundant = true
param keyVaultPurgeProtection = true
