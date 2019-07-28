[![Build Status](https://dev.azure.com/alexdrenea/CosmosDb.Net/_apis/build/status/CosmosDb.Net-CI?branchName=master)](https://dev.azure.com/alexdrenea/CosmosDb.Net/_build/latest?definitionId=11&branchName=master)
# CosmosDB.Net
CosmosDB.Net is a library that helps development against an Azure Cosmos DB database - SQL or Graph. It is a wrapper over the latest stable official [Azure Cosmos DB .NET SDK Version 3.0](https://github.com/Azure/azure-cosmos-dotnet-v3) as well as the [Gremlin.NET](https://github.com/apache/tinkerpop/tree/master/gremlin-dotnet) driver. 

Install using nuget: `nuget install Gremlin.Net`

### CosmosClientSql
The `CosmosClientSql` class is a wrapper over Cosmos Client that enables calls via the SQL API. The wrapper exposes the underlaying Cosmos SDK objects so a caller can access the full functionality of the SDK.

To get an instance of the client, use one of the static initializer methods found on the `CosmosClientSql` class:
 - `CosmosClientSql.GetByAccountEndpoint(...)` initializes a client based on the Account Endpoint (https://_yourAccountEndpoint_.documents.azure.com:443/) and Account Key
 - `CosmosClientSql.GetByConnectionString(...)` initializes a client based on a CosmosDB Connection String (AccountEndpoint=_yourAccountEndpoint_;AccountKey=_yourAccountKey_;
 - `CosmosClientSql.GetByAccountName(...)` initializes a client based on the Account name and Account Key

```csharp
 //Initialize a CosmosClientSql. If database or container do not exist, throw and exception.
 var sqlClient = await CosmosClientSql.GetByAccountName(accountName, accountKey, databaseId, containerId);
```

There are also options for the initializer to automatically create the database and collection in case they do not exist, in which case an additional parameter `CreateOptions` must be provided:

```csharp
 //Initialize a CosmosClientSql.
 //If database and container do not exist, initialize them with a 1000Ru Database throughput and a Partition Key under the `pk` property
 var sqlClient = await CosmosClientSql.GetByConnectionString(
					connectionString, 
					databaseId, 
					containerId, 
					new CreateOptions(databaseId, databaseId, "/pk") 
					{ 
						DatabaseThrouhput = 1000
					});
```

### CosmosClientGraph

The `CosmosClientGraph` class is a higher lever wrapper that encapsulates the `CosmosClientSql` client as well as a `GremlinServer` instance from Gremlin.NET. The built-in `CosmosEntitySerializer` offers seamless transformations to and from the internal Cosmos DB Graphson format that is used to create a "Vertex" or an "Edge".

To get an instance of the client, use one of the two initializer methods on the `CosmosClientGraph` class.
- `CosmosClientGraph.GetClientWithSql(...)` returns a client that is backed by both Gremlin.NET and CosmosClient 
- `CosmosClientGraph.GetClient(...)` returns a client that is backed just by a Gremlin.NET  GremlinServer instance. With this client, only Gremlin Traversals are allowed.

```csharp
//Initialize a graph client
var graphClient = CosmosClientGraph.GetClientWithSql(accountName, accountKey, databaseId, containerId);
```

### CosmosResponse

The library exposes a built-in response construct as a result for all calls called `CosmosResponse`. It encapsulates a result or an exception so you don't have to try catch, just check if the result is Successful or not.
CosmosResponse includes other useful metrics, either calculated internally (such as `ExecutionTimeMs`) or returned by the CosmosClient calls (such as `RequestCharge`, `ContinuationToken` or `ETag`). 


# 1. Model annotations

To be valid Cosmos Documents, your data models must expose certain properties such as id. Additionally, if you are using a Partitioned collection, you must define which property of your Model will be used for partitioning. That property is usually called Partition Key, but you can call it anything you like in your data model. This isn't a big problem when you only have one model / data type that you want to insert in a collection, but it becomes a problem when you want to share that collection for multiple document types - which is a recommended data modeling pattern for Cosmos DB.

CosmosDb.NET offers an elegant solution for this problem which won't require any changes to your existing POCOs and Domain classes. You can use model annotations to specify which property will be the Partition Key of the entity, you can use the Id attribute to define which property will serve as the Id, or you can annotate the entire class with the Label attribute to define a label for the object so it can be properly deserialized back when retrieving.

### Example
 
``` csharp
[Label("Value = Movie")]
public class MovieModel
{
	[Id]
	public string MovieId { get; set; }

	[PartitionKey]
	public string Title { get; set; }
	
	public string Tagline { get; set; }
	public string Overview { get; set; }

	public DateTime ReleaseDate { get; set; }
}
 
public class Cast
{
	[Id]
	public string Id => $"{MovieTitle}-{Order}";

	[PartitionKey]
	public string ActorName { get; set; }

	public string MovieTitle { get; set; }
	public string MovieId { get; set; }

	public string CharacterName { get; set; }
}

public class Actor
{
	[PartitionKey]
	public string Name { get; set; }
}
```

In the example above:
 - The `MovieModel` object will have the label "Movie" in the database, the value of the `Title` property will be the Partition Key and `id` will be the value of `MovieId`.
 - The `Actor` object will just be called "Actor" in the database (no label attribute means just use the class Name as the label) as well as since there is no id property defined, a GUID will be generated for each entry.
 - Each document has a different Partition Key property, but the CosmosDB.NET will transform and map those properties to the property that was defined at the container level.


### Remarks
1.  When you annotate a property with one of the attributes, the final document that is inserted in the database will contain both the original property name and the annotated one:
 For the example above, the movie document will look like this (assuming we're inserting in a collection with PartitionKeyPath = 'PartitionKey':
 
```csharp
{
    "label": "Movie",
    "id": "58",
    "PartitionKey": "Avatar",
    "MovieId": "58",
    "Title": "Avatar",
    "Tagline": "Jack is back!",
    "Overview": "Captain Jack Sparrow works his way out of a blood debt with the ghostly Davey Jones, he also attempts to avoid eternal damnation.",
    "ReleaseDate": "2006-06-20T00:00:00"
}
```

2. Enums are inserted as their integer value. Be careful when updating your enums -> make sure you mark them with explicit integer values so they don't get messed up if you insert a new value not at the end of the enums.


# Seamless Multi API for a graph database (SQL or Gremlin)

When connecting to a graph enabled collection using the `CosmosClientGraph`, the caller has the ability to seamlessly use either SQL Queries via the SQL API or Gremlin Traversals via the GremlinServer/Client to get data into or out of your database.


### Insert / Upsert data
Inserting and updating data difficult and prone to error, especially since the current version of CosmosDB does not support the fluent API present in the Gremlin.NET driver.

With CosmosDb.NET, with the help of the internal `CosmosEntitySerializer` you can insert vertices and edges by simply providing the instance of the data object you want to insert/upsert as such:

```csharp
var actorResult = await _cosmosClientGraph.InsertVertex(actor);
var castResult = await _comosClientGraph.InsertVertex(cast);

//Create an edge between an actor and cast.
var edgeResult = await _cosmosClientGraph.InsertEdge(new PlayedInEdge(), actor, cast);
```

**Notes:** 
At the moment, the library only supports basic data types as edge properties (numbers, string, bool, datetime). Complex structures (nested objects, arrays, dictionaries) might be supported at a later time.


### Read / Query data

Reading a document with a known Id and Partition key is the most basic and fastest operation in Cosmos DB. CosmosDB.NET exposes the Read operation for both SQL and Gremlin databases with automatic mapping to the original model.

```csharp
//**SQL:**
var movieResult = await _sqlClient.ReadDocument<Movie>(movie.MovieId, movie.Title);

//**Gremlin:**
var movieResult = await _graphClient.ReadVertex<Movie>(movie.MovieId, movie.Title);
```

On a Graph Database, callers can use either Gremlin or Sql to query data:

```csharp
//**SQL:**
var sqlMoviesResult = await _sqlClient.ExecuteSQL<Movie>("select * from c where c.label = 'Movie'");
//

//**Gremlin:**
var graphMoviesResult1 = await _graphClient.ExecuteGremlin<Movie>("g.V().hasLabel('Movie')");

var graphMoviesResult2 = await _graphClient.ExecuteSQL<Movie>("select * from c where c.label = 'Movie'");
```

Lastly, there’s a helper method to get all objects of the same type and filter:

```csharp
//**SQL:**
var moviesResult = await _sqlClient.ReadDocuments<T>

//**Gremlin:**
var movieResult = await _graphClient.ReadVertices<Movie>()
```

These calls are equivalent to running a query that returns all documents with a specific Label. The label is extracted based on the Type T that is provided at runtime (via the Label annotation or the class name).

Check the samples projects for more examples and tests.

### Parametrized traversals

The `CosmosClientGraph` supports parametrized gremlin traversals. If you have traversals that you run multiple times with different parameters, it's recommended to use parameter bindings so that the compiled version of the query can be cached by the server.

```csharp
//Get all movies that an actor played in:
var actor = "Bruce Willis";

//Non-parametrized query
var movies_noParams = await _graphClient.ExecuteGremlin<Movie>($"g.V().hasLabel('Actor').has('PartitionKey','{actor}').out().in()");

//Parametrized query
var movies = await _graphClient.ExecuteGremlin<Movie>(
        "g.V().hasLabel('Actor').has('PartitionKey', actor).out().in()", 
        new Dictionary<string, object> { { "actor", actor} });
```

The difference is very subtle, but the idea is that in the second example, we are passing `actor` without any quotes around it so it's not a literal value, and then we are defining the `actor` value in the parameters dictionary that we send along with the query


# Bulk inserts 

Whether you need to do an initial data load, a migration or a nightly data dump, often time you need to insert many entities in your database at once. 

The easiest way might be to just await each of the insert each item. This only scales to a few entries since it doesn't take advantage of any parallel execution strategies. 
Another way might be to collect all the tasks in an array and await them all at once. However for many (more than 20-50) elements, there will be Task timeout exceptions because all tasks are fired at CosmosDB at the same time so the RU usage will spike for the initial call then exponential back-off will retry until the underlaying Task will get cancelled due to the waits. 

```csharp
var tasks = modelsToInsert.Select(m=>_sqlClient.InsertVertex(m));
var result = await Task.WhenAll(tasks);
//-> some of the results in `result` will contain a Task Timeout exception when more than 50 elements present in the source collection. 
```
 
The best solution for this scenarios is [Dataflow Task Parallel Library (TPL)](https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/dataflow-task-parallel-library) that enables a producer-consumer pattern that is fully parallelized and optimized. CosmosDb.Net offers an implementation of TPL and exposes it via methods available on both the `CosmosClientSql` and `CosmosClientGraph` clients.

```csharp
var movies = DataLoader.LoadMovies();
var insertResult = await _cosmosClient.InsertDocuments(
                          movies,
                          (progress) => { Console.WriteLine($"inserted {progress.Count()} documents"); },
                          reportingInterval: TimeSpan.FromSeconds(10),
                          threads: 4
                        );
```

![Load Performance SQL](https://res.cloudinary.com/alex-tech-blog/image/upload/v1564281703/Blog/2019.07/sql_load_1000_movies_frhrhs.png)
![Load Performance Gremlin](https://res.cloudinary.com/alex-tech-blog/image/upload/v1564281703/Blog/2019.07/gremlin_load_1000_movies_t88fvc.png)
> on a collection with 10.000 RUs

**Notes:** 
Similarly, use `UpsertDocuments`, `InsertVertex`, `UpsertVertex`, `InsertEdges` or `UpsertEdges` methods, with the same signature and features.
 
Edges are a bit more complicated, since they need 3 components to be created: The edge model, the source vertex and the target vertex.
To solve that the library exposes `EntityDefinition` object as a way to combine those 3 elements and then call the `InsertEdges()` method.
 
```csharp
//Load movies
var movies = DataLoader.LoadMovies();
//Load cast and group them by movie
var cast = DataLoader.LoadCast().GroupBy(c => c.MovieId).ToDictionary(k => k.Key, v => v);
//Create edge definitions
var edgeDefinitions = movies.Select(movie => cast[movie.MovieId].SelectMany(c => 
                                                  new EdgeDefinition(
                                                      _graphClient.CosmosSerializer.ToGraphItemBase(m),
                                                      new GraphItemBase(cast.Id, cast.ActorName, "Cast")));

var edgeResults = await _graphClient.InsertEdges(
                            movies,
                            (progress) => { Console.WriteLine($"inserted {progress.Count()} edges"); },
                            reportingInterval: TimeSpan.FromSeconds(10),
                            threads: 4
                          );
```
