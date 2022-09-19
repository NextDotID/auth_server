using System.Security.Claims;
using System.Text.Json.Serialization;

namespace AuthServer.Server.Services.Proof;

public class ProofService : IProofService
{
    private readonly HttpClient httpClient;

    public ProofService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(configuration.GetValue<string>("Proof:Endpoint"));
    }

    public async IAsyncEnumerable<string> FindAvatarsAsync(ClaimsIdentity id)
    {
        string? platform, identity;
        switch (id.AuthenticationType)
        {
            case "Discord":
                platform = "discord";
                identity = id.Name +
                           "#" +
                           id.Claims.FirstOrDefault(c => c.Type == "urn:discord:user:discriminator")?.Value;
                break;
            case "Twitter":
                platform = "twitter";
                identity = id.Name;
                break;
            default:
                platform = id.AuthenticationType?.ToLower();
                identity = id.Name;
                break;
        }

        var page = 0;
        while (true)
        {
            var uri = $"/v1/proof?platform={platform}&identity={identity}&page={page++}";
            ProofResponse? response = await httpClient.GetFromJsonAsync<ProofResponse>(uri);
            if (response == null || !response.Ids.Any())
            {
                break;
            }

            IEnumerable<string> avatars = response.Ids.Select(i => i.Avatar);
            foreach (var avatar in avatars)
            {
                yield return avatar;
            }

            if (response.Pagination.Next == 0)
            {
                break;
            }
        }
    }

    public record ProofResponse(
        [property: JsonPropertyName("pagination")] Pagination Pagination,
        [property: JsonPropertyName("ids")] Id[] Ids);

    public record Pagination(
        [property: JsonPropertyName("total")] int Total,
        [property: JsonPropertyName("per")] int Per,
        [property: JsonPropertyName("current")] int Current,
        [property: JsonPropertyName("next")] int Next);

    public record Id(
        [property: JsonPropertyName("avatar")] string Avatar,
        [property: JsonPropertyName("proofs")] Proof[] Proofs);

    public record Proof(
        [property: JsonPropertyName("platform")]
        string Platform,
        [property: JsonPropertyName("identity")]
        string Identity,
        [property: JsonPropertyName("is_valid")]
        bool IsValid);
}
