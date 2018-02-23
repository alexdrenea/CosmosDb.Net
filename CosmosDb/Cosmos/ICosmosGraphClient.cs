using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmosDb
{
    public interface ICosmosGraphClient
    {
        Task<CosmosResponse> ExecuteGremlingSingle(string queryString);
        Task<CosmosResponse<T>> ExecuteGremlingSingle<T>(string queryString);
        Task<CosmosResponse<IEnumerable<T>>> ExecuteGremlingMulti<T>(string queryString);
    }
}
