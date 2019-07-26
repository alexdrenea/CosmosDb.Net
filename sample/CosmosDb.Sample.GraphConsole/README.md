### Graph Console

Demonstrates usage of the CosmosClient Graph in a console app with various commands.

##

Highlights 
 - example of data loading with performance stats to show various optimization modes. (Load movies, keywords, actors)
 - example of traversals using gremlin,
 - example of queries using sql

 - example of custom response types - for traversals that don't return predefined models

Use the LoadKeywords or LoadMovies commands to analyze the performance of bulk inserts. 
Keywords is a very small document. Movie is a larger size document.

-- show stats output from insetring 1000 keyworks with 4, 8, 16 threads.

-- show stats output from inserting 1000 movies with 4, 8, 16 threads


- inserting edges:
 -one by one using the _cosmosClient.UpsertEdge(edge, source, target, single);
 - multuiple at oince 
   - create EdgeDefinition instances that contain edge, source, target single,
   - use the _Cosmosclient.upsertEdges method, that works just like vertices.