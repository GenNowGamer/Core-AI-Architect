using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace CoreAiArchitect.Core.SchemaEngine;

/// <summary>
/// Gets and sets values on an object graph using camelCase or PascalCase dot paths
/// (e.g. project.name, domain.entities).
/// </summary>
public static class PathBinder
{
    public static object? GetValue(object root, string path)
    {
        if (root is null) throw new ArgumentNullException(nameof(root));
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));

        object? current = root;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (current is null) return null;

            if (current is IList list && int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
            {
                if (index < 0 || index >= list.Count) return null;
                current = list[index];
                continue;
            }

            var prop = FindProperty(current.GetType(), segment);
            if (prop is null) return null;
            current = prop.GetValue(current);
        }

        return current;
    }

    public static void SetValue(object root, string path, object? value)
    {
        if (root is null) throw new ArgumentNullException(nameof(root));
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));

        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        object current = root;

        for (var i = 0; i < segments.Length - 1; i++)
        {
            var segment = segments[i];
            if (current is IList list && int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
            {
                if (index < 0 || index >= list.Count)
                    throw new InvalidOperationException($"Index '{index}' out of range for path '{path}'.");
                current = list[index] ?? throw new InvalidOperationException($"Null list item at '{segment}'.");
                continue;
            }

            var prop = FindProperty(current.GetType(), segment)
                       ?? throw new InvalidOperationException($"Property '{segment}' not found on {current.GetType().Name}.");
            var next = prop.GetValue(current);
            if (next is null)
            {
                next = Activator.CreateInstance(Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType)
                       ?? throw new InvalidOperationException($"Cannot create instance for '{segment}'.");
                prop.SetValue(current, next);
            }

            current = next;
        }

        var leaf = segments[^1];
        if (current is IList leafList && int.TryParse(leaf, NumberStyles.Integer, CultureInfo.InvariantCulture, out var leafIndex))
        {
            if (leafIndex < 0 || leafIndex >= leafList.Count)
                throw new InvalidOperationException($"Index '{leafIndex}' out of range for path '{path}'.");
            leafList[leafIndex] = Coerce(value, leafList[leafIndex]?.GetType() ?? typeof(object));
            return;
        }

        var leafProp = FindProperty(current.GetType(), leaf)
                       ?? throw new InvalidOperationException($"Property '{leaf}' not found on {current.GetType().Name}.");
        leafProp.SetValue(current, Coerce(value, leafProp.PropertyType));
    }

    public static bool EvaluateShowIf(object root, string? path, string? equalsValue)
    {
        if (string.IsNullOrWhiteSpace(path)) return true;
        var actual = GetValue(root, path);
        var actualText = actual switch
        {
            null => string.Empty,
            bool b => b ? "true" : "false",
            _ => Convert.ToString(actual, CultureInfo.InvariantCulture) ?? string.Empty
        };
        return string.Equals(actualText, equalsValue ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static PropertyInfo? FindProperty(Type type, string name) =>
        type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(ToCamel(p.Name), name, StringComparison.OrdinalIgnoreCase));

    private static string ToCamel(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];

    private static object? Coerce(object? value, Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (value is null)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) is null)
                return Activator.CreateInstance(targetType);
            return null;
        }

        if (underlying.IsInstanceOfType(value)) return value;

        if (value is JsonElement je)
            value = ConvertJsonElement(je, underlying);

        if (underlying == typeof(string))
            return Convert.ToString(value, CultureInfo.InvariantCulture);

        if (underlying == typeof(bool))
        {
            if (value is bool b) return b;
            if (bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed)) return parsed;
            return false;
        }

        if (underlying == typeof(double))
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);

        if (underlying == typeof(int))
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);

        if (typeof(IList).IsAssignableFrom(underlying) && value is IEnumerable enumerable and not string)
        {
            var list = (IList?)Activator.CreateInstance(underlying)
                       ?? throw new InvalidOperationException($"Cannot create list of type {underlying.Name}.");
            var itemType = underlying.IsGenericType ? underlying.GetGenericArguments()[0] : typeof(object);
            foreach (var item in enumerable)
                list.Add(Coerce(item, itemType));
            return list;
        }

        return Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
    }

    private static object? ConvertJsonElement(JsonElement je, Type targetType) =>
        je.ValueKind switch
        {
            JsonValueKind.String => je.GetString(),
            JsonValueKind.Number when targetType == typeof(double) => je.GetDouble(),
            JsonValueKind.Number when targetType == typeof(int) => je.GetInt32(),
            JsonValueKind.Number => je.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => je.EnumerateArray().Select(e => ConvertJsonElement(e, typeof(object))).ToList(),
            _ => je.ToString()
        };
}
