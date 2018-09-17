using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CosmosDb.Domain.Helpers
{
    public static class GraphsonHelpers
    {
        private static string[] _ignoredPropertAttributes = new[] { "PartitionKeyAttribute", "LabelAttribute", "IdAttribute", "JsonIgnoreAttribute", "IgnoreDataMemberAttribute" };
        private static string[] _illegalEdgePropertyNames = new[] { "label", "id" };

        //TODO: pass this in from the CosmosClient
        private static string _partitionKeyPropertyName = "PartitionKey";

        public static dynamic ToGraphVertex<T>(T data, Expression<Func<T, object>> pkProperty, Expression<Func<T, object>> labelProp = null, Expression<Func<T, object>> idProp = null)
        {
            var vertexProps = GetBaseProps(data, pkProperty, labelProp, idProp);

            return ToGraphVertexInternal(vertexProps);
        }

        public static dynamic ToGraphVertex<T>(this T entity)
        {
            var vertexProps = GetBaseProps(entity);

            return ToGraphVertexInternal(vertexProps);
        }

        private static dynamic ToGraphVertexInternal(IDictionary<string, object> vertexProps)
        {
            dynamic res = new ExpandoObject();

            res.label = vertexProps["/Label"].ToString();
            res.id = vertexProps["/Id"].ToString();
            res.PartitionKey = vertexProps["/PartitionKey"].ToString();

            var props = res as IDictionary<string, object>;
            foreach (var prop in vertexProps.Where(p => !p.Key.StartsWith("/")))
            {
                props[prop.Key] = new[] { new { id = Guid.NewGuid().ToString(), _value = prop.Value, _meta = new Dictionary<string, object>() } };
            }

            return res;
        }


        public static dynamic ToGraphEdge<T, U, V>(this T entity, U source, V target, bool single = false)
        {
            dynamic res = new ExpandoObject();

            var sourceProps = GetBaseProps(source, expandAllProps: false);
            var targetProps = GetBaseProps(target, expandAllProps: false);
            var entityProps = GetBaseProps(entity, allowEmptyPartitionKey: true, defaultId: single ? $"{sourceProps["/Id"].ToString()}-{targetProps["/Id"].ToString()}" : null);

            res.id = entityProps["/Id"].ToString();
            res.label = entityProps["/Label"].ToString();
            res._isEdge = true;

            res._vertexId = sourceProps["/Id"].ToString();
            res._vertexLabel = sourceProps["/Label"].ToString();
            res.PartitionKey = sourceProps["/PartitionKey"].ToString();

            res._sink = targetProps["/Id"].ToString();
            res._sinkLabel = targetProps["/Label"].ToString();
            res._sinkPartition = targetProps["/PartitionKey"].ToString();

            var propsDic = res as IDictionary<string, object>;
            foreach (var prop in entityProps.Where(p => !p.Key.StartsWith("/") && !_illegalEdgePropertyNames.Contains(p.Key) && p.Key != _partitionKeyPropertyName))
            {
                propsDic[prop.Key] = prop.Value;
            }

            return res;
        }


        public static dynamic ToGraphEdge<T>(this T entity, GraphItemBase source, GraphItemBase target, bool single = false)
        {
            dynamic res = new ExpandoObject();
            var entityProps = GetBaseProps(entity, allowEmptyPartitionKey: true, defaultId: single ? $"{source.Id}-{target.Id}" : null);

            res.id = entityProps["/Id"].ToString();
            res.label = entityProps["/Label"].ToString();
            res._isEdge = true;

            res._vertexId = source.Id;
            res._vertexLabel = source.Label;
            res.PartitionKey = source.PartitionKey;

            res._sink = target.Id;
            res._sinkLabel = target.Label;
            res._sinkPartition = target.PartitionKey;

            var propsDic = res as IDictionary<string, object>;
            foreach (var prop in entityProps.Where(p => !p.Key.StartsWith("/") && !_illegalEdgePropertyNames.Contains(p.Key) && p.Key != _partitionKeyPropertyName))
            {
                propsDic[prop.Key] = prop.Value;
            }

            return res;
        }


        public static T FromGraphson<T>(JObject graphson)
        {
            if (graphson == null) return default(T);
            var dataType = typeof(T);
            var entity = (T)Activator.CreateInstance(dataType);
            var allProps = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();

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
                            dataProp.SetValue(entity, Convert.ChangeType(value, dataProp.PropertyType));
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
            var allProps = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();

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
        public static JObject GraphsonNetToFlatJObject(dynamic obj)
        {
            var instance = new JObject();
            foreach (var p in obj)
            {
                if (p.Key == "properties")
                {
                    foreach (var sp in p.Value)
                    {
                        //var v = (sp.Value as System.<JToken, object>);
                        instance[sp.Key] = ((sp.Value as IEnumerable<object>)?.FirstOrDefault() as Dictionary<string, object>)?.GetValueOrDefault("value") ?? sp.Value.ToString();
                    }
                }
                else
                {
                    instance[p.Key] = (p.Value as JToken)?.Values()?.FirstOrDefault()?.ToString() ?? p.Value.ToString();
                }
            }
            return instance;
        }
        public static JObject GraphsonToFlatJObject(JObject obj)
        {
            var hubMsg = "${{\"count\":\"{obj}\"}}";
            var instance = new JObject();
            foreach (var p in obj)
            {
                if (p.Key == "properties")
                {
                    foreach (var sp in p.Value.OfType<JProperty>())
                    {
                        instance[sp.Name] = sp.Value.FirstOrDefault()?["value"] ?? sp.Value.ToString();
                    }
                }
                else
                {
                    instance[p.Key] = (p.Value as JToken)?.Values()?.FirstOrDefault()?.ToString() ?? p.Value.ToString();
                }
            }
            return instance;
        }

        private static Dictionary<string, object> GetBaseProps<T>(T entity, bool expandAllProps = true, bool allowEmptyPartitionKey = false, string defaultId = null)
        {
            var res = new Dictionary<string, object>();
            var dataType = entity.GetType();
            var allProps = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var pkProp = allProps.Where(p => p.CustomAttributes.Any(a => a.AttributeType.Name == "PartitionKeyAttribute"));
            var labelProp = allProps.Where(p => p.CustomAttributes.Any(a => a.AttributeType.Name == "LabelAttribute"));
            var idProp = allProps.Where(p => p.CustomAttributes.Any(a => a.AttributeType.Name == "IdAttribute"));

            if (pkProp.Count() > 1)
                throw new Exception("More than 1 PartitionKey property defined.");
            if (labelProp.Count() > 1)
                throw new Exception("More than 1 Label property defined.");
            if (idProp.Count() > 1)
                throw new Exception("More than 1 Id property defined.");

            res["/Label"] = labelProp.FirstOrDefault()?.GetValue(entity, null)?.ToString() ?? dataType.Name;
            res["/Id"] = idProp.FirstOrDefault()?.GetValue(entity, null)?.ToString() ?? defaultId ?? Guid.NewGuid().ToString();
            res["/PartitionKey"] = pkProp.FirstOrDefault()?.GetValue(entity, null)?.ToString() ?? (allowEmptyPartitionKey ? "" : throw new Exception("PartitionKey property must have a non-empty value"));

            if (expandAllProps)
            {
                //Todo - nicer way ?
                var props = allProps.Where(p => !p.CustomAttributes.Any(a => _ignoredPropertAttributes.Contains(a.AttributeType.Name)))
                                    .Select(p => new KeyValuePair<string, object>(p.Name, p.GetValue(entity) ?? ""));
                foreach (var p in props)
                    res.Add(p.Key, p.Value);
            }

            return res;
        }

        private static Dictionary<string, object> GetBaseProps<T>(T entity, Expression<Func<T, object>> pkProperty, Expression<Func<T, object>> labelProp = null, Expression<Func<T, object>> idProp = null, bool expandAllProps = true)
        {
            var res = new Dictionary<string, object>();
            var dataType = entity.GetType();

            if (pkProperty == null)
                throw new Exception("PartitionKey property defined must be defined.");

            res["Label"] = dataType.GetProperty(labelProp.GetName() ?? "")?.GetValue(entity, null)?.ToString() ?? dataType.Name;
            res["Id"] = dataType.GetProperty(idProp.GetName() ?? "")?.GetValue(entity, null)?.ToString() ?? Guid.NewGuid().ToString();
            res["PropertyKey"] = dataType.GetProperty(pkProperty.GetName()).GetValue(entity, null)?.ToString() ?? throw new Exception("PartitionKey property must have a non-empty value");

            if (expandAllProps)
            {
                //Todo - nicer way ?
                var props = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                        .Where(p => !p.CustomAttributes.Any(a => _ignoredPropertAttributes.Contains(a.AttributeType.Name)))
                                        .Select(p => new KeyValuePair<string, object>(p.Name, p.GetValue(entity) ?? ""));
                foreach (var p in props)
                    res.Add(p.Key, p.Value);
            }

            return res;
        }
    }
}
