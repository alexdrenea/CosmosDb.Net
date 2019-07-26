using CosmosDb.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace CosmosDb.Tests.TestData.Models
{
    public class TestModel
    {
        [Id]
        public string Id { get; set; }

        [PartitionKey]
        public string Pk { get; set; }
    }
}
