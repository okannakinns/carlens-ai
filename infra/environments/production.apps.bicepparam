using '../apps.bicep'

param environmentName = 'production'
param imageTag = readEnvironmentVariable('CARLENS_IMAGE_TAG')
param activeRevisionsMode = 'Multiple'
param apiMinReplicas = 2
param apiMaxReplicas = 10
param webMinReplicas = 2
param webMaxReplicas = 10
param workerMinReplicas = 1
param workerMaxReplicas = 3
