using Microsoft.Graph.Beta.Models;

namespace MCPhappey.Tools.Extensions;

public static class GraphExtensions
{
    public static EmailAddress ToEmailAddress(
        this string mail) => new() { Address = mail.Trim() };

    public static Recipient ToRecipient(
        this string mail) => new() { EmailAddress = mail.ToEmailAddress() };


}
