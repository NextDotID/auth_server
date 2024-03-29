﻿using System.Security.Claims;
using System.Security.Principal;
using AuthServer.Server.Extensions;
using AuthServer.Server.Services.Avatar;
using AuthServer.Server.Services.Proof;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nethereum.Hex.HexConvertors.Extensions;

namespace AuthServer.Server.Controllers;
public class AuthenticationController : Controller
{
    private readonly IProofService proofService;
    private readonly IAvatarService avatarService;

    public AuthenticationController(IProofService proofService, IAvatarService avatarService)
    {
        this.proofService = proofService;
        this.avatarService = avatarService;
    }

    [HttpGet("~/authenticate")]
    public async Task<IActionResult> SignIn(
        [FromQuery(Name = "redirect_uri")] string redirectUri,
        [FromQuery(Name = "expired_at")] long expiredAt,
        [FromQuery(Name = "state")] string state)
    {
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out Uri? uri) ||
            !uri.IsAbsoluteUri ||
            !string.IsNullOrEmpty(uri.Query))
        {
            return View("Error", "BadRequest");
        }

        Response.Cookies.Delete("redirect_uri");
        Response.Cookies.Delete("expired_at");
        Response.Cookies.Delete("state");

        Response.Cookies.Append("redirect_uri", redirectUri);
        Response.Cookies.Append("expired_at", expiredAt.ToString());
        Response.Cookies.Append("state", state);

        return View("Authenticate", await HttpContext.GetExternalProvidersAsync());
    }

    [HttpPost("~/authenticate")]
    public async Task<IActionResult> SignIn([FromForm] string provider)
    {
        if (string.IsNullOrWhiteSpace(provider) || !await HttpContext.IsProviderSupportedAsync(provider))
        {
            return View("Error", "BadRequest");
        }

        return Challenge(new AuthenticationProperties { RedirectUri = "/authorize" }, provider);
    }

    [Authorize]
    [HttpGet("~/authorize")]
    public async Task<IActionResult> Authorize()
    {
        (var platform, var identity) = ExtractIdentity(User.Identity);
        if (string.IsNullOrEmpty(platform) || string.IsNullOrEmpty(identity))
        {
            return View("Error", "Forbidden");
        }

        var localAvatars = avatarService.GetAvailableAvatars().ToList();
        List<string> selectables = await proofService
            .FindAvatarsAsync(platform, identity)
            .Where(s => localAvatars.Contains(s, StringComparer.OrdinalIgnoreCase))
            .ToListAsync();

        if (!selectables.Any())
        {
            return View("Error", "Unauthorized");
        }

        if (Request.Cookies.TryGetValue("redirect_uri", out var redirectUri) &&
            Uri.TryCreate(redirectUri, UriKind.Absolute, out Uri? uri) &&
            Request.Cookies.TryGetValue("expired_at", out var expiredAtText) &&
            long.TryParse(expiredAtText, out var expiredAt))
        {
            TempData["redirect_uri"] = uri.ToString();
            TempData["expired_at"] = DateTimeOffset.FromUnixTimeSeconds(expiredAt);
            return View("Authorize", selectables);
        }

        return View("Error", "BadRequest");
    }

    [Authorize]
    [HttpPost("~/authorize")]
    public async Task<IActionResult> Authorize([FromForm] string avatar)
    {
        (var platform, var identity) = ExtractIdentity(User.Identity);
        if (string.IsNullOrEmpty(platform) || string.IsNullOrEmpty(identity))
        {
            return View("Error", "Forbidden");
        }

        IEnumerable<string> avatars = avatarService.GetAvailableAvatars();
        if (!avatars.Contains(avatar, StringComparer.OrdinalIgnoreCase))
        {
            return View("Error", "BadRequest");
        }

        var found = await proofService
            .FindAvatarsAsync(platform, identity)
            .AnyAsync(i => i.IsTheSameHex(avatar));
        if (!found)
        {
            return View("Error", "Unauthorized");
        }

        if (Request.Cookies.TryGetValue("redirect_uri", out var redirectUri) &&
            Uri.TryCreate(redirectUri, UriKind.Absolute, out Uri? uri) &&
            Request.Cookies.TryGetValue("expired_at", out var expiredAtText) &&
            long.TryParse(expiredAtText, out var expiredAt) &&
            Request.Cookies.TryGetValue("state", out var state) &&
            !string.IsNullOrEmpty(state))
        {
            var sig = await avatarService.GetBase58SignatureAsync(avatar, redirectUri, expiredAt, state);
            var subkey = await avatarService.GetSubkeyAsync(avatar);
            var subCertSig = await avatarService.GetSubkeyCertificationSignatureAsync(avatar);

            return Redirect($"{uri}?avatar={avatar}&expired_at={expiredAt}&state={state}&subkey={subkey}&subkey_cert_sig={subCertSig}&sig={sig}");
        }

        return View("Error", "BadRequest");
    }

    [HttpGet("~/signout")]
    [HttpPost("~/signout")]
    public IActionResult SignOutCurrentUser()
    {
        // Instruct the cookies middleware to delete the local cookie created
        // when the user agent is redirected from the external identity provider
        // after a successful authentication flow (e.g Google or Facebook).
        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private (string? Platform, string? Identity) ExtractIdentity(IIdentity? id)
    {
        string? platform, identity;
        switch (id?.AuthenticationType)
        {
            case "Discord":
                platform = "discord";
                identity = id.Name +
                           "#" +
                           (id as ClaimsIdentity)!.Claims.FirstOrDefault(c => c.Type == "urn:discord:user:discriminator")?.Value;
                break;
            case "Twitter":
                platform = "twitter";
                identity = id.Name;
                break;
            default:
                platform = id?.AuthenticationType?.ToLower();
                identity = id?.Name;
                break;
        }

        return (platform, identity);
    }
}
