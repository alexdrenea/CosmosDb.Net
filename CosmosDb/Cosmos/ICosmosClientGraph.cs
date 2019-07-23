using CosmosDb.Domain;
using Gremlin.Net.Driver;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CosmosDb
{
    public interface ICosmosClientGraph
    {
        GremlinServer GremlinServer { get; }
        CosmosClient Client { get; }
        Database Database { get; }
        Container Container { get; }

        /// <summary>
        /// Gets a new intance of a GremlinClient so callers can execute query directly without going though the wrapper methods.
        /// </summary>
        GremlinClient GetGremlinClient();

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
        Task<CosmosResponse<T>> ExecuteGremlinSingle<T>(string queryString, Dictionary<string, object> bindings = null);

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
        Task<CosmosResponse<IEnumerable<T>>> ExecuteGremlin<T>(string queryString, Dictionary<string, object> bindings = null);

        /// <summary>
        /// Insert a vertex into the database.
        /// This call uses the SQL API to insert the vertex as a document.
        /// </summary>
        /// <param name="entity">Entity to insert</param>
        /// <exception cref="InvalidOperationException">Throws invalid operation exception if the GraphClient was initialized without a CosmosSQLClient</exception>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters</returns>
        Task<CosmosResponse> InsertVertex<T>(T entity);

        /// <summary>
        /// Insert multiple vertices into the database using a TPL Dataflow block.
        /// This call uses the SQL API to insert the vertices as documents.
        /// </summary>
        /// <param name="entities">Entites to insert</param>
        /// <param name="reportingCallback">[Optional] A method to be called every <paramref name="reportingIntervalS"/> seconds with an array of responses for all processed. Generally used to provide a progress update to callers. Defaults to null./></param>
        /// <param name="reportingIntervalS">[Optional] interval in seconds to to call the reporting callback. Defaults to 10s</param>
        /// <param name="threads">[Optional] Number of threads to use for the paralel execution. Defaults to 4</param>
        /// <exception cref="InvalidOperationException">Throws invalid operation exception if the GraphClient was initialized without a <see cref="CosmosClientSql"/>.</exception>
        /// <example>
        /// <![CDATA[
        /// await _client.InsertVertex(elements, (partial) => { Console.WriteLine($"inserted {partial.Count()} vertices");
        /// ]]>
        /// </example>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters.</returns>
        Task<IEnumerable<CosmosResponse>> InsertVertex<T>(IEnumerable<T> entities, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10);

        /// <summary>
        /// Upsert (Create or Update) a vertex into the database.
        /// This call uses the SQL API to upsert the vertex as a document.
        /// </summary>
        /// <param name="entity">Entity to upsert</param>
        /// <exception cref="InvalidOperationException">Throws invalid operation exception if the GraphClient was initialized without a CosmosSQLClient</exception>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters</returns>
        Task<CosmosResponse> UpsertVertex<T>(T entity);

        /// <summary>
        /// Upsert (Insert or Update) multiple vertices into the database using a TPL Dataflow block.
        /// This call uses the SQL API to upsert the vertices as documents.
        /// </summary>
        /// <param name="entities">Entites to upsert</param>
        /// <param name="reportingCallback">[Optional] A method to be called every <paramref name="reportingIntervalS"/> seconds with an array of responses for all processed. Generally used to provide a progress update to callers. Defaults to null./></param>
        /// <param name="reportingIntervalS">[Optional] interval in seconds to to call the reporting callback. Defaults to 10s</param>
        /// <param name="threads">[Optional] Number of threads to use for the paralel execution. Defaults to 4</param>
        /// <exception cref="InvalidOperationException">Throws invalid operation exception if the GraphClient was initialized without a <see cref="CosmosClientSql"/>.</exception>
        /// <example>
        /// <![CDATA[
        /// await _client.UpsertVertex(elements, (partial) => { Console.WriteLine($"upserted {partial.Count()} vertices");
        /// ]]>
        /// </example>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters.</returns>
        Task<IEnumerable<CosmosResponse>> UpsertVertex<T>(IEnumerable<T> entities, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10);

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
        Task<CosmosResponse> InsertEdge<T, U, V>(T edge, U source, V target, bool single = false);

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
        Task<CosmosResponse> UpsertEdge<T, U, V>(T edge, U source, V target, bool single = false);

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
        Task<CosmosResponse> InsertEdge<T>(T edge, GraphItemBase source, GraphItemBase target, bool single = false);

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
        Task<CosmosResponse> UpsertEdge<T>(T edge, GraphItemBase source, GraphItemBase target, bool single = false);



        /// <summary>
        /// Read a graph vertex using the SQL API. 
        /// Forward the request to the SQL Client with a JObject type and then convert the resulting graphson document into our entity using the serialization helper.
        /// </summary>
        /// <returns><see cref="CosmosResponse"/> that encapsulates the result of the query and tracks success status along with various performance parameters.</returns>
        Task<CosmosResponse<T>> ReadVertex<T>(string docId, string partitionKey);

        /// <summary>
        /// Execute a SQL statement against the graph database.
        /// Forward the request to the SQL Client with a JObject type and then convert the resulting graphson documents into our entity using the serialization helper.
        /// </summary>
        /// <param name="query">Query to execute</param>
        /// <param name="pagedResults">true to return only one page of the result set, false(Default) to return all results. </param>
        /// <param name="continuationToken">token to pass into the query iterator to resume from a specific page. Should be present when using pageResults = true</param>
        /// <param name="cancellationToken">cancellatinToken used to cancel an operation in progress.</param>
        /// <returns><see cref="CosmosResponse"/> that encapsulates the result of the query and tracks success status along with various performance parameters.</returns>
        Task<CosmosResponse<IEnumerable<T>>> ExecuteSQL<T>(string query, bool pagedResults = false, string continuationToken = "", CancellationToken cancellationToken = default(CancellationToken));
    }
}
