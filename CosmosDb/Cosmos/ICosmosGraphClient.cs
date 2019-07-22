using CosmosDb.Domain;
using Gremlin.Net.Driver;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CosmosDb
{
    public interface ICosmosGraphClient
    {
        GremlinServer GremlinServer { get; }
        CosmosClient Client { get; }
        Database Database { get; }
        Container Container { get; }

        Task<CosmosResponse<T>> ExecuteGremlinSingle<T>(string queryString);
        Task<CosmosResponse<IEnumerable<T>>> ExecuteGremlin<T>(string queryString);

        Task<CosmosResponse> InsertVertex<T>(T entity);
        Task<CosmosResponse> UpsertVertex<T>(T entity);

        Task<CosmosResponse> InsertEdge<T, U, V>(T edge, U source, V target, bool single = false);
        Task<CosmosResponse> UpsertEdge<T, U, V>(T edge, U source, V target, bool single = false);
        Task<CosmosResponse> InsertEdge<T>(T edge, GraphItemBase source, GraphItemBase target, bool single = false);
        Task<CosmosResponse> UpsertEdge<T>(T edge, GraphItemBase source, GraphItemBase target, bool single = false);



        Task<CosmosResponse<T>> ReadVertex<T>(string docId, string partitionKey);
        Task<CosmosResponse<IEnumerable<T>>> ExecuteSQL<T>(string query, bool pagedResults = false, string continuationToken = "", CancellationToken cancellationToken = default(CancellationToken));

        //Task<IEnumerable<CosmosResponse>> InsertGraphVertex<T>(IEnumerable<T> vertices, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10);
        //Task<IEnumerable<CosmosResponse>> UpsertGraphVertex<T>(IEnumerable<T> vertices, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10);

    }
}
