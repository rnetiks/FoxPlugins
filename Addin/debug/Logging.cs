using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

public class Logging
{
    public static int MAX_DEPTH { get; set; } = 5;

    [Flags]
    public enum ConfigOption
    {
        None         = 0,
        Properties   = 1 << 0,
        Fields       = 1 << 1,
        Constants    = 1 << 2,
        Events       = 1 << 3,
        Methods      = 1 << 4,
        Constructors = 1 << 5,
        NestedTypes  = 1 << 6,
        Interfaces   = 1 << 7,
        Attributes   = 1 << 8,
        All          = Properties | Fields | Constants | Events | Methods
                       | Constructors | NestedTypes | Interfaces | Attributes
    }

    public static ConfigOption Config { get; set; } = ConfigOption.Properties | ConfigOption.Fields;

    public static string Dump<T>(T obj, int indentLevel = 0)
        => _logType(obj, indentLevel);

    public static string DumpAll<T>(T obj, int indentLevel = 0)
        => _logType(obj, indentLevel,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);



    private static string _logType<T>(T obj, int indentLevel = 0,
        BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
    {
        if (obj == null)
            return "null";

        Type type = obj.GetType();
        var sb = new StringBuilder();
        string indent = new string(' ', indentLevel * 2);
        bool wroteAnything = false;

        if ((Config & ConfigOption.Attributes) == ConfigOption.Attributes)
        {
            object[] attrs = type.GetCustomAttributes(false);
            if (attrs.Length > 0)
            {
                sb.AppendLine($"{indent}{type.Name} Attributes:");
                foreach (object attr in attrs)
                    sb.AppendLine($"{indent}  [{attr.GetType().Name}]");
                wroteAnything = true;
            }
        }

        if ((Config & ConfigOption.Interfaces) == ConfigOption.Interfaces)
        {
            Type[] interfaces = type.GetInterfaces();
            if (interfaces.Length > 0)
            {
                sb.AppendLine($"{indent}{type.Name} Interfaces:");
                foreach (Type iface in interfaces)
                    sb.AppendLine($"{indent}  {FormatTypeName(iface)}");
                wroteAnything = true;
            }
        }

        if ((Config & ConfigOption.NestedTypes) == ConfigOption.NestedTypes)
        {
            Type[] nested = type.GetNestedTypes(flags | BindingFlags.Static);
            if (nested.Length > 0)
            {
                sb.AppendLine($"{indent}{type.Name} Nested Types:");
                foreach (Type nt in nested)
                {
                    string kind = nt.IsEnum     ? "enum"
                        : nt.IsValueType ? "struct"
                        : typeof(Delegate).IsAssignableFrom(nt) ? "delegate"
                        : nt.IsInterface ? "interface"
                        : "class";
                    string generics = nt.IsGenericTypeDefinition
                        ? $"<{string.Join(", ", nt.GetGenericArguments().Select(a => a.Name).ToArray())}>"
                        : "";
                    sb.AppendLine($"{indent}  {kind} {nt.Name}{generics}");
                    if (nt.IsEnum)
                    {
                        foreach (string enumName in Enum.GetNames(nt))
                        {
                            object enumVal = Enum.Parse(nt, enumName);
                            sb.AppendLine($"{indent}    {enumName} = {Convert.ToInt64(enumVal)}");
                        }
                    }
                }
                wroteAnything = true;
            }
        }

        if ((Config & ConfigOption.Constants) == ConfigOption.Constants)
        {
            FieldInfo[] constants = type.GetFields(flags | BindingFlags.Static)
                .Where(f => f.IsLiteral && !f.IsInitOnly)
                .ToArray();
            if (constants.Length > 0)
            {
                sb.AppendLine($"{indent}{type.Name} Constants:");
                foreach (FieldInfo c in constants)
                {
                    try
                    {
                        object value = c.GetRawConstantValue();
                        sb.AppendLine($"{indent}  const {c.FieldType.Name} {c.Name} = {value}");
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine($"{indent}  {c.Name}: <Error: {ex.Message}>");
                    }
                }
                wroteAnything = true;
            }
        }

        if ((Config & ConfigOption.Fields) == ConfigOption.Fields)
        {
            FieldInfo[] fields = type.GetFields(flags)
                .Where(f => !f.IsLiteral
                            && !f.IsInitOnly
                            && !f.IsDefined(typeof(CompilerGeneratedAttribute), false))
                .ToArray();
            if (fields.Length > 0)
            {
                sb.AppendLine($"{indent}{type.Name} Fields:");
                foreach (FieldInfo field in fields)
                {
                    try
                    {
                        object value = field.GetValue(obj);
                        string staticMark = field.IsStatic ? "static " : "";
                        sb.AppendLine($"{indent}  {staticMark}{field.FieldType.Name} {field.Name}: {FormatValue(value, indentLevel + 1)}");
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine($"{indent}  {field.Name}: <Error: {ex.Message}>");
                    }
                }
                wroteAnything = true;
            }
        }

        if ((Config & ConfigOption.Properties) == ConfigOption.Properties)
        {
            PropertyInfo[] properties = type.GetProperties(flags);
            if (properties.Length > 0)
            {
                sb.AppendLine($"{indent}{type.Name} Properties:");
                foreach (PropertyInfo property in properties)
                {
                    try
                    {
                        if (property.GetIndexParameters().Length > 0)
                        {
                            sb.AppendLine($"{indent}  {property.PropertyType.Name} {property.Name}: <Indexer>");
                            continue;
                        }
                        string accessors = $"{(property.CanRead ? "get" : "")}{(property.CanRead && property.CanWrite ? "; " : "")}{(property.CanWrite ? "set" : "")}";
                        object value = property.CanRead ? property.GetValue(obj, null) : null;
                        string valueStr = property.CanRead
                            ? FormatValue(value, indentLevel + 1)
                            : "<write-only>";
                        sb.AppendLine($"{indent}  {property.PropertyType.Name} {property.Name} {{ {accessors} }}: {valueStr}");
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine($"{indent}  {property.Name}: <Error: {ex.Message}>");
                    }
                }
                wroteAnything = true;
            }
        }

        if ((Config & ConfigOption.Events) == ConfigOption.Events)
        {
            EventInfo[] events = type.GetEvents(flags);
            if (events.Length > 0)
            {
                sb.AppendLine($"{indent}{type.Name} Events:");
                foreach (EventInfo ev in events)
                {
                    try
                    {
                        FieldInfo backingField = type.GetField(ev.Name,
                            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        if (backingField != null)
                        {
                            Delegate del = backingField.GetValue(obj) as Delegate;
                            Delegate[] subscribers = del?.GetInvocationList();
                            int count = subscribers?.Length ?? 0;
                            string subInfo = count == 0
                                ? "no subscribers"
                                : $"{count} subscriber{(count == 1 ? "" : "s")}: "
                                  + string.Join(", ", subscribers.Select(d =>
                                      $"{d.Method.DeclaringType?.Name}.{d.Method.Name}").ToArray());
                            sb.AppendLine($"{indent}  {ev.EventHandlerType?.Name} {ev.Name}: {subInfo}");
                        }
                        else
                        {
                            sb.AppendLine($"{indent}  {ev.Name}: <backing field not found>");
                        }
                    }
                    catch (Exception ex)
                    {
                        sb.AppendLine($"{indent}  {ev.Name}: <Error: {ex.Message}>");
                    }
                }
                wroteAnything = true;
            }
        }

        if ((Config & ConfigOption.Constructors) == ConfigOption.Constructors)
        {
            ConstructorInfo[] ctors = type.GetConstructors(flags | BindingFlags.Static);
            if (ctors.Length > 0)
            {
                sb.AppendLine($"{indent}{type.Name} Constructors:");
                foreach (ConstructorInfo ctor in ctors)
                {
                    string staticMark = ctor.IsStatic ? "static " : "";
                    string paramList = string.Join(", ", ctor.GetParameters()
                        .Select(p => $"{FormatTypeName(p.ParameterType)} {p.Name}"
                                     + ((p.Attributes & ParameterAttributes.HasDefault) != 0 ? $" = {p.DefaultValue ?? "null"}" : "")).ToArray());
                    sb.AppendLine($"{indent}  {staticMark}{type.Name}({paramList})");
                }
                wroteAnything = true;
            }
        }

        if ((Config & ConfigOption.Methods) == ConfigOption.Methods)
        {
            MethodInfo[] methods = type.GetMethods(flags)
                .Where(m => !m.IsSpecialName && m.DeclaringType != typeof(object))
                .ToArray();
            if (methods.Length > 0)
            {
                sb.AppendLine($"{indent}{type.Name} Methods:");
                foreach (MethodInfo method in methods)
                {
                    string staticMark = method.IsStatic ? "static " : "";
                    string generics = method.IsGenericMethodDefinition
                        ? $"<{string.Join(", ", method.GetGenericArguments().Select(a => a.Name).ToArray())}>"
                        : "";
                    string paramList = string.Join(", ", method.GetParameters()
                        .Select(p => $"{FormatTypeName(p.ParameterType)} {p.Name}"
                                     + ((p.Attributes & ParameterAttributes.HasDefault) != 0 ? $" = {p.DefaultValue ?? "null"}" : "")).ToArray());
                    sb.AppendLine($"{indent}  {staticMark}{FormatTypeName(method.ReturnType)} {method.Name}{generics}({paramList})");
                }
                wroteAnything = true;
            }
        }

        if (!wroteAnything)
            sb.AppendLine($"{indent}  <nothing to show — check Logging.Config>");

        return sb.ToString();
    }
    private static string FormatTypeName(Type type)
    {
        if (!type.IsGenericType)
            return type.Name;

        string baseName = type.Name.Substring(0, type.Name.IndexOf('`'));
        string args = string.Join(", ", type.GetGenericArguments().Select(FormatTypeName).ToArray());
        return $"{baseName}<{args}>";
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
            const int maxItems = 10;
            foreach (var item in enumerable)
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
                    collectionSb.Append(item);
                else
                    collectionSb.Append($"{{{item.GetType().Name}}}");

                first = false;
                count++;
            }

            collectionSb.Append("]");
            return collectionSb.ToString();
        }

        if (indentLevel < MAX_DEPTH)
        {
            return $"{{\n{_logType(value, indentLevel + 1)}\n{indent}}}";
        }
        else
        {
            return $"{{{valueType.Name}}} (Max depth reached)";
        }
    }
}