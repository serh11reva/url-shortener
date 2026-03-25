@description('Azure region.')
param location string

@description('Prefix for Log Analytics workspace and Container Apps environment names.')
param namePrefix string

@description('Environment label (e.g. dev, prod).')
param environmentName string

@description('Container image for the API (ACR or Docker Hub).')
param apiContainerImage string

@description('Ingress target port (matches Dockerfile EXPOSE / ASPNETCORE_URLS).')
param apiTargetPort int = 8080

@description('Minimum replicas for the API app.')
param minReplicas int = 1

@description('Maximum replicas for the API app. Default 1 = no scale-out.')
param maxReplicas int = 1

@description('Connection strings and secrets for the API container.')
@secure()
param cosmosConnectionString string

@secure()
param redisConnectionString string

@secure()
param serviceBusConnectionString string

var workspaceName = '${namePrefix}-logs-${environmentName}'
var envName = '${namePrefix}-cae-${environmentName}'
var appName = '${namePrefix}-api-${environmentName}'

resource workspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: workspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: envName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: workspace.properties.customerId
        sharedKey: workspace.listKeys().primarySharedKey
      }
    }
  }
}

resource apiContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: appName
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: apiTargetPort
        transport: 'auto'
      }
      secrets: [
        {
          name: 'cosmos-connection'
          value: cosmosConnectionString
        }
        {
          name: 'redis-connection'
          value: redisConnectionString
        }
        {
          name: 'messaging-connection'
          value: serviceBusConnectionString
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: apiContainerImage
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ConnectionStrings__cosmos'
              secretRef: 'cosmos-connection'
            }
            {
              name: 'ConnectionStrings__cache'
              secretRef: 'redis-connection'
            }
            {
              name: 'ConnectionStrings__messaging'
              secretRef: 'messaging-connection'
            }
          ]
          // TCP probes: default health HTTP routes are Development-only in ServiceDefaults; TCP avoids failed probes in Production.
          probes: [
            {
              type: 'Liveness'
              tcpSocket: {
                port: apiTargetPort
              }
              initialDelaySeconds: 15
              periodSeconds: 20
            }
            {
              type: 'Readiness'
              tcpSocket: {
                port: apiTargetPort
              }
              initialDelaySeconds: 10
              periodSeconds: 15
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
    }
  }
}

@description('Public HTTPS URL of the API (no trailing slash).')
output apiUrl string = 'https://${apiContainerApp.properties.configuration.ingress.fqdn}'

output containerAppName string = apiContainerApp.name
output managedEnvironmentId string = containerAppsEnvironment.id
