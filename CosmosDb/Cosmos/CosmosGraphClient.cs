using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CosmosDb.Domain;
using CosmosDb.Domain.Helpers;
using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CosmosDb
{
    public class CosmosGraphClient : ICosmosGraphClient
    {
        private const string GraphEndpointFormat = "{0}.gremlin.cosmosdb.azure.com";
        private const string RESULTSET_ATTRIBUTE_RU = "x-ms-total-request-charge";
        private const string RESULTSET_ATTRIBUTE_STATUSCODE = "x-ms-status-code";
        private const string RESULTSET_ATTRIBUTE_RETRYAFTER = "x-ms-retry-after-ms";


        public GremlinServer GremlinServer { get; private set; }
        public ICosmosSqlClient CosmosSqlClient { get; private set; }


        public async Task<CosmosResponse> ExecuteGremlingSingle(string queryString)
        {
            using (var gremlinClient = GetGremlinClient())
            {
                var start = DateTime.Now;
                var result = await gremlinClient.SubmitWithSingleResultAsync<object>(queryString);
                return new CosmosResponse<object> { Result = result, StatusCode = System.Net.HttpStatusCode.OK, RequestCharge = -1, ExecutionTime = DateTime.Now.Subtract(start) };
            }
        }

        public async Task<CosmosResponse<T>> ExecuteGremlingSingle<T>(string queryString)
        {
            try
            {
                using (var gremlinClient = GetGremlinClient())
                {
                    var start = DateTime.Now;
                    var graphResult = await gremlinClient.SubmitAsync<object>(queryString);
                    var graphResultString = JsonConvert.SerializeObject(graphResult);
                    var graphResultJObject = JsonConvert.DeserializeObject<IEnumerable<JObject>>(graphResultString);

                    var res = graphResultJObject.Select(SerializationHelpers.FromGraphson<T>).ToArray();

                    return new CosmosResponse<T> { Result = res.First(), StatusCode = System.Net.HttpStatusCode.OK, RequestCharge = GetValueOrDefault<double>(graphResult.StatusAttributes, RESULTSET_ATTRIBUTE_RU), ExecutionTime = DateTime.Now.Subtract(start) };
                }
            }
            catch (Exception e)
            {
                return new CosmosResponse<T>
                {
                    Result = default(T),
                    Error = e,
                    RequestCharge = -1
                };
            }
        }

        public async Task<CosmosResponse<IEnumerable<T>>> ExecuteGremlingMulti<T>(string queryString)
        {
            try
            {
                using (var gremlinClient = GetGremlinClient())
                {
                    var start = DateTime.Now;
                    var graphResult = await gremlinClient.SubmitAsync<object>(queryString);
                    var graphResultString = JsonConvert.SerializeObject(graphResult);
                    var graphResultJObject = JsonConvert.DeserializeObject<IEnumerable<JObject>>(graphResultString);

                    var res = graphResultJObject.Select(SerializationHelpers.FromGraphson<T>).ToArray();

                    return new CosmosResponse<IEnumerable<T>>
                    {
                        Result = res,
                        StatusCode = System.Net.HttpStatusCode.OK,
                        RequestCharge = GetValueOrDefault<double>(graphResult.StatusAttributes, RESULTSET_ATTRIBUTE_RU),
                        ExecutionTime = DateTime.Now.Subtract(start)
                    };
                }
            }
            catch (Exception e)
            {
                return new CosmosResponse<IEnumerable<T>>
                {
                    Result = null,
                    Error = e,
                    RequestCharge = -1
                };
            }
        }


        //TODO -> support for bindings (parametrized queries)
        //public Task<CosmosResponse> InsertVertex<T>(T vertex)
        //{

        //}

        //public Task<CosmosResponse> UpsertVertex<T>(T vertex)
        //{

        //}


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



        private GremlinClient GetGremlinClient()
        {
            return new GremlinClient(GremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType);
        }

        private static T GetValueOrDefault<T>(IReadOnlyDictionary<string, object> dictionary, string key)
        {
            if (dictionary.ContainsKey(key))
            {
                try
                {
                    return (T)Convert.ChangeType(dictionary[key], typeof(T));
                }
                catch
                {
                    return default(T);
                }
            }

            return default(T);
        }
    }
}
