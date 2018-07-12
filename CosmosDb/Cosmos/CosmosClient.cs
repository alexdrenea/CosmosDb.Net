using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.Graphs;
using CosmosDb.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Collections.Concurrent;
using System.Threading;

namespace CosmosDb
{
    public class CosmosClient : ICosmosClient
    {
        private DocumentClient _client;
        private DocumentCollection _collection;
        private string _databaseName;
        private string _partitionKeyPropertyName;
        //TODO: Log queries and performance

        private CosmosClient() { }

        #region DocumentDB API

        public async Task<CosmosResponse> InsertDocument<T>(T document)
        {
            try
            {
                ResourceResponse<Document> response = await _client.CreateDocumentAsync(_collection.AltLink, document);

                return new CosmosResponse { Result = response.Resource, RU = response.RequestCharge };
            }
            catch (Exception e)
            {
                return new CosmosResponse { Error = e };
            }
        }
        public Task<IEnumerable<CosmosResponse>> InsertDocuments<T>(IEnumerable<T> documents, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10)
        {
            return ProcessMultipleDocuments(documents, InsertDocument, reportingCallback, threads, reportingIntervalS);
        }

        public async Task<CosmosResponse> UpsertDocument<T>(T document)
        {
            try
            {
                ResourceResponse<Document> response = await _client.UpsertDocumentAsync(_collection.AltLink, document);

                return new CosmosResponse { Result = response.Resource, RU = response.RequestCharge };
            }
            catch (Exception e)
            {
                return new CosmosResponse { Error = e };
            }
        }
        public Task<IEnumerable<CosmosResponse>> UpsertDocuments<T>(IEnumerable<T> documents, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10)
        {
            return ProcessMultipleDocuments(documents, UpsertDocument, reportingCallback, threads, reportingIntervalS);
        }

        public async Task<CosmosResponse<T>> ReadDocument<T>(string docId, string partitionKey)
        {
            try
            {
                var documentLink = $"{_collection.AltLink}/docs/{docId}";
                var response = await _client.ReadDocumentAsync<T>(documentLink, new RequestOptions { PartitionKey = new PartitionKey(partitionKey) });

                return new CosmosResponse<T> { Result = response.Document, RU = response.RequestCharge };
            }
            catch (Exception e)
            {
                return new CosmosResponse<T> { Error = e };
            }
        }


        public Task<CosmosResponse> InsertGraphVertex<T>(T vertex)
        {
            return InsertDocument(vertex.ToGraphVertex());
        }

        public Task<CosmosResponse> UpsertGraphVertex<T>(T vertex)
        {
            return UpsertDocument(vertex.ToGraphVertex());
        }

        public async Task<CosmosResponse<T>> ReadGraphVertex<T>(string docId, string partitionKey)
        {
            var doc = await ReadDocument<JObject>(docId, partitionKey);
            if (!doc.IsSuccessful)
                return new CosmosResponse<T> { Error = doc.Error };

            return new CosmosResponse<T> { Result = GraphsonHelpers.FromDocument<T>(doc.Result), RU = doc.RU };
        }

        public Task<CosmosResponse> InsertGraphEdge<T, U, V>(T edge, U source, V target)
        {
            return InsertDocument(edge.ToGraphEdge(source, target));
        }

        public Task<CosmosResponse> UpsertGraphEdge<T, U, V>(T edge, U source, V target)
        {
            return UpsertDocument(edge.ToGraphEdge(source, target));
        }

        #endregion

        #region SQL API

        public async Task<CosmosResponse<IEnumerable<T>>> ExecuteSQL<T>(string query, bool expectGraphson = true)
        {
            try
            {
                //not sure why can't do CreateDocumentQuery<JObject> directly -> throws errors when trying to serialize to T
                var queryRes = _client.CreateDocumentQuery(_collection.AltLink, query, new FeedOptions { EnableCrossPartitionQuery = true });
                var queryResArray = queryRes.ToArray();

                var res = expectGraphson ? queryResArray.Select(q => GraphsonHelpers.FromDocument<T>(JsonConvert.DeserializeObject<JObject>(q.ToString()))) : queryResArray.Select(q => JsonConvert.DeserializeObject<T>(q.ToString()));

                return new CosmosResponse<IEnumerable<T>>() { Result = res.Cast<T>().ToArray() };
            }
            catch (Exception e)
            {
                return new CosmosResponse<IEnumerable<T>> { Error = e };
            }
        }

        #endregion

        #region GRAPH API

        public async Task<CosmosResponse> ExecuteGremlingSingle(string queryString)
        {
            try
            {
                var query = _client.CreateGremlinQuery<dynamic>(_collection, queryString);
                var feedResponse = await query.ExecuteNextAsync();
                var result = feedResponse.FirstOrDefault();
                return new CosmosResponse { Result = result, RU = feedResponse.RequestCharge };
            }
            catch (Exception e)
            {
                return new CosmosResponse { Error = e };
            }
        }

        public async Task<CosmosResponse<T>> ExecuteGremlingSingle<T>(string queryString)
        {
            try
            {
                var query = _client.CreateGremlinQuery<dynamic>(_collection, queryString);
                var feedResponse = await query.ExecuteNextAsync();
                var result = feedResponse.Select(r => (T)ConvertResultTo<T>(r)).FirstOrDefault();
                return new CosmosResponse<T> { Result = result, RU = feedResponse.RequestCharge };
            }
            catch (Exception e)
            {
                return new CosmosResponse<T> { Error = e };
            }
        }

