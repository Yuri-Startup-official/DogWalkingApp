# API architecture

## Responsibilities

The API is an ASP.NET Core modular monolith backed by PostgreSQL through Entity Framework Core. It is the only component that reads or writes persistent application data.

| Folder | Responsibility |
| --- | --- |
| `Controllers/` | HTTP routing, authentication requirements, and resource authorization |
| `Dtos/` | Input validation and response contracts; do not serialize EF entities directly |
| `Models/` | Persisted users, pets, orders, applications, and support conversations |
| `Data/` | EF Core context, schema mappings, and safe design-time configuration |
| `Migrations/` | Versioned PostgreSQL schema changes |
| `Services/` | Token creation and cross-cutting application services |
| `infra/local/` | Local-only PostgreSQL container and ignored `.env` credentials |

Web, desktop, and later mobile clients share these APIs and business rules.

The API remains one deployable modular monolith while the product is small. Keep modules independent by domain; introduce separate services only if scale or team ownership later requires them. Product-level boundaries and target folders are described in the frontend repository's `docs/architecture/SYSTEM_ARCHITECTURE.md`.

## Current authorization model

- Public registration and login issue a short-lived bearer token after validating credentials.
- Passwords are stored as BCrypt hashes. Password hashes are excluded from JSON responses.
- `owner`, `sitter`, and `both` are application roles. Administrative and support permissions are internal and must never be granted by public registration.
- A `both` account uses one identity for both workspaces. The selected workspace is not an authorization boundary; provider actions must be checked against provider profile/verification state as that workflow is added.
- The server derives the caller identity from the validated token. A request body cannot select another user's pets, orders, or address.
- Open-order search exposes an approximate area. The exact address is returned only to the owner or the assigned sitter.
- Auth routes have rate limits; CORS origins are configured separately from authentication.
- The packaged Windows renderer uses the `dogwalking://app` origin; production deployments must include it explicitly in `Cors:AllowedOrigins` alongside the web origin.

## Current request areas

- **Identity:** `/api/Users/register`, `/api/Users/login`, `/api/Users/me`
- **Pets:** `/api/Pets/mine`, `/api/Pets`
- **Orders:** create/list/detail, open-order search, applications, owner assignment, start, and completion under `/api/Orders`
- **Support:** customer conversation list/create, protected message history, staff assignment, reply, and status under `/api/support/conversations`

New contracts should use DTOs, validate ownership on the server, and return only the data required by that caller.

## Target domain boundaries

Keep HTTP controllers thin and organize new use cases within these domains:

- **Identity:** registration, login, recovery, sessions, verification, and role checks.
- **Profiles and pets:** owner/provider profile, pet details, and privacy-sensitive documents.
- **Services and availability:** provider service types, service areas, hours, and capacity.
- **Marketplace and orders:** search, order lifecycle, applications, assignment, cancellation, and disputes.
- **Conversations:** order-linked owner/provider communication.
- **Support:** private customer-to-manager conversations and staff queue.
- **Payments:** payment attempts, refunds, payout records, reconciliation, and webhook processing.
- **Trust and notifications:** reviews, safety reports, in-app events, and delivery adapters.

The existing project is currently organized by technical folder (`Controllers/`, `Models/`, `Dtos/`, `Data/`). Do not move code solely to match the target folder diagram; preserve migration history and keep domain boundaries explicit as modules grow.

## Manager support chat module

Support is a server-owned conversation domain available to owners and sitters in web/desktop and later mobile. It is separate from owner-to-sitter order conversations. The current desktop/web client refreshes the conversation list every five seconds and the active thread every four seconds.

Persisted entities:

- `SupportConversation`: requester, optional order reference, topic, status, assigned manager, and timestamps.
- `SupportMessage`: conversation, sender, text, and sent time.

Access rules:

- A requester can list and read only their own conversations. A conversation can reference only an order they own or are assigned to.
- A manager account has the internal `support` role, cannot be created through public registration, and must claim an unassigned conversation before replying.
- Managers see the unassigned queue and conversations assigned to themselves. A concurrent claim can succeed for only one manager.
- The server checks conversation ownership and staff role on every request. Customer replies reopen a conversation; staff replies set it to waiting; support staff can resolve or reopen it.
- Every conversation and order reference is checked server-side. The client cannot grant staff privileges.

The first version uses REST and polling; an authenticated SignalR hub can replace polling later without changing message ownership rules. Messages are text-only. File attachments, read receipts, and staff audit history are not implemented yet and should be designed before production support operations.

## Future integrations

- **Maps:** geocoding and coordinates supplied by a server-side provider adapter; the API continues to control address precision.
- **Payments:** separate payment/refund/payout records and signed, idempotent provider webhooks. Order status is not payment status.
- **Notifications:** email and push are delivery adapters; messages and order state remain in the API database.

## Security work before public launch

- Email verification and password recovery.
- Database-level unique index for normalized email.
- Revocable/rotating sessions and an explicit sign-out policy.
- Staff audit history for assignment and resolution actions.
- Audit and access tests for user data, addresses, support threads, and staff permissions.
- Production HTTPS, managed secrets, backups, retention, and operational monitoring.

The API currently has no explicit `/v1` route prefix. Decide and document API versioning before public mobile clients or third-party consumers depend on these contracts.
