using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using TmsOmsIntegration.Api.Settings;

namespace TmsOmsIntegration.Api.Filters;

internal sealed class ApiKeyAuthFilter(IOptions<TmsWebhookSettings> options) : IAuthorizationFilter
{
    public const string HeaderName = "X-Api-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var providedKey = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (!IsValid(providedKey, options.Value.ApiKey))
            context.Result = new UnauthorizedResult();
    }

    private static bool IsValid(string providedKey, string expectedKey)
    {
        if (string.IsNullOrEmpty(expectedKey) || string.IsNullOrEmpty(providedKey))
            return false;

        // Hashing first gives both sides the same length, so the constant-time
        // comparison does not leak the key length either.
        return CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(providedKey)),
            SHA256.HashData(Encoding.UTF8.GetBytes(expectedKey)));
    }
}
