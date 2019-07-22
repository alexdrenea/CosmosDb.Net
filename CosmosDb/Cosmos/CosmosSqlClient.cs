using CosmosDb.Domain.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Collections.Concurrent;
using System.Threading;
using CosmosDb.Domain;
using Microsoft.Azure.Cosmos;
using System.IO;

namespace CosmosDb
{
    public enum ApiKind
    {
        Sql,
        Gremlin
    }

    public class CosmosSqlClient : ICosmosSqlClient
    {
        private const string SqlAccountEndpointFormat = "https://{0}.documents.azure.com:443/";

        public CosmosClient Client { get; private set; }
        public Database Database { get; private set; }
        public Container Container { get; private set; }

        protected ApiKind ApiKind { get; set; } = ApiKind.Sql;

        protected string _partitionKeyPropertyName;


        #region Initialization

        public static Task<CosmosSqlClient> GetCosmosDbClient(string connectionString, string databaseId, string containerId, int initialContainerRUs = 400, string partitionKeyPath = "/PartitionKey", bool forceCreate = true)
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

        public static Task<CosmosSqlClient> GetCosmosDbClient(string connectionString, string databaseId, ContainerProperties settings, int initialContainerRUs = 400, bool forceCreate = true)
        {
            var cco = new CosmosClientOptions()
            {
                ConnectionMode = ConnectionMode.Direct,
                RequestTimeout = new TimeSpan(1, 0, 0),
            };
            return GetCosmosDbClientInternal(new CosmosClient(connectionString, cco), databaseId, settings, initialContainerRUs, forceCreate);
        }


        public static Task<CosmosSqlClient> GetCosmosDbClient(string accountEndpoint, string databaseId, string containerId, string key, int initialContainerRUs = 400, string partitionKeyPath = "/PartitionKey", bool forceCreate = true)
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

            return GetCosmosDbClientInternal(new CosmosClient(accountEndpoint, key, cco), databaseId, ccs, initialContainerRUs, forceCreate);
        }

        public static Task<CosmosSqlClient> GetCosmosDbClient(string accountEndpoint, string databaseId, ContainerProperties containerSettings, string key, int initialContainerRUs = 400, bool forceCreate = true)
        {
            var cco = new CosmosClientOptions()
            {
                ConnectionMode = ConnectionMode.Direct,
                RequestTimeout = new TimeSpan(1, 0, 0),
            };
            return GetCosmosDbClientInternal(new CosmosClient(accountEndpoint, key, cco), databaseId, containerSettings, initialContainerRUs, forceCreate);
        }


        internal static async Task<CosmosSqlClient> GetCosmosDbClientForGremlin(string accountName, string databaseId, string containerId, string key, int initialContainerRUs = 400, string partitionKeyPath = "/PartitionKey", bool forceCreate = true)
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

            var accountEndpoint = string.Format(CosmosSqlClient.SqlAccountEndpointFormat, accountName);
            var res = await GetCosmosDbClientInternal(new CosmosClient(accountEndpoint, key, cco), databaseId, ccs, initialContainerRUs, forceCreate);
            res.ApiKind = ApiKind.Gremlin;

