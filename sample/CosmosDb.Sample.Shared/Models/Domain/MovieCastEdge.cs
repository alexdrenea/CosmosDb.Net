using CosmosDB.Net.Domain.Attributes;

namespace CosmosDb.Sample.Shared.Models.Domain
{
    [Label(Value = "hasCast")]
    public class MovieCastEdge
    {
        public int Order { get; set; }
            }
}
