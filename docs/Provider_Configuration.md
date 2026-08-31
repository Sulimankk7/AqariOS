# Communication Provider Configuration

## Active providers

| Capability | Application abstraction | Active infrastructure provider |
| --- | --- | --- |
| Email | `IEmailSender` | `ResendEmailSender` |
| SMS | `ISmsSender` | `InfobipSmsSender` |

Application code depends on these interfaces only. Provider SDKs and `HttpClient` usage remain in Infrastructure.

## Configuration

Set production values with the deployment environment's secret/configuration mechanism. The API supports ASP.NET Core environment-variable keys:

```text
Resend__ApiKey
Resend__SenderEmail
Resend__SenderName

Infobip__ApiKey
Infobip__BaseUrl
Infobip__Sender

Otp__HashKey
```

`src/PropertyOS.Api/appsettings.json` contains safe defaults only. For local development, use the ignored `src/PropertyOS.Api/appsettings.Local.json`; it is loaded only when `ASPNETCORE_ENVIRONMENT=Development`. Environment variables are applied last and therefore override both default and local files. Production must not depend on `appsettings.Local.json`.

Never commit API keys or put them in frontend configuration. Provider implementations do not log API keys, email/SMS bodies, or unmasked recipients.

`Otp__HashKey` is a separate high-entropy server secret used for HMAC-protecting persisted OTP codes; it must be supplied through the same local-secret or production-secret mechanism and must never be logged.

## Development test endpoints

These endpoints exist only while the API runs in Development, require an authenticated caller, use the provider-neutral abstractions, and return sanitized errors:

- `POST /api/v1/development/email/test`
- `POST /api/v1/development/sms/test`

They are diagnostic endpoints, not production communication APIs. Outside Development they return `404`.

## Provider notes

- Resend uses its HTTPS API with the configured sender. A Resend account may require a verified sending domain; trial/testing restrictions depend on the account.
- Infobip sends `POST /sms/3/messages` with `Authorization: App {API_KEY}`. SMS destinations must be E.164 values including `+`. The sender is configured rather than invented. Infobip trial accounts can restrict sender IDs and delivery to verified destinations; use the sender and destination rules shown in the Infobip dashboard.

Legacy Brevo and Twilio code remains isolated and is not registered for `IEmailSender` or `ISmsSender`.
