@description('Azure region.')
param location string

@description('Globally unique Redis cache name (alphanumeric and hyphens).')
param cacheName string

@description('SKU for Azure Cache for Redis.')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param skuName string = 'Basic'

@description('Family: C for Basic/Standard, P for Premium.')
@allowed([
  'C'
  'P'
])
param skuFamily string = 'C'

@description('Cache capacity (e.g. 0 = Basic C0, 1 = C1).')
param skuCapacity int = 0

resource redis 'Microsoft.Cache/redis@2023-08-01' = {
  name: cacheName
  location: location
  properties: {
    sku: {
      name: skuName
      family: skuFamily
      capacity: skuCapacity
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

var primaryKey = redis.listKeys().primaryKey

@description('StackExchange.Redis connection string for ConnectionStrings__cache.')
output connectionString string = '${redis.name}.redis.cache.windows.net:6380,password=${primaryKey},ssl=True,abortConnect=False'

output hostName string = redis.properties.hostName
output name string = redis.name
