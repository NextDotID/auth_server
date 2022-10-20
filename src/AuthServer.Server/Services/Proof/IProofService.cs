using System.Security.Claims;

namespace AuthServer.Server.Services.Proof;

public interface IProofService
{
    IAsyncEnumerable<string> FindAvatarsAsync(string platform, string identity);
}
