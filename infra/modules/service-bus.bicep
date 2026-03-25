@description('Azure region.')
param location string

@description('Globally unique Service Bus namespace name (6–50 chars, alphanumeric and hyphens).')
param namespaceName string

@description('Queue for click analytics (matches ClickAnalytics:QueueName).')
param clicksQueueName string = 'clicks'

@description('Basic is lowest cost and supports queues; use Standard if you need topics/sessions.')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param skuName string = 'Basic'

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: namespaceName
  location: location
  sku: {
    name: skuName
    tier: skuName
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource clicksQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: clicksQueueName
  properties: {
    maxDeliveryCount: 10
    deadLetteringOnMessageExpiration: true
    enablePartitioning: false
  }
}

resource rootRule 'Microsoft.ServiceBus/namespaces/AuthorizationRules@2022-10-01-preview' existing = {
  name: 'RootManageSharedAccessKey'
  parent: serviceBusNamespace
}

var primaryKey = rootRule.listKeys().primaryKey

@description('Connection string for AddAzureServiceBusClient("messaging").')
output connectionString string = 'Endpoint=sb://${serviceBusNamespace.name}.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=${primaryKey};'

output namespaceHostName string = '${serviceBusNamespace.name}.servicebus.windows.net'
output queueName string = clicksQueue.name
