using CosmosDb.Cosmos;
using CosmosDb.Domain.Helpers;
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
    public class ConversionTests
    {
        private static List<MovieCsv> _movies;
        private static List<CastCsv> _cast;
        private static ICosmosClient _cosmosClient;

        [ClassInitialize]
        public static async Task LoadSampleData(TestContext context)
        {
            _movies = Helpers.GetFromCsv<MovieCsv>("TestData/Samples/movies_lite.csv");
            _cast = Helpers.GetFromCsv<CastCsv>("TestData/Samples/movies_cast_lite.csv");

            Assert.AreEqual(4802, _movies.Count());
            Assert.AreEqual(106257, _cast.Count());

            _cosmosClient = await CosmosDbClient.GetCosmosDbClient("AccountEndpoint=https://mlsdatabasesql.documents.azure.com:443/;AccountKey=YdpG8nEhoeSXZjHoD9d4h4UJUEFyLJu89PqM7zqm9EBHjF6FXedA2nKAZTqmhJ7zcGHzJAv2WC3BnNXNBl9yJg==;", "test", "test1", forceCreate: false);
        }

        [TestMethod]
        public async Task InsertCosmosDocument()
        {
            var movie = _movies.First();
            var cast = _cast.Where(c => c.TmdbId == movie.TmdbId).ToList();

            var movieFull = MapperHelpers.GetMovieFull(movie, cast);

            var movieDoc = SerializationHelpers.ToCosmosDocument(movieFull);


            //var insert = await _cosmosClient.InsertDocument(movieDoc);

            var upsert = await _cosmosClient.InsertDocument(movieDoc);
            var read = await _cosmosClient.ReadDocument<MovieFull>(movie.TmdbId, movie.Title);
        }

        [TestMethod]
        public async Task UpsertCosmosDocument()
        {
            var movie = _movies.ElementAt(1);
            var cast = _cast.Where(c => c.TmdbId == movie.TmdbId).ToList();

            var movieFull = MapperHelpers.GetMovieFull(movie, cast);

            var upsert = await _cosmosClient.UpsertDocument(movieFull);
            var read = await _cosmosClient.ReadDocument<MovieFull>(movie.TmdbId, movie.Title);
        }

        //TODO: test for document with ignored attributes

        //TODO: test for document without attributes

        //TODO: Test for values in the document -> check each type, including complex type and arrays

        //TODO: test for graph format

        //TODO: test for graph format values


        //TODO: Test for edges


        //TODO: test for reading data from sql

        //TODO: test for reading data from graph
    }
}
