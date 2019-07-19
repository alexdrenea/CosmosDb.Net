
using CosmosDb.Domain.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Collections.Concurrent;
using System.Threading;
using System.Reflection;
using CosmosDb.Domain;
using Microsoft.Azure.Cosmos;
using CosmosDb.Cosmos;
using System.IO;

namespace CosmosDb
{
    public class CosmosDbClient : ICosmosClient
    {
        public CosmosClient Client { get; private set; }
        public Database Database { get; private set; }
        public Container Container { get; private set; }

        private string _partitionKeyPropertyName;

        #region Initialization

        public static Task<CosmosDbClient> GetCosmosDbClient(string connectionString, string databaseId, string containerId, int initialContainerRUs = 400, string partitionKeyPath = "/PartitionKey", bool forceCreate = true)
        {
            var cco = new CosmosClientOptions()
            {
                ConnectionMode = ConnectionMode.Direct,
                RequestTimeout = new TimeSpan(1, 0, 0),
            };

            var ccs = new ContainerProperties
            {
                Id = containerId,
                PartitionKeyPath = partitionKeyPath
            };

            return GetCosmosDbClientInternal(new CosmosClient(connectionString, cco), databaseId, ccs, initialContainerRUs, forceCreate);
        }

        public static Task<CosmosDbClient> GetCosmosDbClient(string accountName, string databaseId, string containerId, string key, int initialContainerRUs = 400, string partitionKeyPath = "/PartitionKey", bool forceCreate = true)
        {
            var cco = new CosmosClientOptions()
            {
                ConnectionMode = ConnectionMode.Direct,
                RequestTimeout = new TimeSpan(1, 0, 0),
            };

            var ccs = new ContainerProperties
            {
                Id = containerId,
                PartitionKeyPath = partitionKeyPath
            };

            return GetCosmosDbClientInternal(new CosmosClient(accountName, key, cco), databaseId, ccs, initialContainerRUs, forceCreate);
        }

        public static Task<CosmosDbClient> GetCosmosDbClient(string connectionString, string databaseId, ContainerProperties settings, int initialContainerRUs = 400, bool forceCreate = true)
        {
            var cco = new CosmosClientOptions()
            {
                ConnectionMode = ConnectionMode.Direct,
                RequestTimeout = new TimeSpan(1, 0, 0),
            };

            return GetCosmosDbClientInternal(new CosmosClient(connectionString, cco), databaseId, settings, initialContainerRUs, forceCreate);
        }

        public static Task<CosmosDbClient> GetCosmosDbClient(string accountName, string databaseId, ContainerProperties containerSettings, string key, int initialContainerRUs = 400, bool forceCreate = true)
        {
            var cco = new CosmosClientOptions()
            {
                ConnectionMode = ConnectionMode.Direct,
                RequestTimeout = new TimeSpan(1, 0, 0),
            };
            return GetCosmosDbClientInternal(new CosmosClient(accountName, key, cco), databaseId, containerSettings, initialContainerRUs, forceCreate);
        }


        private static async Task<CosmosDbClient> GetCosmosDbClientInternal(CosmosClient client, string databaseId, ContainerProperties containerSettings, int initialContainerRUs, bool forceCreate = true)
        {
            Database database = null;
            Container container = null;

            if (forceCreate)
            {
                database = await client.CreateDatabaseIfNotExistsAsync(databaseId);
                container = await database.CreateContainerIfNotExistsAsync(containerSettings, initialContainerRUs);
            }
            else
            {
                database = client.GetDatabase(databaseId);
                var ensureDbExists = await database.ReadAsync();
                if (ensureDbExists.StatusCode == System.Net.HttpStatusCode.NotFound)
                    throw new Exception($"Database '{databaseId}' not found. Use forceCreate:true if you want the database to be created for you.");

                container = database.GetContainer(containerSettings.Id);
                var ensureExists = await container.ReadContainerAsync();
                if (ensureExists.StatusCode == System.Net.HttpStatusCode.NotFound)
                    throw new Exception($"Container '{containerSettings.Id}' not found. Use forceCreate:true if you want a collection to be created for you.");
            }

            
            var res = new CosmosDbClient
            {
                Client = client,
                Database = database,
                Container = container
            };
            res._partitionKeyPropertyName = containerSettings.PartitionKeyPath.Trim('/');
            return res;
        }

        #endregion


        public Task<CosmosResponse<IEnumerable<T>>> ExecuteSQL<T>(string query, bool expectGraphson = true)
        {
            //var queryRes = Container.CreateItemQuery<T>(query);
            //queryRes.
            return null;
        }

        public async Task<ItemResponse<T>> InsertDocument<T>(T document)
        {
            var add = await Container.CreateItemAsync<T>(document);
            return add;
        }

