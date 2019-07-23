using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CosmosDb.Domain;
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Exceptions;
using Gremlin.Net.Structure.IO.GraphSON;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CosmosDb
{
    public class CosmosClientGraph : ICosmosClientGraph
    {
        private const string GraphEndpointFormat = "{0}.gremlin.cosmosdb.azure.com";
        private const string RESULTSET_ATTRIBUTE_RU = "x-ms-total-request-charge";
        private const string RESULTSET_ATTRIBUTE_STATUSCODE = "x-ms-status-code";
        private const string RESULTSET_ATTRIBUTE_RETRYAFTER = "x-ms-retry-after-ms";
        private const string RESULTSET_ATTRIBUTE_ACTIVITYID = "x-ms-activity-id";

        //Made this private so users aren't tempted to use methods from whithin the SqlClient directly.
        private CosmosClientSql CosmosSqlClient { get; set; }

        private CosmosEntitySerializer CosmosSerializer;

        public GremlinServer GremlinServer { get; private set; }

        public CosmosClient Client => CosmosSqlClient?.Client;
        public Database Database => CosmosSqlClient?.Database;
        public Container Container => CosmosSqlClient?.Container;


        #region Initialization

        /// <summary>
        /// Initialize a SQL backed Cosmos Graph Client.
        /// If <paramref name="forceCreate"/> is false and <paramref name="databaseId"/> or <paramref name="containerId"/> are not found, method will throw exception.
        /// </summary>
        /// <param name="accountName">Name of the Cosmos account to connect to. (i.e [yourAccount] from -> https://yourAccount.documents.azure.com:443/)</param>
        /// <param name="key">Account Key from the Key blade in the portal</param>
        /// <param name="forceCreate">Indicates if should create a new database and container if they are not present.</param>
        /// <param name="initialContainerRUs">[Optional] Throughput to request at the container level. Parameter only valid if <paramref name="forceCreate"/> is true and a container is being created.</param>
        /// <param name="partitionKeyPath">[Optional] PartitionKey descriptor. Must start with a / and a property with this name must exsit in every document that will be inserted in this collection. Parameter only valid if <paramref name="forceCreate"/> is true and a container is being created.</param>
        /// <exception cref="Exception">If <paramref name="forceCreate"/> is false and <paramref name="databaseId"/> or <paramref name="containerId"/> are not found, method will throw exception.</exception>
        /// <returns>Reference to a Graph CosmosClient</returns>
        public static async Task<ICosmosClientGraph> GetClientWithSql(string accountName, string key, string databaseId, string containerId, int initialContainerRUs = 400, string partitionKeyPath = "/PartitionKey", bool forceCreate = true)
        {
            var sqlClient = (CosmosClientSql)(await CosmosDb.CosmosClientSql.GetByAccountName(accountName, key, databaseId, containerId, initialContainerRUs, partitionKeyPath, forceCreate));

            var gremlinEndpoint = string.Format(CosmosClientGraph.GraphEndpointFormat, accountName);
            var server = new GremlinServer(gremlinEndpoint, 443, username: "/dbs/" + databaseId + "/colls/" + containerId, enableSsl: true, password: key);

            return new CosmosClientGraph
            {
                GremlinServer = server,
                CosmosSqlClient = sqlClient,
                CosmosSerializer = sqlClient.CosmosSerializer,
            };
        }

        /// <summary>
        /// Initialize a Cosmos Graph Client that is purely backed by Gremlin.NET and has no Cosmos component.
        /// Some operatios will not be allowed on a client obtained from this helper.
        /// </summary>
        /// <param name="accountName">Name of the Cosmos account to connect to. (i.e [yourAccount] from -> https://yourAccount.documents.azure.com:443/)</param>
        /// <param name="key">Account Key from the Key blade in the portal</param>
        /// <param name="partitionKeyPath">ParitionKey path for the container. Current implemetation only supports simple paths. Used in the deserialization process.</param>
        /// <returns>Reference to a Graph CosmosClient</returns>
        public static ICosmosClientGraph GetClient(string accountName, string key, string databaseId, string containerId, string partitionKeyPath = "/PartitionKey")
        {
            var gremlinEndpoint = string.Format(CosmosClientGraph.GraphEndpointFormat, accountName);
            var server = new GremlinServer(gremlinEndpoint, 443, username: "/dbs/" + databaseId + "/colls/" + containerId, enableSsl: true, password: key);
            return new CosmosClientGraph
            {
                GremlinServer = server,
                CosmosSerializer = new CosmosEntitySerializer(partitionKeyPath.Trim('/'))
            };
        }


        #endregion


        #region Gremlin.NET calls  - pure graph

        /// <summary>
        /// Executes a Gremlin traversal and returns a singe result back.
        /// If the traversal returns more than 1 result, this method will return the first result only.
        /// When the type of the result is unknown (using steps like tree() or path(), send T as JObject and manually deserialize.
        /// </summary>
        /// <typeparam name="T">Type to convert the results to</typeparam>
        /// <param name="queryString">Gremlin traversal</param>
        /// <param name="bindings">[Optional] Collection of parameters and their values to be sent to the gremlin server along with the query.</param>
        /// <example>
        /// <![CDATA[
        /// await cosmosGraphClient.ExecuteGremlin<Movie>("g.V().hasLabel('Movie').has('Language', 'en').has('Budget', gt(1000000))");
        /// await cosmosGraphClient.ExecuteGremlin<Movie>("g.V().hasLabel('Movie').has('Language',lang).has('Budget',gt(budget))", new Dictionary<string, object> { { "lang", "en" }, { "budget", 1000000 } });
        /// ]]>
        /// </example>
        /// <returns>CosmosResponse wrapped Array of results.</returns>
        public async Task<CosmosResponse<T>> ExecuteGremlinSingle<T>(string queryString, Dictionary<string, object> bindings = null)
        {
            //The ExecuteSingle is just a wrapper against the same SubitAsync Graph call that returns the first result in the set.
            var res = await ExecuteGremlin<T>(queryString, bindings);

            var cosmosRes = res.Clone<T>();
            cosmosRes.Result = res.Result != null ? res.Result.FirstOrDefault() : default(T);

            return cosmosRes;
        }

        /// <summary>
        /// Executes a Gremlin traversal. 
        /// When the type of the result is unknown (using steps like tree() or path(), send T as JObject and manually deserialize
        /// </summary>
        /// <typeparam name="T">Type to convert the results to</typeparam>
        /// <param name="queryString">Gremlin traversal</param>
        /// <param name="bindings">[Optional] Collection of parameters and their values to be sent to the gremlin server along with the query.</param>
        /// <example>
        /// <![CDATA[
        /// await cosmosGraphClient.ExecuteGremlin<Movie>("g.V().hasLabel('Movie').has('Language', 'en').has('Budget', gt(1000000))");
        /// await cosmosGraphClient.ExecuteGremlin<Movie>("g.V().hasLabel('Movie').has('Language',lang).has('Budget',gt(budget))", new Dictionary<string, object> { { "lang", "en" }, { "budget", 1000000 } });
        /// ]]>
        /// </example>
        /// <returns>CosmosResponse wrapped Array of results.</returns>
        public async Task<CosmosResponse<IEnumerable<T>>> ExecuteGremlin<T>(string queryString, Dictionary<string, object> bindings = null)
        {

            try
            {
                using (var gremlinClient = GetGremlinClient())
                {
                    var start = DateTime.Now;
                    var graphResult = await gremlinClient.SubmitAsync<object>(queryString, bindings);
                    var graphResultString = JsonConvert.SerializeObject(graphResult);
                    var graphResultJObject = JsonConvert.DeserializeObject<IEnumerable<JObject>>(graphResultString);

                    var res = graphResultJObject.Select(CosmosSerializer.FromGraphson<T>).ToArray();

                    return new CosmosResponse<IEnumerable<T>>
                    {
                        Result = res,
                        StatusCode = System.Net.HttpStatusCode.OK,
                        RequestCharge = Helpers.GetValueOrDefault<double>(graphResult.StatusAttributes, RESULTSET_ATTRIBUTE_RU),
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
                    RequestCharge = Helpers.GetValueOrDefault<double>(e.StatusAttributes, RESULTSET_ATTRIBUTE_RU),
                    RetryAfter = TimeSpan.FromMilliseconds(Helpers.GetValueOrDefault<int>(e.StatusAttributes, RESULTSET_ATTRIBUTE_RETRYAFTER)),
                    ActivityId = Helpers.GetValueOrDefault<string>(e.StatusAttributes, RESULTSET_ATTRIBUTE_ACTIVITYID),
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


        //TODO: Fluent API -> when CosmosDB gets bytecode support
        //public GraphTraversalSource GetGraphTraversalSource()
        //{
        //    using (var gremlinClient = GetGremlinClient())
        //    {
        //        var start = DateTime.Now;
        //        var g = AnonymousTraversalSource.Traversal().WithRemote(new DriverRemoteConnection(gremlinClient));
        //        return g;
        //    }
        //}

        #endregion

        #region CosmosSQL calls - calls into a CosmosDB Graph using SQL API

        /// <summary>
        /// Insert a vertex into the database.
        /// This call uses the SQL API to insert the vertex as a document.
        /// </summary>
        /// <param name="entity">Entity to insert</param>
        /// <exception cref="InvalidOperationException">Throws invalid operation exception if the GraphClient was initialized without a CosmosSQLClient</exception>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters</returns>
        public Task<CosmosResponse> InsertVertex<T>(T entity)
        {
            EnsureCosmosSqlClient();
            return CosmosSqlClient.InsertDocumentInternal(CosmosSerializer.ToGraphVertex<T>(entity));
        }

        /// <summary>
        /// Insert multiple vertices into the database using a TPL Dataflow block.
        /// This call uses the SQL API to insert the vertices as documents.
        /// </summary>
        /// <param name="entities">Entites to insert</param>
        /// <param name="reportingCallback">[Optional] A method to be called every <paramref name="reportingIntervalS"/> seconds with an array of responses for all processed. Generally used to provide a progress update to callers. Defaults to null./></param>
        /// <param name="reportingIntervalS">[Optional] interval in seconds to to call the reporting callback. Defaults to 10s</param>
        /// <param name="threads">[Optional] Number of threads to use for the paralel execution. Defaults to 4</param>
        /// <exception cref="InvalidOperationException">Throws invalid operation exception if the GraphClient was initialized without a <see cref="CosmosSqlClient"/>.</exception>
        /// <example>
        /// <![CDATA[
        /// await _client.InsertVertex(elements, (partial) => { Console.WriteLine($"inserted {partial.Count()} vertices");
        /// ]]>
        /// </example>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters.</returns>
        public Task<IEnumerable<CosmosResponse>> InsertVertex<T>(IEnumerable<T> entities, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10)
        {
            EnsureCosmosSqlClient();
            //Could've used InsertVertex instead of the lambda, but I don't want to the EnsureCosmosClient() for every call
            return CosmosSqlClient.ProcessMultipleDocuments(entities, (T entity) => { return CosmosSqlClient.InsertDocumentInternal(CosmosSerializer.ToGraphVertex<T>(entity)); }, reportingCallback, threads, reportingIntervalS);
        }

        /// <summary>
        /// Upsert (Insert or Update) a vertex into the database.
        /// This call uses the SQL API to upsert the vertex as a document.
        /// </summary>
        /// <param name="entity">Entity to upsert</param>
        /// <exception cref="InvalidOperationException">Throws invalid operation exception if the GraphClient was initialized without a CosmosSQLClient</exception>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters</returns>
        public Task<CosmosResponse> UpsertVertex<T>(T entity)
        {
            EnsureCosmosSqlClient();
            return CosmosSqlClient.UpsertDocumentInternal(CosmosSerializer.ToGraphVertex<T>(entity));
        }

        /// <summary>
        /// Upsert (Insert or Update) multiple vertices into the database using a TPL Dataflow block.
        /// This call uses the SQL API to upsert the vertex]ices as a document.
        /// </summary>
        /// <param name="entities">Entites to upsert</param>
        /// <param name="reportingCallback">[Optional] A method to be called every <paramref name="reportingIntervalS"/> seconds with an array of responses for all processed. Generally used to provide a progress update to callers. Defaults to null./></param>
        /// <param name="reportingIntervalS">[Optional] interval in seconds to to call the reporting callback. Defaults to 10s</param>
        /// <param name="threads">[Optional] Number of threads to use for the paralel execution. Defaults to 4</param>
        /// <exception cref="InvalidOperationException">Throws invalid operation exception if the GraphClient was initialized without a <see cref="CosmosSqlClient"/>.</exception>
        /// <example>
        /// <![CDATA[
        /// await _client.UpsertVertex(elements, (partial) => { Console.WriteLine($"upserted {partial.Count()} vertices");
        /// ]]>
        /// </example>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters.</returns>
        public Task<IEnumerable<CosmosResponse>> UpsertVertex<T>(IEnumerable<T> entities, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10)
        {
            EnsureCosmosSqlClient();
            //Could've used UpserVertex instead of the lambda, but I don't want to the EnsureCosmosClient() for every call
            return CosmosSqlClient.ProcessMultipleDocuments(entities, (T entity) => { return CosmosSqlClient.UpsertDocumentInternal(CosmosSerializer.ToGraphVertex<T>(entity)); }, reportingCallback, threads, reportingIntervalS);
        }


        /// <summary>
        /// Insert an edge into the database by providing the Edge domain model and references to its source and target as domain models
        /// This call uses the SQL API to insert the edge as a document.
        /// </summary>
        /// <param name="edge">Edge entity to insert</param>
        /// <param name="source">Source vertex of the edge</param>
        /// <param name="target">Target vertex of the edge</param>
        /// <param name="single">
        /// [Optional] Indicates if there can only be one edge of this kind between the 2 vertices. Defaults to false.
        /// i.e an edge defining a 'isFriend' relationship between 2 people needs to be singe:true because only one friend edge makes sense.
        /// i.e an edge defining a 'visited' relationship between a person and a restaurant needs to be single:false because a person can visit the restaurant multiple times
        /// </param>
        /// <exception cref="InvalidOperationException">Throws invalid operation exception if the GraphClient was initialized without a CosmosSQLClient</exception>
        /// <remarks>Inserting the same edge twice with single:false will succeed and generate a new edge instance, while with single:true it will fail.</remarks>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters</returns>
        public Task<CosmosResponse> InsertEdge<T, U, V>(T edge, U source, V target, bool single = false)
        {
            EnsureCosmosSqlClient();
            return CosmosSqlClient.InsertDocumentInternal(CosmosSerializer.ToGraphEdge(edge, source, target, single));
        }

        /// <summary>
        /// Upsert an edge into the database by providing the Edge domain model and references to its source and target as domain models
        /// This call uses the SQL API to upsert the edge as a document.
        /// </summary>
        /// <param name="edge">Edge entity to upsert</param>
        /// <param name="source">Source vertex of the edge</param>
        /// <param name="target">Target vertex of the edge</param>
        /// <param name="single">
        /// [Optional] Indicates if there can only be one edge of this kind between the 2 vertices. Defaults to false.
        /// i.e an edge defining a 'isFriend' relationship between 2 people needs to be singe:true because only one friend edge makes sense.
        /// i.e an edge defining a 'visited' relationship between a person and a restaurant needs to be single:false because a person can visit the restaurant multiple times
        /// </param>
        /// <exception cref="InvalidOperationException">Throws invalid operation exception if the GraphClient was initialized without a CosmosSQLClient</exception>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters</returns>
        public Task<CosmosResponse> UpsertEdge<T, U, V>(T edge, U source, V target, bool single = false)
        {
            EnsureCosmosSqlClient();
            return CosmosSqlClient.UpsertDocumentInternal(CosmosSerializer.ToGraphEdge(edge, source, target, single));
        }

        /// <summary>
        /// Insert an edge into the database by referencing the source and target vertices just by their base properties (id, partitionKey, label).
        /// This call uses the SQL API to insert the edge as a document.
        /// </summary>
        /// <param name="edge">Edge entity to insert</param>
        /// <param name="source">Source vertex of the edge</param>
        /// <param name="target">Target vertex of the edge</param>
        /// <param name="single">
        /// [Optional] Indicates if there can only be one edge of this kind between the 2 vertices. Defaults to false.
        /// i.e an edge defining a 'isFriend' relationship between 2 people needs to be singe:true because only one friend edge makes sense.
        /// i.e an edge defining a 'visited' relationship between a person and a restaurant needs to be single:false because a person can visit the restaurant multiple times
        /// </param>
        /// <exception cref="InvalidOperationException">Throws invalid operation exception if the GraphClient was initialized without a CosmosSQLClient</exception>
        /// <remarks>Inserting the same edge twice with single:false will succeed and generate a new edge instance, while with single:true it will fail.</remarks>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters</returns>
        public Task<CosmosResponse> InsertEdge<T>(T edge, GraphItemBase source, GraphItemBase target, bool single = false)
        {
            EnsureCosmosSqlClient();
            return CosmosSqlClient.InsertDocumentInternal(CosmosSerializer.ToGraphEdge(edge, source, target, single));
        }

        /// <summary>
        /// Upsert an edge into the database by referencing the source and target vertices just by their base properties (id, partitionKey, label).
        /// This call uses the SQL API to upsert the edge as a document.
        /// </summary>
        /// <param name="edge">Edge entity to upsert</param>
        /// <param name="source">Source vertex of the edge</param>
        /// <param name="target">Target vertex of the edge</param>
        /// <param name="single">
        /// [Optional] Indicates if there can only be one edge of this kind between the 2 vertices. Defaults to false.
        /// i.e an edge defining a 'isFriend' relationship between 2 people needs to be singe:true because only one friend edge makes sense.
        /// i.e an edge defining a 'visited' relationship between a person and a restaurant needs to be single:false because a person can visit the restaurant multiple times
        /// </param>
        /// <exception cref="InvalidOperationException">Throws invalid operation exception if the GraphClient was initialized without a CosmosSQLClient</exception>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters</returns>
        public Task<CosmosResponse> UpsertEdge<T>(T edge, GraphItemBase source, GraphItemBase target, bool single = false)
        {
            EnsureCosmosSqlClient();
            return CosmosSqlClient.UpsertDocumentInternal(CosmosSerializer.ToGraphEdge(edge, source, target, single));
        }

        /// <summary>
        /// Read a graph vertex using the SQL API. 
        /// Forward the request to the SQL Client with a JObject type and then convert the resulting graphson document into our entity using the serialization helper.
        /// </summary>
        /// <returns><see cref="CosmosResponse"/> that encapsulates the result of the query and tracks success status along with various performance parameters.</returns>
        public async Task<CosmosResponse<T>> ReadVertex<T>(string docId, string partitionKey)
        {
            EnsureCosmosSqlClient();
            var res = await CosmosSqlClient.ReadDocument<JObject>(docId, partitionKey);
            var cosmosResult = res.Clone<T>();

            if (!res.IsSuccessful)
                return cosmosResult;

            cosmosResult.Result = CosmosSerializer.FromGraphson<T>(res.Result);
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
        /// <returns><see cref="CosmosResponse"/> that encapsulates the result of the query and tracks success status along with various performance parameters.</returns>
        public async Task<CosmosResponse<IEnumerable<T>>> ExecuteSQL<T>(string query, bool pagedResults = false, string continuationToken = "", CancellationToken cancellationToken = default(CancellationToken))
        {
            EnsureCosmosSqlClient();
            var res = await CosmosSqlClient.ExecuteSQL<JObject>(query, pagedResults, continuationToken, cancellationToken);

            var cosmosResult = res.Clone<IEnumerable<T>>();
            if (typeof(T) == typeof(JObject))
                return cosmosResult;

            if (!res.IsSuccessful)
                return cosmosResult;

            cosmosResult.Result = res.Result.Select(CosmosSerializer.FromGraphson<T>).ToArray();

            return cosmosResult;
        }


        #endregion

        #region Helpers

        private GremlinClient GetGremlinClient()
        {
            return new GremlinClient(GremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType);
        }

        private void EnsureCosmosSqlClient()
        {
            if (CosmosSqlClient == null)
                throw new InvalidOperationException("You must initialize the CosmosGraphClient with a CosmosSQLClient to be able to call this method");
        }

        #endregion
    }
}
