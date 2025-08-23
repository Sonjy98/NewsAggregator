
---

# News App (ASP.NET + React + MySQL)

Personalized news feed:

* Users register/login (JWT)
* Save keywords (preferences)
* Fetch feed from **newsdata.io** based on keywords
* Optional **email digest** (“Email me”)

---

## Stack

* **Backend:** ASP.NET Core 8 (C#), EF Core 8, Pomelo MySQL, MailKit (SMTP)
* **Frontend:** Vite + React + TypeScript
* **DB:** MySQL 8
* **Auth:** JWT (HS256)

---

## Repo layout

```
Project 2025/
├─ NewsFeedBackend/                 # ASP.NET 8 Web API
│  ├─ Controllers/
│  │  ├─ AuthController.cs
│  │  ├─ PreferencesController.cs
│  │  ├─ ExternalNewsController.cs
│  │  └─ EmailController.cs
│  ├─ Data/AppDbContext.cs
│  ├─ Models/User.cs, UserPreference.cs
│  ├─ Services/EmailSender.cs
│  ├─ Program.cs
│  └─ appsettings*.json
├─ vite-project/                    # Vite + React + TS (your frontend folder)
│  ├─ src/ (components, lib, etc.)
│  ├─ package.json
│  └─ .env / .env.local
├─ docker-compose.dev.yml           # Dev stack (MySQL + API + Frontend)
└─.env # For Docker
```

> If your frontend folder is named differently, adjust paths in `docker-compose.dev.yml`.

---

## Prerequisites (local dev without Docker)

| Tool         | Version / Notes                   |
| ------------ | --------------------------------- |
| **.NET SDK** | 8.0.x                             |
| **Node.js**  | 18+ (recommend LTS 20.x)          |
| **MySQL**    | 8.0.x                             |
| **EF CLI**   | `dotnet tool update -g dotnet-ef` |
| **API key**  | newsdata.io                       |

**NuGet packages (already added):**
`Microsoft.EntityFrameworkCore.* 8.x`, `Pomelo.EntityFrameworkCore.MySql 8.x`, `MailKit`, `Microsoft.AspNetCore.Authentication.JwtBearer`.

---

## 1) MySQL (local)

Create DB/user:

```sql
CREATE DATABASE newsfeed CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
CREATE USER 'newsuser'@'localhost' IDENTIFIED BY 'YourStrongPass1!';
GRANT ALL PRIVILEGES ON newsfeed.* TO 'newsuser'@'localhost';
FLUSH PRIVILEGES;
```

---

## 2) Backend config (local)

`NewsFeedBackend/appsettings.Development.json`:

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
    "ApiKey": "REPLACE_ME",
    "DefaultLanguage": "en",
    "DefaultCountry": "",
    "DefaultCategory": "",
    "DefaultQuery": ""
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

---

## 3) Migrations / schema

From `NewsFeedBackend/`:

```powershell
dotnet tool update -g dotnet-ef
dotnet restore
# if you don't have a migration yet:
# dotnet ef migrations add InitialCreate
dotnet ef database update
```

Creates tables `Users` and `UserPreferences` etc.

---

## 4) Run locally (no Docker)

**Backend**

```powershell
cd "Project 2025\NewsFeedBackend"
dotnet run --urls "http://localhost:5000"
```

**Frontend**

`vite-project/.env.local`:

```
VITE_API_BASE=http://localhost:5000
```

Then:

```powershell
cd "Project 2025\vite-project"
npm install
npm run dev
```

Open: `http://localhost:5173`

---

## 5) Docker (recommended dev stack)

**What it runs**

* `db` – MySQL 8
* `api` – ASP.NET backend (listens on host `http://localhost:5000`)
* `frontend` – Vite dev server (host `http://localhost:5173`)

**.env for Compose (in repo root, next to docker-compose.dev.yml):**

```
# MySQL
MYSQL_PASSWORD=YourStrongPass1!
MYSQL_ROOT_PASSWORD=YourStrongRootPass1!

# JWT
JWT_KEY=use-a-true-random-32+byte-secret-here-pretty-please-1234567890

# NewsData
NEWSDATA_API_KEY=REPLACE_ME

# SMTP (Mailtrap sandbox example)
SMTP_HOST=sandbox.smtp.mailtrap.io
SMTP_PORT=587
SMTP_SECURE=false
SMTP_USER=MAILTRAP_USER
SMTP_PASSWORD=MAILTRAP_PASS
SMTP_FROM=no-reply@example.test
```

**Start the stack**

```powershell
cd "Project 2025"
docker compose -f docker-compose.dev.yml up --build
```

> If port **3306** is already used by a local MySQL, either stop it **or** set the `db` service to `ports: ["3307:3306"]` (containers still use `Server=db;Port=3306`).

**Stop**

```powershell
docker compose -f docker-compose.dev.yml down
```

**Useful checks**

```powershell
# statuses
docker compose -f docker-compose.dev.yml ps

# tail logs
docker compose -f docker-compose.dev.yml logs -f api
docker compose -f docker-compose.dev.yml logs -f db

# inspect API connection string inside container
docker compose -f docker-compose.dev.yml exec api printenv | findstr ConnectionStrings__Default

# query DB inside container
docker compose -f docker-compose.dev.yml exec db mysql -unewsuser -p%MYSQL_PASSWORD% -e "USE newsfeed; SHOW TABLES;"
```

**Notes**

* Environment variables in Compose (e.g., `ConnectionStrings__Default`, `Jwt__Key`) **override** `appsettings.json`.
* On first boot, if API starts before DB is ready, just:

  ```powershell
  docker compose -f docker-compose.dev.yml restart api
  ```

---

## 6) Features & flows

* **Auth**

  * `POST /api/auth/register` `{ email, password }` → returns `{ token, userId, email }`
  * `POST /api/auth/login` same response; token saved in `localStorage`.

* **Preferences** (Bearer)

  * `GET /api/preferences` → `string[]`
  * `POST /api/preferences` `{ keyword }` → updated `string[]`
  * `DELETE /api/preferences/{keyword}` → `204`

* **News**

  * **Personalized:** `GET /api/externalnews/for-me` (Bearer)
    Uses saved keywords; if none, **fallback** to default feed (language/country/category/query).
  * **Search:** `GET /api/externalnews/search?q=term`
  * **Public default:** `GET /api/externalnews/newsdata`

* **Email digest**

  * `POST /api/email/send?max=10` (Bearer)
    Pulls preferences → calls newsdata.io → sends HTML digest via SMTP.

Frontend:

* Login/Register page (scoped CSS module, light inputs)
* Sticky **toolbar** (shows logged-in email, “Email me”, “Log out”)
* Preferences pill UI (add/remove keywords)
* News feed with images, source/date, “Open story” links

---

## 7) Troubleshooting

* **401 Unauthorized**

  * Token missing/expired → Log in again. Frontend clears bad tokens automatically on 401.

* **CORS**

  ```csharp
  builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
      p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));
  app.UseCors();
  ```

* **HS256 key size (`IDX10720`)**

  * Use a random **32+ byte** `Jwt:Key`.

* **MySQL connect errors at startup (in Docker)**

  * Wait for DB to be healthy, then `docker compose … restart api`.
  * Optional: enable retries on EF:

    ```csharp
    builder.Services.AddDbContext<AppDbContext>(opt =>
      opt.UseMySql(cs, new MySqlServerVersion(new Version(8,0,0)),
        my => my.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null)));
    ```

* **Port conflicts**

  * 5173 (Vite) or 5000/5001 (API) or 3306 (MySQL) may already be used. Change host ports in Compose:

    ```yaml
    api:
      ports: ["5001:8080"]
    db:
      ports: ["3307:3306"]   # or remove to keep DB internal-only
    frontend:
      ports: ["5173:5173"]
    ```
  * Update `VITE_API_BASE` accordingly.

---

## 8) Security tips

* Never commit real secrets. Use **User Secrets** (local) or `.env` (Docker, not committed).
* For real SMTP (not Mailtrap), use a provider (e.g., Brevo/SendGrid/Postmark) and app-specific passwords.
* Validate inputs on both frontend and backend.

---

## 9) Quick API smoke test

```powershell
# Register
curl -X POST http://localhost:5000/api/auth/register ^
  -H "Content-Type: application/json" ^
  -d "{ \"email\": \"demo@example.com\", \"password\": \"Passw0rd!\" }"

# Use the token from the response:
set TOKEN=eyJhbGciOi...

# Add a preference
curl -X POST http://localhost:5000/api/preferences ^
  -H "Authorization: Bearer %TOKEN%" -H "Content-Type: application/json" ^
  -d "{ \"keyword\": \"sports\" }"

# Personalized feed
curl "http://localhost:5000/api/externalnews/for-me" -H "Authorization: Bearer %TOKEN%"

# Email me
curl -X POST "http://localhost:5000/api/email/send?max=10" -H "Authorization: Bearer %TOKEN%"
```

---
