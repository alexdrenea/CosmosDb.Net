using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CosmosDb.Domain;
using CosmosDb.Domain.Helpers;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Exceptions;
using Gremlin.Net.Structure.IO.GraphSON;
using Microsoft.Azure.Cosmos;
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
        private const string RESULTSET_ATTRIBUTE_ACTIVITYID = "x-ms-activity-id";

        public GremlinServer GremlinServer { get; private set; }
        
        //Made this private so users aren't tempted to use methods from whithin the SqlClient directly.
        private CosmosSqlClient CosmosSqlClient { get; set; }


        public CosmosClient Client => CosmosSqlClient?.Client;
        public Database Database => CosmosSqlClient?.Database;
        public Container Container => CosmosSqlClient?.Container;


        #region Initialization

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


        #endregion


        #region Gremlin.NET calls  - pure graph

        public async Task<CosmosResponse<T>> ExecuteGremlinSingle<T>(string queryString)
        {
            //The ExecuteSingle is just a wrapper against the same SubitAsync Graph call that returns the first result in the set.
            var res = await ExecuteGremlin<T>(queryString);

            var cosmosRes = res.Clone<T>();
            cosmosRes.Result = res.Result != null ? res.Result.FirstOrDefault() : default(T);

            return cosmosRes;
        }

        public async Task<CosmosResponse<IEnumerable<T>>> ExecuteGremlin<T>(string queryString)
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
            catch (ResponseException e)
            {
                return new CosmosResponse<IEnumerable<T>>
                {
                    Result = null,
                    Error = e,
                    RequestCharge = GetValueOrDefault<double>(e.StatusAttributes, RESULTSET_ATTRIBUTE_RU),
                    RetryAfter = TimeSpan.FromMilliseconds(GetValueOrDefault<int>(e.StatusAttributes, RESULTSET_ATTRIBUTE_RETRYAFTER)),
                    ActivityId = GetValueOrDefault<string>(e.StatusAttributes, RESULTSET_ATTRIBUTE_ACTIVITYID),
                };
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

        //CosmosDB SQL dependent calls

        #endregion

        #region CosmosSQL calls - calls into a CosmosDB Graph using SQL API

        public Task<CosmosResponse> InsertVertex<T>(T entity)
        {
            EnsureCosmosSqlClient();
            return CosmosSqlClient.InsertDocumentInternal(entity.ToGraphVertex<T>());
        }

        public Task<CosmosResponse> UpsertVertex<T>(T entity)
        {
            EnsureCosmosSqlClient();
            return CosmosSqlClient.UpsertDocumentInternal(entity.ToGraphVertex<T>());
        }


        public Task<CosmosResponse> InsertEdge<T, U, V>(T edge, U source, V target, bool single = false)
        {
            EnsureCosmosSqlClient();
            return CosmosSqlClient.InsertDocumentInternal(SerializationHelpers.ToGraphEdge(edge, source, target, single));
        }

        public Task<CosmosResponse> UpsertEdge<T, U, V>(T edge, U source, V target, bool single = false)
        {
            EnsureCosmosSqlClient();
            return CosmosSqlClient.UpsertDocumentInternal(SerializationHelpers.ToGraphEdge(edge, source, target, single));
        }

        public Task<CosmosResponse> InsertEdge<T>(T edge, GraphItemBase source, GraphItemBase target, bool single = false)
        {
            EnsureCosmosSqlClient();
            return CosmosSqlClient.InsertDocumentInternal(SerializationHelpers.ToGraphEdge(edge, source, target, single));
        }

        public Task<CosmosResponse> UpsertEdge<T>(T edge, GraphItemBase source, GraphItemBase target, bool single = false)
        {
            EnsureCosmosSqlClient();
            return CosmosSqlClient.UpsertDocumentInternal(SerializationHelpers.ToGraphEdge(edge, source, target, single));
        }


        /// <summary>
        /// Read a graph vertex using the SQL API
        /// Forward the request to the SQL Client with a JObject type and then convert the resulting graphson document into our entity using the serialization helper.
        /// </summary>
        /// <returns></returns>
        public async Task<CosmosResponse<T>> ReadVertex<T>(string docId, string partitionKey)
        {
            EnsureCosmosSqlClient();
            var res = await CosmosSqlClient.ReadDocument<JObject>(docId, partitionKey);
            var cosmosResult = res.Clone<T>();

            if (!res.IsSuccessful)
                return cosmosResult;

            cosmosResult.Result = SerializationHelpers.FromGraphson<T>(res.Result);
            return cosmosResult;
        }

        /// <summary>
        /// Execute a SQL statement against the graph database.
        /// Forward the request to the SQL Client with a JObject type and then convert the resulting graphson documents into our entity using the serialization helper.
        /// </summary>
        /// <param name="query">Query to execute</param>
        /// <param name="pagedResults">true to return only one page of the result set, false(Default) to return all results. </param>
        /// <param name="continuationToken">token to pass into the query iterator to resume from a specific page. Should be present when using pageResults = true</param>
        /// <param name="cancellationToken">cancellatinToken used to cancel an operation in progress.</param>
        /// <returns>CosmosResult with collection of results</returns>
        public async Task<CosmosResponse<IEnumerable<T>>> ExecuteSQL<T>(string query, bool pagedResults = false, string continuationToken = "", CancellationToken cancellationToken = default(CancellationToken))
        {
            EnsureCosmosSqlClient();
            var res = await CosmosSqlClient.ExecuteSQL<JObject>(query, pagedResults, continuationToken, cancellationToken);

            var cosmosResult = res.Clone<IEnumerable<T>>();
            if (typeof(T) == typeof(JObject))
                return cosmosResult;

            if (!res.IsSuccessful)
                return cosmosResult;

            cosmosResult.Result = res.Result.Select(SerializationHelpers.FromGraphson<T>).ToArray();

            return cosmosResult;
        }


        #endregion

        #region Helpers

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

        private void EnsureCosmosSqlClient()
        {
            if (CosmosSqlClient == null)
                throw new InvalidOperationException("You must initialize the CosmosGraphClient with a CosmosSQLClient to be able to call this method");
        }

        #endregion
    }
}
