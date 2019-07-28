# CosmosDB.Net Samples

 The samples demosntrate how to use the `CosmosDB.Net` library in a real life use case. We are builing an IMBD like application with a list of movies, cast and actors.
 
 There are 2 samples, one that uses an Azure Cosmos DB SQL collection and one that uses a Graph database for the same usecase. Each sample will demonstrate how to perform basic operations on each instance.
 The entities entered in each sample are going to be identical, but we will use a different data model in the graph database to take advantage of traversal strategies for solving different use cases.
 
 ### Entities

``` csharp
 public class Movie
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
    public List<string> Genres { get; set; }
    public List<string> Keywords { get; set; }

    public decimal AvgRating { get; set; }
    public int Votes { get; set; }

    public MovieFormat Format { get; set; }
}
 
public class Cast
{
    [Id]
    public string Id => $"{MovieTitle}-{Order}";

    [PartitionKey]
    public string ActorName { get; set; }

    public string MovieTitle { get; set; }
    public string MovieId { get; set; }

    public string Character { get; set; }
    public int Order { get; set; }
    public bool Uncredited { get; set; }
}

public class Actor
{
	[PartitionKey]
	public string Name { get; set; }
}
```

### Operations

The console samples allow you to run various commands against the database and it's easy to add some more for extra use cases. Most notable, it shows how to load data in the database and run basic queries using the library.
Use the '?' or 'help' command to get more information when running the sample.