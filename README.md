# Social Media Feed API

A scalable social media backend built with .NET and Clean Architecture, designed to handle millions of posts and concurrent reads. Supports public and private posts, nested comments, likes, image uploads, and a real-time feed — with performance and security as top priorities.

---

## Links

| | |
|---|---|
| 🎥 Video Walkthrough | [YouTube](https://youtu.be/PYHiAgOh7Sc) |
| 🌐 Live Demo | [social-media-frontend-rust.vercel.app](https://social-media-frontend-rust.vercel.app) |
| 📁 Repository | [github.com/monirseccse/SocialMedia](https://github.com/monirseccse/SocialMedia) |

---

## Features

- ✅ JWT authentication with refresh token rotation
- ✅ Create posts with text and image (Cloudinary)
- ✅ Public and private post visibility
- ✅ Feed with newest posts first (cursor-based pagination)
- ✅ Like / unlike posts, comments, and replies
- ✅ Nested comments and replies
- ✅ Show who liked a post, comment, or reply
- ✅ Protected routes — only authenticated users can access the feed

---

## Tech Stack

| Layer | Technology |
|---|---|
| Language & Framework | C# / .NET |
| Architecture | Clean Architecture |
| Database | PostgreSQL (Write Primary + Read Replica) |
| Cache | Redis |
| Background Jobs | Hangfire |
| Image Storage | Cloudinary CDN |
| Authentication | JWT + Refresh Token |
| Deployment | Docker + Render |

---

## Project Structure

```
src/
├── SocialMedia.Domain/          # Entities, Enums — no dependencies
├── SocialMedia.Application/     # Business logic, interfaces, DTOs
├── SocialMedia.Infrastructure/  # EF Core, Redis, Hangfire, Cloudinary
└── SocialMedia.API/             # Controllers, middleware, extensions
```

Each layer depends only inward. The API layer never references Infrastructure directly. All infrastructure concerns are hidden behind Application interfaces.

---

## Architecture & Design Decisions

---

### 1. Read / Write Database Replica

**Problem:** A single database handling both reads and writes becomes a bottleneck. Feed reads are extremely frequent. Writes happen constantly.

**Decision:** PostgreSQL is split into a write primary and a read replica.

- All `INSERT`, `UPDATE`, `DELETE` operations go to the write primary
- All `SELECT` queries (feed, like checks, comment lists) go to the read replica
- Read and write traffic never compete for the same DB resources

---

### 2. Two-Layer Redis Cache for Feed

**Problem:** Caching the feed per-user would create millions of identical cache keys — one for each user seeing the same public posts. Expensive and wasteful.

**Decision:** The feed cache is split into two layers merged at read time.

```
Layer 1 — Shared public feed      feed:public:cursor:{timestamp}       TTL: 60s
           One key shared by all users regardless of how many there are

Layer 2 — Per-user private posts  feed:private:{userId}:cursor:{ts}    TTL: 60s
           Only exists for users who have created private posts
```

On every feed request, both layers are fetched from Redis, merged in memory, re-sorted by `created_at DESC`, and returned as one unified feed. Users with no private posts only hit the shared Layer 1 key.

**Invalidation:**

| Event | Action |
|---|---|
| New public post | `DEL feed:public:cursor:0` |
| New private post | `DEL feed:private:{userId}:cursor:0` |
| Like / comment | Nothing — TTL handles staleness |
| Post edited or deleted | `DEL` relevant key |

Like and comment counts are allowed to be up to 60 seconds stale on the feed. This is standard practice — Instagram and Facebook do the same.

Every API response includes an `X-Data-Source: cache | database` header so the cache behaviour is visible during testing.

---

### 3. Cursor-Based Pagination

**Problem:** Offset pagination (`LIMIT 20 OFFSET 1000`) is slow on millions of rows and produces duplicate or missing posts when new content arrives between pages.

**Decision:** Cursor-based pagination using a composite `(created_at, id)` cursor.

```sql
WHERE (created_at, id) < (@cursor_time, @cursor_id)
ORDER BY created_at DESC, id DESC
LIMIT 20
```

Using both columns eliminates tie-breaking issues when two posts share the same timestamp. Each page is stable and consistent regardless of new posts being created. Each page has its own Redis cache key — invalidating page 1 never affects other pages.

---

### 4. Hot Row Problem — Like and Comment Counts

**Problem:** Incrementing `like_count` on the posts table on every like causes row-level lock contention in PostgreSQL. A viral post receiving thousands of likes per second creates thousands of concurrent `UPDATE` statements queuing on the same row, degrading the entire database.

**Decision:** `like_count` is never updated synchronously. A Hangfire recurring job batch-updates all counts every 20 seconds.

```
User likes post
    ├─ INSERT into likes table    ← immediate, source of truth
    └─ Return 200 to user         ← done

Hangfire recurring job (every 20s)
    ├─ Find all likes with UpdatedAt >= now - 25s
    ├─ GROUP BY (target_id, target_type)
    └─ COUNT(*) → UPDATE like_count   ← one UPDATE per post per 20s
```

`COUNT(*)` from the real `likes` table is always correct and idempotent. If two workers run simultaneously for the same post, both compute the same value — no count corruption is possible.

**Why 20 seconds:**

The recurring job interval (20s) is intentionally shorter than the cache TTL (60s). This guarantees that when the cache expires and falls back to the database, the database has already been updated at least once by the job. A cache miss always returns a fresh count.

**Multi-pod safety:** Hangfire stores jobs in PostgreSQL. The recurring job is coordinated by a distributed lock — only one pod runs it at a time. Other pods skip automatically.

---

### 5. Like State vs Like Count

`like_count` can be 60 seconds stale. Whether the current user has liked something (`IsLiked`) must always be accurate — it directly controls the like button state.

The feed cache stores only structure — post content, counts, timestamps. `IsLiked` is never cached. On every feed load, one separate query fetches liked state for all post IDs in the current page using the unique index, then merges it into the response. This is always fresh regardless of cache TTL.

---

### 6. Polymorphic Like System

A single `likes` table handles likes on posts, comments, and replies using a `TargetType` discriminator. Soft delete is used (`DeletedAt`) instead of hard delete so the recurring job can detect unlikes via `UpdatedAt` and recount correctly.

A partial unique index prevents duplicate likes while allowing re-likes after unlike:

```sql
UNIQUE (user_id, target_id, target_type) WHERE deleted_at IS NULL
```

---

### 7. Database Indexing

Every index is designed for a specific query pattern — not added generically.

| Index | Purpose |
|---|---|
| `Like (UserId, TargetId, TargetType)` UNIQUE WHERE `deleted_at IS NULL` | Like check + duplicate prevention |
| `Like (UpdatedAt)` | Recurring job dirty row lookup |
| `Post (Visibility, CreatedAt, Id)` | Public feed query with cursor |
| `Post (AuthorId, CreatedAt)` | Author profile feed query |
| `Comment (PostId, ParentCommentId, CreatedAt)` | Top-level comments and replies |
| `User (Email)` UNIQUE | Login lookup |
| `RefreshToken (Token)` UNIQUE | Token validation |
| `RefreshToken (UserId)` | Token revocation on logout |

Composite indexes follow the rule: **filter columns first, sort columns after**. This allows the database to use the index for both filtering and ordering without a secondary sort step.

---

### 8. Authentication — JWT + Refresh Token

Access tokens are short-lived JWTs. Refresh tokens are stored in the database with a unique index on the token value for fast validation and a `UserId` index for fast revocation on logout.

All feed and post endpoints are protected routes. Post visibility (`private` vs `public`) is enforced server-side on every request — never trusted from the client.

---

### 9. Image Upload — Cloudinary

Images are uploaded to Cloudinary directly from the API. Only the Cloudinary CDN URL is stored in the database. Images are never served through the application server — all media bandwidth goes through Cloudinary's CDN edge network.

---

### 10. Deployment

The application is containerized with Docker and deployed on Render. All secrets (database connection strings, Redis URL, Cloudinary credentials, JWT secret) are injected as environment variables at runtime — never hardcoded or committed to the repository.

---

## Planned Improvements

### Rate Limiting — API Gateway

Rate limiting belongs at the API Gateway level, not inside the application server. Enforcing limits inside the app means abusive traffic has already consumed a server thread and hit the middleware stack before being rejected. At the gateway level, traffic is blocked before it ever reaches the application.

Planned approach: place NGINX or Cloudflare in front of the application and define per-endpoint, per-user rate limits at that layer. This protects all endpoints uniformly with no application code changes required.

---

## Scalability Summary

| Problem | Solution |
|---|---|
| DB read overload | Read replica separates read and write traffic |
| Feed query cost at scale | Redis cache with shared public key across all users |
| Private posts mixed into feed | Two-layer cache merged in memory at read time |
| Offset pagination on millions of rows | Cursor-based pagination on `(created_at, id)` |
| Hot row on `like_count` | Hangfire recurring job, `COUNT(*)` every 20 seconds |
| Duplicate like race condition | Partial unique index at DB level |
| Image and media bandwidth | Cloudinary CDN — app server never serves images |
| Multi-pod job duplication | Hangfire distributed lock via PostgreSQL |
| Stale like button state | `IsLiked` always fetched fresh, never in shared cache |
| Rate limiting (planned) | API Gateway — stops traffic before reaching the app |

---

## Getting Started

```bash
# Clone the repository
git clone https://github.com/monirseccse/SocialMedia.git

# Set environment variables
cp .env.example .env
# Fill in: ConnectionStrings, Redis, Cloudinary, JWT settings

# Run with Docker
docker compose up --build

# API runs at
http://localhost:5000
```

---

## Environment Variables

Create an `appsettings.json` or set these as environment variables on your server. 

```
DefaultConnection=        # PostgreSQL write primary connection string
ReadReplicaConnection=    # PostgreSQL read replica connection string
Redis=                    # Upstash Redis connection string (ssl=True)

SecretKey=                              # Long random string, min 32 characters
Issuer=SocialFeed
Audience=SocialFeedUsers
AccessTokenExpiryMinutes=15
RefreshTokenExpiryDays=7

CloudName=                       # From Cloudinary dashboard
ApiKeyApiKey=                          # From Cloudinary dashboard
ApiSecret=                       # From Cloudinary dashboard
Folder=social_media/posts

CacheSettings__FeedPublicPostsTtlMinutes=1
CacheSettings__FeedPrivatePostsTtlMinutes=1

FeedSyncSettings__CutoffSeconds=25
```
