# API architecture

## Responsibilities

The API is an ASP.NET Core modular monolith backed by PostgreSQL through Entity Framework Core. It is the only component that reads or writes persistent application data.

| Folder | Responsibility |
| --- | --- |
| `Controllers/` | HTTP routing, authentication requirements, and resource authorization |
| `Dtos/` | Input validation and response contracts; do not serialize EF entities directly |
| `Models/` | Persisted users, pets, orders, and order applications |
| `Data/` | EF Core context, schema mappings, and safe design-time configuration |
| `Migrations/` | Versioned PostgreSQL schema changes |
| `Services/` | Token creation and cross-cutting application services |
| `infra/local/` | Local-only PostgreSQL container and ignored `.env` credentials |

Web, desktop, and later mobile clients share these APIs and business rules.

## Current authorization model

- Public registration and login issue a short-lived bearer token after validating credentials.
- Passwords are stored as BCrypt hashes. Password hashes are excluded from JSON responses.
- `owner`, `sitter`, and `both` are application roles. Administrative and support permissions are internal and must never be granted by public registration.
- The server derives the caller identity from the validated token. A request body cannot select another user's pets, orders, or address.
- Open-order search exposes an approximate area. The exact address is returned only to the owner or the assigned sitter.
- Auth routes have rate limits; CORS origins are configured separately from authentication.
- The packaged Windows renderer uses the `dogwalking://app` origin; production deployments must include it explicitly in `Cors:AllowedOrigins` alongside the web origin.

## Current request areas

- **Identity:** `/api/Users/register`, `/api/Users/login`, `/api/Users/me`
- **Pets:** `/api/Pets/mine`, `/api/Pets`
- **Orders:** create/list/detail, open-order search, applications, owner assignment, start, and completion under `/api/Orders`

New contracts should use DTOs, validate ownership on the server, and return only the data required by that caller.

## Manager support chat module — planned

Support is a server-owned conversation domain, available to owners and sitters in web/desktop and later mobile. Keep it distinct from an owner-to-sitter service conversation.

Initial entities:

- `SupportConversation`: requester, optional order reference, topic, status, assigned manager, and timestamps.
- `SupportMessage`: conversation, sender, text, sent time, and read time.
- `SupportAuditEvent`: staff assignment and status/access changes where operational review requires it.

Access rules:

- A requester can list and read only their own conversations and can send while the conversation is open.
- A manager can read/reply only with an internal support permission; assignment and resolution are audited.
- Every conversation and order reference is checked server-side. The client cannot grant staff privileges.
- Start with persisted text messages. Add private file storage only with MIME/size checks, access checks, retention, and malware scanning.

Suggested API shape: REST for conversation lists/history and creation, with an authenticated SignalR hub for live message delivery. Both paths call the same authorization and persistence service. If real-time delivery is deferred, the same model can start with REST polling without changing ownership rules.

## Future integrations

- **Maps:** geocoding and coordinates supplied by a server-side provider adapter; the API continues to control address precision.
- **Payments:** separate payment/refund/payout records and signed, idempotent provider webhooks. Order status is not payment status.
- **Notifications:** email and push are delivery adapters; messages and order state remain in the API database.

## Security work before public launch

- Email verification and password recovery.
- Database-level unique index for normalized email.
- Revocable/rotating sessions and an explicit sign-out policy.
- Atomic assignment under concurrent sitter applications.
- Audit and access tests for user data, addresses, support threads, and staff permissions.
- Production HTTPS, managed secrets, backups, retention, and operational monitoring.
