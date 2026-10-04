using System.Text.Json.Serialization;
using TmsOmsIntegration.Api.Settings;
using TmsOmsIntegration.Application.History;
using TmsOmsIntegration.Application.TmsEvents;
using TmsOmsIntegration.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddOptions<TmsWebhookSettings>()
    .Bind(builder.Configuration.GetSection(TmsWebhookSettings.SectionName))
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.ApiKey),
        $"{TmsWebhookSettings.SectionName}:ApiKey must be configured (see appsettings.model.json).")
    .ValidateOnStart();

builder.Services.AddScoped<ReceiveTmsEvent>();
builder.Services.AddScoped<GetOrderHistory>();
builder.Services.AddInfrastructure();

builder.Services.AddSubscriber<TmsEventReceived, ProcessTmsEventHandler>();
builder.Services.AddSubscriber<TmsEventProcessed, RecordHistoryHandler>();

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
