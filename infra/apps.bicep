targetScope = 'resourceGroup'

@allowed([
  'staging'
  'production'
])
param environmentName string

@minLength(44)
@maxLength(44)
@description('Immutable container image tag in sha-<40 hexadecimal characters> format.')
param imageTag string

@allowed([
  'Single'
  'Multiple'
])
@description('Container Apps revision mode. Production uses Multiple for blue-green traffic control.')
param activeRevisionsMode string

@maxLength(64)
@description('Current API revision receiving production traffic. Leave empty for staging and the first production deployment.')
param stableApiRevisionName string = ''

@maxLength(64)
@description('Current Web revision receiving production traffic. Leave empty for staging and the first production deployment.')
param stableWebRevisionName string = ''

@description('Whether this deployment should create or update the Worker revision.')
param deployWorker bool = true

@minValue(1)
param apiMinReplicas int

@minValue(1)
param apiMaxReplicas int

@minValue(1)
param webMinReplicas int

@minValue(1)
param webMaxReplicas int

@minValue(1)
param workerMinReplicas int

@minValue(1)
param workerMaxReplicas int

@description('Additional resource tags.')
param tags object = {}

var environmentShortName = environmentName == 'production' ? 'prod' : 'stg'
var uniqueSuffix = substring(uniqueString(subscription().subscriptionId, environmentName), 0, 8)
var resourceNames = {
  api: 'ca-carlens-api-${environmentShortName}'
  apiIdentity: 'id-carlens-api-${environmentShortName}'
  applicationInsights: 'appi-carlens-${environmentShortName}'
  containerAppsEnvironment: 'cae-carlens-${environmentShortName}'
  containerRegistry: 'carlens${environmentShortName}${uniqueSuffix}'
  keyVault: 'kv-carlens-${environmentShortName}-${uniqueSuffix}'
  web: 'ca-carlens-web-${environmentShortName}'
  webIdentity: 'id-carlens-web-${environmentShortName}'
  worker: 'ca-carlens-worker-${environmentShortName}'
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
var revisionSuffix = 'sha-${substring(imageTag, 4, 12)}'
var candidateApiRevisionName = '${resourceNames.api}--${revisionSuffix}'
var candidateWebRevisionName = '${resourceNames.web}--${revisionSuffix}'
var candidateWorkerRevisionName = '${resourceNames.worker}--${revisionSuffix}'
var apiTraffic = empty(stableApiRevisionName) ? [
  {
    label: 'stable'
    revisionName: candidateApiRevisionName
    weight: 100
  }
] : [
  {
    label: 'stable'
    revisionName: stableApiRevisionName
    weight: 100
  }
  {
    label: 'candidate'
    revisionName: candidateApiRevisionName
    weight: 0
  }
]
var webTraffic = empty(stableWebRevisionName) ? [
  {
    label: 'stable'
    revisionName: candidateWebRevisionName
    weight: 100
  }
] : [
  {
    label: 'stable'
    revisionName: stableWebRevisionName
    weight: 100
  }
  {
    label: 'candidate'
    revisionName: candidateWebRevisionName
    weight: 0
  }
]

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2026-01-01' existing = {
  name: resourceNames.containerAppsEnvironment
}

resource registry 'Microsoft.ContainerRegistry/registries@2025-11-01' existing = {
  name: resourceNames.containerRegistry
}

resource keyVault 'Microsoft.KeyVault/vaults@2025-05-01' existing = {
  name: resourceNames.keyVault
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: resourceNames.applicationInsights
}

resource apiIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' existing = {
  name: resourceNames.apiIdentity
}

resource webIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' existing = {
  name: resourceNames.webIdentity
}

resource workerIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2024-11-30' existing = {
  name: resourceNames.workerIdentity
}

