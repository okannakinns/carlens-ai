using '../migration-job.bicep'

param environmentName = 'staging'
param imageTag = readEnvironmentVariable('CARLENS_IMAGE_TAG')
param replicaTimeoutSeconds = 900
