﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace FMData.Xml
{
    /// <summary>
    /// Extension Method Holding Class
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Converts a Dictionary to an Object.
        /// </summary>
        public static object ToObject(this IDictionary<string, object> source, Type type)
        {
            var someObject = Activator.CreateInstance(type);
            return ToObjectHelper(source, someObject);
        }

        /// <summary>
        /// https://stackoverflow.com/a/4944547/86860
        /// </summary>
        public static T ToObject<T>(this IDictionary<string, object> source) where T : class, new()
        {
            var someObject = new T();
            return (T)ToObjectHelper(source, someObject);
        }

        /// <summary>
        /// Helper method for 'ToObject' conversion from Dictionary of object properties.
        /// </summary>
        private static object ToObjectHelper(IDictionary<string, object> source, object someObject)
        {
            var someObjectType = someObject.GetType().GetTypeInfo();

            foreach (var item in source)
            {
                someObjectType
                    .DeclaredProperties
                    .FirstOrDefault(k
                        => k.Name.Equals(item.Key, StringComparison.CurrentCultureIgnoreCase)
                        || (k.GetCustomAttribute<DataMemberAttribute>() != null && k.GetCustomAttribute<DataMemberAttribute>().Name.Equals(item.Key, StringComparison.CurrentCultureIgnoreCase))
                        )
                    ?.SetValue(someObject, item.Value, null);
            }

            return someObject;
        }

        /// <summary>
        /// https://stackoverflow.com/a/4944547/86860
        /// </summary>
        public static IDictionary<string, object> AsDictionary(this object source, bool includeNulls = true)
        {
            var props = source
                .GetType()
                .GetTypeInfo()
                .DeclaredProperties
                .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null);

            if (!includeNulls)
            {
                props = props
                    .Where(p => p.GetValue(source, null) != null)
                    // need a way to exclude 'default' values for ints, dates, etc
                    .Where(p 
                        => !(p.PropertyType.GetTypeInfo().IsValueType == true
                        && p.GetValue(source, null) != Activator.CreateInstance(p.PropertyType))
                    );
            }

            return props.ToDictionary
            (
                propInfo => propInfo.GetCustomAttribute<DataMemberAttribute>() == null ? propInfo.Name : propInfo.GetCustomAttribute<DataMemberAttribute>().Name,
                propInfo => propInfo.GetValue(source, null)
            );
        }
    }
}