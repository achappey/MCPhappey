using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MCPhappey.Common.Models;
using Microsoft.Graph.Beta.Models;

namespace MCPhappey.Tools.Graph.Users;

[Description("Please fill in the user details.")]
public class GraphNewUser
{
    [JsonPropertyName("givenName")]
    [Required]
    [Description("The users's given name.")]
    public string GivenName { get; set; } = default!;

    [JsonPropertyName("displayName")]
    [Required]
    [Description("The users's display name.")]
    public string DisplayName { get; set; } = default!;

    [JsonPropertyName("userPrincipalName")]
    [Required]
    [EmailAddress]
    [Description("The users's principal name.")]
    public string UserPrincipalName { get; set; } = default!;

    [JsonPropertyName("mailNickname")]
    [Required]
    [Description("The users's mail nickname.")]
    public string MailNickname { get; set; } = default!;

    [JsonPropertyName("jobTitle")]
    [Required]
    [Description("The users's job title.")]
    public string JobTitle { get; set; } = default!;


    [JsonPropertyName("accountEnabled")]
    [Required]
    [DefaultValue(true)]
    [Description("Account enabled.")]
    public bool AccountEnabled { get; set; }

    [JsonPropertyName("forceChangePasswordNextSignIn")]
    [Required]
    [DefaultValue(true)]
    [Description("Force password change.")]
    public bool ForceChangePasswordNextSignIn { get; set; }

    [JsonPropertyName("password")]
    [Required]
    [Description("The users's password.")]
    public string Password { get; set; } = default!;

    [JsonPropertyName("department")]
    [Description("The users's department.")]
    public string? Department { get; set; }

    [JsonPropertyName("companyName")]
    [Description("The users's company name.")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("mobilePhone")]
    [Description("The users's mobile phone number.")]
    public string? MobilePhone { get; set; }

    [JsonPropertyName("businessPhone")]
    [Description("The users's business phone number.")]
    public string? BusinessPhone { get; set; }
}

[Description("Please fill in the user details.")]
public class GraphUpdateUser
{
    [Required]
    [JsonPropertyName("givenName")]
    [Description("The users's given name.")]
    public string GivenName { get; set; } = default!;

    [JsonPropertyName("displayName")]
    [Required]
    [Description("The users's display name.")]
    public string DisplayName { get; set; } = default!;

    [JsonPropertyName("jobTitle")]
    [Required]
    [Description("The users's job title.")]
    public string JobTitle { get; set; } = default!;

    [JsonPropertyName("accountEnabled")]
    [Required]
    [DefaultValue(true)]
    [Description("Account enabled.")]
    public bool AccountEnabled { get; set; }

    [JsonPropertyName("department")]
    [Description("The users's department.")]
    public string? Department { get; set; }

    [JsonPropertyName("companyName")]
    [Description("The users's company name.")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("mobilePhone")]
    [Description("The users's mobile phone number.")]
    public string? MobilePhone { get; set; }

    [JsonPropertyName("businessPhone")]
    [Description("The users's business phone number.")]
    public string? BusinessPhone { get; set; }

}



[Description("Please fill in the user name: {0}")]
public class GraphDeleteUser : IHasName
{
    [JsonPropertyName("name")]
    [Description("Name of the user.")]
    public string Name { get; set; } = default!;
}