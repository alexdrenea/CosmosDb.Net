using Microsoft.Azure.Cosmos;
using System;

namespace CosmosDB.Net
{
    public class CreateOptions
    {
        //Default client options Direct mode 1h timeout for backwards compatibility
        public static CosmosClientOptions DefaultClientOptions = new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Direct,
            RequestTimeout = new TimeSpan(1, 0, 0),
            AllowBulkExecution = false,
        };

        public CreateOptions(string databaseId, string containerId, string partitionKeyPath = "/PartitionKey")
        {
            DatabaseId = databaseId;
            Container = new ContainerProperties
            {
                Id = containerId,
                PartitionKeyPath = partitionKeyPath
            };
            ClientOptions = DefaultClientOptions;
        }

        /// <summary>
        /// Get or set options for creating the Cosmos Client
        /// </summary>
        public CosmosClientOptions ClientOptions { get; set; }

        public int? DatabaseThrouhput { get; set; }

        public int? ContainerThroughput { get; set; }

        public string DatabaseId { get; set; }

        public ContainerProperties Container { get; set; }
    }
}