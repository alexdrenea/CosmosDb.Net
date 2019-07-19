﻿using CosmosDb.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace CosmosDb.Tests.TestData.Models
{
    /// <summary>
    /// A movie model that has all components of a potential real world model that someone might insert in a database.
    /// The model is annotated with all Key Cosmos Properties
    /// The model includes arrays and complex types
    /// </summary>
    public class Movie
    {
        [Id]
        public string MovieId { get; set; }
        [PartitionKey]
        public string Title { get; set; }

        [Label]
        public string Label => "Movie";

        public DateTime ReleaseDate { get; set; }
        public int Runtime { get; set; }
        public long Budget { get; set; }
        public Rating Rating { get; set; }
        public List<Cast> Cast { get; set; }

        public static Movie GetTestModel(string title)
        {
            var rnd = new Random();
            return new Movie
            {
                Title = title,
                MovieId = $"{rnd.Next(100)}-{title}",
                ReleaseDate = DateTime.Today,
                Budget = 1000000,
                Runtime = 121,
                Rating = Rating.GetTestRating(title),
                Cast = new List<Cast>(new[] { Models.Cast.GetTestMovieCast(title) })
            };
        }
    }

    /// <summary>
    /// A movie model that does not use attributes to mark Key Cosmos Properties.
    /// This model has to be used with Expression Member helper methods
    /// </summary>
    public class MovieNoAttributes
    {
        public string MovieId { get; set; }
        public string Title { get; set; }
        public DateTime ReleaseDate { get; set; }
        public int Runtime { get; set; }
        public long Budget { get; set; }
        public Rating Rating { get; set; }
        public List<Cast> Cast { get; set; }


        public static MovieNoAttributes GetTestModel(string title)
        {
            var rnd = new Random();
            return new MovieNoAttributes
            {
                Title = title,
                MovieId = $"{rnd.Next(100)}-{title}",
                ReleaseDate = DateTime.Today,
                Budget = 1000000,
                Runtime = 121,
                Rating = Rating.GetTestRating(title),
                Cast = new List<Cast>(new[] { Models.Cast.GetTestMovieCast(title) })
            };
        }
    }

    /// <summary>
    /// A movie model that does not define a label or Id property
    /// When generating a cosmos document or vertex, an Id and a label property will be generated
    /// </summary>
    public class MovieNoLabelNoId
    {
        public string MovieId { get; set; }
       
        [PartitionKey]
        public string Title { get; set; }
        public DateTime ReleaseDate { get; set; }
        public int Runtime { get; set; }
        public long Budget { get; set; }
        public Rating Rating { get; set; }
        public List<Cast> Cast { get; set; }

        public static MovieNoLabelNoId GetTestModel(string title)
        {
            var rnd = new Random();
            return new MovieNoLabelNoId
            {
                Title = title,
                MovieId = $"{rnd.Next(100)}-{title}",
                ReleaseDate = DateTime.Today,
                Budget = 1000000,
                Runtime = 121,
                Rating = Rating.GetTestRating(title),
                Cast = new List<Cast>(new[] { Models.Cast.GetTestMovieCast(title) })
            };
        }
    }

    /// <summary>
    /// A movie model that defines some properties to be ignored
    /// When generating a cosmos document or vertex, those properties should not be present
    /// </summary>
    public class MovieIgnoredAttributes
    {
        [Id]
        public string MovieId { get; set; }
        [PartitionKey]
        public string Title { get; set; }

        [Label]
        public string Label => "Movie";

        [IgnoreDataMember]
        public DateTime ReleaseDate { get; set; }

        [JsonIgnore]
        public int Runtime { get; set; }
        public long Budget { get; set; }
        public Rating Rating { get; set; }
        public List<Cast> Cast { get; set; }

        public static MovieIgnoredAttributes GetTestModel(string title)
        {
            var rnd = new Random();
            return new MovieIgnoredAttributes
            {
                Title = title,
                MovieId = $"{rnd.Next(100)}-{title}",
                ReleaseDate = DateTime.Today,
                Budget = 1000000,
                Runtime = 121,
                Rating = Rating.GetTestRating(title),
                Cast = new List<Cast>(new[] { Models.Cast.GetTestMovieCast(title) })
            };
        }
    }

    /// <summary>
    /// A movie model that contains some properties that can't be present in a model -id, label, etc.
    /// </summary>
    public class MovieIllegalPropertyNames
    {
        [Id]
        public string Id { get; set; }
        
        [PartitionKey]
        public string Title { get; set; }

        [Label]
        public string Label => "Movie";

        public DateTime ReleaseDate { get; set; }
        public int Runtime { get; set; }
        public long Budget { get; set; }
        public Rating Rating { get; set; }
        public List<Cast> Cast { get; set; }

        public static MovieIllegalPropertyNames GetTestModel(string title)
        {
            var rnd = new Random();
            return new MovieIllegalPropertyNames
            {
                Title = title,
                Id = $"{rnd.Next(100)}-{title}",
                ReleaseDate = DateTime.Today,
                Budget = 1000000,
                Runtime = 121,
                Rating = Rating.GetTestRating(title),
                Cast = new List<Cast>(new[] { Models.Cast.GetTestMovieCast(title) })
            };
        }
    }

    /// <summary>
    /// A movie model that might represent a Graph vertex.
    /// </summary>
    public class MovieGraph
    {
        [Id]
        public string MovieId { get; set; }
        [PartitionKey]
        public string Title { get; set; }

        [Label]
        public string Label => "Movie";

        public DateTime ReleaseDate { get; set; }
        public int Runtime { get; set; }
        public long Budget { get; set; }

        public static MovieGraph GetTestModel(string title)
        {
            var rnd = new Random();
            return new MovieGraph
            {
                Title = title,
                MovieId = $"{rnd.Next(100)}-{title}",
                ReleaseDate = DateTime.Today,
                Budget = 1000000,
                Runtime = 121,
            };
        }
    }

    /// <summary>
    /// A more detailed movie model that represents a movie based on the sample data present in the testing suite.
    /// To be used for performance tests for inserting a lot of documents.
    /// </summary>
    public class MovieFull
    {
        [Id]
        public string TmdbId { get; set; }
        [PartitionKey]
        public string Title { get; set; }
        public string Tagline { get; set; }
        public string Overview { get; set; }

        public DateTime ReleaseDate { get; set; }

        public int Runtime { get; set; }
        public long Budget { get; set; }
        public long Revenue { get; set; }

        public string Language { get; set; }
        public string Genres { get; set; }
        public string Keywords { get; set; }

        public Rating Rating { get; set; }

        public List<Cast> Cast { get; set; }
    }

}