# Furina — Smart Clinic & Wellness Solution for Pets

## TASK-11: Nền tảng Database Multi-tenant & Xác thực (Sprint 1)

Đã hoàn thành phần schema + RLS + seed của task này. Trạng thái các AC:

- **AC-1** (RLS cách ly tenant, kết nối bằng role không phải superuser): ✅ verify bằng dữ liệu thật — xem "Test thủ công" bên dưới.
- **AC-2** (migration chạy sạch từ DB rỗng): ✅ `dotnet ef database update` từ schema rỗng, không lỗi.
- **AC-3** (rollback không mất dữ liệu bảng khác): ✅ `dotnet ef database update 0` xoá sạch, apply lại không lỗi.
- Seed script idempotent: ✅ chạy `dotnet run` 2 lần liên tiếp, không lỗi, không tạo trùng dữ liệu.

## Cấu trúc

```
src/
  Furina.Domain/          Entities thuần (Tenant, User, Role, UserRole, RefreshToken)
  Furina.Infrastructure/  EF Core DbContext, migrations, RLS, seed, multi-tenancy plumbing
  Furina.Api/              ASP.NET Core Web API (entrypoint, DI wiring)
db/init/                  Script khởi tạo role Postgres (chạy tự động bởi docker-compose)
docker-compose.yml        Postgres 16 local cho dev
```

## Cách hoạt động của multi-tenancy

- Mọi bảng nghiệp vụ (`users`, `roles`, `user_roles`, `refresh_tokens`) có cột `tenant_id` và bật
  `ROW LEVEL SECURITY` + `FORCE ROW LEVEL SECURITY` với policy `tenant_id = current_setting('app.tenant_id')::uuid`.
- App kết nối Postgres bằng role `furina_app` — **không phải superuser, không bypassrls** — nên RLS
  luôn được áp dụng, không thể bị bỏ qua từ tầng ứng dụng.
- Mỗi HTTP request có một `ITenantContext` (scoped) chứa tenant hiện tại (sẽ được set bởi middleware
  JWT ở task tiếp theo). `TenantConnectionInterceptor` tự động chạy `SET app.tenant_id = ...` mỗi khi
  EF Core mở một connection Postgres mới cho request đó.

## Chạy local

```bash
docker compose up -d                     # Postgres 16 + tạo role furina_app
export FURINA_DB_CONNECTION="Host=localhost;Port=5433;Database=furina;Username=postgres;Password=furina_superuser_dev_pw"
dotnet ef database update --project src/Furina.Infrastructure --startup-project src/Furina.Infrastructure

export ASPNETCORE_ENVIRONMENT=Development
dotnet run --project src/Furina.Api        # tự seed tenant "demo-clinic" + admin mẫu khi khởi động
```

Mật khẩu/connection string trong `appsettings.Development.json` và `db/init/01-roles.sql` chỉ dùng cho
Postgres chạy local qua Docker — không phải secret thật, không dùng cho production.

## Test thủ công RLS (đã verify, xem log task)

```bash
# Kết nối bằng role app (không phải superuser)
docker exec -e PGPASSWORD=furina_app_dev_pw furina-postgres psql -U furina_app -d furina -c "
SET app.tenant_id = '<tenant-a-id>';
SELECT email FROM users;   -- chỉ thấy user của tenant A
"
```

## TASK-12: Middleware phân giải tenant + xác thực JWT + phân quyền role

Trạng thái AC:

- **AC-1** (login trả access+refresh, JWT chứa đúng tenant_id/role): ✅ verify bằng cách decode JWT thật.
- **AC-2** (thiếu tenant → 400 trước business logic): ✅ `TenantResolutionMiddleware` chạy đầu pipeline, chưa chạm DbContext của controller nào.
- **AC-3** (revoke refresh token thật sự, verify bằng DB): ✅ logout ghi `revoked_at`, verify trực tiếp bằng `psql`, không chỉ tin response code.
- **AC-4** (role không đủ quyền → 403): ✅ Receptionist gọi `/admin/ping` (Owner-only) nhận 403.
- Bonus (ngoài AC gốc nhưng đúng tinh thần bảo mật): token hợp lệ của tenant A gọi API dưới tenant B → 403 `tenant_mismatch` (`TenantClaimGuardMiddleware`).

### Endpoints

- `POST /auth/login` — `{email, password}` → `{accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt}`. Cần header `X-Tenant-Id: <slug>` (dev) hoặc subdomain (prod).
- `POST /auth/refresh` — `{refreshToken}` → access token mới.
- `POST /auth/logout` — `{refreshToken}` → 204, revoke ngay trong DB (idempotent).
- `GET /admin/ping` — ví dụ endpoint `[Authorize(Policy = Policies.OwnerOnly)]` dùng để test phân quyền.

### Tài khoản seed (dev)

4 user, mỗi role một cái, cùng mật khẩu `ChangeMe123!`:
`owner@demo-clinic.furina.local`, `vet@...`, `receptionist@...`, `superadmin@...` (xem `SeedData.EmailFor`).

