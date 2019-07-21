using CosmosDb.Domain;
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
    public class DocumentDataTypesTests
    {
        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
        }

        [TestMethod]
        public void GenerateCosmosDocumentFromModelWithAttributes()
        {
            var movie = Movie.GetTestModel("The Network");
            var movieDoc = SerializationHelpers.ToCosmosDocument(movie) as IDictionary<string, object>;
            Assert.IsNotNull(movieDoc, "Failed to convert movie to Document");

            //Test that properties are present in the output document
            var errors = new List<string>();

            if (!movieDoc.ContainsKey("id")) errors.Add("Document missing Id property");
            if (!movieDoc.ContainsKey("label")) errors.Add("Document missing Label property");
            if (!movieDoc.ContainsKey("PartitionKey")) errors.Add("Document missing PartitionKey property");
            if (!movieDoc.ContainsKey("Budget")) errors.Add("Document missing Budget property");
            if (!movieDoc.ContainsKey("ReleaseDate")) errors.Add("Document missing ReleaseDate property");
            if (!movieDoc.ContainsKey("Runtime")) errors.Add("Document missing Runtime property");
            if (!movieDoc.ContainsKey("Rating")) errors.Add("Document missing Rating property");
            if (!movieDoc.ContainsKey("Cast")) errors.Add("Document missing Cast property");
            if (!movieDoc.ContainsKey("MovieId")) errors.Add("Document missing MovieId property");
            if (!movieDoc.ContainsKey("Title")) errors.Add("Document missing Title property");
            if (!movieDoc.ContainsKey("Format")) errors.Add("Document missing Title property");

            Assert.IsFalse(errors.Any(), string.Join(Environment.NewLine, errors.ToArray()));
            Assert.AreEqual(11, movieDoc.Keys.Count(), "Document has extra properties");

            //Test values
            Assert.AreEqual(movie.MovieId, movieDoc["id"], "id not matching");
            Assert.AreEqual(movie.Label, movieDoc["label"], "label not matching");
            Assert.AreEqual(movie.Title, movieDoc["PartitionKey"], "partitionKey not matching");
            Assert.AreEqual(movie.MovieId, movieDoc["MovieId"], "MovieId not matching");
            Assert.AreEqual(movie.Title, movieDoc["Title"], "Title not matching");
            Assert.AreEqual(movie.Rating, movieDoc["Rating"], "Rating not matching");
            Assert.AreEqual(movie.Cast, movieDoc["Cast"], "Cast not matching");
            Assert.AreEqual(movie.Budget, movieDoc["Budget"], "Budget not matching");
            Assert.AreEqual(movie.ReleaseDate, movieDoc["ReleaseDate"], "ReleaseDate not matching");
            Assert.AreEqual(movie.Runtime, movieDoc["Runtime"], "Runtime not matching");
            Assert.AreEqual(movie.Format, movieDoc["Format"], "Format not matching");
        }

        [TestMethod]
        public void GenerateCosmosDocumentFromModelWithNoLabelAndId()
        {
            var movie = MovieNoLabelNoId.GetTestModel("The Network");
            var movieGraph = SerializationHelpers.ToCosmosDocument(movie) as IDictionary<string, object>;
            Assert.IsNotNull(movieGraph, "Failed to convert MovieNoLabelNoId to Document");

            //Test that properties are present in the output document
            var errors = new List<string>();

            if (!movieGraph.ContainsKey("id")) errors.Add("Document missing Id property");
            if (!movieGraph.ContainsKey("label")) errors.Add("Document missing Label property");
            if (!movieGraph.ContainsKey("PartitionKey")) errors.Add("Document missing PartitionKey property");
            //Since movieId is not maked as Label, it should be present in the output doc
            if (!movieGraph.ContainsKey("MovieId")) errors.Add("Document missing MovieId property");
            if (!movieGraph.ContainsKey("Budget")) errors.Add("Document missing Budget property");
            if (!movieGraph.ContainsKey("ReleaseDate")) errors.Add("Document missing ReleaseDate property");
            if (!movieGraph.ContainsKey("Runtime")) errors.Add("Document missing Runtime property");
            if (!movieGraph.ContainsKey("Rating")) errors.Add("Document missing Rating property");
            if (!movieGraph.ContainsKey("Cast")) errors.Add("Document missing Cast property");
            if (!movieGraph.ContainsKey("Title")) errors.Add("Document missing Title property");

            Assert.IsFalse(errors.Any(), string.Join(Environment.NewLine, errors.ToArray()));
            Assert.AreEqual(10, movieGraph.Keys.Count(), "Document has extra properties");

            //Test values
            // Assert.AreEqual(movie.Id, movieGraph["id"], "id not matching"); -> id will be a guid
            Assert.AreEqual(movie.GetType().Name, movieGraph["label"], "label not matching");
            Assert.AreEqual(movie.Title, movieGraph["PartitionKey"], "partitionKey not matching");

            Assert.AreEqual(movie.MovieId, movieGraph["MovieId"], "MovieId not matching");
            Assert.AreEqual(movie.Title, movieGraph["Title"], "Title not matching");

            Assert.AreEqual(movie.Rating, movieGraph["Rating"], "Rating not matching");
            Assert.AreEqual(movie.Cast, movieGraph["Cast"], "Cast not matching");
            Assert.AreEqual(movie.Budget, movieGraph["Budget"], "Budget not matching");
            Assert.AreEqual(movie.ReleaseDate, movieGraph["ReleaseDate"], "ReleaseDate not matching");
            Assert.AreEqual(movie.Runtime, movieGraph["Runtime"], "Runtime not matching");
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void GenerateCosmosDocumentFromModelWithNoPartitionKey()
        {
            var movie = MovieNoAttributes.GetTestModel("The Network");
            SerializationHelpers.ToCosmosDocument(movie);
        }

        [TestMethod]
        public void GenerateCosmosDocumentFromModelWithIgnoredProperties()
        {
            var movie = MovieIgnoredAttributes.GetTestModel("The Network");
            var movieGraph = SerializationHelpers.ToCosmosDocument(movie) as IDictionary<string, object>;

            Assert.IsNotNull(movieGraph, "Failed to convert to Document");

            //Test that properties are present in the output document
            var errors = new List<string>();

            if (!movieGraph.ContainsKey("id")) errors.Add("Document missing Id property");
            if (!movieGraph.ContainsKey("label")) errors.Add("Document missing Label property");
            if (!movieGraph.ContainsKey("PartitionKey")) errors.Add("Document missing PartitionKey property");
            if (!movieGraph.ContainsKey("Budget")) errors.Add("Document missing Budget property");
            if (movieGraph.ContainsKey("ReleaseDate")) errors.Add("Document contains ignored ReleaseDate property");
            if (movieGraph.ContainsKey("Runtime")) errors.Add("Document contains ignored Runtime property");
            if (!movieGraph.ContainsKey("Rating")) errors.Add("Document missing Rating property");
            if (!movieGraph.ContainsKey("Cast")) errors.Add("Document missing Cast property");
            if (!movieGraph.ContainsKey("MovieId")) errors.Add("Document missing MovieId property");
            if (!movieGraph.ContainsKey("Title")) errors.Add("Document missing Title property");

            Assert.IsFalse(errors.Any(), string.Join(Environment.NewLine, errors.ToArray()));
            Assert.AreEqual(8, movieGraph.Keys.Count(), "Document has extra properties");

            //Test values
            Assert.AreEqual(movie.MovieId, movieGraph["id"], "id not matching");
            Assert.AreEqual(movie.Label, movieGraph["label"], "label not matching");
            Assert.AreEqual(movie.Title, movieGraph["PartitionKey"], "partitionKey not matching");

            Assert.AreEqual(movie.MovieId, movieGraph["MovieId"], "MovieId not matching");
            Assert.AreEqual(movie.Title, movieGraph["Title"], "Title not matching");

            Assert.AreEqual(movie.Rating, movieGraph["Rating"], "Rating not matching");
            Assert.AreEqual(movie.Cast, movieGraph["Cast"], "Cast not matching");
            Assert.AreEqual(movie.Budget, movieGraph["Budget"], "Budget not matching");
        }

        [TestMethod]
        public void GenerateCosmosDocumentFromModelWithNoAttributes()
        {
            var movie = MovieNoAttributes.GetTestModel("The Network");

            Assert.ThrowsException<Exception>(() => SerializationHelpers.ToCosmosDocument(movie));

            //Using the
            var movieGraph = SerializationHelpers.ToCosmosDocument(movie, exp => exp.Title) as IDictionary<string, object>;

            Assert.IsNotNull(movieGraph, "Failed to convert to Cosmos Document");

            //Test that properties are present in the output document
            var errors = new List<string>();

            if (!movieGraph.ContainsKey("id")) errors.Add("Document missing Id property");
            if (!movieGraph.ContainsKey("label")) errors.Add("Document missing Label property");
            if (!movieGraph.ContainsKey("PartitionKey")) errors.Add("Document missing PartitionKey property");
            if (!movieGraph.ContainsKey("Budget")) errors.Add("Document missing Budget property");
            if (!movieGraph.ContainsKey("ReleaseDate")) errors.Add("Document missing ReleaseDate property");
            if (!movieGraph.ContainsKey("Runtime")) errors.Add("Document missing Runtime property");
            if (!movieGraph.ContainsKey("Rating")) errors.Add("Document missing Rating property");
            if (!movieGraph.ContainsKey("Cast")) errors.Add("Document missing Cast property");

            Assert.IsFalse(errors.Any(), string.Join(Environment.NewLine, errors.ToArray()));
            Assert.AreEqual(10, movieGraph.Keys.Count(), "Document has extra properties");

            //Test values
            // Assert.AreEqual(movie.Id, movieGraph["id"], "id not matching"); -> id will be a guid
            Assert.AreEqual(movie.GetType().Name, movieGraph["label"], "label not matching");
            Assert.AreEqual(movie.Title, movieGraph["PartitionKey"], "partitionKey not matching");

            Assert.AreEqual(movie.MovieId, movieGraph["MovieId"], "MovieId not matching");
            Assert.AreEqual(movie.Title, movieGraph["Title"], "Title not matching");

            Assert.AreEqual(movie.Rating, movieGraph["Rating"], "Rating not matching");
            Assert.AreEqual(movie.Cast, movieGraph["Cast"], "Cast not matching");
            Assert.AreEqual(movie.Budget, movieGraph["Budget"], "Budget not matching");
            Assert.AreEqual(movie.ReleaseDate, movieGraph["ReleaseDate"], "ReleaseDate not matching");
            Assert.AreEqual(movie.Runtime, movieGraph["Runtime"], "Runtime not matching");
        }

        [TestMethod]
        public void GenerateCosmosDocumentFromModelWithIllegalProperties()
        {
            var movie = MovieIllegalPropertyNames.GetTestModel("The Network");

            var movieGraph = SerializationHelpers.ToCosmosDocument(movie) as IDictionary<string, object>;
            Assert.IsNotNull(movieGraph, "Failed to convert to Cosmos Document");

            //Test that properties are present in the output document
            var errors = new List<string>();

            if (!movieGraph.ContainsKey("id")) errors.Add("Document missing Id property");
            if (!movieGraph.ContainsKey("label")) errors.Add("Document missing Label property");
            if (!movieGraph.ContainsKey("PartitionKey")) errors.Add("Document missing PartitionKey property");
            if (!movieGraph.ContainsKey("Budget")) errors.Add("Document missing Budget property");
            if (!movieGraph.ContainsKey("ReleaseDate")) errors.Add("Document missing ReleaseDate property");
            if (!movieGraph.ContainsKey("Runtime")) errors.Add("Document missing Runtime property");
            if (!movieGraph.ContainsKey("Rating")) errors.Add("Document missing Rating property");
            if (!movieGraph.ContainsKey("Cast")) errors.Add("Document missing Cast property");
            if (!movieGraph.ContainsKey("Title")) errors.Add("Document missing Title property");

            Assert.IsFalse(errors.Any(), string.Join(Environment.NewLine, errors.ToArray()));
            Assert.AreEqual(9, movieGraph.Keys.Count(), "Document has extra properties");

            //Test values
            // Assert.AreEqual(movie.Id, movieGraph["id"], "id not matching"); -> id will be a guid
            Assert.AreEqual(movie.Label, movieGraph["label"], "label not matching");
            Assert.AreEqual(movie.Title, movieGraph["PartitionKey"], "partitionKey not matching");

            Assert.AreEqual(movie.Title, movieGraph["Title"], "Title not matching");

            Assert.AreEqual(movie.Rating, movieGraph["Rating"], "Rating not matching");
            Assert.AreEqual(movie.Cast, movieGraph["Cast"], "Cast not matching");
            Assert.AreEqual(movie.Budget, movieGraph["Budget"], "Budget not matching");
            Assert.AreEqual(movie.ReleaseDate, movieGraph["ReleaseDate"], "ReleaseDate not matching");
            Assert.AreEqual(movie.Runtime, movieGraph["Runtime"], "Runtime not matching");
        }

        [TestMethod]
        public void GenerateCosmosDocumentFromModelWithNoAttributesProvidingId()
        {
            var movie = MovieNoAttributes.GetTestModel("The Network");

            Assert.ThrowsException<Exception>(() => SerializationHelpers.ToCosmosDocument(movie));

            //Using the
            var movieGraph = SerializationHelpers.ToCosmosDocument(movie, pkProperty: model => model.Title, idProp: model => movie.MovieId) as IDictionary<string, object>;

            Assert.IsNotNull(movieGraph, "Failed to convert to Cosmos Document");

            //Test that properties are present in the output document
            var errors = new List<string>();

            if (!movieGraph.ContainsKey("id")) errors.Add("Document missing Id property");
            if (!movieGraph.ContainsKey("label")) errors.Add("Document missing Label property");
            if (!movieGraph.ContainsKey("PartitionKey")) errors.Add("Document missing PartitionKey property");
            if (!movieGraph.ContainsKey("Budget")) errors.Add("Document missing Budget property");
            if (!movieGraph.ContainsKey("ReleaseDate")) errors.Add("Document missing ReleaseDate property");
            if (!movieGraph.ContainsKey("Runtime")) errors.Add("Document missing Runtime property");
            if (!movieGraph.ContainsKey("Rating")) errors.Add("Document missing Rating property");
            if (!movieGraph.ContainsKey("Cast")) errors.Add("Document missing Cast property");

            Assert.IsFalse(errors.Any(), string.Join(Environment.NewLine, errors.ToArray()));
            Assert.AreEqual(10, movieGraph.Keys.Count(), "Document has extra properties");

            //Test values
            Assert.AreEqual(movie.MovieId, movieGraph["id"], "id not matching");
            Assert.AreEqual(movie.GetType().Name, movieGraph["label"], "label not matching");
            Assert.AreEqual(movie.Title, movieGraph["PartitionKey"], "partitionKey not matching");

            Assert.AreEqual(movie.MovieId, movieGraph["MovieId"], "MovieId not matching");
            Assert.AreEqual(movie.Title, movieGraph["Title"], "Title not matching");

            Assert.AreEqual(movie.Rating, movieGraph["Rating"], "Rating not matching");
            Assert.AreEqual(movie.Cast, movieGraph["Cast"], "Cast not matching");
            Assert.AreEqual(movie.Budget, movieGraph["Budget"], "Budget not matching");
            Assert.AreEqual(movie.ReleaseDate, movieGraph["ReleaseDate"], "ReleaseDate not matching");
            Assert.AreEqual(movie.Runtime, movieGraph["Runtime"], "Runtime not matching");
        }



        //TODO: test for reading data from graph
    }
}
