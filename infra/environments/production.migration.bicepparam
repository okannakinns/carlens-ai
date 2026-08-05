using '../migration-job.bicep'

param environmentName = 'production'
param imageTag = readEnvironmentVariable('CARLENS_IMAGE_TAG')
param replicaTimeoutSeconds = 900
