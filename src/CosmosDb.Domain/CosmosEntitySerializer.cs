using CosmosDB.Net.Domain.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CosmosDB.Net.Domain
{
    public class CosmosEntitySerializer
    {
        internal enum BaseProperties
        {
            Id,
            Label,
            PartitionKey,
        }

        /// <summary>
        /// Defines a set of attributes that mark properties that will be ignored during the object decomposition process.
        /// If we add the cosmos attributes in that collection, then the output document will omit the properties that are marked with an attribute
        /// i.e. if we mark Title as [Id] then Title will not exsit int he output document, only Id will.
        /// Leaving those commented, means the output document will contain both Title and id properties. 
        /// </summary>
        private static string[] _ignoredPropertyAttributes = new[] { "JsonIgnoreAttribute", "IgnoreDataMemberAttribute" /*,"PartitionKeyAttribute"*/ /*,"LabelAttribute"*/ /*,"IdAttribute"*/ };


        private readonly string _partitionKeyPropertyName = "PartitionKey";
        private readonly string _idPropertyName = "id";

        private IReadOnlyDictionary<BaseProperties, string> _propertyNamesMap;

        /// <summary>
        /// Defines a set of properties that the convertes will add on to the result set as the default properties
        /// </summary>
        private IEnumerable<string> _ignoredPropertyNames;

        public CosmosEntitySerializer(string partitionKeyPropertyName = "PartitionKey")
        {
            _partitionKeyPropertyName = partitionKeyPropertyName;
            _propertyNamesMap = new Dictionary<BaseProperties, string>
            {
                { BaseProperties.Id, "id" },
                { BaseProperties.Label, "label" },
                { BaseProperties.PartitionKey, _partitionKeyPropertyName }
            };
            _ignoredPropertyNames = _propertyNamesMap.Values.Select(p => p.ToLower()).ToArray();
        }

        /// <summary>
        /// A static instance of the Serializer that uses the default 'PartitionKey' as the PartitionKeyPropertyname. 
        /// </summary>
        public static CosmosEntitySerializer Default => new CosmosEntitySerializer();

        /// <summary>
        /// Returns a GraphItemBase object (id, label, pk) for a given entity that is annotated with the needed attributes.
        /// </summary>
        /// <param name="entity">entity to extract GraphItemBase object from</param>
        public GraphItemBase ToGraphItemBase<T>(T entity)
        {
            var props = GetObjectPropValues(entity, expandAllProps: false);
            return new GraphItemBase
            {
                Id = props[_propertyNamesMap[BaseProperties.Id]].ToString(),
                Label = props[_propertyNamesMap[BaseProperties.Label]].ToString(),
                PartitionKey = props[_propertyNamesMap[BaseProperties.PartitionKey]].ToString()
            };
        }

        /// <summary>
        /// Converts a given entity to a dynamic representation that can be used to insert the document in the database as a CosmosDB Document
        /// The model must define key properties (id, label, partition key) via attributes.
        /// </summary>
        /// <remarks>
        /// If an Id property is not identified by using the <see cref="CosmosDb.Attributes.IdAttribute">IdAttribute</see> then a Guid will be generated for the id/>
        /// If an Label property is not identified by using the <see cref="CosmosDb.Attributes.LabelAttribute">LabelAttribute</see> the class name will be assigned to the label label property/>
        /// If an PartitionKey property is not identified by using the <see cref="CosmosDb.Attributes.PartitionKeyAttribute">PartitionKeyAttribute</see> then an exception will be thrown/>
        /// </remarks>
        /// <returns>
        /// dynamic object containing a key-value collection of a cosmosDB document entry in a CosmosDB Graph collection
        /// </returns>
        public IDictionary<string, object> ToCosmosDocument<T>(T entity)
        {
            return GetObjectPropValues(entity);
        }

        /// <summary>
        /// Converts a given entity to a dynamic representation that can be used to insert the document in the database as a CosmosDB Document
        /// The model must define key properties (id, label, partition key) via attributes.
        /// </summary>
        /// <remarks>
        /// If an Id expression is not passed in then a Guid will be generated for the id property/>
        /// If a Label expression is not passed, will look for a Label Attribute at the class level on T, if that's not found then the class name will be assigned to the label label property/>
        /// If an PartitionKey expression is not passed in then an exception will be thrown/>
        /// </remarks>
        /// <returns>
        /// dynamic object containing a key-value collection of a cosmosDB document entry in a CosmosDB Graph collection
        /// </returns>
        public IDictionary<string, object> ToCosmosDocument<T>(T entity, Expression<Func<T, object>> pkProperty, Expression<Func<T, object>> idProp = null, Expression<Func<T, object>> labelProp = null)
        {
            return GetObjectPropValuesManual(entity, pkProperty, idProp, labelProp);
        }


        /// <summary>
        /// Converts a given entity to a dynamic representation that can be used to insert the document in the database as a Graph Vertex.
        /// The model does not to be annotated with Attributes defining the key properties, but you need to provide an expression that points to properties you want to use.
        /// </summary>
        /// <remarks>
        /// If an Id expression is not passed in then a Guid will be generated for the id property/>
        /// If a Label expression is not passed, will look for a Label Attribute at the class level on T, if that's not found then the class name will be assigned to the label label property/>
        /// If an PartitionKey expression is not passed in then an exception will be thrown/>
        /// </remarks>
        /// <returns>
        /// dynamic object containing a key-value collection of a graph vertex entry in a CosmosDB Graph collection
        /// </returns>
        /// <example>
        /// ToGraphVertex(entity, pkProperty: entity => entity.Title, labelProp: entity => entity.ItemType, idProp: entity => entity.EntityId) 
        /// </example>
        public IDictionary<string, object> ToGraphVertex<T>(T data, Expression<Func<T, object>> pkProperty, Expression<Func<T, object>> idProp = null, Expression<Func<T, object>> labelProp = null)
        {
            var vertexProps = GetObjectPropValuesManual(data, pkProperty, idProp, labelProp);
            return ToGraphVertexInternal(vertexProps);
        }


        /// <summary>
        /// Converts a given entity to a dynamic representation that can be used to insert the document in the database as a Graph Vertex.
        /// The model must define key properties (id, label, partition key) via attributes.
        /// </summary>
        /// <remarks>
        /// If an Id property is not identified by using the <see cref="CosmosDb.Attributes.IdAttribute">IdAttribute</see> then a Guid will be generated for the id/>
        /// If an Label property is not identified by using the <see cref="CosmosDb.Attributes.LabelAttribute">LabelAttribute</see> the class name will be assigned to the label label property/>
        /// If an PartitionKey property is not identified by using the <see cref="CosmosDb.Attributes.PartitionKeyAttribute">PartitionKeyAttribute</see> then an exception will be thrown/>
        /// </remarks>
        /// <returns>
        /// dynamic object containing a key-value collection of a graph vertex entry in a CosmosDB Graph collection
        /// </returns>
        public IDictionary<string, object> ToGraphVertex<T>(T entity)
        {
            var vertexProps = GetObjectPropValues(entity);
            return ToGraphVertexInternal(vertexProps);
        }

        /// <summary>
        /// Generates a graph vertex model representation that can be inserted in a CosmosDB collection.
        /// The required format needs 'id','labe' and the defined PropertyKey property name (usually 'partitionKey') as simple key-value properties.
        /// All other additional properties of the Vertex must be stored in a specific Graphson format as such:
        /// Key: propertyName
        /// Value (array of value details containing id, _value, meta properties) 
        /// [
        ///  {
        ///    "id": _uniqueId
        ///    "_value": _actualPropertyValue
        ///     "meta": _metadata (can be null)
        ///  }
        /// ]
        /// </summary>
        private IDictionary<string, object> ToGraphVertexInternal(IDictionary<string, object> vertexProps)
        {
            //https://docs.microsoft.com/en-us/azure/cosmos-db/gremlin-support
            // Cosmos does not support complex graph properties. Those need to be serialized as a string when encountered.

            var res = new Dictionary<string, object>();
            res[_propertyNamesMap[BaseProperties.Id]] = vertexProps[_propertyNamesMap[BaseProperties.Id]].ToString();
            res[_propertyNamesMap[BaseProperties.Label]] = vertexProps[_propertyNamesMap[BaseProperties.Label]].ToString();
            res[_propertyNamesMap[BaseProperties.PartitionKey]] = vertexProps[_partitionKeyPropertyName].ToString();

            foreach (var prop in vertexProps.Where(p => !_ignoredPropertyNames.Contains(p.Key.ToLower())))
            {
                var value = prop.Value;
                if (!IsTypeDirectlySerializableToGraph(value.GetType()))
                    value = JsonConvert.SerializeObject(prop.Value);

                res[prop.Key] = new[]
                {
                    new
                    {
                        id = Guid.NewGuid().ToString(),
                        _value = value,
                        _meta = new Dictionary<string, object>()
                    }
                };
            }

            return res;
        }

        /// <summary>
        /// Converts a given entity to a dynamic representation that can be used to insert the document in the database as a Graph Edge.
        /// The entity does not need to have any annotations, everything will be inherited from the vertices it connects.
        /// The source and destination vertices must be provided.
        /// </summary>
        /// <param name="single">
        /// Specifies if there can only be one edge of this kind between the 2 vertices
        /// i.e an edge defining a 'isFriend' relationship between 2 people needs to be singe:true because only one friend edge makes sense.
        /// i.e an edge defining a 'visited' relationship between a person and a restaurant needs to be single:false because a person can visit the restaurant multiple times
        /// </param>
        /// <returns>
        /// dynamic object containing a key-value collection of a graph edge entry in a CosmosDB Graph collection
        /// </returns>
        public IDictionary<string, object> ToGraphEdge<T, U, V>(T entity, U source, V target, bool single = false)
        {
            return ToGraphEdge<T>(entity, ToGraphItemBase(source), ToGraphItemBase(target), single);
        }

        /// <summary>
        /// Converts a given entity to a dynamic representation that can be used to insert the document in the database as a Graph Edge.
        /// The entity does not need to have any annotations, everything will be inherited from the vertices it connects.
        /// Only the base properties (id,label, partitionKey) of the source and destination vertice needs to be provided.
        /// </summary>
        /// <param name="single">
        /// Specifies if there can only be one edge of this kind between the 2 vertices
        /// i.e an edge defining a 'isFriend' relationship between 2 people needs to be singe:true because only one friend edge makes sense.
        /// i.e an edge defining a 'visited' relationship between a person and a restaurant needs to be single:false because a person can visit the restaurant multiple times
        /// </param>
        /// <returns>
        /// dynamic object containing a key-value collection of a graph edge entry in a CosmosDB Graph collection
        /// </returns>
        public IDictionary<string, object> ToGraphEdge<T>(T entity, GraphItemBase source, GraphItemBase target, bool single = false)
        {
            var entityProps = GetObjectPropValues(entity, throwOnEmptyPartitionKey: false, generateIdIfEmpty: !single);
            if (single && string.IsNullOrEmpty(entityProps[_propertyNamesMap[BaseProperties.Id]].ToString()))
                entityProps[_propertyNamesMap[BaseProperties.Id]] = $"{source.Id}-{target.Id}";

            var res = new Dictionary<string, object>();
            res[_propertyNamesMap[BaseProperties.Id]] = entityProps[_propertyNamesMap[BaseProperties.Id]].ToString();
            res[_propertyNamesMap[BaseProperties.Label]] = entityProps[_propertyNamesMap[BaseProperties.Label]].ToString();
            res[_propertyNamesMap[BaseProperties.PartitionKey]] = source.PartitionKey;
            res["_isEdge"] = true;
            res["_vertexId"] = source.Id;
            res["_vertexLabel"] = source.Label;
            res["_sink"] = target.Id;
            res["_sinkLabel"] = target.Label;
            res["_sinkPartition"] = target.PartitionKey;

            foreach (var prop in entityProps.Where(p => !_ignoredPropertyNames.Contains(p.Key.ToLower())))
            {
                //TODO: Handle complex types. Either throw an exception or handle it correctly
                res[prop.Key] = prop.Value;
            }

            return res;
        }


        /// <summary>
        /// When reading a vertex from a graph database using either SQL or Gremlin.NET Api, we need to convert the incoming graphson result into the model.
        /// IF the  caller does not know the type and calls this with a generic type (JObject, object or dynamic)
        /// </summary>
        public JObject FromGraphsonToJobject(JObject document)
        {
            if (document == null) return default(JObject);
            var entity = new JObject();

            //If document was read via the Sql API, then its representation will contain all properties at the root of the JObject, and the values will be found under the `_value` property
            var graphsonValuePropName = "_value";
            var graphson = document;
            //If document was read via the Gremlin API, then its representation will contain all properties in a 'properties' JObject, and the values will be found under the `value` property
            var properties = document["properties"] as JObject;
            if (properties != null)
            {
                graphson = properties;
                graphsonValuePropName = "value";
            }

            foreach (var p in graphson)
            {
                var value = p.Value.FirstOrDefault()?[graphsonValuePropName]?.ToString() ?? p.Value.ToString();
                entity.Add(p.Key, JToken.FromObject(value));
            }

            return entity;
        }

        /// <summary>
        /// When reading a vertex from a graph database using either SQL or Gremlin.NET Api, we need to convert the incoming graphson result into the model.
        /// </summary>
        public T FromGraphson<T>(JObject document)
        {
            if (document == null) return default(T);
            var dataType = typeof(T);
            var entity = (T)Activator.CreateInstance(dataType);
            var allProps = dataType.GetRuntimeProperties().ToList();

            //If document was read via the Sql API, then its representation will contain all properties at the root of the JObject, and the values will be found under the `_value` property
            var graphsonValuePropName = "_value";
            var graphson = document;
            //If document was read via the Gremlin API, then its representation will contain all properties in a 'properties' JObject, and the values will be found under the `value` property
            var properties = document["properties"] as JObject;
            if (properties != null)
            {
                graphson = properties;
                graphsonValuePropName = "value";
            }

            foreach (var p in graphson)
            {
                var dataProp = allProps.FirstOrDefault(x => x.Name == p.Key);
                var value = p.Value.FirstOrDefault()?[graphsonValuePropName]?.ToString() ?? p.Value.ToString();
                if (dataProp != null && !string.IsNullOrEmpty(value))
                {
                    try
                    {
                        object serializedValue = null;

                        if (dataProp.PropertyType.GetTypeInfo().BaseType == typeof(Enum))
                        {
                            //Enums have to be parsed since we're storing their integer value.
                            dataProp.SetValue(entity, Convert.ChangeType(value, typeof(int)));
                        }
                        else if (IsTypeDirectlySerializableToGraph(dataProp.PropertyType) && dataProp.PropertyType.GetTypeInfo().ImplementedInterfaces.Contains(typeof(System.IConvertible)))
                        {   //can be converted directly
                            serializedValue = Convert.ChangeType(value, dataProp.PropertyType);
                        }
                        else
                        {   //try a json serializer
                            serializedValue = JsonConvert.DeserializeObject(value, dataProp.PropertyType);
                        }

                        dataProp.SetValue(entity, serializedValue);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Can't set value '{value}' to property '{dataProp.Name}'. Error: {e.Message}");
                    }
                }
            }

            return entity;
        }


        /// <summary>
        /// Sanitizes a value for it to become valid as a PartitionKey or ID.
        /// </summary>
        /// <param name="value">value to sanitize</param>
        /// <returns>sanitized value</returns>
        public static string SanitizeValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;

            //TODO: a sanitization metod can be used to convert raw values for id and pk into friendlier version.
            //i.e. create slug, trim, toLower().

            return value;
        }

        public static string GetLabelForType(Type dataType)
        {
            return dataType.GetTypeInfo().GetCustomAttribute<LabelAttribute>()?.Value ?? dataType.Name;
        }

        /// <summary>
        /// Given object entity, transform it into a colleciton of key -  values
        /// Replace the property names according to the custom id/label/partition key attributes
        /// </summary>
        /// <param name="expandAllProps">true to expand app properties of the object, false to just extract id/label/partition key</param>
        /// <param name="allowEmptyPartitionKey"></param>
        /// <param name="defaultId">
        /// Value to be used as a custom id, if an id property is not present.
        /// If left null, and an ID property is not found on the object, will generate a GUID for id.
        /// If a label attribute is not present, a label property will be generated and its value will be the class name.
        /// </param>
        private IDictionary<string, object> GetObjectPropValues<T>(
            T entity, 
            bool expandAllProps = true, 
            bool throwOnEmptyPartitionKey = true, 
            bool generateIdIfEmpty = true)
        {
            var allProps = entity.GetType().GetRuntimeProperties()
                                           .Where(p => !p.CustomAttributes.Any(a => _ignoredPropertyAttributes.Contains(a.AttributeType.Name))).ToArray();

            var pkProp = allProps.Where(p => p.CustomAttributes.Any(a => a.AttributeType.Name == "PartitionKeyAttribute"));
            var labelProp = allProps.Where(p => p.CustomAttributes.Any(a => a.AttributeType.Name == "LabelAttribute"));
            var idProp = allProps.Where(p => p.CustomAttributes.Any(a => a.AttributeType.Name == "IdAttribute"));

            if (pkProp.Count() > 1)
                throw new Exception("More than 1 PartitionKey property defined.");
            if (labelProp.Count() > 1)
                throw new Exception("More than 1 Label property defined.");
            if (idProp.Count() > 1)
                throw new Exception("More than 1 Id property defined.");

            return GetObjectPropValuesInternal(
                   entity,
                   allProps,
                   pkProp.FirstOrDefault(),
                   idProp.FirstOrDefault(),
                   labelProp.FirstOrDefault(),
                   expandAllProps,
                   throwOnEmptyPartitionKey,
                   generateIdIfEmpty
            );
        }


        private IDictionary<string, object> GetObjectPropValuesManual<T>(
            T entity, 
            Expression<Func<T, object>> pkProperty = null, 
            Expression<Func<T, object>> idProp = null, 
            Expression<Func<T, object>> labelProp = null, 
            bool expandAllProps = true)
        {
            var dataType = entity.GetType();
            var propertyDefinitions = dataType.GetRuntimeProperties()
                                       .Where(p => !p.CustomAttributes.Any(a => _ignoredPropertyAttributes.Contains(a.AttributeType.Name))).ToArray();

            return GetObjectPropValuesInternal(
                    entity,
                    propertyDefinitions,
                    propertyDefinitions.FirstOrDefault(f => f.Name == pkProperty.GetName()),
                    propertyDefinitions.FirstOrDefault(f => f.Name == idProp.GetName()),
                    propertyDefinitions.FirstOrDefault(f => f.Name == labelProp.GetName()),
                    expandAllProps
            );
        }

        private IDictionary<string, object> GetObjectPropValuesInternal<T>(
            T entity,
            IEnumerable<PropertyInfo> allProperties,
            PropertyInfo pkPropertyInfo = null,
            PropertyInfo idPropertyInfo = null,
            PropertyInfo labelPropertyInfo = null,
            bool expandAllProps = true,
            bool throwOnEmptyPartitionKey = true,
            bool generateIdIfEmpty = true)
        {
            var res = new Dictionary<string, object>();
            var dataType = entity.GetType();
            if (!(allProperties?.Any() == true))
                allProperties = dataType.GetRuntimeProperties().Where(p => !p.CustomAttributes.Any(a => _ignoredPropertyAttributes.Contains(a.AttributeType.Name)));

            if (pkPropertyInfo == null) pkPropertyInfo = allProperties.FirstOrDefault(f => f.Name == _partitionKeyPropertyName);
            if (idPropertyInfo == null) idPropertyInfo = allProperties.FirstOrDefault(f => f.Name == _idPropertyName);

            res[_propertyNamesMap[BaseProperties.PartitionKey]] =
                       SanitizeValue(pkPropertyInfo?.GetValue(entity)?.ToString())
                    ?? (throwOnEmptyPartitionKey ? throw new Exception("PartitionKey property must have a non-empty value") : string.Empty);

            res[_propertyNamesMap[BaseProperties.Id]] = SanitizeValue(idPropertyInfo?.GetValue(entity)?.ToString())
                    ?? (generateIdIfEmpty ? Guid.NewGuid().ToString() : string.Empty);
            
            res[_propertyNamesMap[BaseProperties.Label]] = 
                        labelPropertyInfo?.GetValue(entity, null)?.ToString() 
                    ?? dataType.GetTypeInfo().GetCustomAttribute<LabelAttribute>()?.Value 
                    ?? dataType.Name;

            if (expandAllProps)
            {
                var properties = allProperties.Where(p => !_ignoredPropertyNames.Contains(p.Name.ToLower()))
                                              .Select(p => new KeyValuePair<string, object>(p.Name, p.GetValue(entity) ?? ""));
                foreach (var p in properties)
                    if (!res.ContainsKey(p.Key)) res.Add(p.Key, p.Value);
            }

            return res;
        }


        public static bool IsTypeDirectlySerializableToGraph(Type t)
        {
            var ti = t.GetTypeInfo();
            return ti.IsPrimitive || ti.IsEnum || t == typeof(DateTime) || t == typeof(string);
        }
    }
}
