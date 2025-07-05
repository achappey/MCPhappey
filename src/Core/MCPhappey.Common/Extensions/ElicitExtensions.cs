
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Common.Extensions;

public static class ElicitExtensions
{
    public static void EnsureAccept(
        this ElicitResult message)
    {
        if (message.Action != "accept")
        {
            throw new Exception(JsonSerializer.Serialize(message, JsonSerializerOptions.Web));
        }
    }

    public static string GetJsonPropertyName(this PropertyInfo prop) =>
           prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? prop.Name;

    public static string? GetDescription(this PropertyInfo prop) =>
        prop.GetCustomAttribute<DescriptionAttribute>()?.Description;

    public static object? GetDefaultValue(this PropertyInfo prop) =>
        prop.GetCustomAttribute<DefaultValueAttribute>()?.Value;

    public static bool IsEmail(this PropertyInfo prop) =>
        prop.GetCustomAttribute<EmailAddressAttribute>() != null;

    public static List<string> GetRequiredProperties(Type type)
    {
        return type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => Attribute.IsDefined(p, typeof(RequiredAttribute)))
            .Select(p =>
            {
                var jsonAttr = p.GetCustomAttribute<JsonPropertyNameAttribute>();
                return jsonAttr?.Name ?? p.Name;
            })
            .ToList();
    }

    public static ElicitRequestParams CreateElicitRequestParamsForType<T>()
    {
        var type = typeof(T);
        var description = type.GetCustomAttribute<DescriptionAttribute>()?.Description
           ?? $"Please fill in the details for {type.Name}";

        var properties = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(
                prop => prop.GetJsonPropertyName(),
                prop => prop.ToElicitSchemaDef()
            );

        var required = GetRequiredProperties(typeof(T));

        return new ElicitRequestParams
        {
            Message = description,
            RequestedSchema = new ElicitRequestParams.RequestSchema
            {
                Properties = properties,
                Required = required
            }
        };
    }

    public static (double? min, double? max) GetRange(this PropertyInfo prop)
    {
        var rangeAttr = prop.GetCustomAttribute<RangeAttribute>();
        if (rangeAttr != null)
        {
            // These can be double, int, etc.
            double? min = rangeAttr.Minimum as double? ?? Convert.ToDouble(rangeAttr.Minimum);
            double? max = rangeAttr.Maximum as double? ?? Convert.ToDouble(rangeAttr.Maximum);
            return (min, max);
        }
        return (null, null);
    }

    public static int? GetMinLength(this PropertyInfo prop) =>
        prop.GetCustomAttribute<MinLengthAttribute>()?.Length;

    public static int? GetMaxLength(this PropertyInfo prop) =>
        prop.GetCustomAttribute<MaxLengthAttribute>()?.Length;

    public static ElicitRequestParams.PrimitiveSchemaDefinition ToElicitSchemaDef(this PropertyInfo prop)
    {
        var title = prop.Name;
        var desc = prop.GetDescription();
        var type = prop.PropertyType;

        // Enum detection (works for nullable enums too)
        var enumType = Nullable.GetUnderlyingType(type) ?? (type.IsEnum ? type : null);

        if (enumType != null && enumType.IsEnum)
        {
            var enumFields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
            var enumValues = enumFields
                .Select(field =>
                    field.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? field.Name
                ).ToArray();

            // Friendly names via [Display] attribuut, eventueel voor weergave
            var friendlyNames = enumFields
                .Select(field =>
                    field.GetCustomAttribute<DisplayAttribute>()?.Name ?? field.Name
                ).ToArray();

            // Only set enumNames if they differ from enum values
            return new ElicitRequestParams.EnumSchema
            {
                Title = title,
                Description = desc,
                Enum = enumValues,
                EnumNames = !enumValues.SequenceEqual(friendlyNames) ? friendlyNames : null
            };
        }

        // Use switch for type logic (add more cases as needed)
        return type switch
        {
            Type t when t == typeof(string) => new ElicitRequestParams.StringSchema
            {
                Title = title,
                Format = prop.IsEmail() ? "email" : null,
                Description = desc,
                MinLength = prop.GetMinLength(),
                MaxLength = prop.GetMaxLength()
            },
            Type t when t == typeof(int) || t == typeof(int?) ||
                        t == typeof(decimal) || t == typeof(decimal?) ||
                        t == typeof(double) || t == typeof(double?) =>
            new ElicitRequestParams.NumberSchema
            {
                Title = title,
                Description = desc,
                Minimum = prop.GetRange().min,
                Maximum = prop.GetRange().max
            },
            Type t when t == typeof(Uri) => new ElicitRequestParams.StringSchema
            {
                Title = title,
                Format = "uri",
                Description = desc
            },
            Type t when t == typeof(DateTime) || t == typeof(DateTime?) => new ElicitRequestParams.StringSchema
            {
                Title = title,
                Format = "date-time",
                Description = desc
            },
            Type t when t == typeof(bool) || t == typeof(bool?) => new ElicitRequestParams.BooleanSchema
            {
                Title = title,
                Description = desc,
                Default = prop.GetDefaultValue() as bool?
            },
            // Add more cases if needed (enums, arrays, etc.)
            _ => new ElicitRequestParams.StringSchema
            {
                Title = title,
                Description = desc
            }
        };
    }

    public static T MapToObject<T>(this IDictionary<string, JsonElement> dict) where T : new()
    {
        var obj = new T();
        var type = typeof(T);

        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            // Pak JSON property name indien aanwezig
            var jsonName = prop.Name;
            var jsonAttr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (jsonAttr != null)
                jsonName = jsonAttr.Name;

            if (dict.TryGetValue(jsonName, out var el))
            {
                object? value = null;
                var t = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                try
                {
                    if (t.IsEnum)
                    {
                        if (el.ValueKind == JsonValueKind.String && Enum.TryParse(t, el.GetString(), true, out var enumVal))
                            value = enumVal;
                    }
                    else if (t == typeof(string))
                        value = el.ValueKind == JsonValueKind.String ? el.GetString() : null;
                    else if (t == typeof(int))
                        value = el.ValueKind == JsonValueKind.Number ? el.GetInt32() : default(int);
                    else if (t == typeof(long))
                        value = el.ValueKind == JsonValueKind.Number ? el.GetInt64() : default(long);
                    else if (t == typeof(bool))
                        value = el.ValueKind == JsonValueKind.True ? true :
                                el.ValueKind == JsonValueKind.False ? false : (bool?)null;
                    else if (t == typeof(double))
                        value = el.ValueKind == JsonValueKind.Number ? el.GetDouble() : default(double);
                    else if (t == typeof(DateTime))
                        value = el.ValueKind == JsonValueKind.String ? el.GetDateTime() : default(DateTime);
                    else
                    {
                        // fallback voor complexe types (nested objects/arrays)
                        value = el.Deserialize(t);
                    }
                }
                catch
                {
                    // bij conversiefout, laat property op default
                }

                if (value != null || Nullable.GetUnderlyingType(prop.PropertyType) != null)
                    prop.SetValue(obj, value);
            }
        }
        return obj;
    }

    public static async Task<T> GetElicitResponse<T>(this IMcpServer mcpServer, CancellationToken cancellationToken) where T : new()
    {
        var elicitParams = CreateElicitRequestParamsForType<T>();
        var elicitResult = await mcpServer.ElicitAsync(elicitParams, cancellationToken: cancellationToken);
        elicitResult.EnsureAccept();

        if (elicitResult.Content == null)
        {
           throw new Exception(elicitResult.Action);
        }

        return elicitResult.Content.MapToObject<T>();
    }
}
