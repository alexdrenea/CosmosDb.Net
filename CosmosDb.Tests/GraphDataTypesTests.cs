using CosmosDb.Cosmos;
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
    public class GraphDataTypesTests
    {
        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
        }

        [TestMethod]
        public void GenerateGraphVertexFromModelWithAttributes()
        {
            var movie = Movie.GetTestModel("The Network");
            var movieGraph = SerializationHelpers.ToGraphVertex(movie) as IDictionary<string, object>;

            Assert.IsNotNull(movieGraph, "Failed to convert to Graph Vertex");

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
            if (!movieGraph.ContainsKey("MovieId")) errors.Add("Document missing MovieId property");
            if (!movieGraph.ContainsKey("Title")) errors.Add("Document missing Title property");

            Assert.IsFalse(errors.Any(), string.Join(Environment.NewLine, errors.ToArray()));
            Assert.AreEqual(10, movieGraph.Keys.Count(), "Document has extra properties");

            //Test values
            Assert.AreEqual(movie.MovieId, movieGraph["id"], "id not matching");
            Assert.AreEqual(movie.Label, movieGraph["label"], "label not matching");
            Assert.AreEqual(movie.Title, movieGraph["PartitionKey"], "partitionKey not matching");

            AssertGraphProperty(movie.MovieId, movieGraph, "MovieId");
            AssertGraphProperty(movie.Title, movieGraph, "Title");
            AssertGraphProperty(movie.Rating, movieGraph, "Rating");
            AssertGraphProperty(movie.Cast, movieGraph, "Cast");

            AssertGraphProperty(movie.Budget, movieGraph, "Budget");
            AssertGraphProperty(movie.ReleaseDate, movieGraph, "ReleaseDate");
            AssertGraphProperty(movie.Runtime, movieGraph, "Runtime");
        }

        [TestMethod]
        public void GenerateGraphVertexFromModelWithNoLabelAndId()
        {
            var movie = MovieNoLabelNoId.GetTestModel("The Network");
            var movieGraph = SerializationHelpers.ToGraphVertex(movie) as IDictionary<string, object>;

            Assert.IsNotNull(movieGraph, "Failed to convert to Graph Vertex");

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

            AssertGraphProperty(movie.Rating, movieGraph, "Rating");
            AssertGraphProperty(movie.Cast, movieGraph, "Cast");

            AssertGraphProperty(movie.Title, movieGraph, "Title");
            AssertGraphProperty(movie.MovieId, movieGraph, "MovieId");
            AssertGraphProperty(movie.Budget, movieGraph, "Budget");
            AssertGraphProperty(movie.ReleaseDate, movieGraph, "ReleaseDate");
            AssertGraphProperty(movie.Runtime, movieGraph, "Runtime");
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void GenerateGraphVertexFromModelWithNoPartitionKey()
        {
            var movie = MovieNoAttributes.GetTestModel("The Network");
            SerializationHelpers.ToGraphVertex(movie);
        }

        [TestMethod]
        public void GenerateGraphVertexFromModelWithIgnoredProperties()
        {
            var movie = MovieIgnoredAttributes.GetTestModel("The Network");
            var movieGraph = SerializationHelpers.ToGraphVertex(movie) as IDictionary<string, object>;

            Assert.IsNotNull(movieGraph, "Failed to convert to Graph Vertex");

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

            AssertGraphProperty(movie.MovieId, movieGraph, "MovieId");
            AssertGraphProperty(movie.Title, movieGraph, "Title");
            AssertGraphProperty(movie.Rating, movieGraph, "Rating");
            AssertGraphProperty(movie.Cast, movieGraph, "Cast");
            AssertGraphProperty(movie.Budget, movieGraph, "Budget");
        }

        [TestMethod]
        public void GenerateGraphVertexFromModelWithNoAttributes()
        {
            var movie = MovieNoAttributes.GetTestModel("The Network");

            Assert.ThrowsException<Exception>(() => SerializationHelpers.ToGraphVertex(movie));

            //Using the
            var movieGraph = SerializationHelpers.ToGraphVertex(movie, exp => exp.Title) as IDictionary<string, object>;

            Assert.IsNotNull(movieGraph, "Failed to convert to Graph Vertex");

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

            AssertGraphProperty(movie.Rating, movieGraph, "Rating");
            AssertGraphProperty(movie.Cast, movieGraph, "Cast");
            AssertGraphProperty(movie.Title, movieGraph, "Title");

            AssertGraphProperty(movie.Budget, movieGraph, "Budget");
            AssertGraphProperty(movie.ReleaseDate, movieGraph, "ReleaseDate");
            AssertGraphProperty(movie.Runtime, movieGraph, "Runtime");
        }

        [TestMethod]
        public void GenerateGraphVertexFromModelWithIllegalProperties()
        {
            var movie = MovieIllegalPropertyNames.GetTestModel("The Network");

            var movieGraph = SerializationHelpers.ToGraphVertex(movie) as IDictionary<string, object>;
            Assert.IsNotNull(movieGraph, "Failed to convert to Graph Vertex");

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

            AssertGraphProperty(movie.Rating, movieGraph, "Rating");
            AssertGraphProperty(movie.Cast, movieGraph, "Cast");
            AssertGraphProperty(movie.Title, movieGraph, "Title");

            AssertGraphProperty(movie.Budget, movieGraph, "Budget");
            AssertGraphProperty(movie.ReleaseDate, movieGraph, "ReleaseDate");
            AssertGraphProperty(movie.Runtime, movieGraph, "Runtime");
        }

        [TestMethod]
        public void GenerateGraphVertexFromModelWithNoAttributesProvidingId()
        {
            var movie = MovieNoAttributes.GetTestModel("The Network");

            Assert.ThrowsException<Exception>(() => SerializationHelpers.ToGraphVertex(movie));

            //Using the
            var movieGraph = SerializationHelpers.ToGraphVertex(movie, pkProperty: model => model.Title, idProp: model => movie.MovieId) as IDictionary<string, object>;

            Assert.IsNotNull(movieGraph, "Failed to convert to Graph Vertex");

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

            AssertGraphProperty(movie.Rating, movieGraph, "Rating");
            AssertGraphProperty(movie.Cast, movieGraph, "Cast");
            AssertGraphProperty(movie.MovieId, movieGraph, "MovieId");
            AssertGraphProperty(movie.Title, movieGraph, "Title");

            AssertGraphProperty(movie.Budget, movieGraph, "Budget");
            AssertGraphProperty(movie.ReleaseDate, movieGraph, "ReleaseDate");
            AssertGraphProperty(movie.Runtime, movieGraph, "Runtime");
        }

        //TODO: Fix this -> issue when we're generating a guid id, if you get the GraphVertex object twice,
        //you'll get 2 different id properties so you can't create the edge.
        [TestMethod]
        public void GenerateGraphEdgeWithVertexReferencesNoIdProperty()
        {
            //Initialize Objects
            var movieTitle = "The Network";
            var movie = MovieGraph.GetTestModel(movieTitle);
            var rating = Rating.GetTestRating(movieTitle);
            var cast = Cast.GetTestMovieCast(movieTitle);
            var movieRating = new MovieRatingEdge { SiteName = rating.SiteName };

            var movieGraph = SerializationHelpers.ToGraphVertex(movie) as IDictionary<string, object>;
            var ratingGraph = SerializationHelpers.ToGraphVertex(rating) as IDictionary<string, object>;
            var castGraph = SerializationHelpers.ToGraphVertex(cast) as IDictionary<string, object>;

            Assert.IsNotNull(movieGraph, "Failed to convert movie to Graph Vertex");
            Assert.IsNotNull(ratingGraph, "Failed to convert rating to Graph Vertex");
            Assert.IsNotNull(castGraph, "Failed to convert cast to Graph Vertex");

            //Generate an edge 
            var movieRatingEdgeSingle = SerializationHelpers.ToGraphEdge(movieRating, movie, rating, true) as IDictionary<string, object>;
            var movieRatingEdgeSingle2 = SerializationHelpers.ToGraphEdge(movieRating, movie, rating, true) as IDictionary<string, object>;
            Assert.IsNotNull(movieRatingEdgeSingle, "Failed to convert movie to Graph Vertex");
            Assert.IsNotNull(movieRatingEdgeSingle2, "Failed to convert movie to Graph Vertex");
            //Ensure all properties are present in the result
            AssertGraphEdgeHasBaseProperties(movieRatingEdgeSingle);
            //Ensure properties have proper values
            Assert.AreEqual($"{movieGraph["id"]}-{ratingGraph["id"]}", movieRatingEdgeSingle["id"], "id not matching");
            Assert.AreEqual(movieRating.GetType().Name, movieRatingEdgeSingle["label"], "label not matching");
            Assert.IsTrue(movieRatingEdgeSingle.ContainsKey("SiteName"), "Edge missing SiteName Property");
            Assert.AreEqual(movieRating.SiteName, movieRatingEdgeSingle["SiteName"], "SiteName not matching");
            AssertGraphEdgeValues(movieRatingEdgeSingle, movieGraph, ratingGraph);
            //Ensure that 2 single edges have the same Ids
            Assert.AreEqual(movieRatingEdgeSingle["id"], movieRatingEdgeSingle2["id"], "Ids for 2 single edges should match");

            var movieRatingEdgeMulti = SerializationHelpers.ToGraphEdge(movieRating, movie, rating, false) as IDictionary<string, object>;
            var movieRatingEdgeMulti2 = SerializationHelpers.ToGraphEdge(movieRating, movie, rating, false) as IDictionary<string, object>;
            Assert.IsNotNull(movieRatingEdgeMulti, "Failed to convert movie to Graph Vertex");
            Assert.IsNotNull(movieRatingEdgeMulti2, "Failed to convert movie to Graph Vertex");
            //Ensure all properties are present in the result
            AssertGraphEdgeHasBaseProperties(movieRatingEdgeSingle);
            //Ensure properties have proper values
            Assert.AreEqual(movieRating.GetType().Name, movieRatingEdgeMulti["label"], "label not matching");
            Assert.IsTrue(movieRatingEdgeMulti.ContainsKey("SiteName"), "Edge missing SiteName Property");
            Assert.AreEqual(movieRating.SiteName, movieRatingEdgeMulti["SiteName"], "SiteName not matching");
            AssertGraphEdgeValues(movieRatingEdgeMulti, movieGraph, ratingGraph);
            //Ensure that when we don't request a single edge, that the ids generated are different every time.
            Assert.AreNotEqual(movieRatingEdgeMulti["id"], movieRatingEdgeMulti2["id"], "id should be dynamic");
        }

        [TestMethod]
        public void GenerateGraphEdgeWithVertexReferencesWithIdProperty()
        {
            //Initialize Objects
            var movieTitle = "The Network";
            var movie = MovieGraph.GetTestModel(movieTitle);
            var cast = Cast.GetTestMovieCast(movieTitle);
            var movieCast = new MovieCastEdge { Order = cast.Order };

            var movieGraph = SerializationHelpers.ToGraphVertex(movie) as IDictionary<string, object>;
            var castGraph = SerializationHelpers.ToGraphVertex(cast) as IDictionary<string, object>;

            Assert.IsNotNull(movieGraph, "Failed to convert movie to Graph Vertex");
            Assert.IsNotNull(castGraph, "Failed to convert cast to Graph Vertex");

            //Generate an edge 
            var movieCastEdgeSingle = SerializationHelpers.ToGraphEdge(movieCast, movie, cast, true) as IDictionary<string, object>;
            var movieCastEdgeSingle2 = SerializationHelpers.ToGraphEdge(movieCast, movie, cast, true) as IDictionary<string, object>;
            Assert.IsNotNull(movieCastEdgeSingle, "Failed to convert movie to Graph Vertex");
            Assert.IsNotNull(movieCastEdgeSingle2, "Failed to convert movie to Graph Vertex");
            //Ensure all properties are present in the result
            AssertGraphEdgeHasBaseProperties(movieCastEdgeSingle);
            //Ensure properties have proper values
            Assert.AreEqual($"{movieGraph["id"]}-{castGraph["id"]}", movieCastEdgeSingle["id"], "id not matching");
            Assert.AreEqual(movieCast.GetType().Name, movieCastEdgeSingle["label"], "label not matching");
            Assert.IsTrue(movieCastEdgeSingle.ContainsKey("Order"), "Edge missing Order Property");
            Assert.AreEqual(movieCast.Order, movieCastEdgeSingle["Order"], "ORder not matching");
            AssertGraphEdgeValues(movieCastEdgeSingle, movieGraph, castGraph);
            //Ensure that 2 single edges have the same Ids
            Assert.AreEqual(movieCastEdgeSingle["id"], movieCastEdgeSingle2["id"], "Ids for 2 single edges should match");

            var movieRatingEdgeMulti = SerializationHelpers.ToGraphEdge(movieCast, movie, cast, false) as IDictionary<string, object>;
            var movieRatingEdgeMulti2 = SerializationHelpers.ToGraphEdge(movieCast, movie, cast, false) as IDictionary<string, object>;
            Assert.IsNotNull(movieRatingEdgeMulti, "Failed to convert movie to Graph Vertex");
            Assert.IsNotNull(movieRatingEdgeMulti2, "Failed to convert movie to Graph Vertex");
            //Ensure all properties are present in the result
            AssertGraphEdgeHasBaseProperties(movieCastEdgeSingle);
            //Ensure properties have proper values
            Assert.AreEqual(movieCast.GetType().Name, movieRatingEdgeMulti["label"], "label not matching");
            Assert.IsTrue(movieRatingEdgeMulti.ContainsKey("Order"), "Edge missing Order Property");
            Assert.AreEqual(movieCast.Order, movieRatingEdgeMulti["Order"], "Order not matching");
            AssertGraphEdgeValues(movieRatingEdgeMulti, movieGraph, castGraph);
            //Ensure that when we don't request a single edge, that the ids generated are different every time.
            Assert.AreNotEqual(movieRatingEdgeMulti["id"], movieRatingEdgeMulti2["id"], "id should be dynamic");
        }

        [TestMethod]
        public void GenerateGraphEdgeWithGraphItemBase()
        {
            //Initialize Objects
            var movieTitle = "The Network";
            var movieGraphItemBase = new GraphItemBase { Id = "movieId", Label = "Movie", PartitionKey = movieTitle };
            var ratingItemBase = new GraphItemBase { Id = "ratingId", Label = "Rating", PartitionKey = movieTitle };
            var movieRating = new MovieRatingEdge { SiteName = "SiteName" };

            //Generate a single edge 
            var movieRatingEdgeSingle = SerializationHelpers.ToGraphEdge(movieRating, movieGraphItemBase, ratingItemBase, single: true) as IDictionary<string, object>;
            var movieRatingEdgeSingle2 = SerializationHelpers.ToGraphEdge(movieRating, movieGraphItemBase, ratingItemBase, single: true) as IDictionary<string, object>;
            Assert.IsNotNull(movieRatingEdgeSingle, "Failed to convert movie to Graph Vertex");
            Assert.IsNotNull(movieRatingEdgeSingle2, "Failed to convert movie to Graph Vertex");

            //Ensure all properties are present in the result
            AssertGraphEdgeHasBaseProperties(movieRatingEdgeSingle);
            //Ensure all property values match the inputs
            AssertGraphEdgeValues(movieRatingEdgeSingle, movieGraphItemBase, ratingItemBase);
            Assert.AreEqual($"{movieGraphItemBase.Id}-{ratingItemBase.Id}", movieRatingEdgeSingle["id"], "id not matching");
            Assert.AreEqual(movieRating.GetType().Name, movieRatingEdgeSingle["label"], "label not matching");
            Assert.IsTrue(movieRatingEdgeSingle.ContainsKey("SiteName"), "Edge missing SiteName Property");
            Assert.AreEqual(movieRating.SiteName, movieRatingEdgeSingle["SiteName"], "SiteName not matching");
            //Ensure that 2 single edges have the same Ids
            Assert.AreEqual(movieRatingEdgeSingle["id"], movieRatingEdgeSingle2["id"], "Ids for 2 single edges should match");


            //Generate a multi edge
            var movieRatingEdgeMulti1 = SerializationHelpers.ToGraphEdge(movieRating, movieGraphItemBase, ratingItemBase, false) as IDictionary<string, object>;
            var movieRatingEdgeMulti2 = SerializationHelpers.ToGraphEdge(movieRating, movieGraphItemBase, ratingItemBase, false) as IDictionary<string, object>;
            Assert.IsNotNull(movieRatingEdgeMulti1, "Failed to convert movie to Graph Vertex");
            Assert.IsNotNull(movieRatingEdgeMulti2, "Failed to convert movie to Graph Vertex");
            //Ensure all properties are present in the result
            AssertGraphEdgeHasBaseProperties(movieRatingEdgeMulti1);
            //Ensure all property values match the inputs
            AssertGraphEdgeValues(movieRatingEdgeMulti1, movieGraphItemBase, ratingItemBase);
            Assert.AreEqual(movieRating.GetType().Name, movieRatingEdgeMulti1["label"], "label not matching");
            Assert.IsTrue(movieRatingEdgeSingle.ContainsKey("SiteName"), "Edge missing SiteName Property");
            Assert.AreEqual(movieRating.SiteName, movieRatingEdgeSingle["SiteName"], "SiteName not matching");
            //Ensure that 2 multi edges have the different Ids
            Assert.AreNotEqual(movieRatingEdgeMulti1["id"], movieRatingEdgeMulti2["id"], "id should be dynamic");


        }





        private void AssertGraphEdgeValues(IDictionary<string, object> edge, IDictionary<string, object> source, IDictionary<string, object> dest)
        {
            Assert.AreEqual(source["PartitionKey"], edge["PartitionKey"], "partitionKey not matching");

            Assert.AreEqual(source["id"], edge["_vertexId"], "source vertex id not matching");
            Assert.AreEqual(source["label"], edge["_vertexLabel"], "source vertex label not matching");

            Assert.AreEqual(dest["id"], edge["_sink"], "destination vertex id not matching");
            Assert.AreEqual(dest["label"], edge["_sinkLabel"], "destination vertex label not matching");
            Assert.AreEqual(dest["PartitionKey"], edge["_sinkPartition"], "destination vertex partitionKey not matching");
        }

        private void AssertGraphEdgeValues(IDictionary<string, object> edge, GraphItemBase source, GraphItemBase dest)
        {
            Assert.AreEqual(source.PartitionKey, edge["PartitionKey"], "partitionKey not matching");

            Assert.AreEqual(source.Id, edge["_vertexId"], "source vertex id not matching");
            Assert.AreEqual(source.Label, edge["_vertexLabel"], "source vertex label not matching");

            Assert.AreEqual(dest.Id, edge["_sink"], "destination vertex id not matching");
            Assert.AreEqual(dest.Label, edge["_sinkLabel"], "destination vertex label not matching");
            Assert.AreEqual(dest.PartitionKey, edge["_sinkPartition"], "destination vertex partitionKey not matching");
        }

        private void AssertGraphEdgeHasBaseProperties(IDictionary<string, object> edge)
        {
            var errors = new List<string>();

            if (!edge.ContainsKey("id")) errors.Add("Edge missing Id property");
            if (!edge.ContainsKey("label")) errors.Add("Edge missing Label property");
            if (!edge.ContainsKey("PartitionKey")) errors.Add("Edge missing PartitionKey property");
            if (!edge.ContainsKey("_isEdge")) errors.Add("Edge missing _isEdge property");
            if (!edge.ContainsKey("_vertexId")) errors.Add("Edge missing _vertexId property");
            if (!edge.ContainsKey("_vertexLabel")) errors.Add("Edge missing _vertexLabel property");
            if (!edge.ContainsKey("_sink")) errors.Add("Edge missing _sink property");
            if (!edge.ContainsKey("_sinkLabel")) errors.Add("Edge missing _sinkLabel property");
            if (!edge.ContainsKey("_sinkPartition")) errors.Add("Edge missing _sinkPartition property");

            Assert.IsFalse(errors.Any(), string.Join(Environment.NewLine, errors.ToArray()));
        }

        /// <summary>
        /// Validates that our Document Output matches the format expected by the CosmosDB Graph format.
        /// each property of the vertex must be wrapped in a complex type:
        /// {
        ///  "id": "id",
        ///  "_value": [value],
        ///  "meta": ?
        /// }
        /// This method validates that the value provided in the original instance (expected) matches the value in the output document
        /// </summary>
        private void AssertGraphProperty<T>(T expected, IDictionary<string, object> graphObject, string propertyName)
        {
            var expandGraphProperty = graphObject[propertyName] as Array;
            Assert.IsNotNull(expandGraphProperty);
            Assert.AreEqual(1, expandGraphProperty.Length);

            var graphProperty = expandGraphProperty.GetValue(0);
            var graphPropertyType = graphProperty.GetType();
            var graphPropertyProperties = graphPropertyType.GetProperties();
            Assert.AreEqual(3, graphPropertyProperties.Length); // Expect 3 properties under a graphson node -> id, _value, meta

            var getValueFromGraphProperty = graphPropertyType.GetProperty("_value").GetValue(graphProperty);

            Assert.AreEqual(expected, getValueFromGraphProperty);
        }




        //TODO: test for reading data from graph
    }
}
