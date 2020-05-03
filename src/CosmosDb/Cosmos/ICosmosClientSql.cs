using CosmosDB.Net.Domain;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CosmosDB.Net
{
    public interface ICosmosClientSql
    {
        CosmosClient Client { get; }
        Database Database { get; }
        Container Container { get; }
        CosmosEntitySerializer CosmosSerializer { get; }


        /// <summary>
        /// Insert a document into the database. 
        /// The 3 main properties needed to properly insert a document (id, partitionkey and label) are going to be inferred as follows:
        /// If the type is decorated with attributes (<see cref="CosmosDB.Net.Domain.Attributes.PartitionKeyAttribute"/>,  <see cref="CosmosDB.Net.Domain.Attributes.IdAttribute"/> or <see cref="CosmosDB.Net.Domain.Attributes.LabelAttribute"/>)
        /// then the values of those properties will be used.
        /// If attributes are not detected, the values from the properties specified as parameters of this method will be tried next.
        /// If property expressions are not provided, the values of default properties will be tried next (id or whatever the PartitionKey property is set on the target collection). A label property will not be attempted.
        /// If there are still no values at this point the following defaults will be used:
        ///  id -> new guid
        ///  label -> name fo type
        ///  partitionkey -> throw an exception.
        /// </summary>
        /// <param name="document">Entity to insert</param>
        /// <param name="pkProperty">Mandatory. Accessor func for the method to extraact partitionKey for your object.</param>
        /// <param name="idProperty">Mandatory. Accessor func for the method to extract id for your object. Mandatory if your type T does not contain a property called id.</param>
        /// <param name="labelProperty">Optional. Accessor func for the method to extract label for your object.</param>
        /// <remarks>The method will fail if a value cannot be found for partition key</remarks>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters</returns>
        Task<CosmosResponse> InsertDocument<T>(T document, Expression<Func<T, object>> pkProperty = null, Expression<Func<T, object>> idProperty = null, Expression<Func<T, object>> labelProperty = null);

        /// <summary>
        /// Insert a document stream into the database.
        /// </summary>
        /// <param name="stream">Stream to insert</param>
        /// <param name="partitionKey">PartitionKey to send</param>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters</returns>
        Task<CosmosResponse> InsertDocument(Stream stream, PartitionKey partitionKey);

        /// <summary>
        /// Insert multiple documents into the database using a TPL Dataflow block.
        /// This method will attempt to automatically collect partitionKey / id information based on the <see cref="CosmosDB.Net.Domain.Attributes.PartitionKeyAttribute"/> and <see cref="CosmosDB.Net.Domain.Attributes.IdAttribute"/>
        /// </summary>
        /// <param name="documents">Documents to insert</param>
        /// <param name="reportingCallback">[Optional] Method to be called based on the <paramref name="reportingInterval"/>. Generally used to provide a progress update to callers. Defaults to null./></param>
        /// <param name="reportingInterval">[Optional] interval in seconds to to call the reporting callback. Defaults to 10s</param>
        /// <param name="threads">[Optional] Number of threads to use for the paralel execution. Defaults to 4</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
        /// <example>
        /// <![CDATA[
        /// await _client.InsertDocuments(elements, (partial) => { Console.WriteLine($"inserted {partial.Count()} documents");
        /// ]]>
        /// </example>
        /// <remarks>The method will fail if the type T is not annotated with the partitionKey and id</remarks>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters.</returns>
        Task<IEnumerable<CosmosResponse>> InsertDocuments<T>(IEnumerable<T> documents, Action<IEnumerable<CosmosResponse>> reportingCallback = null, TimeSpan? reportingInterval = null, int threads = 4, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Insert multiple documents into the database using a TPL Dataflow block.
        /// The documents that are passed in are going to be added to the database using the Stream APIs so they will not be seriazlied / deseriazlied in the SDK
        /// Each document passed in must contain a property that matches the partitionKey property assigned on the collection
        /// </summary>
        /// <param name="documents">Documents to insert</param>
        /// <param name="reportingCallback">[Optional] Method to be called based on the <paramref name="reportingInterval"/>. Generally used to provide a progress update to callers. Defaults to null./></param>
        /// <param name="reportingInterval">[Optional] interval in seconds to to call the reporting callback. Defaults to 10s</param>
        /// <param name="threads">[Optional] Number of threads to use for the paralel execution. Defaults to 4</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
        /// <example>
        /// <![CDATA[
        /// await _client.InsertDocuments(elements, (partial) => { Console.WriteLine($"inserted {partial.Count()} documents");
        /// ]]>
        /// </example>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters.</returns>
        Task<IEnumerable<CosmosResponse>> InsertDocuments(IEnumerable<(Stream stream, PartitionKey pk)> documents, Action<IEnumerable<CosmosResponse>> reportingCallback = null, TimeSpan? reportingInterval = null, int threads = 4, CancellationToken cancellationToken = default(CancellationToken));


        /// <summary>
        /// Upsert (Insert or Create) a document into the database.
        /// The 3 main properties needed to properly insert a document (id, partitionkey and label) are going to be inferred as follows:
        /// If the type is decorated with attributes (<see cref="CosmosDB.Net.Domain.Attributes.PartitionKeyAttribute"/>,  <see cref="CosmosDB.Net.Domain.Attributes.IdAttribute"/> or <see cref="CosmosDB.Net.Domain.Attributes.LabelAttribute"/>)
        /// then the values of those properties will be used.
        /// If attributes are not detected, the values from the properties specified as parameters of this method will be tried next.
        /// If property expressions are not provided, the values of default properties will be tried next (id or whatever the PartitionKey property is set on the target collection). A label property will not be attempted.
        /// If there are still no values at this point the following defaults will be used:
        ///  id -> new guid
        ///  label -> name fo type
        ///  partitionkey -> throw an exception.
        /// </summary>
        /// <param name="document">Entity to upsert</param>
        /// <param name="pkProperty">Mandatory. Accessor func for the method to extraact partitionKey for your object.</param>
        /// <param name="idProperty">Mandatory. Accessor func for the method to extract id for your object. Mandatory if your type T does not contain a property called id.</param>
        /// <param name="labelProperty">Optional. Accessor func for the method to extract label for your object.</param>
        /// <remarks>The method will fail if a value cannot be found for partition key</remarks>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters</returns>
        Task<CosmosResponse> UpsertDocument<T>(T document, Expression<Func<T, object>> pkProperty = null, Expression<Func<T, object>> idProperty = null, Expression<Func<T, object>> labelProperty = null);


        /// <summary>
        /// Upsert (Insert or Create) a document stream into the database.
        /// </summary>
        /// <param name="stream">Stream to insert</param>
        /// <param name="partitionKey">PartitionKey to send</param>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters</returns>
        Task<CosmosResponse> UpsertDocument(Stream stream, PartitionKey partitionKey);

        /// <summary>
        /// Upsert (Insert or Update) multiple documents into the database using a TPL Dataflow block.
        /// This method will attempt to automatically collect partitionKey / id information based on the <see cref="CosmosDB.Net.Domain.Attributes.PartitionKeyAttribute"/> and <see cref="CosmosDB.Net.Domain.Attributes.IdAttribute"/>
        /// </summary>
        /// <param name="documents">Documents to upsert</param>
        /// <param name="reportingCallback">[Optional] Method to be called based on the <paramref name="reportingInterval"/>. Generally used to provide a progress update to callers. Defaults to null./></param>
        /// <param name="reportingInterval">[Optional] interval in seconds to to call the reporting callback. Defaults to 10s</param>
        /// <param name="threads">[Optional] Number of threads to use for the paralel execution. Defaults to 4</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
        /// <example>
        /// <![CDATA[
        /// await _client.InsertDocuments(elements, (partial) => { Console.WriteLine($"upserted {partial.Count()} documents");
        /// ]]>
        /// </example>
        /// <remarks>The method will fail if the type T is not annotated with the partitionKey and id</remarks>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters.</returns>
        Task<IEnumerable<CosmosResponse>> UpsertDocuments<T>(IEnumerable<T> documents, Action<IEnumerable<CosmosResponse>> reportingCallback = null, TimeSpan? reportingInterval = null, int threads = 4, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Upsert (Insert or Update) multiple document streams into the database using a TPL Dataflow block.
        /// The documents that are passed in are going to be added to the database using the Stream APIs so they will not be seriazlied / deseriazlied in the SDK
        /// each document passed in must contain a property that matches the partitionKey property assigned on the collection
        /// </summary>
        /// <param name="documents">Documents to upsert</param>
        /// <param name="reportingCallback">[Optional] Method to be called based on the <paramref name="reportingInterval"/>. Generally used to provide a progress update to callers. Defaults to null./></param>
        /// <param name="reportingInterval">[Optional] interval in seconds to to call the reporting callback. Defaults to 10s</param>
        /// <param name="threads">[Optional] Number of threads to use for the paralel execution. Defaults to 4</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
        /// <example>
        /// <![CDATA[
        /// await _client.InsertDocuments(elements, (partial) => { Console.WriteLine($"upserted {partial.Count()} documents");
        /// ]]>
        /// </example>
        /// <returns><see cref="CosmosResponse"/> that tracks success status along with various performance parameters.</returns>
        Task<IEnumerable<CosmosResponse>> UpsertDocuments(IEnumerable<(Stream stream, PartitionKey pk)> documents, Action<IEnumerable<CosmosResponse>> reportingCallback = null, TimeSpan? reportingInterval = null, int threads = 4, CancellationToken cancellationToken = default(CancellationToken));



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
