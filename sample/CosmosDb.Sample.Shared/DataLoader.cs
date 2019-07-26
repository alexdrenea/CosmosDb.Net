using CosmosDb.Sample.Shared.Models.Csv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CosmosDb.Sample.Shared
{
    public static class DataLoader
    {
        public static List<MovieCsv> LoadMovies()
        {
            return GetDataFromCsv<MovieCsv>("Data/movies_lite.csv");
        }

        public static List<ActorCsv> LoadCast()
        {
            return GetDataFromCsv<ActorCsv>("Data/movies_cast_lite.csv");
        }

        public static List<T> GetDataFromCsv<T>(string fName)
        {
            using (var reader = new StreamReader(fName))
            {
                using (var csv = new CsvHelper.CsvReader(reader))
                {
                    csv.Configuration.ReadingExceptionOccurred = (ex) =>
                    {
                        return true;
                    };
                    return csv.GetRecords<T>().ToList();
                }
            }
        }
    }
}
