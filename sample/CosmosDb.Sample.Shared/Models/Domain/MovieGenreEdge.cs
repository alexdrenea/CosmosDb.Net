using CosmosDB.Net.Domain.Attributes;

namespace CosmosDb.Sample.Shared.Models.Domain
{
    [Label(Value = "isGenre")]
    public class MovieGenreEdge {
    }


    [Label(Value = "hasMovie")]
    public class GenreMovieEdge
    {
    }

}
