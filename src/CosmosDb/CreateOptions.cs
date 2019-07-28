using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Text;

namespace CosmosDB.Net
{
    public class CreateOptions
    {
        public CreateOptions(string databaseId, string containerId, string partitionKeyPath = "/PartitionKey")
        {
            DatabaseId = databaseId;
            Container = new ContainerProperties
            {
                Id = containerId,
                PartitionKeyPath = partitionKeyPath
            };
        }

        public int? DatabaseThrouhput { get; set; }

        public int? ContainerThroughput { get; set; }

        public string DatabaseId { get; set; }

        public ContainerProperties Container { get; set; }
    }
}