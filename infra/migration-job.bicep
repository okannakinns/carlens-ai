targetScope = 'resourceGroup'

@allowed([
  'staging'
  'production'
])
param environmentName string

@minLength(44)
@maxLength(44)
@description('Immutable API image tag in sha-<40 hexadecimal characters> format.')
param imageTag string

@description('Maximum duration of a migration execution in seconds.')
@minValue(60)
@maxValue(1800)
param replicaTimeoutSeconds int = 900

@description('Additional resource tags.')
param tags object = {}

var environmentShortName = environmentName == 'production' ? 'prod' : 'stg'
var uniqueSuffix = substring(uniqueString(subscription().subscriptionId, environmentName), 0, 8)
var resourceNames = {
  apiIdentity: 'id-carlens-api-${environmentShortName}'
  containerAppsEnvironment: 'cae-carlens-${environmentShortName}'
  containerRegistry: 'carlens${environmentShortName}${uniqueSuffix}'
  keyVault: 'kv-carlens-${environmentShortName}-${uniqueSuffix}'
  migrationJob: 'job-carlens-migrate-${environmentShortName}'
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

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2025-07-01' existing = {
  name: resourceNames.containerAppsEnvironment
}

resource registry 'Microsoft.ContainerRegistry/registries@2025-11-01' existing = {
  name: resourceNames.containerRegistry
}

resource keyVault 'Microsoft.KeyVault/vaults@2025-05-01' existing = {
  name: resourceNames.keyVault
}

resource apiIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' existing = {
  name: resourceNames.apiIdentity
}

resource migrationJob 'Microsoft.App/jobs@2025-07-01' = {
  name: resourceNames.migrationJob
  location: resourceGroup().location
  tags: commonTags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${apiIdentity.id}': {}
    }
  }
  properties: {
    environmentId: containerAppsEnvironment.id
    workloadProfileName: 'Consumption'
    configuration: {
      triggerType: 'Manual'
      replicaTimeout: replicaTimeoutSeconds
      replicaRetryLimit: 0
      identitySettings: [
        {
          identity: apiIdentity.id
          lifecycle: 'None'
        }
      ]
      manualTriggerConfig: {
        parallelism: 1
        replicaCompletionCount: 1
      }
      registries: [
        {
          identity: apiIdentity.id
          server: registry.properties.loginServer
        }
      ]
      secrets: [
        {
          identity: apiIdentity.id
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/postgres-connection-string'
          name: 'postgres-connection-string'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'migrations'
          image: '${registry.properties.loginServer}/carlens-api:${imageTag}'
          command: [
            '/app/efbundle'
          ]
          env: [
            {
              name: 'DOTNET_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ConnectionStrings__Postgres'
              secretRef: 'postgres-connection-string'
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
    }
  }
}

output jobName string = migrationJob.name
output imageReference string = '${registry.properties.loginServer}/carlens-api:${imageTag}'
