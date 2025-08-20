using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json;
using MCPhappey.Common.Extensions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.ModelContext;

public static class ModelContextUtils
{
    public enum ElicitFieldType
    {
        [EnumMember(Value = "String")]
        String,

        [EnumMember(Value = "Email")]
        Email,
        [EnumMember(Value = "Date")]
        Date,
        [EnumMember(Value = "DateTime")]
        DateTime,
        [EnumMember(Value = "Uri")]
        Uri,
        [EnumMember(Value = "Number")]
        Number,
        [EnumMember(Value = "Enum")]
        Enum,
        [EnumMember(Value = "Boolean")]
        Boolean
    }

    [Description("Test Elicit capabilites by requesting a form with a single field")]
    [McpServerTool(Title = "Elicit single-field form test",
        ReadOnly = true, Idempotent = true, OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextUtils_TestElicit(
        [Description("Elicit message")]
        string message,
        [Description("Field type")]
        ElicitFieldType fieldType,
        [Description("Field name")]
        string fieldName,
        [Description("Field description")]
        string description,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var propName = string.IsNullOrWhiteSpace(fieldName) ? "value" : fieldName;

        ElicitRequestParams.PrimitiveSchemaDefinition schema = fieldType switch
        {
            ElicitFieldType.String => new ElicitRequestParams.StringSchema
            {
                Title = propName,
                Description = description
            },
            ElicitFieldType.Email => new ElicitRequestParams.StringSchema
            {
                Title = propName,
                Description = description,
                Format = "email"
            },
            ElicitFieldType.Date => new ElicitRequestParams.StringSchema
            {
                Title = propName,
                Description = description,
                Format = "date"
            },
            ElicitFieldType.DateTime => new ElicitRequestParams.StringSchema
            {
                Title = propName,
                Description = description,
                Format = "date-time"
            },
            ElicitFieldType.Uri => new ElicitRequestParams.StringSchema
            {
                Title = propName,
                Description = description,
                Format = "uri"
            },
            ElicitFieldType.Number => new ElicitRequestParams.NumberSchema
            {
                Title = propName,
                Description = description
            },
            ElicitFieldType.Enum => new ElicitRequestParams.EnumSchema
            {
                Title = propName,
                Description = description,
                // Replace with your own options if desired
                Enum = ["Option1", "Option2"],
                EnumNames = ["Option 1", "Option 2"]
            },
            ElicitFieldType.Boolean => new ElicitRequestParams.BooleanSchema
            {
                Title = propName,
                Description = description
            },
            _ => throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, null)
        };

        var elicitRequest = new ElicitRequestParams
        {
            RequestedSchema = new ElicitRequestParams.RequestSchema
            {
                Properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>
                {
                    [propName] = schema
                }
            },
            Message = message
        };

        var elicitResult = await requestContext.Server.ElicitAsync(elicitRequest, cancellationToken: cancellationToken);
        return JsonSerializer.Serialize(elicitResult, JsonSerializerOptions.Web).ToTextCallToolResponse();
    }

}

