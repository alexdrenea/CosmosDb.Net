using System;

namespace CosmosDB.Net.Domain.Attributes
{
    public class LabelAttribute : Attribute
    {
        public string Value { get; set; }
    }
}