            return res;
        }

        private static async Task<CosmosSqlClient> GetCosmosDbClientInternal(CosmosClient client, string databaseId, ContainerProperties containerSettings, int initialContainerRUs, bool forceCreate)
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


            var res = new CosmosSqlClient
            {
                Client = client,
                Database = database,
                Container = container
            };

            var r = await container.ReadContainerAsync();
            res._partitionKeyPropertyName = r.Resource.PartitionKeyPath.Trim('/');

            return res;
        }

        #endregion

        public Task<CosmosResponse> InsertDocument<T>(T document)
        {
            return AddDocInternal(document, (internalDoc) => Container.CreateItemAsync(internalDoc, new PartitionKey(internalDoc[_partitionKeyPropertyName].ToString())));

            //    try
            //    {
            //        var start = DateTime.Now;

            //        var internalDoc = ConvertEntityToCosmos(document);
            //        var res = await Container.CreateItemAsync(internalDoc, new PartitionKey(internalDoc[_partitionKeyPropertyName].ToString()));

            //        return res.ToCosmosResponse(DateTime.Now.Subtract(start));
            //    }
            //    catch (CosmosException cex)
            //    {
            //        return cex.ToCosmosResponse();
            //    }
            //    catch (Exception e)
            //    {
            //        return new CosmosResponse { Error = e, StatusCode = System.Net.HttpStatusCode.InternalServerError };
            //    }
        }

        public Task<IEnumerable<CosmosResponse>> InsertDocuments<T>(IEnumerable<T> documents, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10)
        {
            return null;
            //return ProcessMultipleDocuments(documents, InsertDocument, reportingCallback, threads, reportingIntervalS);
        }

        public Task<CosmosResponse> UpsertDocument<T>(T document)
        {
            return AddDocInternal(document, (internalDoc) => Container.UpsertItemAsync(internalDoc, new PartitionKey(internalDoc[_partitionKeyPropertyName].ToString())));

            //try
            //{
            //    var start = DateTime.Now;

            //    var internalDoc = ConvertEntityToCosmos(document);
            //    var res = await Container.UpsertItemAsync(internalDoc, new PartitionKey(internalDoc[_partitionKeyPropertyName].ToString()));


            //    return res.ToCosmosResponse(DateTime.Now.Subtract(start));
            //}
            //catch (CosmosException cex)
            //{
            //    return cex.ToCosmosResponse();
            //}
            //catch (Exception e)
            //{
            //    return new CosmosResponse { Error = e, StatusCode = System.Net.HttpStatusCode.InternalServerError };
            //}
        }

        public Task<IEnumerable<CosmosResponse>> UpsertDocuments<T>(IEnumerable<T> documents, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10)
        {
            return null;
            //return ProcessMultipleDocuments(documents, UpsertDocument, reportingCallback, threads, reportingIntervalS);
        }



        private async Task<CosmosResponse> AddDocInternal<T>(T document, Func<IDictionary<string, object>, Task<ItemResponse<IDictionary<string,object>>>> addFunc)
        {
            try
            {
                var start = DateTime.Now;

                var internalDoc = ConvertEntityToCosmos(document);
                var res = await addFunc.Invoke(internalDoc);

                return res.ToCosmosResponse(DateTime.Now.Subtract(start));
            }
            catch (CosmosException cex)
            {
                return cex.ToCosmosResponse();
            }
            catch (Exception e)
            {
                return new CosmosResponse { Error = e, StatusCode = System.Net.HttpStatusCode.InternalServerError };
            }
        }


        public async Task<CosmosResponse<T>> ReadDocument<T>(string docId, string partitionKey)
        {
            DateTime start = DateTime.Now;
            switch (ApiKind)
            {
                case ApiKind.Sql:
                    var res = await Container.ReadItemAsync<T>(docId, new PartitionKey(partitionKey));
                    return null;
                case ApiKind.Gremlin:
                    var graphsonResult = await Container.ReadItemAsync<JObject>(docId, new PartitionKey(partitionKey));
                    //var doc = SerializationHelpers.FromGraphson<T>(graphsonResult);
                    return null;
                default:
                    throw new InvalidOperationException("This SDK only supports SQL or Gremlin Cosmos databases.");
            }
        }

        //TODO = add cancellation token
        public async Task<CosmosResponse<IEnumerable<T>>> ExecuteSQL<T>(string query)
        {
            var res = new List<T>();
            var tempRes = new List<string>();
            var iterator = Container.GetItemQueryIterator<string>(new QueryDefinition(query));
            while (iterator.HasMoreResults)
            {
                var readNext = await iterator.ReadNextAsync();

                tempRes.AddRange(readNext.Resource);
            }

            return null;
        }



        public Task<CosmosResponse> InsertGraphEdge<T, U, V>(T edge, U source, V target)
        {
            throw new NotImplementedException();
        }


        public Task<CosmosResponse> UpsertGraphEdge<T, U, V>(T edge, U source, V target, bool single = false)
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



        protected IDictionary<string, object> ConvertEntityToCosmos<T>(T entity)
        {
            switch (ApiKind)
            {
                case ApiKind.Sql:
                    return entity.ToCosmosDocument<T>();
                case ApiKind.Gremlin:
                    return entity.ToGraphVertex<T>();
                default:
                    throw new NotSupportedException("This library only supports Sql And Gremlin Api Types.");
            }

        }

        protected T ConvertCosmosResultToEntity<T>(JObject response)
        {
            //switch (ApiKind)
            //{
            //    case ApiKind.Sql:
            //        return JsonConver.ToCosmosDocument<T>();
            //    case ApiKind.Gremlin:
            //        return entity.ToGraphVertex<T>();
            //    default:
            //        throw new NotSupportedException("This library only supports Sql And Gremlin Api Types.");
            //}
            return default(T);    
        }

        private async Task<IEnumerable<CosmosResponse>> ProcessMultipleDocuments<T>(IEnumerable<T> documents, Func<T, Task<CosmosResponse>> execute, Action<IEnumerable<CosmosResponse>> reportingCallback, int threads = 4, int reportingIntervalS = 10)
        {
            ConcurrentBag<CosmosResponse> cb = new ConcurrentBag<CosmosResponse>();
            Timer statsTimer = null;
            if (reportingCallback != null && reportingIntervalS != -1)
            {
                statsTimer = new Timer(_ =>
                {
                    reportingCallback?.Invoke(cb.ToArray());
                }, null, reportingIntervalS * 1000, reportingIntervalS * 1000);
            }

            var actionBlock = new ActionBlock<T>(async (i) =>
            {
                var res = await execute(i);
                cb.Add(res);
            }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = threads });

            foreach (var i in documents)
            {
                actionBlock.Post(i);
            }
            actionBlock.Complete();
            await actionBlock.Completion;

            statsTimer?.Dispose();
            //TODO: call reporting callback to say we're done.
            return cb.ToArray();
        }



    }
}