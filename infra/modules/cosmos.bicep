@description('Azure region for the Cosmos DB account.')
param location string

@description('Globally unique name for the Cosmos DB account (lowercase letters, numbers, hyphens; max 44).')
param accountName string

@description('SQL API database id (matches app Cosmos:DatabaseId).')
param databaseName string = 'shortener'

@description('Container id (matches app Cosmos:ContainerId).')
param containerName string = 'urls'

@description('When true, the account uses serverless RU (good for low/variable traffic).')
param enableServerless bool = true

@description('Provisioned throughput (RU/s) for the database when not serverless. Ignored when serverless.')
param provisionedDatabaseThroughput int = 400

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: accountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    enableAutomaticFailover: false
    enableMultipleWriteLocations: false
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    // Single region only — no geo-replication (cost and minimal footprint).
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    capabilities: enableServerless ? [{ name: 'EnableServerless' }] : []
  }
}

resource sqlDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: cosmosAccount
  name: databaseName
  properties: enableServerless
    ? {
        resource: {
          id: databaseName
        }
      }
    : {
        resource: {
          id: databaseName
        }
        options: {
          throughput: provisionedDatabaseThroughput
        }
      }
}

resource urlsContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  parent: sqlDatabase
  name: containerName
  properties: {
    resource: {
      id: containerName
      partitionKey: {
        paths: ['/pk']
        kind: 'Hash'
      }
      defaultTtl: -1
    }
  }
}

var primaryKey = cosmosAccount.listKeys().primaryMasterKey

@description('Connection string for Microsoft.Azure.Cosmos / Aspire AddAzureCosmosClient("cosmos").')
output connectionString string = 'AccountEndpoint=${cosmosAccount.properties.documentEndpoint};AccountKey=${primaryKey};'

output accountNameOut string = cosmosAccount.name
output documentEndpoint string = cosmosAccount.properties.documentEndpoint
