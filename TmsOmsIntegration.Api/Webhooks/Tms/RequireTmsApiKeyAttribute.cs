using Microsoft.AspNetCore.Mvc;

namespace TmsOmsIntegration.Api.Webhooks.Tms;

/// <summary>
/// Applies <see cref="ApiKeyAuthFilter"/>; TypeFilter resolves its dependencies from DI,
/// so the filter itself does not need to be registered.
/// </summary>
public sealed class RequireTmsApiKeyAttribute() : TypeFilterAttribute(typeof(ApiKeyAuthFilter));
