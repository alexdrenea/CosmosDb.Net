using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gremlin.Net.Driver;

namespace CosmosDb
{
    public class CosmosGraphClient : ICosmosGraphClient
    {
        private GremlinServer _server;

        public async Task<CosmosResponse> ExecuteGremlingSingle(string queryString)
        {
            using (var gremlinClient = new GremlinClient(_server))
            {
                var start = DateTime.Now;
                var result = await gremlinClient.SubmitWithSingleResultAsync<object>(queryString);
                var duration = (int)DateTime.Now.Subtract(start).TotalMilliseconds;
                return new CosmosResponse { Result = result, RU = -1, ExecutionTimeMs = duration };
            }
        }
        public async Task<CosmosResponse<T>> ExecuteGremlingSingle<T>(string queryString)
        {
            try
            {
                using (var gremlinClient = new GremlinClient(_server))
                {
                    var start = DateTime.Now;
                    var result = await gremlinClient.SubmitWithSingleResultAsync<T>(queryString);
                    var duration = (int)DateTime.Now.Subtract(start).TotalMilliseconds;
                    return new CosmosResponse<T> { Result = result, RU = -1, ExecutionTimeMs = duration };
                }
            }
            catch (Exception e)
            {
                return new CosmosResponse<T> { Result = default(T), Error = e, RU = -1 };
            }
        }
        public async Task<CosmosResponse<IEnumerable<T>>> ExecuteGremlingMulti<T>(string queryString)
        {
            try
            {
                using (var gremlinClient = new GremlinClient(_server))
                {
                    var start = DateTime.Now;
                    var result = await gremlinClient.SubmitAsync<T>(queryString);
                    var duration = (int)DateTime.Now.Subtract(start).TotalMilliseconds;
                    return new CosmosResponse<IEnumerable<T>> { Result = result, RU = -1, ExecutionTimeMs = duration };
                }
            }
            catch (Exception e)
            {
                return new CosmosResponse<IEnumerable<T>> { Result = null, Error = e, RU = -1 };
            }
        }

        public static async Task<ICosmosGraphClient> GetCosmosGraphClient(string connectionString, string key, string dbName, string collectionName, bool forceCreate = true, int initialRU = 400, string partitionKeyPropertyName = "PartitionKey")
        {
            var server = new GremlinServer(connectionString, 443, username: "/dbs/" + dbName + "/colls/" + collectionName, enableSsl: true, password: key);
            return new CosmosGraphClient { _server = server };

            //using (var gremlinClient = new GremlinClient(_server))
            //{
            //    var result =
            //        await gremlinClient.SubmitWithSingleResultAsync<bool>("g.V().has('name', 'gremlin').hasNext()");
            //}
            //var client = new DocumentClient(new Uri(connectionString), key);

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
}