resource api 'Microsoft.App/containerApps@2026-01-01' = {
  name: resourceNames.api
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
      activeRevisionsMode: activeRevisionsMode
      identitySettings: [
        {
          identity: apiIdentity.id
          lifecycle: 'None'
        }
      ]
      ingress: union(
        {
          allowInsecure: false
          external: false
          targetPort: 8080
          transport: 'auto'
        },
        activeRevisionsMode == 'Multiple' ? {
          traffic: apiTraffic
        } : {}
      )
      maxInactiveRevisions: 10
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
        {
          identity: apiIdentity.id
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/redis-connection-string'
          name: 'redis-connection-string'
        }
        {
          identity: apiIdentity.id
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/rabbitmq-uri'
          name: 'rabbitmq-uri'
        }
        {
          identity: apiIdentity.id
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/internal-api-key'
          name: 'internal-api-key'
        }
      ]
    }
    template: {
      revisionSuffix: revisionSuffix
      terminationGracePeriodSeconds: 60
      containers: [
        {
          name: 'api'
          image: '${registry.properties.loginServer}/carlens-api:${imageTag}'
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
              value: 'true'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: applicationInsights.properties.ConnectionString
            }
            {
              name: 'OTEL_SERVICE_NAME'
              value: 'carlens-api'
            }
            {
              name: 'OTEL_RESOURCE_ATTRIBUTES'
              value: 'deployment.environment.name=${environmentName}'
            }
            {
              name: 'ConnectionStrings__Postgres'
              secretRef: 'postgres-connection-string'
            }
            {
              name: 'Redis__ConnectionString'
              secretRef: 'redis-connection-string'
            }
            {
              name: 'RabbitMQ__Uri'
              secretRef: 'rabbitmq-uri'
            }
            {
              name: 'Security__InternalApiKey'
              secretRef: 'internal-api-key'
            }
          ]
          probes: [
            {
              type: 'Startup'
              httpGet: {
                path: '/health/live'
                port: 8080
                scheme: 'HTTP'
              }
              failureThreshold: 30
              initialDelaySeconds: 1
              periodSeconds: 2
              successThreshold: 1
              timeoutSeconds: 2
            }
            {
              type: 'Liveness'
              httpGet: {
                path: '/health/live'
                port: 8080
                scheme: 'HTTP'
              }
              failureThreshold: 3
              initialDelaySeconds: 10
              periodSeconds: 15
              successThreshold: 1
              timeoutSeconds: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health/ready'
                port: 8080
                scheme: 'HTTP'
              }
              failureThreshold: 3
              initialDelaySeconds: 5
              periodSeconds: 10
              successThreshold: 1
              timeoutSeconds: 5
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
      scale: {
        minReplicas: apiMinReplicas
        maxReplicas: apiMaxReplicas
      }
    }
  }
}

resource web 'Microsoft.App/containerApps@2026-01-01' = {
  name: resourceNames.web
  location: resourceGroup().location
  tags: commonTags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${webIdentity.id}': {}
    }
  }
  properties: {
    environmentId: containerAppsEnvironment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: activeRevisionsMode
      identitySettings: [
        {
          identity: webIdentity.id
          lifecycle: 'None'
        }
      ]
      ingress: union(
        {
          allowInsecure: false
          external: true
          targetPort: 8080
          transport: 'auto'
        },
        activeRevisionsMode == 'Multiple' ? {
          traffic: webTraffic
        } : {}
      )
      maxInactiveRevisions: 10
      registries: [
        {
          identity: webIdentity.id
          server: registry.properties.loginServer
        }
      ]
      secrets: [
        {
          identity: webIdentity.id
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/redis-connection-string'
          name: 'redis-connection-string'
        }
        {
          identity: webIdentity.id
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/internal-api-key'
          name: 'internal-api-key'
        }
      ]
    }
    template: {
      revisionSuffix: revisionSuffix
      terminationGracePeriodSeconds: 60
      containers: [
        {
          name: 'web'
          image: '${registry.properties.loginServer}/carlens-web:${imageTag}'
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED'
              value: 'true'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: applicationInsights.properties.ConnectionString
            }
            {
              name: 'OTEL_SERVICE_NAME'
              value: 'carlens-web'
            }
            {
              name: 'OTEL_RESOURCE_ATTRIBUTES'
              value: 'deployment.environment.name=${environmentName}'
            }
            {
              name: 'CarlensApi__BaseUrl'
              value: 'https://${api.properties.configuration.ingress.fqdn}'
            }
            {
              name: 'Redis__ConnectionString'
              secretRef: 'redis-connection-string'
            }
            {
              name: 'Security__InternalApiKey'
              secretRef: 'internal-api-key'
            }
          ]
          probes: [
            {
              type: 'Startup'
              httpGet: {
                path: '/health/live'
                port: 8080
                scheme: 'HTTP'
              }
              failureThreshold: 30
              initialDelaySeconds: 1
              periodSeconds: 2
              successThreshold: 1
              timeoutSeconds: 2
            }
            {
              type: 'Liveness'
              httpGet: {
                path: '/health/live'
                port: 8080
                scheme: 'HTTP'
              }
              failureThreshold: 3
              initialDelaySeconds: 10
              periodSeconds: 15
              successThreshold: 1
              timeoutSeconds: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health/ready'
                port: 8080
                scheme: 'HTTP'
              }
              failureThreshold: 3
              initialDelaySeconds: 5
              periodSeconds: 10
              successThreshold: 1
              timeoutSeconds: 5
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
      scale: {
        minReplicas: webMinReplicas
        maxReplicas: webMaxReplicas
      }
    }
  }
}

