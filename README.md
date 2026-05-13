# SocialMedia API

A RESTful social media backend built with **ASP.NET Core 8**, following clean architecture. Supports posts (public/private), likes, nested comments, JWT authentication, Redis caching, and background count synchronization via Hangfire.

---

## Architecture

```
SocialMedia.sln
├── SocialMedia.Domain          # Entities, enums — no external dependencies
├── SocialMedia.Application     # Business logic, service interfaces, request/response models
├── SocialMedia.Infrastructure  # EF Core, repositories, JWT, Argon2, Cloudinary, Hangfire, Redis
└── SocialMedia.Api             # ASP.NET Core host, controllers, middleware, DI wiring
```

The layers follow a strict dependency rule: Domain ← Application ← Infrastructure ← Api.

---

## Tech Stack

| Concern | Technology |
|---|---|
| Framework | ASP.NET Core 8 |
| Database | PostgreSQL (Render.com) via Npgsql + EF Core 8 |
| Cache | Redis (Upstash) via StackExchange.Redis |
| Auth | JWT Bearer + Argon2id password hashing |
| Background jobs | Hangfire (PostgreSQL storage) |
| Image storage | Cloudinary |
| Logging | Serilog (console sink) |
| API docs | Swagger / Swashbuckle |

---

## Features

- **Auth** — register, login, JWT access token (15 min) + refresh token (7 days), Argon2id hashing
- **Posts** — create with optional image upload, public or private visibility
- **Feed** — cursor-paginated merged feed (public posts + own private posts), Redis-cached per user
- **Likes** — toggle like/unlike on posts or comments (soft-delete pattern)
- **Comments** — top-level and nested replies, cursor-paginated
- **Count sync** — Hangfire recurring job syncs denormalized `LikeCount` / `CommentCount` every 20 seconds
- **Dual DbContext** — write context + read-only replica context for read/write separation

---

## Project Structure

```
SocialMedia.Domain/
  Entities/           User, Post, Comment, Like, RefreshToken
  Enums/              PostVisibility, LikeTargetType

SocialMedia.Application/
  Services/
    Interfaces/       IAuthService, IPostService, IFeedService, ICacheService, repositories…
    Implementations/  AuthService, PostService, FeedService
  ClientModels/       Request + Response DTOs
  Settings/           CacheSettings, FeedSyncSettings

SocialMedia.Infrastructure/
  DbContexts/         ApplicationDbContext, ReadOnlyDbContext
  Repositories/       Generic + typed (Post, Comment, Like)
  Services/           CacheService, CloudinaryService, TokenService, PasswordHasher
  BackgroundJobs/     FeedSyncJob
  Migrations/

SocialMedia.Api/
  Controllers/        AuthController, PostController, FeedController, HelloController
  Extensions/         ServiceCollectionExtensions, ApplicationBuilderExtensions, HangfireExtensions
  Middleware/         GlobalExceptionMiddleware
```

---

## API Endpoints

### Auth — `/api/auth` (anonymous)

| Method | Route | Description |
|---|---|---|
| POST | `/api/auth/register` | Register a new user |
| POST | `/api/auth/login` | Login, returns access + refresh tokens |
| POST | `/api/auth/refresh` | Exchange a refresh token for new tokens |

### Posts — `/api/post` (requires auth)

| Method | Route | Description |
|---|---|---|
| GET | `/api/post` | Get own posts (cursor-paginated) |
| POST | `/api/post` | Create a post (multipart: content, image?, visibility) |

### Feed — `/api/feed` (requires auth)

| Method | Route | Description |
|---|---|---|
| GET | `/api/feed` | Merged public + own private feed (cursor-paginated) |
| POST | `/api/feed/like` | Toggle like on a post or comment |
| POST | `/api/feed/comment` | Add a comment (supports replies via `parentCommentId`) |
| GET | `/api/feed/{postId}/comments` | Get comments on a post (cursor-paginated) |
| GET | `/api/feed/{targetId}/likers` | Get users who liked a post or comment |

### Health

| Method | Route | Description |
|---|---|---|
| GET | `/api/hello` | Health check — returns `{ "health": "ok" }` |

#### Pagination

All paginated endpoints accept `?cursor=<ISO8601 datetime>&limit=<1-50>` and return:

```json
{
  "data": [...],
  "hasNextPage": true,
  "nextCursor": "2026-05-10T12:34:56.789Z"
}
```

Pass `nextCursor` as `cursor` on the next request to fetch the following page.

---

## Caching Strategy

| Cache key | Cached value | Invalidated when |
|---|---|---|
| `feed:public:{cursor}:{limit}` | Public posts page (limit+1 items) | New public post created |
| `feed:private:{userId}:{cursor}:{limit}` | User's private posts page | User creates a private post |

Both TTLs are configurable via `CacheSettings` in appsettings. On public post creation the public cache is fully evicted and the first page is pre-warmed.

---

## Configuration

Copy `appsettings.json` and fill in your values:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "<postgres-write-url>",
    "ReadReplicaConnection": "<postgres-read-url>",
    "Redis": "<redis-url>,password=<pwd>,ssl=True,abortConnect=False"
  },
  "Jwt": {
    "SecretKey": "<min-32-char secret>",
    "Issuer": "SocialFeed",
    "Audience": "SocialFeedUsers",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },
  "CacheSettings": {
    "FeedPublicPostsTtlMinutes": 5,
    "FeedPrivatePostsTtlMinutes": 5
  },
  "FeedSyncSettings": {
    "CutoffSeconds": 25
  },
  "Cloudinary": {
    "CloudName": "<cloud-name>",
    "ApiKey": "<api-key>",
    "ApiSecret": "<api-secret>",
    "Folder": "social_media/posts"
  }
}
```

---

## Running Locally

**Prerequisites:** .NET 8 SDK, PostgreSQL, Redis

```bash
# Restore packages
dotnet restore

# Apply database migrations
dotnet ef database update --project SocialMedia.Infrastructure --startup-project SocialMedia.Api

# Run
dotnet run --project SocialMedia.Api
```

Swagger UI is available at `http://localhost:<port>/swagger`.  
Hangfire dashboard is at `http://localhost:<port>/hangfire` (requires an authenticated Admin user).

---

## Database Migrations

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> \
  --project SocialMedia.Infrastructure \
  --startup-project SocialMedia.Api

# Apply
dotnet ef database update \
  --project SocialMedia.Infrastructure \
  --startup-project SocialMedia.Api
```

---

## Background Jobs

`FeedSyncJob` runs every 20 seconds and updates the denormalized `LikeCount` and `CommentCount` columns on `Post` and `Comment` rows that were touched within the last `FeedSyncSettings:CutoffSeconds` window. This keeps aggregate counts accurate without hitting the counts on every like/comment write.
