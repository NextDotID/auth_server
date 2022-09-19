using Microsoft.AspNetCore.Authentication;

namespace AuthServer.Server.Extensions;

public static class HttpContextExtensions
{
    public static async Task<AuthenticationScheme[]> GetExternalProvidersAsync(this HttpContext context)
    {
        context = context ?? throw new ArgumentNullException(nameof(context));
        IAuthenticationSchemeProvider schemeProviders = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
        IEnumerable<AuthenticationScheme> schemes = await schemeProviders.GetAllSchemesAsync();

        return schemes.Where(s => !string.IsNullOrEmpty(s.DisplayName)).ToArray();
    }

    public static async Task<bool> IsProviderSupportedAsync(this HttpContext context, string provider)
    {
        context = context ?? throw new ArgumentNullException(nameof(context));
        AuthenticationScheme[] schemes = await context.GetExternalProvidersAsync();
        return schemes.Any(s => s.DisplayName!.Equals(provider, StringComparison.OrdinalIgnoreCase));
    }
}
