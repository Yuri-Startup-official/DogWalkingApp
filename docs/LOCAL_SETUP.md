# Local API setup

## Configuration

The API uses PostgreSQL. Put the database connection string in `ConnectionStrings:DefaultConnection` using .NET user secrets or the `ConnectionStrings__DefaultConnection` environment variable.

Do not commit JWT signing keys. Set a local key of at least 32 bytes with:

```powershell
dotnet user-secrets set "Jwt:Key" "<your-random-secret-at-least-32-bytes>"
```

The issuer and audience are configured in `appsettings.json`. Use a different signing key for every environment.

## Database

After configuring a local development database, create or update its schema with the EF Core migration commands from the API project directory. Never run database updates against a production connection during local development.

## Accounts

Registration accepts roles `Owner`, `Walker`, or `Both`. Passwords must be at least 10 characters. The API stores a BCrypt password hash and returns only a small user profile with the access token.
