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
        private Lazy<CosmosContainer> _client;
        private Lazy<ICosmosGraphClient> _graphClient;

        public CosmosDbConfig()
        {
            _client  = new Lazy<CosmosContainer>(
                () =>
                {
                    return CosmosClientExtionsions.GetCosmosContainer(ConnectionString, Database, Collection, false).GetAwaiter().GetResult();
                });
            _graphClient = new Lazy<ICosmosGraphClient>(
               () =>
               {
                   return CosmosGraphClient.GetCosmosGraphClient(GraphEndpoint, AuthKey, Database, Collection, false).GetAwaiter().GetResult();
               });
        }

        public string Name { get; set; }
        public string Endpoint { get; set; }
        public string GraphEndpoint { get; set; }
        public string AuthKey { get; set; }
        public string Database { get; set; }
        public string ConnectionString { get; set; }
        public string Collection { get; set; }
        
        public Lazy<CosmosContainer> Client { get { return _client; } }
        public Lazy<ICosmosGraphClient> GraphClient { get { return _graphClient; } }
    }
}