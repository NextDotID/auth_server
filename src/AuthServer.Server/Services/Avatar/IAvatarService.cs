namespace AuthServer.Server.Services.Avatar;

public interface IAvatarService
{
    ValueTask<string> GetBase58SignatureAsync(string avatar, string redirectUri, long expiredAt, string state);

    ValueTask<string> GetSubkeyAsync(string avatar);

    ValueTask<string> GetSubkeyCertificationSignatureAsync(string avatar);

    IEnumerable<string> GetAvailableAvatars();
}
