param location string
param environmentName string
param infrastructureSubnetId string
param logAnalyticsWorkspaceId string
param zoneRedundant bool
param tags object

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2026-01-01' = {
  name: environmentName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'azure-monitor'
    }
    publicNetworkAccess: 'Enabled'
    vnetConfiguration: {
      infrastructureSubnetId: infrastructureSubnetId
      internal: false
    }
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
    zoneRedundant: zoneRedundant
  }
}

#disable-next-line use-recent-api-versions // Latest API that supports diagnostic category groups.
resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'send-to-log-analytics'
  scope: containerAppsEnvironment
  properties: {
    logAnalyticsDestinationType: 'Dedicated'
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
    workspaceId: logAnalyticsWorkspaceId
  }
}

output environmentId string = containerAppsEnvironment.id
output environmentName string = containerAppsEnvironment.name
