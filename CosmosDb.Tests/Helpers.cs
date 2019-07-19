using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CosmosDb.Tests
{
    public class Helpers
    {
        public static List<T> GetFromCsv<T>(string fName)
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