resource worker 'Microsoft.App/containerApps@2026-01-01' = if (deployWorker) {
  name: resourceNames.worker
  location: resourceGroup().location
  tags: commonTags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${workerIdentity.id}': {}
    }
  }
  properties: {
    environmentId: containerAppsEnvironment.id
    workloadProfileName: 'Consumption'
    configuration: {
      activeRevisionsMode: 'Single'
      identitySettings: [
        {
          identity: workerIdentity.id
          lifecycle: 'None'
        }
      ]
      maxInactiveRevisions: 5
      registries: [
        {
          identity: workerIdentity.id
          server: registry.properties.loginServer
        }
      ]
      secrets: [
        {
          identity: workerIdentity.id
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/postgres-connection-string'
          name: 'postgres-connection-string'
        }
        {
          identity: workerIdentity.id
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/redis-connection-string'
          name: 'redis-connection-string'
        }
        {
          identity: workerIdentity.id
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/rabbitmq-uri'
          name: 'rabbitmq-uri'
        }
        {
          identity: workerIdentity.id
          keyVaultUrl: '${keyVault.properties.vaultUri}secrets/openai-api-key'
          name: 'openai-api-key'
        }
      ]
    }
    template: {
      revisionSuffix: revisionSuffix
      terminationGracePeriodSeconds: 150
      containers: [
        {
          name: 'aiworker'
          image: '${registry.properties.loginServer}/carlens-aiworker:${imageTag}'
          env: [
            {
              name: 'DOTNET_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: applicationInsights.properties.ConnectionString
            }
            {
              name: 'OTEL_SERVICE_NAME'
              value: 'carlens-aiworker'
            }
            {
              name: 'OTEL_RESOURCE_ATTRIBUTES'
              value: 'deployment.environment.name=${environmentName}'
            }
            {
              name: 'ConnectionStrings__Postgres'
              secretRef: 'postgres-connection-string'
            }
            {
              name: 'Redis__ConnectionString'
              secretRef: 'redis-connection-string'
            }
            {
              name: 'RabbitMQ__Uri'
              secretRef: 'rabbitmq-uri'
            }
            {
              name: 'OpenAI__ApiKey'
              secretRef: 'openai-api-key'
            }
          ]
          resources: {
            cpu: json('1.0')
            memory: '2Gi'
          }
        }
      ]
      scale: {
        minReplicas: workerMinReplicas
        maxReplicas: workerMaxReplicas
        rules: [
          {
            name: 'rabbitmq-analysis-queue'
            custom: {
              type: 'rabbitmq'
              metadata: {
                mode: 'QueueLength'
                protocol: 'amqp'
                queueName: 'listing-analysis-requested'
                tls: 'enable'
                value: '1'
              }
              auth: [
                {
                  secretRef: 'rabbitmq-uri'
                  triggerParameter: 'host'
                }
              ]
            }
          }
        ]
      }
    }
  }
}

output apiName string = api.name
output apiFqdn string = api.properties.configuration.ingress.fqdn
output apiImageReference string = '${registry.properties.loginServer}/carlens-api:${imageTag}'
output apiRevisionName string = candidateApiRevisionName
output webName string = web.name
output webFqdn string = web.properties.configuration.ingress.fqdn
output webImageReference string = '${registry.properties.loginServer}/carlens-web:${imageTag}'
output webRevisionName string = candidateWebRevisionName
output candidateWebFqdn string = activeRevisionsMode == 'Multiple' && !empty(stableWebRevisionName)
  ? replace(web.properties.configuration.ingress.fqdn, '${web.name}.', '${web.name}---candidate.')
  : web.properties.configuration.ingress.fqdn
output workerName string = resourceNames.worker
output workerImageReference string = '${registry.properties.loginServer}/carlens-aiworker:${imageTag}'
output workerRevisionName string = candidateWorkerRevisionName
output revisionSuffix string = revisionSuffix
