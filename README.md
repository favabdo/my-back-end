# NileTechno API

## Setup

1. Copy `.env.example` to `.env` and set `DB_CONNECTION`, `CORS_ALLOWED_ORIGINS`, and `JWT_KEY`.

```bash
dotnet restore
dotnet ef migrations add InitialCreate -p src/NileTechno.Infrastructure -s src/NileTechno.API
dotnet ef database update -p src/NileTechno.Infrastructure -s src/NileTechno.API
dotnet run --project src/NileTechno.API
```

Swagger: `/swagger`
