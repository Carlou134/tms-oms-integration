using TmsOmsIntegration.Api.Webhooks.Tms;
using TmsOmsIntegration.Application.TmsEvents;
using TmsOmsIntegration.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddOptions<TmsWebhookOptions>()
    .Bind(builder.Configuration.GetSection(TmsWebhookOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey),
        $"{TmsWebhookOptions.SectionName}:ApiKey must be configured (see appsettings.model.json).")
    .ValidateOnStart();

builder.Services.AddScoped<ReceiveTmsEvent>();
builder.Services.AddInfrastructure();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
