
using System.Net.Http.Headers;
using MCPhappey.Common.Models;

namespace MCPhappey.Scrapers.Extensions;

public static class HttpExtensions
{
    public static HttpClient WithBearer(
           this HttpClient client,
           string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }
}

