namespace CosmosDb.Domain
{
    public class GraphItemBase
    {
        public string Label { get; set; }
        public string Id { get; set; }
        public string PartitionKey { get; set; }
    }
}