## TASK-13: Container hoá API (Dockerfile + docker-compose dev)

Trạng thái AC:

- **AC-1** (`docker compose up` trên máy sạch → API+Postgres+Redis lên đủ, `/health` 200): ✅ verify thật — xoá volume, pull image mới, `docker compose up` từ đầu.
- **AC-2** (container không chạy bằng root): ✅ `docker run --entrypoint id furina-api ...` → `uid=999(furina)`.
- **AC-3** (sửa code, dev qua docker-compose tự reload không cần rebuild image): ✅ verify thật — sửa `HealthController.cs`, `/health` phản ánh thay đổi trong vài giây, image không rebuild lại.

### Chạy dev (khuyến nghị, không cần cài .NET/Postgres/Redis)

```bash
docker compose up          # lên Postgres + Redis + migrator (chạy 1 lần rồi thoát) + API (dotnet watch, hot-reload)
curl http://localhost:8080/health
```

Sửa code trong `src/` — `dotnet watch` bên trong container `api` tự rebuild và reload, không cần `docker compose build` lại.

### Build & chạy image production

```bash
docker build -t furina-api .
docker run --rm -p 8080:8080 \
  -e ConnectionStrings__Furina="Host=<postgres-host>;Port=5432;Database=furina;Username=furina_app;Password=<...>" \
  -e Jwt__Secret="<production-secret>" \
  furina-api
```

Image production dùng Dockerfile 2 stage (`build` bằng `dotnet/sdk:9.0`, `runtime` bằng `dotnet/aspnet:9.0`, chỉ copy output publish), chạy bằng user `furina` (không phải root). Task gốc ghi `dotnet 8.0` nhưng project đã chốt `.NET 9` từ lúc scaffold (TASK-11) nên Dockerfile dùng đúng image `9.0` khớp với `net9.0` thật của code.

### Ghi chú

- `migrator` chạy `dotnet ef database update` bằng role `postgres` (superuser) — `api` tự nó không có quyền tạo bảng (đúng thiết kế RLS ở TASK-11).
- Redis đã lên trong compose theo yêu cầu DoD nhưng **chưa được code nào dùng** (chưa có tính năng caching) — sẽ wiring khi có task cần cache.
- Port host: Postgres `5433`, Redis `6381` (tránh trùng với container Redis của project khác đang chạy sẵn trên máy — `6380`), API `8080`.

## TASK-14: Pipeline CI/CD

Repo: https://github.com/khuongdp1402/furina-Smart-Clinic-Wellness-Solution-for-Pets.-

Trạng thái AC (verify thật trên GitHub, không phải chỉ đọc YAML):

- **AC-1** (PR test fail → không merge được, branch protection chặn thật chứ không chỉ cảnh báo): ✅ tạo PR #1 với 1 test cố tình fail, bật branch protection yêu cầu check `build-and-test`, gọi merge API → GitHub trả **405 "Required status check build-and-test is failing"**. PR đã đóng, nhánh đã xoá sau khi verify.
- **AC-2** (merge vào main → image mới đúng tag SHA trên registry): ✅ push lên `main` kích hoạt `release.yml`, job `test` pass rồi mới tới job `build-image` (phụ thuộc `test`), push thành công 2 tag: `ghcr.io/khuongdp1402/furina-smart-clinic-wellness-solution-for-pets:<sha>` và `:latest`.

### 2 workflow

- **`.github/workflows/pr.yml`** — mọi PR vào `main`: restore, build, test (`dotnet test` + coverage qua coverlet), comment coverage lên PR. Check tên `build-and-test` — đây chính là context required trong branch protection.
- **`.github/workflows/release.yml`** — push vào `main`: job `test` chạy lại (không tin PR check cũ), job `build-image` (`needs: test`) build Docker image bằng `Dockerfile` gốc và push GHCR với tag `<commit-sha>` + `latest`. Tên image bị hạ về lowercase + bỏ ký tự cuối không hợp lệ (`.`/`-`) vì tên repo có chữ hoa.

### Branch protection (đã bật thật trên GitHub, qua API)

`main` yêu cầu status check `build-and-test` pass (`enforce_admins: true` — áp dụng cả với admin, không có ngoại lệ).

### Test project

`tests/Furina.Tests` (xUnit) — hiện có test cho `JwtTokenService` (claims đúng, hash refresh token đúng, không bao giờ lưu token thật). Chạy: `dotnet test`.

## Việc còn lại (Sprint 1 đã xong — TASK-11/12/13/14)

- Refresh token hiện không rotate (giữ nguyên giá trị đến khi hết hạn/bị revoke) — cân nhắc rotate nếu cần siết bảo mật hơn ở sprint sau.
- Test coverage hiện chỉ có `JwtTokenService`; các phần còn lại (middleware, controllers) chưa có test tự động, mới verify bằng tay qua HTTP + psql.
