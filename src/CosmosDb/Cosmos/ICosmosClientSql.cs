using CosmosDb.Domain;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CosmosDb
{
    public interface ICosmosClientSql
    {
        CosmosClient Client { get; }
        Database Database { get; }
        Container Container { get; }
        CosmosEntitySerializer CosmosSerializer { get; }

        /// <summary>
        /// Insert a document into the database.
        /// </summary>
        /// <param name="document">Entity to insert</param>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters</returns>
        Task<CosmosResponse> InsertDocument<T>(T document);

        /// <summary>
        /// Insert multiple documents into the database using a TPL Dataflow block.
        /// </summary>
        /// <param name="documents">Documents to insert</param>
        /// <param name="reportingCallback">[Optional] A method to be called every <paramref name="reportingIntervalS"/> seconds with an array of responses for all processed. Generally used to provide a progress update to callers. Defaults to null./></param>
        /// <param name="reportingIntervalS">[Optional] interval in seconds to to call the reporting callback. Defaults to 10s</param>
        /// <param name="threads">[Optional] Number of threads to use for the paralel execution. Defaults to 4</param>
        /// <example>
        /// <![CDATA[
        /// await _client.InsertDocuments(elements, (partial) => { Console.WriteLine($"inserted {partial.Count()} documents");
        /// ]]>
        /// </example>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters.</returns>
        Task<IEnumerable<CosmosResponse>> InsertDocuments<T>(IEnumerable<T> documents, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10);

        /// <summary>
        /// Upsert (Insert or Create) a document into the database.
        /// </summary>
        /// <param name="document">Entity to update</param>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters</returns>
        Task<CosmosResponse> UpsertDocument<T>(T document);

        /// <summary>
        /// Upsert (Insert or Update) multiple documents into the database using a TPL Dataflow block.
        /// </summary>
        /// <param name="documents">Documents to upsert</param>
        /// <param name="reportingCallback">[Optional] A method to be called every <paramref name="reportingIntervalS"/> seconds with an array of responses for all processed. Generally used to provide a progress update to callers. Defaults to null./></param>
        /// <param name="reportingIntervalS">[Optional] interval in seconds to to call the reporting callback. Defaults to 10s</param>
        /// <param name="threads">[Optional] Number of threads to use for the paralel execution. Defaults to 4</param>
        /// <example>
        /// <![CDATA[
        /// await _client.InsertDocuments(elements, (partial) => { Console.WriteLine($"upserted {partial.Count()} documents");
        /// ]]>
        /// </example>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters.</returns>
        Task<IEnumerable<CosmosResponse>> UpsertDocuments<T>(IEnumerable<T> documents, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10);

        /// <summary>
        /// Read a document by its Id and Partition Key. 
        /// This is the fastest operation possible in a CosmosDB collection.
        /// </summary>
        Task<CosmosResponse<T>> ReadDocument<T>(string docId, string partitionKey);

        /// <summary>
        /// Gets all documents of the given type from the collection.
        /// </summary>
        /// <param name="filter">Optional filter argument (i.e "budget &gt; 100000 and revenue &lt; 3000000".</param>
        /// <param name="label">Type of document to retrieve. If empty, attempt to get value from the Attribute name or class name.</param>
        /// <param name="cancellationToken">cancellatinToken used to cancel an operation in progress.</param>
        /// <returns>Collection of results.</returns>
        Task<CosmosResponse<IEnumerable<T>>> ReadDocuments<T>(string filter = "", string label = "", CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Executes a SQL Query against the collection
        /// </summary>
        /// <param name="query">Query to execute</param>
        /// <param name="pagedResults">true to return only one page of the result set, false(Default) to return all results. </param>
        /// <param name="continuationToken">token to pass into the query iterator to resume from a specific page. Should be present when using <paramref name="pagedResults"/> = true</param>
        /// <param name="cancellationToken">cancellatinToken used to cancel an operation in progress.</param>
        /// <returns>Collection of results.</returns>
        Task<CosmosResponse<IEnumerable<T>>> ExecuteSQL<T>(string query, bool pagedResults = false, string continuationToken = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}
