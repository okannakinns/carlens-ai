param location string
param virtualNetworkId string
param privateEndpointsSubnetId string
param postgresqlServerName string
param postgresqlDatabaseName string
param postgresqlAdministratorLogin string

@secure()
param postgresqlAdministratorPassword string

param postgresqlSkuName string

@allowed([
  'Burstable'
  'GeneralPurpose'
  'MemoryOptimized'
])
param postgresqlSkuTier string

@minValue(32)
param postgresqlStorageSizeGb int

@minValue(7)
@maxValue(35)
param postgresqlBackupRetentionDays int

@allowed([
  'Disabled'
  'SameZone'
  'ZoneRedundant'
])
param postgresqlHighAvailabilityMode string

param redisName string
param redisSkuName string

@allowed([
  'Disabled'
  'Enabled'
])
param redisHighAvailability string

param tags object

var postgresqlPrivateDnsZoneName = 'privatelink.postgres.database.azure.com'
var redisPrivateDnsZoneName = 'privatelink.redis.azure.net'
var postgresqlHighAvailability = postgresqlHighAvailabilityMode == 'Disabled'
  ? {
      mode: 'Disabled'
    }
  : {
      mode: postgresqlHighAvailabilityMode
      standbyAvailabilityZone: '2'
    }

resource postgresqlServer 'Microsoft.DBforPostgreSQL/flexibleServers@2025-08-01' = {
  name: postgresqlServerName
  location: location
  tags: tags
  sku: {
    name: postgresqlSkuName
    tier: postgresqlSkuTier
  }
  properties: {
    administratorLogin: postgresqlAdministratorLogin
    administratorLoginPassword: postgresqlAdministratorPassword
    authConfig: {
      activeDirectoryAuth: 'Disabled'
      passwordAuth: 'Enabled'
    }
    availabilityZone: '1'
    backup: {
      backupRetentionDays: postgresqlBackupRetentionDays
      geoRedundantBackup: 'Disabled'
    }
    createMode: 'Default'
    highAvailability: postgresqlHighAvailability
    maintenanceWindow: {
      customWindow: 'Enabled'
      dayOfWeek: 0
      startHour: 1
      startMinute: 0
    }
    network: {
      publicNetworkAccess: 'Disabled'
    }
    storage: {
      autoGrow: 'Enabled'
      storageSizeGB: postgresqlStorageSizeGb
    }
    version: '18'
  }
}

resource postgresqlDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2025-08-01' = {
  parent: postgresqlServer
  name: postgresqlDatabaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

resource postgresqlPrivateDnsZone 'Microsoft.Network/privateDnsZones@2024-06-01' = {
  name: postgresqlPrivateDnsZoneName
  location: 'global'
  tags: tags
}

resource postgresqlDnsLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = {
  parent: postgresqlPrivateDnsZone
  name: 'carlens-vnet-link'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: virtualNetworkId
    }
  }
}

resource postgresqlPrivateEndpoint 'Microsoft.Network/privateEndpoints@2025-05-01' = {
  name: 'pe-${postgresqlServerName}'
  location: location
  tags: tags
  properties: {
    privateLinkServiceConnections: [
      {
        name: 'postgresql-server'
        properties: {
          groupIds: [
            'postgresqlServer'
          ]
          privateLinkServiceId: postgresqlServer.id
        }
      }
    ]
    subnet: {
      id: privateEndpointsSubnetId
    }
  }
}

resource postgresqlDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2025-05-01' = {
  parent: postgresqlPrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'postgresql'
        properties: {
          privateDnsZoneId: postgresqlPrivateDnsZone.id
        }
      }
    ]
  }
}

resource redis 'Microsoft.Cache/redisEnterprise@2025-07-01' = {
  name: redisName
  location: location
  tags: tags
  sku: {
    name: redisSkuName
  }
  properties: {
    encryption: {}
    highAvailability: redisHighAvailability
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Disabled'
  }
}

resource redisDatabase 'Microsoft.Cache/redisEnterprise/databases@2025-07-01' = {
  parent: redis
  name: 'default'
  properties: {
    accessKeysAuthentication: 'Enabled'
    clientProtocol: 'Encrypted'
    clusteringPolicy: 'OSSCluster'
    evictionPolicy: 'NoEviction'
    modules: []
    persistence: {
      aofEnabled: true
      aofFrequency: '1s'
      rdbEnabled: false
    }
    port: 10000
  }
}

resource redisPrivateDnsZone 'Microsoft.Network/privateDnsZones@2024-06-01' = {
  name: redisPrivateDnsZoneName
  location: 'global'
  tags: tags
}

resource redisDnsLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = {
  parent: redisPrivateDnsZone
  name: 'carlens-vnet-link'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: virtualNetworkId
    }
  }
}

resource redisPrivateEndpoint 'Microsoft.Network/privateEndpoints@2025-05-01' = {
  name: 'pe-${redisName}'
  location: location
  tags: tags
  properties: {
    privateLinkServiceConnections: [
      {
        name: 'redis-enterprise'
        properties: {
          groupIds: [
            'redisEnterprise'
          ]
          privateLinkServiceId: redis.id
        }
      }
    ]
    subnet: {
      id: privateEndpointsSubnetId
    }
  }
}

resource redisDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2025-05-01' = {
  parent: redisPrivateEndpoint
  name: 'default'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'redis'
        properties: {
          privateDnsZoneId: redisPrivateDnsZone.id
        }
      }
    ]
  }
}

output postgresqlDatabaseName string = postgresqlDatabase.name
output postgresqlHostName string = '${postgresqlServer.name}.postgres.database.azure.com'
output redisHostName string = '${redis.name}.${location}.redis.azure.net'
output redisPort int = redisDatabase.properties.port
