using CosmosDb.Domain;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CosmosDb
{
    public interface ICosmosSqlClient
    {
        CosmosClient Client { get; }
        Database Database { get; }
        Container Container { get; }


        Task<CosmosResponse> InsertDocument<T>(T document);
        Task<IEnumerable<CosmosResponse>> InsertDocuments<T>(IEnumerable<T> documents, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10);

        Task<CosmosResponse> UpsertDocument<T>(T document);
        Task<IEnumerable<CosmosResponse>> UpsertDocuments<T>(IEnumerable<T> documents, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10);

        Task<CosmosResponse<T>> ReadDocument<T>(string docId, string partitionKey);
        Task<CosmosResponse<IEnumerable<T>>> ExecuteSQL<T>(string query, bool pagedResults = false, string continuationToken = "", CancellationToken cancellationToken = default(CancellationToken));
    }
}
