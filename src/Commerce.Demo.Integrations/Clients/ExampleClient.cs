using System.Net.Http.Json;

namespace Commerce.Demo.Integrations.Clients;

internal sealed class ExampleClient(HttpClient client)
{
    public IAsyncEnumerable<string?> GetCitiesAsync()
    {
        return client.GetFromJsonAsAsyncEnumerable<string>("api/cities");
    }
}