        public async Task<CosmosResponse<IEnumerable<T>>> ExecuteGremlingMulti<T>(string queryString)
        {
            var query = _client.CreateGremlinQuery<dynamic>(_collection, queryString);
            var data = new List<T>();
            var RUs = 0.0;

            while (query.HasMoreResults)
            {
                var feedResponse = await query.ExecuteNextAsync();
                data.AddRange(feedResponse.Select(dr => (T)ConvertResultTo<T>(dr)));
                RUs += feedResponse.RequestCharge;
            }

            return new CosmosResponse<IEnumerable<T>> { Result = data, RU = RUs };
        }

        #endregion

        #region StoredProcs and UDFs

        //https://docs.microsoft.com/en-us/azure/cosmos-db/documentdb-sql-query

        public async Task<CosmosResponse> EnsureStoredProcExists(string id, string body)
        {
            throw new NotImplementedException();
        }

        public async Task<CosmosResponse> DeleteStoredProc(string id)
        {
            throw new NotImplementedException();

        }

        public async Task<CosmosResponse> ExecuteStoredProd(string id)
        {
            throw new NotImplementedException();

        }

        public async Task<CosmosResponse> CreateUserDefinedFunctionIfNotExists(string id, string body)
        {
            try
            {
                var udfFeedUri = $"{UriFactory.CreateDocumentCollectionUri(_databaseName, _collection.Id)}/udfs";
                var udfs = await _client.ReadUserDefinedFunctionFeedAsync(udfFeedUri);
                var existingUDF = udfs.FirstOrDefault(a => a.Id == id);
                if (existingUDF != null)
                {
                    return new CosmosResponse() { Result = new string[] { existingUDF.Id, existingUDF.Body }, RU = udfs.RequestCharge };
                }
                else
                {
                    var udf = new UserDefinedFunction
                    {
                        Id = id,
                        Body = body,
                    };
                    var createdUdf = await _client.CreateUserDefinedFunctionAsync(_collection.AltLink, udf);
                    return new CosmosResponse() { Result = new string[] { createdUdf.Resource.Id, createdUdf.Resource.Body }, RU = createdUdf.RequestCharge };
                }
            }
            catch (Exception ex)
            {
                return new CosmosResponse() { Error = ex };
            }

        }

        public async Task<CosmosResponse> DeleteUserDefinedFunction(string id)
        {
            try
            {
                var funcLink = UriFactory.CreateUserDefinedFunctionUri(_databaseName, _collection.Id, id);
                var deletedUdf = await _client.DeleteUserDefinedFunctionAsync(funcLink);
                return new CosmosResponse() { Result = true, RU = deletedUdf.RequestCharge };
            }
            catch (Exception ex)
            {
                return new CosmosResponse() { Error = ex };
            }
        }

        #endregion

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
            return cb.ToArray();
        }

        private T ConvertResultTo<T>(object result)
        {
            if (typeof(T) == typeof(Object))
            {
                return (T)result;
            }
            if (typeof(T).IsPrimitive)
            {
                return (T)Convert.ChangeType(result, typeof(T));
            }
            else
            {
                return (T)GraphsonHelpers.FromGraphson<T>((JObject)JsonConvert.DeserializeObject(result.ToString()));
            }
        }

        public static async Task<ICosmosClient> GetCosmosClient(string connectionString, string key, string dbName, string collectionName, bool forceCreate = true, int initialRU = 400, string partitionKeyPropertyName = "PartitionKey")
        {
            var client = new DocumentClient(new Uri(connectionString), key,
               new ConnectionPolicy
               {
                   ConnectionMode = ConnectionMode.Direct,
                   ConnectionProtocol = Protocol.Tcp,
                   RequestTimeout = new TimeSpan(1, 0, 0),
                   MaxConnectionLimit = 50,
                   RetryOptions = new RetryOptions
                   {
                       MaxRetryAttemptsOnThrottledRequests = 10,
                       MaxRetryWaitTimeInSeconds = 30
                   }
               });

            if (!forceCreate)
            {
                var database = client.CreateDatabaseQuery()
                  .AsEnumerable()
                  .FirstOrDefault(x => x.Id == dbName);

                if (database == null)
                    throw new Exception($"Unable to find a database named {dbName}");

                var collection = client.CreateDocumentCollectionQuery(database.SelfLink)
                    .AsEnumerable()
                    .FirstOrDefault(x => x.Id == collectionName);

                if (collection == null)
                    throw new Exception($"Unable to find collection named {collectionName}");

                return new CosmosClient { _client = client, _collection = collection, _partitionKeyPropertyName = partitionKeyPropertyName, _databaseName = dbName };
            }
            else
            {
                var database = new Database { Id = dbName };
                var collection = new DocumentCollection { Id = collectionName };
                collection.PartitionKey.Paths.Add($"/{partitionKeyPropertyName}");

                database = await client.CreateDatabaseIfNotExistsAsync(database);
                collection = await client.CreateDocumentCollectionIfNotExistsAsync(
                    UriFactory.CreateDatabaseUri(dbName),
                    collection,
                    new RequestOptions
                    {
                        OfferThroughput = initialRU,
                        ConsistencyLevel = ConsistencyLevel.Session
                    });

                return new CosmosClient { _client = client, _collection = collection, _partitionKeyPropertyName = partitionKeyPropertyName, _databaseName = dbName };
            }
        }

    }
}
