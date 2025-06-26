
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;

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

    public static ElicitRequestParams CreateElicitRequestParamsForType<T>(this string message)
    {
        var properties = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(
                prop => prop.GetJsonPropertyName(),
                prop => prop.ToElicitSchemaDef()
            );

        var required = GetRequiredProperties(typeof(T));

        return new ElicitRequestParams
        {
            Message = message,
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
            var enumNames = Enum.GetNames(enumType);
            // Try to get [Display(Name="...")] friendly names
            var friendlyNames = enumType
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(field =>
                {
                    var display = field.GetCustomAttribute<DisplayAttribute>();
                    return display?.Name ?? field.Name;
                })
                .ToArray();

            // Only set enumNames if they differ from enum values
            return new ElicitRequestParams.EnumSchema
            {
                Title = title,
                Description = desc,
                Enum = enumNames,
                EnumNames = !enumNames.SequenceEqual(friendlyNames) ? friendlyNames : null
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
}
