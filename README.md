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

## US-38: Thiết kế UI/UX & Bộ nhận diện thương hiệu (Design System)

Trạng thái AC:

- **AC-1** (design token áp vào trang mẫu, đúng màu/font/spacing): ✅ `web/landing/app/design-preview` — verify bằng Playwright (`web/landing/tests/landing.spec.ts`).
- **AC-2** (landing page đủ 8 section theo wiki, không thiếu/thừa): ✅ `web/landing/app/page.tsx` — verify bằng Playwright, đếm + kiểm tra thứ tự `aria-label` của từng section.

Spec đầy đủ: `docs/superpowers/specs/2026-09-11-design-system-landing-design.md` (nội dung thiết kế gốc: wiki Taiga trang `thiet-ke-uiux`). Plan triển khai: `docs/superpowers/plans/2026-09-11-design-system-landing.md`.

### Chạy local

```bash
cd web/landing
npm install
npm run dev -- --port 3100   # port 3000 có thể bị chiếm bởi app khác trên máy dev

# Kiểm tra token + contrast (không cần chạy web server)
node ../design-system/contrast-check.mjs
node --test ../design-system/contrast.test.mjs

# E2E (Playwright tự chạy dev server trên port 3100)
npm run test:e2e
```

### Bug thật đã phát hiện & sửa trong lúc triển khai

- Turbopack chặn `@import` CSS vượt ra ngoài thư mục app ("leaves the filesystem root") — chặn đúng cách `web/landing` import token dùng chung từ `../../design-system/`. Sửa bằng `turbopack.root` trong `next.config.ts` trỏ lên `web/`.
- `npx shadcn@latest init` mặc định (`-d`) cài **Base UI** (`@base-ui/react`), không phải Radix như spec yêu cầu — phải chỉ định rõ `-b radix`.
- Mỗi lần chạy `shadcn init`/`add`, CLI ghi đè toàn bộ `app/globals.css` và có thể xoá `--font-heading` (Fraunces) về font mặc định — phải khôi phục token sau mỗi lần chạy CLI.

### Chưa làm (theo đúng phạm vi wiki "Việc CHƯA làm")

- Logo thật (mới có concept mô tả).
- A/B test copy landing page.
- Audit WCAG chính thức (có sanity-check tự động qua `contrast-check.mjs`, không thay thế audit thật).
- Tenant Admin / Super Admin Portal (Sprint 5/6) áp dụng cùng token với tông khác — chưa tồn tại, sẽ import `web/design-system` khi tới sprint đó.
- Next 15.5.25 (pin theo spec) có 2 lỗ hổng PostCSS mức build-time (XSS/path traversal khi build, không phải runtime browser) — bản vá đòi hỏi nâng lên Next 16, ngoài phạm vi task này.

## Việc còn lại (không thuộc scope TASK-11/12/13)

- Pipeline CI/CD — task cuối của Sprint 1.
- Refresh token hiện không rotate (giữ nguyên giá trị đến khi hết hạn/bị revoke) — cân nhắc rotate nếu cần siết bảo mật hơn ở sprint sau.
