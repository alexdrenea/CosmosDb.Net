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
        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
        }

        //TODO: test to validate that CosmosResponse has all properties
        //TODO: test to validate CosmosResponse built from CosmosExcepion
        //TODO: test for Clone<> operations
    }
}
