using System.Collections.Concurrent;
using System.Text;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using SimpleBase;

namespace AuthServer.Server.Services.Avatar;

public class Secp256K1AvatarService : IAvatarService
{
    private readonly IDictionary<string, EthECKey> subkeys;
    private readonly IDictionary<string, string> certificationSigs;

    public Secp256K1AvatarService(IConfiguration configuration)
    {
        subkeys = new ConcurrentDictionary<string, EthECKey>();
        certificationSigs = new ConcurrentDictionary<string, string>();
        var signer = new EthereumMessageSigner();

        foreach (AvatarStore avatarStore in configuration.GetSection("Avatars").GetChildren().Select(a => a.Get<AvatarStore>()))
        {
            EthECKey subkey = new EthECKey(avatarStore.Subkey.PrivateKey);
            var recoveredAvatar = EthECKey.RecoverFromSignature(
                MessageSigner.ExtractEcdsaSignature(avatarStore.Subkey.CertificationSignature),
                signer.HashPrefixedMessage(Encoding.UTF8.GetBytes($"Subkey certification signature: {subkey.GetPubKey(true).ToHex(true)}"))).GetPubKey(true).ToHex(true);
            if (avatarStore.Avatar.IsTheSameHex(recoveredAvatar))
            {
                subkeys.Add(avatarStore.Avatar, subkey);
                certificationSigs.Add(avatarStore.Avatar, avatarStore.Subkey.CertificationSignature);
            }
        }
    }

    public ValueTask<string> GetBase58SignatureAsync(string avatar, string redirectUri, long expiredAt, string state)
    {
        if (!subkeys.TryGetValue(avatar, out EthECKey? key))
        {
            return ValueTask.FromResult(string.Empty);
        }

        var signer = new EthereumMessageSigner();

        // Construct a message having potential risk?
        var message = $"avatar={avatar}\nredirect_uri={redirectUri}\nexpired_at={expiredAt}\nstate={state}";
        var sig = signer.Sign(Encoding.UTF8.GetBytes(message), key).HexToByteArray();
        return ValueTask.FromResult(Base58.Bitcoin.Encode(sig));
    }

    public ValueTask<string> GetSubkeyAsync(string avatar)
    {
        if (!subkeys.TryGetValue(avatar, out EthECKey? key))
        {
            return ValueTask.FromResult(string.Empty);
        }

        return ValueTask.FromResult(key.GetPubKey(true).ToHex(true));
    }

    public ValueTask<string> GetSubkeyCertificationSignatureAsync(string avatar)
    {
        return ValueTask.FromResult(certificationSigs.TryGetValue(avatar, out var sig)
            ? sig
            : string.Empty);
    }

    public IEnumerable<string> GetAvailableAvatars()
    {
        return subkeys.Keys;
    }

    public class AvatarStore
    {
        public string Avatar { get; set; } = null!;
        public Subkey Subkey { get; set; } = null!;
    }

    public class Subkey
    {
        public string PrivateKey { get; set; } = null!;
        public string CertificationSignature { get; set; } = null!;
    }
}
