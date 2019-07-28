# Graph Console Sample

Demonstrates usage of the CosmosClient Graph in a console app with various commands.

### Data Model

Using the base entities, the datamodel for the graph sample also creates edges and exposes a couple more vertices for easy graph traversals:

```
  +-------+                      +------+                      +-------+                      +-------+
  |       |                      |      |                      |       |                      |       |
  | Actor |-----playedIn-------->| Cast |<------hasCast--------| Movie |-----hasKeyword------>|Keyword|
  |       |                      |      |                      |       |                      |       |
  +-------+                      +------+                      +-------+                      +-------+
                                                                        \                     +-------+
                                                                         \                    |       |
                                                                          `---isGenre-------->| Genre |
                                                                                              |       |
                                                                                              +-------+
```

