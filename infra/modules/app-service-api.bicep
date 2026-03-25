@description('Azure region.')
param location string

@description('App Service plan name.')
param planName string

@description('Web app name (globally unique).')
param siteName string

@description('Linux App Service plan SKU (e.g. B1, P1v3).')
param skuName string = 'B1'

@description('Worker size (1 = small).')
param skuCapacity int = 1

@description('Container image for the API.')
param apiContainerImage string

@description('Container listens on this port (WEBSITES_PORT).')
param apiTargetPort int = 8080

@secure()
param cosmosConnectionString string

@secure()
param redisConnectionString string

@secure()
param serviceBusConnectionString string

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  kind: 'linux'
  sku: {
    name: skuName
    capacity: skuCapacity
  }
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: siteName
  location: location
  kind: 'app,linux,container'
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOCKER|${apiContainerImage}'
      acrUseManagedIdentityCreds: false
      alwaysOn: skuName != 'F1' && skuName != 'D1'
      appSettings: [
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'false'
        }
        {
          name: 'WEBSITES_PORT'
          value: string(apiTargetPort)
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'ConnectionStrings__cosmos'
          value: cosmosConnectionString
        }
        {
          name: 'ConnectionStrings__cache'
          value: redisConnectionString
        }
        {
          name: 'ConnectionStrings__messaging'
          value: serviceBusConnectionString
        }
      ]
    }
  }
}

output defaultHostName string = webApp.properties.defaultHostName
output apiUrl string = 'https://${webApp.properties.defaultHostName}'
output webAppName string = webApp.name
