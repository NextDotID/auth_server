import json
import secrets
import base58
from eth_keys import keys

print('Please input your private key of Avatar:')
avatar_sk_hex = input().strip('0x')
avatar_sk = keys.PrivateKey(bytes.fromhex(avatar_sk_hex))
avatar_vk_hex = avatar_sk.public_key.to_compressed_bytes().hex()

sk_bytes = secrets.token_bytes(32)
sk = keys.PrivateKey(sk_bytes)
sk_hex = sk_bytes.hex()
vk_hex = sk.public_key.to_compressed_bytes().hex()

payload = ('Subkey certification signature: ' + vk_hex).encode('utf-8')
msg = ('\x19Ethereum Signed Message:\n' + str(len(payload))).encode('utf-8') + payload
sk_cert_sig = avatar_sk.sign_msg(msg)

config_json = {
  "Avatars": [
    {
      "Avatar": '0x' + avatar_vk_hex,
      "Subkey": {
        "PrivateKey": '0x' + sk_hex,
        "CertificationSignature": base58.b58encode(sk_cert_sig.to_bytes()).decode('utf-8')
      }
    }
  ],
}

print(json.dumps(config_json, indent=2))
