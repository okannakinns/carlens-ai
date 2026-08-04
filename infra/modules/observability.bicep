param location string
param logAnalyticsWorkspaceName string
param applicationInsightsName string

@minValue(30)
@maxValue(730)
param logRetentionDays int

@minValue(1)
@maxValue(100)
param logDailyQuotaGb int

param tags object

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2025-02-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  tags: tags
  properties: {
    features: {
      disableLocalAuth: true
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    retentionInDays: logRetentionDays
    sku: {
      name: 'PerGB2018'
    }
    workspaceCapping: {
      dailyQuotaGb: logDailyQuotaGb
    }
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    DisableIpMasking: false
    IngestionMode: 'LogAnalytics'
    RetentionInDays: logRetentionDays
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
output applicationInsightsName string = applicationInsights.name
