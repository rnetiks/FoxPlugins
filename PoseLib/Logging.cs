using System;
using System.Collections;
using System.Reflection;
using System.Text;

public class Logging
{
    public static int MAX_DEPTH { get; set; } = 5;
    public static string GetObjectPropertiesAndFields<T>(T obj, int indentLevel = 0,
        BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
    {
        if (obj == null)
            return "null";

        Type type = obj.GetType();
        PropertyInfo[] properties = type.GetProperties(flags);
        FieldInfo[] fields = type.GetFields(flags);

        var sb = new StringBuilder();
        string indent = new string(' ', indentLevel * 2);

        if (properties.Length > 0)
        {
            sb.AppendLine($"{indent}{type.Name} Properties:");
            foreach (PropertyInfo property in properties)
            {
                try
                {
                    if (property.GetIndexParameters().Length > 0)
                    {
                        sb.AppendLine($"{indent}    {property.Name}: <Indexer Property>");
                        continue;
                    }

                    object value = property.GetValue(obj, null);
                    string valueString = FormatValue(value, indentLevel + 2);
                    sb.AppendLine($"{indent}    {property.Name}: {valueString}");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"{indent}    {property.Name}: <Error: {ex.Message}>");
                }
            }
        }

        if (fields.Length > 0)
        {
            sb.AppendLine($"{indent}{type.Name} Fields:");
            foreach (FieldInfo field in fields)
            {
                try
                {
                    object value = field.GetValue(obj);
                    string valueString = FormatValue(value, indentLevel + 2);
                    sb.AppendLine($"{indent}    {field.Name}: {valueString}");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"{indent}    {field.Name}: <Error: {ex.Message}>");
                }
            }
        }

        if (properties.Length == 0 && fields.Length == 0)
        {
            sb.AppendLine($"{indent}  No accessible properties or fields found");
        }

        return sb.ToString();
    }

    private static string FormatValue(object value, int indentLevel)
    {
        string indent = new string(' ', indentLevel * 2);

        if (value == null)
        {
            return "null";
        }

        Type valueType = value.GetType();

        if (valueType.IsPrimitive ||
            valueType == typeof(string) ||
            valueType == typeof(decimal) ||
            valueType == typeof(DateTime) ||
            valueType == typeof(TimeSpan) ||
            valueType == typeof(Guid))
        {
            return value.ToString();
        }

        if (valueType.IsEnum)
        {
            return $"{valueType.Name}.{value}";
        }

        if (value is IEnumerable enumerable && !(value is string))
        {
            var collectionSb = new StringBuilder();
            collectionSb.Append("[");

            bool first = true;
            int count = 0;
            const int maxItems = 10; foreach (var item in enumerable)
            {
                if (count >= maxItems)
                {
                    collectionSb.Append(", ...");
                    break;
                }

                if (!first) collectionSb.Append(", ");

                if (item == null)
                    collectionSb.Append("null");
                else if (item.GetType().IsPrimitive || item is string || item is decimal || item is DateTime)
                    collectionSb.Append(item.ToString());
                else
                    collectionSb.Append($"{{{item.GetType().Name}}}");

                first = false;
                count++;
            }

            collectionSb.Append("]");
            return collectionSb.ToString();
        }

        if (indentLevel < MAX_DEPTH) {
            return $"{{\n{GetObjectPropertiesAndFields(value, indentLevel + 1)}\n{indent}}}";
        }
        else
        {
            return $"{{{valueType.Name}}} (Max depth reached)";
        }
    }

public static string GetObjectPropertiesAndFieldsAll<T>(T obj, int indentLevel = 0)
    {
        return GetObjectPropertiesAndFields(obj, indentLevel,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    }
}