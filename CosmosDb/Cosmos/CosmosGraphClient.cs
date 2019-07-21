using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CosmosDb.Domain.Helpers;
using Gremlin.Net.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CosmosDb
{
    public class CosmosGraphClient : ICosmosGraphClient
    {
        private const string GraphEndpointFormat = "{0}.gremlin.cosmosdb.azure.com";

        public GremlinServer GremlinServer { get; private set; }
        public ICosmosSqlClient CosmosSqlClient { get; private set; }


        public async Task<CosmosResponse> ExecuteGremlingSingle(string queryString)
        {
            using (var gremlinClient = new GremlinClient(GremlinServer))
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
                using (var gremlinClient = new GremlinClient(GremlinServer))
                {
                    var start = DateTime.Now;

                    var graphResult = await gremlinClient.SubmitWithSingleResultAsync<object>(queryString);
                    var graphResultString = JsonConvert.SerializeObject(graphResult);
                    var graphResultJObject = JsonConvert.DeserializeObject<JObject>(graphResultString);

                    var res = SerializationHelpers.FromGraphson<T>(graphResultJObject);
                    var duration = (int)DateTime.Now.Subtract(start).TotalMilliseconds;

                    return new CosmosResponse<T> { Result = res, RU = -1, ExecutionTimeMs = duration };
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
                using (var gremlinClient = new GremlinClient(GremlinServer))
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


        public static async Task<ICosmosGraphClient> GetCosmosGraphClientWithSql(string accountName, string databaseId, string containerId, string key, int initialContainerRUs = 400, string partitionKeyPath = "/PartitionKey", bool forceCreate = true)
        {
            var sqlClient = await CosmosDb.CosmosSqlClient.GetCosmosDbClientForGremlin(accountName, databaseId, containerId, key, initialContainerRUs, partitionKeyPath, forceCreate);

            var gremlinEndpoint = string.Format(CosmosGraphClient.GraphEndpointFormat, accountName);
            var server = new GremlinServer(gremlinEndpoint, 443, username: "/dbs/" + databaseId + "/colls/" + containerId, enableSsl: true, password: key);

            return new CosmosGraphClient
            {
                GremlinServer = server,
                CosmosSqlClient = sqlClient
            };
        }

        public static ICosmosGraphClient GetCosmosGraphClient(string accountName, string databaseId, string containerId, string key)
        {
            var gremlinEndpoint = string.Format(CosmosGraphClient.GraphEndpointFormat, accountName);
            var server = new GremlinServer(gremlinEndpoint, 443, username: "/dbs/" + databaseId + "/colls/" + containerId, enableSsl: true, password: key);
            return new CosmosGraphClient
            {
                GremlinServer = server
            };
        }

    }
}
