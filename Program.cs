using EmailServer.Data;
using EmailServer.Models;
using EmailServer.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<EmailServerContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("EmailDatabase") ?? "Data Source=emails.db"));
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IApiKeyValidator, ApiKeyValidator>();
builder.Services.AddScoped<IQuotaService, QuotaService>();

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EmailServerContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/api/tenants", async (TenantCreateRequest request, ITenantService service) =>
{
    var tenant = await service.CreateTenantAsync(request.Name, request.Domain, request.MaxMessagesPerDay);
    return Results.Created($"/api/tenants/{tenant.Id}", tenant);
});

app.MapGet("/api/tenants/{tenantId}", async (Guid tenantId, ITenantService service) =>
{
    var tenant = await service.GetTenantAsync(tenantId);
    return tenant is not null ? Results.Ok(tenant) : Results.NotFound();
});

app.MapPost("/api/send", async (EmailSendRequest request, HttpContext http, IApiKeyValidator apiKeyValidator, ITenantService tenantService, IEmailSender sender, IQuotaService quotaService, EmailServerContext db) =>
{
    if (!http.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
    {
        return Results.Unauthorized();
    }

    var tenant = await apiKeyValidator.ValidateAsync(apiKey);
    if (tenant is null)
    {
        return Results.Unauthorized();
    }

    if (!await quotaService.CanSendAsync(tenant))
    {
        return Results.StatusCode(429);
    }

    var message = new EmailMessage
    {
        TenantId = tenant.Id,
        From = request.From,
        To = request.To,
        Subject = request.Subject,
        Body = request.Body,
        CreatedAt = DateTime.UtcNow
    };

    await db.EmailMessages.AddAsync(message);
    await db.SaveChangesAsync();

    var sendResult = await sender.SendAsync(tenant, request);
    if (!sendResult)
    {
        return Results.StatusCode(502);
    }

    await quotaService.RecordSendAsync(tenant, message);
    return Results.Ok(new { message = "queued", tenantId = tenant.Id, messageId = message.Id });
});

app.MapGet("/api/usage/{tenantId}", async (Guid tenantId, ITenantService service, IQuotaService quotaService) =>
{
    var tenant = await service.GetTenantAsync(tenantId);
    if (tenant is null)
    {
        return Results.NotFound();
    }

    var usage = await quotaService.GetUsageAsync(tenant);
    return Results.Ok(usage);
});

app.MapGet("/api/tenants", async (ITenantService service) => await service.GetTenantsAsync());

app.Run();

public partial class Program { }

record TenantCreateRequest(string Name, string Domain, int MaxMessagesPerDay);
record EmailSendRequest(string From, List<string> To, string Subject, string Body);
