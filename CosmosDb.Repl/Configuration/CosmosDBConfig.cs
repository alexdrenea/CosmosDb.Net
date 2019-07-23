using CosmosDb;
using Microsoft.Azure.Cosmos;
using System;

namespace CosmosDb.Repl
{
    /// <summary>
    /// Represents a collection of configuration settings for DocDb connection
    /// </summary>
    public class CosmosDbConfig
    {
        private Lazy<ICosmosClientSql> _client;
        private Lazy<ICosmosClientGraph> _graphClient;

        public CosmosDbConfig()
        {
            _client  = new Lazy<ICosmosClientSql>(
                () =>
                {
                    return CosmosClientSql.GetByConnectionString(ConnectionString, Database, Collection, forceCreate: true).GetAwaiter().GetResult();
                });
            _graphClient = new Lazy<ICosmosClientGraph>(
               () =>
               {
                   return CosmosClientGraph.GetClientWithSql(AccountName, AuthKey, Database, Collection, forceCreate: false).GetAwaiter().GetResult();
               });
        }

        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string AccountName { get; set; }
        public string AuthKey { get; set; }
        public string Database { get; set; }
        public string Collection { get; set; }
        
        public Lazy<ICosmosClientSql> Client { get { return _client; } }
        public Lazy<ICosmosClientGraph> GraphClient { get { return _graphClient; } }
    }
}