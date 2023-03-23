using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using AuthServer.Server.Services.Avatar;
using AuthServer.Server.Services.Proof;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using Nethereum.Util;

namespace AuthServer.Server.Controllers;
[ApiController]
[Route("api/[controller]")]
public class EthereumController : ControllerBase
{
    private readonly IProofService proofService;
    private readonly IAvatarService avatarService;

    public EthereumController(IProofService proofService, IAvatarService avatarService)
    {
        this.proofService = proofService;
        this.avatarService = avatarService;
    }

    [HttpGet("payload/{wallet}")]
    public IActionResult PreSignIn([FromRoute, Required] string wallet)
    {
        if (!wallet.IsValidEthereumAddressHexFormat())
        {
            return BadRequest();
        }

        var guid = Guid.NewGuid().ToString();
        Response.Cookies.Delete("ethereum_nonce");
        Response.Cookies.Append("ethereum_nonce", guid);
        Response.Cookies.Delete("ethereum_address");
        Response.Cookies.Append("ethereum_address", wallet);

        var payload = $"Address: {wallet}\nLogin: {Request.Host}\nNonce: {guid}";
        return Ok(new { payload });
    }

    [HttpPost("verify/{signature}")]
    public async Task<IActionResult> SignIn([FromRoute, Required] string signature)
    {
        var nonce = Request.Cookies["ethereum_nonce"];
        var wallet = Request.Cookies["ethereum_address"];

        if (!signature.IsHex() ||
            string.IsNullOrWhiteSpace(nonce) ||
            string.IsNullOrWhiteSpace(wallet) ||
            !wallet.IsValidEthereumAddressHexFormat())
        {
            return BadRequest();
        }

        var payload = $"Address: {wallet}\nLogin: {Request.Host}\nNonce: {nonce}";
        var signer = new EthereumMessageSigner();
        var address = EthECKey.RecoverFromSignature(
                MessageSigner.ExtractEcdsaSignature(signature),
                signer.HashPrefixedMessage(Encoding.UTF8.GetBytes(payload)))
            .GetPublicAddress();

        if (!wallet.IsTheSameAddress(address))
        {
            return Unauthorized();
        }

        var localAvatars = avatarService.GetAvailableAvatars().ToList();
        List<string> selectables = await proofService
            .FindAvatarsAsync("ethereum", wallet.ToLower())
            .Where(s => localAvatars.Contains(s, StringComparer.OrdinalIgnoreCase))
            .ToListAsync();

        if (!localAvatars.Intersect(selectables).Any())
        {
            return Unauthorized();
        }

        var principal = new ClaimsPrincipal();
        principal.AddIdentity(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, wallet.ToLower()) }, "ethereum"));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Ok();
    }
}
