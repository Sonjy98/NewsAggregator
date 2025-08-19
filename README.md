heck yeah—here’s a tight, mentor-friendly **README.md** you can drop into the repo. It’s Windows-first (PowerShell), but I added quick notes for macOS/Linux too.

---

# News App (ASP.NET + React + MySQL)

Personalized news feed:

* Users register/login (JWT)
* Save keywords (preferences)
* Fetch feed from **newsdata.io** based on keywords

---

## Prerequisites

| Tool                 | Version / Notes                                                                           |
| -------------------- | ----------------------------------------------------------------------------------------- |
| **.NET SDK**         | **8.0.x** (project targets net8). If your `.csproj` says `net9.0`, change it to `net8.0`. |
| **Node.js**          | 18+ (recommend LTS 20.x)                                                                  |
| **MySQL Server**     | 8.0.x                                                                                     |
| **Package managers** | npm (bundled with Node)                                                                   |
| **EF CLI**           | `dotnet tool update -g dotnet-ef` (uses .NET 8 runtime)                                   |
| **API key**          | newsdata.io (free tier is fine)                                                           |

> macOS/Linux: swap `^` line continuations for `\` in shell commands.

---

## Project layout

```
Project 2025/
├─ NewsFeedBackend/            # ASP.NET 8 Web API
│  ├─ Controllers/
│  │  ├─ AuthController.cs
│  │  ├─ PreferencesController.cs
│  │  ├─ ExternalNewsController.cs
│  ├─ Data/AppDbContext.cs
│  ├─ Models/User.cs, UserPreference.cs
│  ├─ Program.cs
│  └─ appsettings*.json
└─ frontend/                   # Vite + React + TS (folder name may differ)
   ├─ src/
   ├─ package.json
   └─ .env
```

---

## 1) Database setup (MySQL)

Open **MySQL Shell / Workbench** and run:

```sql
CREATE DATABASE newsfeed CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
CREATE USER 'newsuser'@'localhost' IDENTIFIED BY 'YourStrongPass1!';
GRANT ALL PRIVILEGES ON newsfeed.* TO 'newsuser'@'localhost';
FLUSH PRIVILEGES;
```

---

## 2) Backend configuration

### 2.1 Set connection string (development)

Edit `NewsFeedBackend/appsettings.Development.json`:

```jsonc
{
  "ConnectionStrings": {
    "Default": "Server=127.0.0.1;Port=3306;Database=newsfeed;User Id=newsuser;Password=YourStrongPass1!;AllowPublicKeyRetrieval=True;SslMode=Preferred"
  },
  "Jwt": {
    "Key": "use-a-random-32+-char-secret-here-please-please",
    "Issuer": "NewsFeedBackend",
    "Audience": "NewsFeedFrontend",
    "ExpiresHours": 12
  },
  "NewsData": {
    "ApiKey": "REPLACE_ME",
    "DefaultLanguage": "en",
    "DefaultCountry": "",
    "DefaultCategory": "",
    "DefaultQuery": "" // leave empty for generic latest
  }
}
```

> **JWT key must be ≥ 32 bytes** for HS256 or you’ll see `IDX10720`.


## 3) Database schema (EF Core)

From `NewsFeedBackend/`:

```powershell
dotnet tool update -g dotnet-ef
dotnet restore
# If migrations already exist, skip the add step:
# dotnet ef migrations add InitialCreate
dotnet ef database update
```

This creates tables `Users` and `UserPreferences` (and any others in your model).

---

## 4) Run the backend

```powershell
cd "Project 2025\NewsFeedBackend"
dotnet run --urls "http://localhost:5000"
```

* Swagger (if enabled): `http://localhost:5000/swagger`
* Health check (basic): `http://localhost:5000/` returns `OK`

---

## 5) Frontend

### 5.1 Configure API base

Create `frontend/.env`:

```
VITE_API_BASE=http://localhost:5000
```

> Restart Vite after editing `.env`.

### 5.2 Install & run

```powershell
cd "Project 2025\frontend"
npm install
npm run dev
```

Open `http://localhost:5173`.

---

## 6) How to use

1. **Register** then **Login**

   * Frontend calls `POST /api/auth/register` or `POST /api/auth/login`.
   * JWT is stored in `localStorage`.

2. **Add preferences**

   * Manage keywords via `GET/POST/DELETE /api/preferences`.

3. **View feed**

   * If logged in: frontend calls `GET /api/externalnews/for-me` (Bearer).
   * If you have **no keywords**, backend falls back to a **default feed** (language/country/category configured).
   * Optional search: `GET /api/externalnews/search?q=term`.
   * Public default feed: `GET /api/externalnews/newsdata` (uses same defaults).

---

## 7) Common issues & fixes

* **401 Unauthorized**
  Token missing/expired → Log in again (clears old token).

* **CORS blocked**
  Ensure backend allows `http://localhost:5173` in `Program.cs`:

  ```csharp
  builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
      p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));
  app.UseCors();
  ```

* **JWT key error** (`IDX10720` / HS256 key too small)
  Use a **32+ byte** secret in `Jwt:Key` (user-secrets recommended).

* **MySQL auth error** (`caching_sha2_password`, RSA key, etc.)
  Use `Server=127.0.0.1;AllowPublicKeyRetrieval=True;SslMode=Preferred` in the connection string.

* **`IHttpClientFactory` not resolved**
  Ensure you registered:

  ```csharp
  builder.Services.AddHttpClient();
  builder.Services.AddHttpClient("newsdata", ...);
  ```

* **EF/Pomelo version mismatch**
  Project targets **.NET 8**; use **EF Core 8** and **Pomelo 8.x**.
  (If you ever target .NET 9, you’ll need compatible EF/Providers.)


---

## 9) API quick reference

* **Auth**
  `POST /api/auth/register` → `{ email, password }`
  `POST /api/auth/login`    → `{ email, password }`

* **Preferences** *(Bearer)*
  `GET /api/preferences` → `string[]`
  `POST /api/preferences` → `{ keyword }` → `string[]`
  `DELETE /api/preferences/{keyword}` → `204`

* **News**
  `GET /api/externalnews/for-me` *(Bearer; default feed if no prefs)*
  `GET /api/externalnews/search?q=term`
  `GET /api/externalnews/newsdata` *(public default feed)*

