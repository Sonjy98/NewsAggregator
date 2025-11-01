# News App — ASP.NET + React + MySQL (+ Semantic Kernel)

Personalized news feed with saved keywords, authenticated feeds, optional email digests, and **LLM-assisted filtering** (Semantic Kernel + Google Gemini using Handlebars prompts).

---

## Table of Contents

* [Stack](#stack)
* [Repo layout](#repo-layout)
* [TL;DR — Quickstart (Docker-first)](#tldr--quickstart-docker-first)
* [Environment variables](#environment-variables)
* [Manual local setup (no Docker)](#manual-local-setup-no-docker)
* [Features & API flows](#features--api-flows)
* [Semantic Kernel + Gemini details](#semantic-kernel--gemini-details)
* [Troubleshooting](#troubleshooting)
* [Security](#security)
* [Mini deployment note](#mini-deployment-note)

---

## Stack

* **Backend:** ASP.NET Core 8 (C#), EF Core 8, Pomelo MySQL
* **Frontend:** Vite + React + TypeScript
* **DB:** MySQL 8
* **Auth:** JWT (HS256)
* **Email:** MailKit (SMTP)
* **LLM:** **Semantic Kernel** + **Google Gemini** (optional but supported) with **Handlebars** prompt templates

---

## Repo layout

```
Project 2025/
├─ NewsFeedBackend/                         # ASP.NET 8 Web API
│  ├─ Controllers/
│  │  ├─ AuthController.cs
│  │  ├─ PreferencesController.cs
│  │  ├─ ExternalNewsController.cs
│  │  └─ EmailController.cs
│  ├─ Data/
│  │  └─ AppDbContext.cs
│  ├─ Models/
│  │  └─ User.cs, UserPreference.cs, (etc.)
│  ├─ Services/
│  │  ├─ EmailSender.cs
│  │  └─ NewsFilterExtractor.cs             # uses Semantic Kernel + Handlebars
│  ├─ Prompts/
│  │  └─ NewsFilter/                        # Handlebars prompt templates for filtering
│  ├─ Program.cs
│  └─ appsettings*.json
├─ vite-project/                            # Vite + React + TS
│  ├─ src/ (components, lib, hooks, etc.)
│  ├─ package.json
│  └─ .env / .env.local
├─ docker-compose.dev.yml                   # Dev stack (MySQL + API + Frontend)
└─ .env                                     # For Docker Compose (NOT committed)
```

> If your frontend folder is named differently, adjust paths in `docker-compose.dev.yml`.

---

## TL;DR — Quickstart (Docker-first)

> Default ports used by this repo’s **dev compose**: **API 5001 → 8080**, **Frontend 5173 → 5173**, **MySQL 3307 → 3306**.

```bash
# 1) Clone
git clone <your-repo-url> project-2025 && cd project-2025

# 2) Create .env (next to docker-compose.dev.yml)
# NOTE: These names map directly to .NET configuration (double underscores).
cat > .env <<'ENV'
# --- MySQL ---
MYSQL_PASSWORD=YourStrongPass1!
MYSQL_ROOT_PASSWORD=YourStrongRootPass1!

# --- JWT (>=32 bytes) ---
Jwt__Key=use-a-true-random-32+byte-secret-here-pretty-please-1234567890

# --- External APIs ---
NewsData__ApiKey=REPLACE_ME
GoogleAi__ApiKey=                                  # optional, enables SK + Gemini

# --- SMTP (Mailtrap example) ---
Smtp__Host=sandbox.smtp.mailtrap.io
Smtp__Port=587
Smtp__Secure=false
Smtp__User=MAILTRAP_USER
Smtp__Password=MAILTRAP_PASS
Smtp__FromEmail=no-reply@example.test
Smtp__FromName=My Epic News
ENV

# 3) Bring up dev stack (MySQL + API + Frontend)
docker compose -f docker-compose.dev.yml up --build -d

# 4) Verify
docker compose -f docker-compose.dev.yml ps
docker compose -f docker-compose.dev.yml logs -f api      # Ctrl+C to stop

# 5) Open
# Frontend: http://localhost:5173
# API:      http://localhost:5001   (Swagger if enabled)
```

**Port conflicts?** Change host ports in `docker-compose.dev.yml`:

```yaml
api:
  ports: ["5002:8080"]
db:
  ports: ["3308:3306"]
frontend:
  ports: ["5174:5173"]
```

**What you should see**

* `db` healthy, `api` running, `frontend` running
* Frontend at `http://localhost:5173` talks to API at `http://localhost:5001`

---

## Environment variables

> These are read by ASP.NET Core via **double-underscore** mapping to `:` (e.g., `Jwt__Key` → `Jwt:Key`). Put these in your **`.env`** used by Docker Compose. For manual local runs, set them in your shell or `appsettings.Development.json`.

| Key                          | Required                  | Example / Notes                                                                                                                    |
| ---------------------------- | ------------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| `ConnectionStrings__Default` | no (dev compose provides) | `Server=db;Port=3306;Database=newsfeed;User Id=newsuser;Password=${MYSQL_PASSWORD};AllowPublicKeyRetrieval=True;SslMode=Preferred` |
| `Jwt__Key`                   | **yes**                   | HS256 secret, **>= 32 bytes**, e.g. `use-a-true-random-32+byte-secret-...`                                                         |
| `NewsData__ApiKey`           | **yes**                   | newsdata.io API key                                                                                                                |
| `GoogleAi__ApiKey`           | optional                  | Enables **Semantic Kernel + Gemini** features                                                                                      |
| `Smtp__Host`                 | optional                  | SMTP host (Mailtrap example: `sandbox.smtp.mailtrap.io`)                                                                           |
| `Smtp__Port`                 | optional                  | `587`                                                                                                                              |
| `Smtp__Secure`               | optional                  | `false` for STARTTLS                                                                                                               |
| `Smtp__User`                 | optional                  | Provider username                                                                                                                  |
| `Smtp__Password`             | optional                  | Provider password / token                                                                                                          |
| `Smtp__FromEmail`            | optional                  | `no-reply@example.test`                                                                                                            |
| `Smtp__FromName`             | optional                  | `My Epic News`                                                                                                                     |
| `VITE_API_BASE`              | frontend                  | For manual local dev: `http://localhost:5001` (Docker dev API). In Docker dev, frontend is prewired.                               |

> **MySQL user/DB** for dev compose are created via the compose image. If running manually, see the [Manual local setup](#manual-local-setup-no-docker).

---

## Manual local setup (no Docker)

### 1) MySQL

Create DB/user:

```sql
CREATE DATABASE newsfeed CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
CREATE USER 'newsuser'@'localhost' IDENTIFIED BY 'YourStrongPass1!';
GRANT ALL PRIVILEGES ON newsfeed.* TO 'newsuser'@'localhost';
FLUSH PRIVILEGES;
```

### 2) Backend config

`NewsFeedBackend/appsettings.Development.json` (or environment variables):

```json
{
  "ConnectionStrings": {
    "Default": "Server=127.0.0.1;Port=3306;Database=newsfeed;User Id=newsuser;Password=YourStrongPass1!;AllowPublicKeyRetrieval=True;SslMode=Preferred"
  },
  "Jwt": {
    "Key": "use-a-true-random-32+byte-secret-here-pretty-please-1234567890",
    "Issuer": "NewsFeedBackend",
    "Audience": "NewsFeedFrontend",
    "ExpiresHours": 12
  },
  "NewsData": {
    "ApiKey": "REPLACE_ME"
  },
  "GoogleAi": {
    "ApiKey": ""
  },
  "Smtp": {
    "Host": "sandbox.smtp.mailtrap.io",
    "Port": 587,
    "Secure": false,
    "User": "MAILTRAP_USER",
    "Password": "MAILTRAP_PASS",
    "FromEmail": "no-reply@example.test",
    "FromName": "My Epic News"
  }
}
```

> **JWT key must be ≥ 32 bytes** or you’ll get `IDX10720`.

### 3) Migrations / schema

From `NewsFeedBackend/`:

```powershell
dotnet tool update -g dotnet-ef
dotnet restore
# If you don't have a migration yet:
# dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 4) Run

**Backend**

```powershell
cd "Project 2025/NewsFeedBackend"
dotnet run --urls "http://localhost:5001"
```

**Frontend**

Create `vite-project/.env.local`:

```
VITE_API_BASE=http://localhost:5001
```

Then:

```powershell
cd "Project 2025/vite-project"
npm install
npm run dev
```

Open: `http://localhost:5173`

---

## Features & API flows

### Auth

* `POST /api/auth/register` `{ email, password }` → `{ token, userId, email }`
* `POST /api/auth/login` → same response (token saved in localStorage by frontend)

### Preferences (Bearer)

* `GET /api/preferences` → `string[]`
* `POST /api/preferences` `{ keyword }` → updated `string[]`
* `DELETE /api/preferences/{keyword}` → `204`

### News

* **Personalized:** `GET /api/externalnews/for-me` (Bearer) — uses saved keywords; if none, falls back to defaults
* **Search:** `GET /api/externalnews/search?q=term`
* **Public default:** `GET /api/externalnews/newsdata`

### Email digest

* `POST /api/email/send?max=10` (Bearer) — pulls preferences → calls newsdata.io → sends HTML digest via SMTP

**Frontend** highlights

* Login/Register page
* Sticky toolbar (logged-in email, “Email me”, “Log out”)
* Preferences pill UI (add/remove keywords)
* News feed with images, source/date, “Open story” links

---

## Semantic Kernel + Gemini details

* The backend integrates **Microsoft Semantic Kernel** for prompt-orchestration.
* **Gemini (Google AI)** is used as the LLM provider (enabled when `GoogleAi__ApiKey` is configured).
* **Prompt templates** are stored under: `NewsFeedBackend/Prompts/NewsFilter/` and loaded by `Services/NewsFilterExtractor.cs` using **Handlebars**.
* The extractor produces a strongly-typed `NewsFilterSpec`:

  ```csharp
  public sealed record NewsFilterSpec(
      string[] IncludeKeywords,
      string[]? ExcludeKeywords,
      string[]? PreferredSources,
      string? Category,
      string? TimeWindow
  );
  ```
* If `GoogleAi__ApiKey` is missing, LLM-assisted filtering is disabled/fails fast; the rest of the app continues to work with classic keyword filtering and newsdata.io feeds.

> Tip: When running via Docker Compose, define `GoogleAi__ApiKey` in `.env`. Avoid using a different name like `GOOGLEAI_API_KEY` unless you map it in compose to the .NET key (`GoogleAi__ApiKey`).

---

## Troubleshooting

**401 Unauthorized**

* Token missing/expired → log in again. Frontend clears bad tokens automatically on 401.

**CORS**

```csharp
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));
app.UseCors();
```

**HS256 key size (`IDX10720`)**

* Use a random **32+ byte** `Jwt:Key` (env `Jwt__Key`).

**MySQL connect errors at startup (Docker)**

* Wait for DB to be healthy, then:

  ```powershell
  docker compose -f docker-compose.dev.yml restart api
  ```
* Optional EF retry policy:

  ```csharp
  builder.Services.AddDbContext<AppDbContext>(opt =>
      opt.UseMySql(cs, new MySqlServerVersion(new Version(8,0,0)),
          my => my.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null)));
  ```

**Port conflicts**

* Change host ports in compose:

  ```yaml
  api:      { ports: ["5002:8080"] }
  db:       { ports: ["3308:3306"] }
  frontend: { ports: ["5174:5173"] }
  ```
* Update `VITE_API_BASE` accordingly.

**Swagger URL not found**

* Check if Swagger is enabled for `Development` only. Ensure `ASPNETCORE_ENVIRONMENT=Development` (in dev compose) and open `http://localhost:5001/swagger`.

**Missing LLM key**

* Error like “Missing GoogleAi:ApiKey” → provide `GoogleAi__ApiKey` in env.

---

## Security

* Never commit real secrets. Use **User Secrets** (local) or a private `.env` for Docker.
* For real SMTP, use a provider (Brevo/SendGrid/Postmark) and app-specific passwords.
* Validate inputs on both frontend and backend.

---

## Mini deployment note

The project is typically deployed to a **GCP VM** using Docker Compose (a separate `docker-compose.yml` with prod settings). Keep secrets on the VM in a private `.env`, not in the repo. Lock down SSH, keep system packages updated, and rotate keys as needed. (Intentionally brief to avoid exposing internal infra details.)

---

## Quick API smoke test

```powershell
# Register
curl -X POST http://localhost:5001/api/auth/register ^
  -H "Content-Type: application/json" ^
  -d "{ \"email\": \"demo@example.com\", \"password\": \"Passw0rd!\" }"

# Use the token from the response:
set TOKEN=eyJhbGciOi...

# Add a preference
curl -X POST http://localhost:5001/api/preferences ^
  -H "Authorization: Bearer %TOKEN%" -H "Content-Type: application/json" ^
  -d "{ \"keyword\": \"sports\" }"

# Personalized feed
curl "http://localhost:5001/api/externalnews/for-me" -H "Authorization: Bearer %TOKEN%"

# Email me
curl -X POST "http://localhost:5001/api/email/send?max=10" -H "Authorization: Bearer %TOKEN%"
```
