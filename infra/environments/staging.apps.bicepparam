using '../apps.bicep'

param environmentName = 'staging'
param imageTag = readEnvironmentVariable('CARLENS_IMAGE_TAG')
param activeRevisionsMode = 'Single'
param apiMinReplicas = 1
param apiMaxReplicas = 2
param webMinReplicas = 1
param webMaxReplicas = 2
param workerMinReplicas = 1
param workerMaxReplicas = 1