        public Task<IEnumerable<CosmosResponse>> InsertDocuments<T>(IEnumerable<T> documents, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10)
        {
            throw new NotImplementedException();
        }

        public async Task<ResponseMessage> UpsertDocument<T>(T document)
        {
            try
            {
                var internalDoc = document.ToCosmosDocument<T>();
                var res = await Container.UpsertItemAsync(internalDoc, new PartitionKey(internalDoc[_partitionKeyPropertyName].ToString()));

                return null;
            }
            catch (Exception e)
            {
                var x = e.Message;
                return null;
            }
        }

        public Task<IEnumerable<CosmosResponse>> UpsertDocuments<T>(IEnumerable<T> documents, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10)
        {
            throw new NotImplementedException();
        }

        public async Task<CosmosResponse<T>> ReadDocument<T>(string docId, string partitionKey)
        {
            var stream = await Container.ReadItemStreamAsync(docId, new PartitionKey(partitionKey));
            var elem3 = await Container.ReadItemAsync<JObject>(docId, new PartitionKey(partitionKey));

            var elem = await Container.ReadItemAsync<T>(docId, new PartitionKey(partitionKey));
            var elem2 = await Container.ReadItemAsync<IDictionary<string,object>>(docId, new PartitionKey(partitionKey));

            var movieFull = SerializationHelpers.FromDocument<T>(elem3.Resource);

            return null;
        }

        public Task<CosmosResponse> InsertGraphVertex<T>(T vertex)
        {
            throw new NotImplementedException();
        }

        public Task<CosmosResponse> UpsertGraphVertex<T>(T vertex)
        {
            throw new NotImplementedException();
        }

        public Task<CosmosResponse<T>> ReadGraphVertex<T>(string docId, string partitionKey)
        {
            throw new NotImplementedException();
        }

        public Task<CosmosResponse> InsertGraphEdge<T, U, V>(T edge, U source, V target)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<CosmosResponse>> InsertGraphVertex<T>(IEnumerable<T> vertices, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10)
        {
            throw new NotImplementedException();
        }

        public Task<CosmosResponse> UpsertGraphEdge<T, U, V>(T edge, U source, V target, bool single = false)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<CosmosResponse>> UpsertGraphVertex<T>(IEnumerable<T> vertices, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10)
        {
            throw new NotImplementedException();
        }

        public Task<CosmosResponse> InsertGraphEdge<T>(T edge, GraphItemBase source, GraphItemBase target)
        {
            throw new NotImplementedException();
        }

        public Task<CosmosResponse> UpsertGraphEdge<T>(T edge, GraphItemBase source, GraphItemBase target, bool single = false)
        {
            throw new NotImplementedException();
        }

        public Task<CosmosResponse> ExecuteGremlingSingle(string queryString)
        {
            throw new NotImplementedException();
        }

        public Task<CosmosResponse<T>> ExecuteGremlingSingle<T>(string queryString)
        {
            throw new NotImplementedException();
        }

        public Task<CosmosResponse<IEnumerable<T>>> ExecuteGremlingMulti<T>(string queryString)
        {
            throw new NotImplementedException();
        }


        //if (!forceCreate)
        //{
        //    var database = client.CreateDatabaseQuery()
        //      .AsEnumerable()
        //      .FirstOrDefault(x => x.Id == dbName);

        //    if (database == null)
        //        throw new Exception($"Unable to find a database named {dbName}");

        //    var collection = client.CreateDocumentCollectionQuery(database.SelfLink)
        //        .AsEnumerable()
        //        .FirstOrDefault(x => x.Id == collectionName);

        //    if (collection == null)
        //        throw new Exception($"Unable to find collection named {collectionName}");

        //    return new CosmosClient { _client = client, _collection = collection, _partitionKeyPropertyName = partitionKeyPropertyName, _databaseName = dbName };
        //}
        //else
        //{
        //    var database = new Database { Id = dbName };
        //    var collection = new DocumentCollection { Id = collectionName };
        //    collection.PartitionKey.Paths.Add($"/{partitionKeyPropertyName}");

        //    database = await client.CreateDatabaseIfNotExistsAsync(database);
        //    collection = await client.CreateDocumentCollectionIfNotExistsAsync(
        //        UriFactory.CreateDatabaseUri(dbName),
        //        collection,
        //        new RequestOptions
        //        {
        //            OfferThroughput = initialRU,
        //            ConsistencyLevel = ConsistencyLevel.Session
        //        });

        //    return new CosmosClient { _client = client, _collection = collection, _partitionKeyPropertyName = partitionKeyPropertyName, _databaseName = dbName };
        //}
    }
}