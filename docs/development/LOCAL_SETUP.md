# Local API setup

## Requirements

- .NET 9 SDK
- Docker Desktop with its Linux container engine running
- PowerShell

The Compose database is for local development and testing only. Its port is bound to `127.0.0.1`; do not reuse these credentials or this database for production.

## Start an isolated PostgreSQL database

The Compose files are kept in `infra/local/`. From the API project root:

```powershell
Push-Location infra/local
Copy-Item .env.example .env
```

Edit `.env` and replace `POSTGRES_PASSWORD` with a random local password. Then start PostgreSQL and wait until it is healthy:

```powershell
docker compose up -d
docker compose ps
Pop-Location
```

The Compose file creates a separate `dogwalking_test` database and a named volume. To stop it later while preserving test data:

```powershell
Push-Location infra/local
docker compose down
Pop-Location
```

## Store local API secrets

Still in the API project root, store the connection string and a fresh JWT signing key in .NET User Secrets. Replace the password value with the one from `.env`:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=dogwalking_test;Username=dogwalking;Password=replace-with-your-local-password"
$jwtKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
dotnet user-secrets set "Jwt:Key" $jwtKey
```

Never commit `.env`, database passwords, or JWT signing keys. If any signing key previously committed to the repository was used in a live environment, rotate it.

## Apply migrations and run the API

The repository pins `dotnet-ef` 9.0.23. If the machine only has the .NET 10 runtime installed, enable major runtime roll-forward for the tool:

```powershell
$env:DOTNET_ROLL_FORWARD = "Major"
dotnet tool restore
dotnet ef database update
dotnet run --urls http://localhost:5199
```

The design-time context reads configuration in this order: `appsettings.json`, `appsettings.Development.json`, .NET User Secrets, then environment variables. It fails if no connection string is configured; it does not fall back to a repository connection string. Check that the connection string names `dogwalking_test` before running migrations. Never run local migration commands against production.

Swagger is available at `http://localhost:5199/swagger` in Development. Start the frontend separately; its default API URL is `http://localhost:5199/api`.

## Account and registration behavior

Registration accepts roles `Owner`, `Walker`, or `Both`. Passwords must be at least 10 characters. The API stores BCrypt password hashes and returns only a small user profile with the access token. Email verification is not implemented yet.

The internal `support` role is deliberately excluded from public registration. Support staff accounts must be provisioned through a controlled administrator process; do not add a public role selector for this permission.
