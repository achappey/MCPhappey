using MCPhappey.Common.Extensions;
using Microsoft.Graph.Beta.Models;
using ModelContextProtocol.Protocol;

namespace MCPhappey.Tools.Extensions;

public static class GraphExtensions
{
    public static EmailAddress ToEmailAddress(
        this string mail) => new() { Address = mail.Trim() };

    public static Recipient ToRecipient(
        this string mail) => new() { EmailAddress = mail.ToEmailAddress() };


}
