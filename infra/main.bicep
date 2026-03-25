targetScope = 'subscription'

@description('Azure region for all resources. Default: West Europe (single region, no geo; typically cost-effective vs many primary regions). Override if your subscription requires another EU region.')
param location string = 'westeurope'

@description('Logical environment name used in resource naming (e.g. dev, staging, prod).')
param environmentName string = 'dev'

@description('Short prefix for resource names (letters and numbers; keep short for global name limits).')
param namePrefix string = 'shortener'

@description('Resource group name to create. Leave empty to use rg-{namePrefix}-{environmentName}.')
param resourceGroupName string = ''

@description('hostingModel = containerApps: Azure Container Apps + Log Analytics. hostingModel = appService: Linux App Service (Web App for Containers).')
@allowed([
  'containerApps'
  'appService'
])
param hostingModel string = 'containerApps'

@description('Container image for the API. Override in CI/CD with your ACR image (e.g. myregistry.azurecr.io/shortener-api:latest). Default is a public sample image for template validation only.')
param apiContainerImage string = 'mcr.microsoft.com/dotnet/samples:aspnetapp'

@minValue(1)
@maxValue(30)
@description('Container Apps minimum replicas. Default 1.')
param containerAppsMinReplicas int = 1

@minValue(1)
@maxValue(30)
@description('Container Apps maximum replicas. Default 1 = single container, no scale-out.')
param containerAppsMaxReplicas int = 1

@description('Cosmos DB account name; leave empty to auto-generate a unique name.')
param cosmosAccountName string = ''

@description('Use Cosmos DB serverless capacity mode (pay per RU; good for light workloads).')
param cosmosEnableServerless bool = true

@description('Azure Cache for Redis name; leave empty to auto-generate.')
param redisCacheName string = ''

@description('Redis SKU. Basic C0 is the smallest/cheapest tier.')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param redisSkuName string = 'Basic'

@description('Service Bus namespace name; leave empty to auto-generate.')
param serviceBusNamespaceName string = ''

@description('Service Bus SKU. Basic is lowest cost and supports queues used by this app.')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param serviceBusSkuName string = 'Basic'

@description('Linux App Service plan name when hostingModel = appService.')
param appServicePlanName string = ''

@description('Globally unique web app name when hostingModel = appService; leave empty to auto-generate.')
param appServiceSiteName string = ''

@description('App Service plan SKU when hostingModel = appService (B1 is a small burstable Linux VM).')
param appServicePlanSku string = 'B1'

var resolvedRgName = !empty(resourceGroupName) ? resourceGroupName : 'rg-${namePrefix}-${environmentName}'
var uniqueSuffix = take(uniqueString(subscription().subscriptionId, resolvedRgName, environmentName, namePrefix), 13)

var cosmosName = !empty(cosmosAccountName)
  ? toLower(cosmosAccountName)
  : toLower('${take(replace(namePrefix, '-', ''), 8)}-cosmos-${environmentName}-${uniqueSuffix}')

var redisName = !empty(redisCacheName)
  ? toLower(redisCacheName)
  : toLower('${take(replace(namePrefix, '-', ''), 8)}-redis-${environmentName}-${uniqueSuffix}')

var sbName = !empty(serviceBusNamespaceName)
  ? toLower(serviceBusNamespaceName)
  : toLower('${take(replace(namePrefix, '-', ''), 8)}-sb-${environmentName}-${uniqueSuffix}')

var resolvedPlanName = !empty(appServicePlanName) ? appServicePlanName : '${namePrefix}-plan-${environmentName}'
var resolvedSiteName = !empty(appServiceSiteName)
  ? toLower(appServiceSiteName)
  : toLower('${take(replace(namePrefix, '-', ''), 8)}-api-${environmentName}-${uniqueSuffix}')

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resolvedRgName
  location: location
  tags: {
    environment: environmentName
    application: 'url-shortener'
  }
}

module cosmos 'modules/cosmos.bicep' = {
  scope: rg
  name: 'cosmos-${environmentName}'
  params: {
    location: location
    accountName: cosmosName
    enableServerless: cosmosEnableServerless
  }
}

module redis 'modules/redis.bicep' = {
  scope: rg
  name: 'redis-${environmentName}'
  params: {
    location: location
    cacheName: redisName
    skuName: redisSkuName
  }
}

module serviceBus 'modules/service-bus.bicep' = {
  scope: rg
  name: 'servicebus-${environmentName}'
  params: {
    location: location
    namespaceName: sbName
    skuName: serviceBusSkuName
  }
}

module containerApps 'modules/container-apps.bicep' = if (hostingModel == 'containerApps') {
  scope: rg
  name: 'container-apps-${environmentName}'
  params: {
    location: location
    namePrefix: namePrefix
    environmentName: environmentName
    apiContainerImage: apiContainerImage
    minReplicas: containerAppsMinReplicas
    maxReplicas: containerAppsMaxReplicas
    cosmosConnectionString: cosmos.outputs.connectionString
    redisConnectionString: redis.outputs.connectionString
    serviceBusConnectionString: serviceBus.outputs.connectionString
  }
}

module appService 'modules/app-service-api.bicep' = if (hostingModel == 'appService') {
  scope: rg
  name: 'app-service-${environmentName}'
  params: {
    location: location
    planName: resolvedPlanName
    siteName: resolvedSiteName
    skuName: appServicePlanSku
    apiContainerImage: apiContainerImage
    cosmosConnectionString: cosmos.outputs.connectionString
    redisConnectionString: redis.outputs.connectionString
    serviceBusConnectionString: serviceBus.outputs.connectionString
  }
}

@description('Resource group id containing the deployment.')
output resourceGroupId string = rg.id

@description('Public base URL for the API when using Container Apps.')
output apiUrlContainerApps string = hostingModel == 'containerApps' ? containerApps!.outputs.apiUrl : ''

@description('Public base URL for the API when using App Service.')
output apiUrlAppService string = hostingModel == 'appService' ? appService!.outputs.apiUrl : ''

@description('Convenience: primary API URL for the selected hosting model.')
output apiUrl string = hostingModel == 'containerApps' ? containerApps!.outputs.apiUrl : appService!.outputs.apiUrl

output cosmosAccountName string = cosmos.outputs.accountNameOut
output redisCacheName string = redis.outputs.name
output serviceBusNamespace string = serviceBus.outputs.namespaceHostName
