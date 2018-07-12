using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDb
{
    public interface ICosmosClient
    {

        Task<CosmosResponse<IEnumerable<T>>> ExecuteSQL<T>(string query, bool expectGraphson = true);

        Task<CosmosResponse> InsertDocument<T>(T document);
        Task<IEnumerable<CosmosResponse>> InsertDocuments<T>(IEnumerable<T> documents, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10);

        Task<CosmosResponse> UpsertDocument<T>(T document);
        Task<IEnumerable<CosmosResponse>> UpsertDocuments<T>(IEnumerable<T> documents, Action<IEnumerable<CosmosResponse>> reportingCallback = null, int threads = 4, int reportingIntervalS = 10);

        Task<CosmosResponse<T>> ReadDocument<T>(string docId, string partitionKey);

        Task<CosmosResponse> InsertGraphVertex<T>(T vertex);
        Task<CosmosResponse> UpsertGraphVertex<T>(T vertex);
        Task<CosmosResponse<T>> ReadGraphVertex<T>(string docId, string partitionKey);

        Task<CosmosResponse> InsertGraphEdge<T, U, V>(T edge, U source, V target);
        Task<CosmosResponse> UpsertGraphEdge<T, U, V>(T edge, U source, V target);

        Task<CosmosResponse> ExecuteGremlingSingle(string queryString);
        Task<CosmosResponse<T>> ExecuteGremlingSingle<T>(string queryString);
        Task<CosmosResponse<IEnumerable<T>>> ExecuteGremlingMulti<T>(string queryString);

        Task<CosmosResponse> CreateUserDefinedFunctionIfNotExists(string id, string body);
        Task<CosmosResponse> DeleteUserDefinedFunction(string id);
    }
}
