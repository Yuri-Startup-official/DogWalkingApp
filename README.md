# DogWalking API

ASP.NET Core API for the web and Windows desktop clients. The API owns access control, order rules, and persistence; clients do not connect to PostgreSQL directly.

## Repository layout

```text
Controllers/           HTTP endpoints and role/resource checks
Dtos/                  Validated request and response shapes
Models/                EF Core entities
Data/                  DbContext and design-time migration factory
Migrations/             PostgreSQL schema history
Services/               JWT and shared application services
infra/local/            Local-only PostgreSQL Compose setup
docs/architecture/      API modules and security boundaries
docs/development/       Local configuration and migration steps
```

## Local development

Follow [the local setup guide](docs/development/LOCAL_SETUP.md). It keeps database and JWT credentials in .NET User Secrets and uses a separate local `dogwalking_test` database.

## Current API surface

- `POST /api/Users/register`, `POST /api/Users/login`, `GET /api/Users/me`
- `GET /api/Pets/mine`, `POST /api/Pets`
- `POST /api/Orders`, `GET /api/Orders/mine`, `GET /api/Orders/available`
- Order applications, owner assignment, start, and completion endpoints under `/api/Orders/{id}`

See [the API architecture](docs/architecture/API.md) for responsibilities, roles, and planned manager support chat.
