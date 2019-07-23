namespace CosmosDb.Domain
{
    public class GraphItemBase
    {
        public GraphItemBase() { }

        public GraphItemBase(string id, string partitionKey, string label)
        {
            Id = id;
            PartitionKey = partitionKey;
            Label = label;
        }

        public string Id { get; set; }
        public string PartitionKey { get; set; }
        public string Label { get; set; }
    }
}
