using CosmosDb.Tests.TestData;
using CosmosDb.Tests.TestData.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CosmosDb.Tests
{
    [TestClass]
    public class CosmosResponseTests
    {
        private static ICosmosSqlClient _cosmosClient;

        private static string cosmosSqlConnectionString = "AccountEndpoint=https://mlsdatabasesql.documents.azure.com:443/;AccountKey=YdpG8nEhoeSXZjHoD9d4h4UJUEFyLJu89PqM7zqm9EBHjF6FXedA2nKAZTqmhJ7zcGHzJAv2WC3BnNXNBl9yJg==;";

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            _cosmosClient = await CosmosSqlClient.GetCosmosDbClient(cosmosSqlConnectionString, "test", "test1", forceCreate: false);
        }


        //TODO: TEst to see that CosmosResponse has all properties
        //TODO: test to see in case of exception that cosmos Response is valid
        //TODO: Test for Clone<> operations
    }
}
