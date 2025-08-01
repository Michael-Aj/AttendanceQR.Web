# AttendanceQR.Web (Starter, SQLite, ASP.NET Core MVC)

## Prereqs
- .NET 8 SDK
- EF Core tools (optional): `dotnet tool install --global dotnet-ef`

## Run (dev)
```bash
dotnet restore
dotnet build
dotnet run
```

A SQLite DB will be created at `./data/attendance.db` on first run. Migrations are applied automatically.

## First steps
1. Browse to `https://localhost:5001` (or the port shown in console).
2. Go to **Create Session** to download a QR PNG.
3. Open the QR on your phone and submit a check‑in.

## Notes
- The QR is valid for ~20 minutes via a per‑session nonce.
- Duplicate submissions per student/session are blocked.

## EF Core (optional)
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```
