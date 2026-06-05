using System.Reflection;

namespace Sentinel.Services.CaseDefinitionEvaluation
{
    /// <summary>
    /// Resolves field values from objects using reflection-based path navigation
    /// </summary>
    public class FieldResolver
    {
        /// <summary>
        /// Resolves a field value from an object using a dot-notation path
        /// </summary>
        /// <param name="source">The source object to navigate</param>
        /// <param name="fieldPath">The path to the field (e.g., "Patient.Age" or "Disease.Name")</param>
        /// <returns>The resolved value, or null if path is invalid or value doesn't exist</returns>
        public object? ResolveFieldValue(object? source, string fieldPath)
        {
            if (source == null || string.IsNullOrWhiteSpace(fieldPath))
            {
                return null;
            }

            var pathParts = fieldPath.Split('.');
            object? currentValue = source;

            foreach (var part in pathParts)
            {
                if (currentValue == null)
                {
                    return null;
                }

                currentValue = GetPropertyValue(currentValue, part);
            }

            return currentValue;
        }

        /// <summary>
        /// Resolves multiple field values (e.g., for collections)
        /// </summary>
        /// <param name="source">The source object</param>
        /// <param name="fieldPath">Path to a collection property</param>
        /// <param name="subPath">Path within each collection item</param>
        /// <returns>List of values from the collection</returns>
        public List<object?> ResolveCollectionValues(object? source, string collectionPath, string? subPath = null)
        {
            var results = new List<object?>();

            if (source == null || string.IsNullOrWhiteSpace(collectionPath))
            {
                return results;
            }

            var collection = ResolveFieldValue(source, collectionPath);
            if (collection == null)
            {
                return results;
            }

            // Check if it's enumerable
            if (collection is System.Collections.IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item == null) continue;

                    if (string.IsNullOrWhiteSpace(subPath))
                    {
                        results.Add(item);
                    }
                    else
                    {
                        var value = ResolveFieldValue(item, subPath);
                        results.Add(value);
                    }
                }
            }
            else
            {
                // Single item, not a collection
                results.Add(collection);
            }

            return results;
        }

        /// <summary>
        /// Checks if a collection contains any items matching a predicate
        /// </summary>
        /// <param name="source">The source object</param>
        /// <param name="collectionPath">Path to the collection</param>
        /// <param name="subPath">Path to property within items</param>
        /// <param name="expectedValue">Value to match</param>
        /// <param name="comparer">Optional custom comparer</param>
        /// <returns>True if any item matches</returns>
        public bool CollectionContains(
            object? source, 
            string collectionPath, 
            string subPath, 
            object? expectedValue,
            IEqualityComparer<object>? comparer = null)
        {
            var values = ResolveCollectionValues(source, collectionPath, subPath);

            if (comparer == null)
            {
                comparer = new CaseInsensitiveComparer();
            }

            return values.Any(v => comparer.Equals(v, expectedValue));
        }

        private object? GetPropertyValue(object source, string propertyName)
        {
            var type = source.GetType();

            // Try exact match first
            var property = type.GetProperty(propertyName, 
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property != null)
            {
                try
                {
                    return property.GetValue(source);
                }
                catch
                {
                    return null;
                }
            }

            // Try as field
            var field = type.GetField(propertyName, 
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (field != null)
            {
                try
                {
                    return field.GetValue(source);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        private class CaseInsensitiveComparer : IEqualityComparer<object>
        {
            public new bool Equals(object? x, object? y)
            {
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;

                if (x is string xStr && y is string yStr)
                {
                    return string.Equals(xStr, yStr, StringComparison.OrdinalIgnoreCase);
                }

                return x.Equals(y);
            }

            public int GetHashCode(object obj)
            {
                if (obj is string str)
                {
                    return str.ToLowerInvariant().GetHashCode();
                }
                return obj.GetHashCode();
            }
        }
    }
}
