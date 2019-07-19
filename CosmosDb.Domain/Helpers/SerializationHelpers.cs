using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CosmosDb.Domain.Helpers
{
    public static class SerializationHelpers
    {
        //TODO: pass this in from the CosmosClient
        private static string _partitionKeyPropertyName = "PartitionKey";

        /// <summary>
        /// Defines a set of attributes that mark properties that will be ignored during the object decomposition process.
        /// </summary>
        //private static string[] _ignoredPropertyAttributes = new[] { "PartitionKeyAttribute", "LabelAttribute", "IdAttribute", "JsonIgnoreAttribute", "IgnoreDataMemberAttribute" };
        private static string[] _ignoredPropertyAttributes = new[] { "JsonIgnoreAttribute", "IgnoreDataMemberAttribute" };

        internal enum KnownProperties
        {
            Id,
            Label,
            PartitionKey,
        }

        private static Dictionary<KnownProperties, string> _propertyNames = new Dictionary<KnownProperties, string>
        {
            { KnownProperties.Id, "id" },
            { KnownProperties.Label, "label" },
            { KnownProperties.PartitionKey, _partitionKeyPropertyName }
        };

        /// <summary>
        /// Defines a set of properties that the convertes will add on to the result set as the default properties
        /// </summary>
        private static string[] _ignoredPropertyNames = _propertyNames.Values.Select(p=>p.ToLower()).ToArray();



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
        public static IDictionary<string, object> ToCosmosDocument<T>(this T entity)
        {
            return GetObjectPropValues(entity);
        }

        /// <summary>
        /// Converts a given entity to a dynamic representation that can be used to insert the document in the database as a CosmosDB Document
        /// The model must define key properties (id, label, partition key) via attributes.
        /// </summary>
        /// <remarks>
        /// If an Id expression is not passed in then a Guid will be generated for the id property/>
        /// If a Label property is not passed in then the class name will be assigned to the label label property/>
        /// If an PartitionKey expression is not passed in then an exception will be thrown/>
        /// </remarks>
        /// <returns>
        /// dynamic object containing a key-value collection of a cosmosDB document entry in a CosmosDB Graph collection
        /// </returns>
        public static IDictionary<string, object> ToCosmosDocument<T>(this T entity, Expression<Func<T, object>> pkProperty, Expression<Func<T, object>> labelProp = null, Expression<Func<T, object>> idProp = null)
        {
            return GetObjectPropValues(entity, pkProperty, labelProp, idProp);
        }


        /// <summary>
        /// Converts a given entity to a dynamic representation that can be used to insert the document in the database as a Graph Vertex.
        /// The model does not to be annotated with Attributes defining the key properties, but you need to provide an expression that points to properties you want to use.
        /// </summary>
        /// <remarks>
        /// If an Id expression is not passed in then a Guid will be generated for the id property/>
        /// If a Label property is not passed in then the class name will be assigned to the label label property/>
        /// If an PartitionKey expression is not passed in then an exception will be thrown/>
        /// </remarks>
        /// <returns>
        /// dynamic object containing a key-value collection of a graph vertex entry in a CosmosDB Graph collection
        /// </returns>
        /// <example>
        /// ToGraphVertex(entity, pkProperty: entity => entity.Title, labelProp: entity => entity.ItemType, idProp: entity => entity.EntityId) 
        /// </example>
        public static dynamic ToGraphVertex<T>(T data, Expression<Func<T, object>> pkProperty, Expression<Func<T, object>> labelProp = null, Expression<Func<T, object>> idProp = null)
        {
            var vertexProps = GetObjectPropValues(data, pkProperty, labelProp, idProp);
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
        public static dynamic ToGraphVertex<T>(this T entity)
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
        private static dynamic ToGraphVertexInternal(IDictionary<string, object> vertexProps)
        {
            var res = new Dictionary<string, object>();
            res[_propertyNames[KnownProperties.Id]] = vertexProps[_propertyNames[KnownProperties.Id]].ToString();
            res[_propertyNames[KnownProperties.Label]] = vertexProps[_propertyNames[KnownProperties.Label]].ToString();
            res[_propertyNames[KnownProperties.PartitionKey]] = vertexProps[_partitionKeyPropertyName].ToString();

            foreach (var prop in vertexProps.Where(p => !_ignoredPropertyNames.Contains(p.Key.ToLower())))
            {
                res[prop.Key] = new[]
                {
                    new
                    {
                        id = Guid.NewGuid().ToString(),
                        _value = prop.Value,
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
        public static dynamic ToGraphEdge<T, U, V>(this T entity, U source, V target, bool single = false)
        {
            var sourceProps = GetObjectPropValues(source, expandAllProps: false);
            var targetProps = GetObjectPropValues(target, expandAllProps: false);

            var sourceGraphIemBase = new GraphItemBase { Id = sourceProps[_propertyNames[KnownProperties.Id]].ToString(), Label = sourceProps[_propertyNames[KnownProperties.Label]].ToString(), PartitionKey = sourceProps[_propertyNames[KnownProperties.PartitionKey]].ToString() };
            var targetGraphIemBase = new GraphItemBase { Id = targetProps[_propertyNames[KnownProperties.Id]].ToString(), Label = targetProps[_propertyNames[KnownProperties.Label]].ToString(), PartitionKey = targetProps[_propertyNames[KnownProperties.PartitionKey]].ToString() };

            return ToGraphEdge<T>(entity, sourceGraphIemBase, targetGraphIemBase, single);
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
        public static dynamic ToGraphEdge<T>(this T entity, GraphItemBase source, GraphItemBase target, bool single = false)
        {
            var entityProps = GetObjectPropValues(entity, allowEmptyPartitionKey: true, defaultId: single ? $"{source.Id}-{target.Id}" : null);

            var res = new Dictionary<string, object>();
            res[_propertyNames[KnownProperties.Id]] = entityProps[_propertyNames[KnownProperties.Id]].ToString();
            res[_propertyNames[KnownProperties.Label]] = entityProps[_propertyNames[KnownProperties.Label]].ToString();
            res[_propertyNames[KnownProperties.PartitionKey]] = source.PartitionKey;
            res["_isEdge"] = true;
            res["_vertexId"] = source.Id;
            res["_vertexLabel"] = source.Label;
            res["_sink"] = target.Id;
            res["_sinkLabel"] = target.Label;
            res["_sinkPartition"] = target.PartitionKey;

            foreach (var prop in entityProps.Where(p => !_ignoredPropertyNames.Contains(p.Key.ToLower())))
            {
                res[prop.Key] = prop.Value;
            }

            return res;
        }



        public static T FromGraphson<T>(JObject graphson)
        {
            if (graphson == null) return default(T);
            var dataType = typeof(T);
            var entity = (T)Activator.CreateInstance(dataType);
            var allProps = dataType.GetRuntimeProperties().ToList();

            var labelProp = allProps.Where(p => p.CustomAttributes.Any(a => a.AttributeType.Name == "LabelAttribute")).FirstOrDefault();
            if (labelProp != null)
                labelProp.SetValue(entity, graphson["label"]?.ToString());
            var idProp = allProps.Where(p => p.CustomAttributes.Any(a => a.AttributeType.Name == "IdAttribute")).FirstOrDefault();
            if (idProp != null)
                idProp.SetValue(entity, graphson["id"]?.ToString());
            var pkProp = allProps.Where(p => p.CustomAttributes.Any(a => a.AttributeType.Name == "PartitionKeyAttribute")).FirstOrDefault();
            if (pkProp != null)
                pkProp.SetValue(entity, graphson["properties"]["PartitionKey"]?.FirstOrDefault()?["value"]?.ToString());

            allProps.Remove(pkProp);
            allProps.Remove(labelProp);
            allProps.Remove(idProp);

            var properties = graphson["properties"] as JObject;
            if (properties != null)
            {
                foreach (var p in properties)
                {
                    var dataProp = allProps.FirstOrDefault(x => x.Name == p.Key);
                    var value = p.Value.FirstOrDefault()?["value"]?.ToString() ?? p.Value.ToString();
                    if (dataProp != null && !string.IsNullOrEmpty(value))
                    {
                        try
                        {
                            if (dataProp.PropertyType.GetTypeInfo().BaseType == typeof(Enum))
                            {
                                dataProp.SetValue(entity, Convert.ChangeType(value, typeof(int)));
                            }
                            else
                            {
                                dataProp.SetValue(entity, Convert.ChangeType(value, dataProp.PropertyType));
                            }
                        }
                        catch (Exception e)
                        {
                            //can't set value - will default to type default
                        }
                    }
                }
            }
            else
            {
                foreach (var prop in allProps)
                {
                    var value = graphson?[prop.Name]?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        try
                        {
                            prop.SetValue(entity, Convert.ChangeType(value, prop.PropertyType));
                        }
                        catch (Exception e)
                        {
                            //can't set value - will default to type default
                        }
                    }
                }
            }
            return entity;
        }

        public static T FromDocument<T>(JObject doc)
        {
            if (doc == null) return default(T);
            var dataType = typeof(T);
            var entity = (T)Activator.CreateInstance(dataType);
            var allProps = dataType.GetRuntimeProperties().ToList();

            var labelProp = allProps.Where(p => p.CustomAttributes.Any(a => a.AttributeType.Name == "LabelAttribute")).FirstOrDefault();
            if (labelProp != null)
                labelProp.SetValue(entity, doc["label"]?.ToString());
            var idProp = allProps.Where(p => p.CustomAttributes.Any(a => a.AttributeType.Name == "IdAttribute")).FirstOrDefault();
            if (idProp != null)
                idProp.SetValue(entity, doc["id"]?.ToString());
            var pkProp = allProps.Where(p => p.CustomAttributes.Any(a => a.AttributeType.Name == "PartitionKeyAttribute")).FirstOrDefault();
            if (pkProp != null)
                pkProp.SetValue(entity, doc["PartitionKey"]?.ToString());
            allProps.Remove(pkProp);
            allProps.Remove(labelProp);
            allProps.Remove(idProp);

            foreach (var p in doc)
            {
                //TODO - serialize with json based on prop type.!!!!
                var dataProp = allProps.FirstOrDefault(x => x.Name == p.Key);
                var value = p.Value.FirstOrDefault()?["_value"]?.ToString();
                if (dataProp != null && !string.IsNullOrEmpty(value))
                {
                    try
                    {
                        dataProp.SetValue(entity, Convert.ChangeType(value, dataProp.PropertyType));
                    }
                    catch (Exception e)
                    {
                        //can't set value - will default to type default
                    }
                }
            }

            return entity;
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
        private static IDictionary<string, object> GetObjectPropValues<T>(T entity, bool expandAllProps = true, bool allowEmptyPartitionKey = false, string defaultId = null)
        {
            var res = new Dictionary<string, object>();
            var dataType = entity.GetType();
            var allProps = dataType.GetRuntimeProperties();

            var pkProp = allProps.Where(p => p.CustomAttributes.Any(a => a.AttributeType.Name == "PartitionKeyAttribute"));
            var labelProp = allProps.Where(p => p.CustomAttributes.Any(a => a.AttributeType.Name == "LabelAttribute"));
            var idProp = allProps.Where(p => p.CustomAttributes.Any(a => a.AttributeType.Name == "IdAttribute"));

            if (pkProp.Count() > 1)
                throw new Exception("More than 1 PartitionKey property defined.");
            if (labelProp.Count() > 1)
                throw new Exception("More than 1 Label property defined.");
            if (idProp.Count() > 1)
                throw new Exception("More than 1 Id property defined.");

            res[_propertyNames[KnownProperties.Label]] = labelProp.FirstOrDefault()?.GetValue(entity, null)?.ToString() ?? dataType.Name;
            res[_propertyNames[KnownProperties.Id]] = idProp.FirstOrDefault()?.GetValue(entity, null)?.ToString() ?? defaultId ?? Guid.NewGuid().ToString();
            res[_propertyNames[KnownProperties.PartitionKey]] = pkProp.FirstOrDefault()?.GetValue(entity, null)?.ToString() ?? (allowEmptyPartitionKey ? "" : throw new Exception("PartitionKey property must have a non-empty value"));

            if (expandAllProps)
            {
                var props = allProps.Where(p => !p.CustomAttributes.Any(a => _ignoredPropertyAttributes.Contains(a.AttributeType.Name)))
                                    .Where(p => !_ignoredPropertyNames.Contains(p.Name.ToLower()))
                                    .Select(p => new KeyValuePair<string, object>(p.Name, p.GetValue(entity) ?? ""));
                foreach (var p in props)
                    res.Add(p.Key, p.Value);
            }

            return res;
        }

        private static IDictionary<string, object> GetObjectPropValues<T>(T entity, Expression<Func<T, object>> pkProperty, Expression<Func<T, object>> labelProp = null, Expression<Func<T, object>> idProp = null, bool expandAllProps = true)
        {
            var res = new Dictionary<string, object>();
            var dataType = entity.GetType();

            if (pkProperty == null)
                throw new Exception("PartitionKey property defined must be defined.");

            res[_propertyNames[KnownProperties.Label]] = dataType.GetRuntimeProperty(labelProp.GetName() ?? "")?.GetValue(entity, null)?.ToString() ?? dataType.Name;
            res[_propertyNames[KnownProperties.Id]] = dataType.GetRuntimeProperty(idProp.GetName() ?? "")?.GetValue(entity, null)?.ToString() ?? Guid.NewGuid().ToString();
            res[_propertyNames[KnownProperties.PartitionKey]] = dataType.GetRuntimeProperty(pkProperty.GetName()).GetValue(entity, null)?.ToString() ?? throw new Exception("PartitionKey property must have a non-empty value");

            if (expandAllProps)
            {
                var props = dataType.GetRuntimeProperties()
                                        .Where(p => !p.CustomAttributes.Any(a => _ignoredPropertyAttributes.Contains(a.AttributeType.Name)))
                                        .Select(p => new KeyValuePair<string, object>(p.Name, p.GetValue(entity) ?? ""));
                foreach (var p in props)
                    res.Add(p.Key, p.Value);
            }

            return res;
        }
    }
}
