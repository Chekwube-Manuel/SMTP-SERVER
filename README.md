# Custom Multi-Tenant Email Server

This project is a starter C# ASP.NET Core email service with multi-tenant support, API key authentication, send quota tracking, and SMTP relay via MailKit.

## Features

- Multi-tenant email sending with tenant API keys
- Tenant creation and listing endpoints
- Send API protected by `X-API-Key`
- Quota tracking for daily messages
- SMTP relay using `MailKit`
- SQLite persistence for tenants and send events

## Run locally

1. Install .NET 8 SDK
2. Open a terminal in `c:\Users\NWCS\Desktop\SMTP_SERVER`
3. Run:

```powershell
dotnet restore
dotnet run --project EmailServer.csproj
```

## Sample API usage

Create a tenant:

```http
POST /api/tenants
Content-Type: application/json

{
  "name": "Acme Corp",
  "domain": "acme.com",
  "maxMessagesPerDay": 500
}
```

Send email:

```http
POST /api/send
X-API-Key: <tenant-api-key>
Content-Type: application/json

{
  "from": "hello@acme.com",
  "to": ["user@example.com"],
  "subject": "Hello from Acme",
  "body": "This is a test email."
}
```

Get tenant usage:

```http
GET /api/usage/{tenantId}
```

## Notes

- Update SMTP settings in `appsettings.json` or environment variables
- The project creates `emails.db` automatically on startup
- Use `http://localhost:5000/swagger` to explore endpoints
