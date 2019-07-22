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
using System.Linq;

namespace CosmosDb
{
    public class CosmosSqlClient : ICosmosSqlClient
    {
        private const string SqlAccountEndpointFormat = "https://{0}.documents.azure.com:443/";

        public CosmosClient Client { get; private set; }
        public Database Database { get; private set; }
        public Container Container { get; private set; }

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
            return InsertDocumentInternal(document.ToCosmosDocument());
        }

        public Task<IEnumerable<CosmosResponse>> InsertDocuments<T>(IEnumerable<T> documents, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10)
        {
            return null;
            //return ProcessMultipleDocuments(documents, InsertDocument, reportingCallback, threads, reportingIntervalS);
        }

        public Task<CosmosResponse> UpsertDocument<T>(T document)
        {
            return UpsertDocumentInternal(document.ToCosmosDocument());
        }

        public Task<IEnumerable<CosmosResponse>> UpsertDocuments<T>(IEnumerable<T> documents, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10)
        {
            return null;
            //return ProcessMultipleDocuments(documents, UpsertDocument, reportingCallback, threads, reportingIntervalS);
        }


        internal Task<CosmosResponse> InsertDocumentInternal(IDictionary<string, object> document)
        {
            if (!document.ContainsKey(_partitionKeyPropertyName))
                throw new InvalidOperationException($"Document does not have a partition key property. Expecting '{_partitionKeyPropertyName}'");
            return AddDocumentInternal(() => Container.CreateItemAsync(document, new PartitionKey(document[_partitionKeyPropertyName].ToString())));
        }

        internal Task<CosmosResponse> UpsertDocumentInternal(IDictionary<string, object> document)
        {
            if (!document.ContainsKey(_partitionKeyPropertyName))
                throw new InvalidOperationException($"Document does not have a partition key property. Expecting '{_partitionKeyPropertyName}'");
            return AddDocumentInternal(() => Container.UpsertItemAsync(document, new PartitionKey(document[_partitionKeyPropertyName].ToString())));
        }

        internal async Task<CosmosResponse> AddDocumentInternal(Func<Task<ItemResponse<IDictionary<string, object>>>> addFunc)
        {
            try
            {
                var start = DateTime.Now;

                var res = await addFunc.Invoke();

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

        /// <summary>
        /// Read a document from graph by its Id and Partition Key. 
        /// This is the fastest operation possible in a CosmosDB collection.
        /// </summary>
        public async Task<CosmosResponse<T>> ReadDocument<T>(string docId, string partitionKey)
        {
            try
            {
                var start = DateTime.Now;

                var res = await Container.ReadItemAsync<T>(docId, new PartitionKey(partitionKey));

                var cr = res.ToCosmosResponse<T, T>(DateTime.Now.Subtract(start));
                cr.Result = res.Resource;
                return cr;
            }
            catch (CosmosException cex)
            {
                return cex.ToCosmosResponse<T>();
            }
            catch (Exception e)
            {
                return new CosmosResponse<T> { Error = e, StatusCode = System.Net.HttpStatusCode.InternalServerError };
            }
        }

        /// <summary>
        /// Executes a SQL Query against the collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query">Query to execute</param>
        /// <param name="pagedResults">true to return only one page of the result set, false(Default) to return all results. </param>
        /// <param name="continuationToken">token to pass into the query iterator to resume from a specific page. Should be present when using pageResults = true</param>
        /// <param name="cancellationToken">cancellatinToken used to cancel an operation in progress.</param>
        /// <returns>Collection of results.</returns>
        public async Task<CosmosResponse<IEnumerable<T>>> ExecuteSQL<T>(string query, bool pagedResults = false, string continuationToken = "", CancellationToken cancellationToken = default(CancellationToken))
        {
            DateTime start = DateTime.Now;
            CosmosResponse<IEnumerable<T>> cosmosResult = new CosmosResponse<IEnumerable<T>>();
            var dataResult = new List<T>();
            try
            {
                var iterator = Container.GetItemQueryIterator<T>(query, continuationToken);
                while (iterator.HasMoreResults)
                {
                    var readNext = await iterator.ReadNextAsync(cancellationToken);

                    dataResult.AddRange(readNext.Resource);

                    cosmosResult.RequestCharge += readNext.RequestCharge;
                    cosmosResult.ContinuationToken = readNext.ContinuationToken;
                    cosmosResult.ActivityId = readNext.ActivityId;
                    cosmosResult.ETag = readNext.ETag;

                    if (pagedResults) //Just read the next page of the result set if we are paging results
                        break;
                }
                cosmosResult.Result = dataResult;
                cosmosResult.ExecutionTime = DateTime.Now.Subtract(start);
                cosmosResult.StatusCode = System.Net.HttpStatusCode.OK;
                return cosmosResult;
            }
            catch (CosmosException cex)
            {
                return cex.ToCosmosResponse<IEnumerable<T>>();
            }
            catch (Exception e)
            {
                return new CosmosResponse<IEnumerable<T>> { Error = e, StatusCode = System.Net.HttpStatusCode.InternalServerError };
            }
        }


        #region Helpers

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

        #endregion
    }
}