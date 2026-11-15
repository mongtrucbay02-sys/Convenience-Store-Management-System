# Products and categories API

All endpoints use the existing session cookie, permission policies and common error envelope (`code`, `message`, `details`, `traceId`). Unsafe API methods require `X-XSRF-TOKEN` matching that session. Responses must not be cached.

## Categories — SP-07/SP-08

| Method | Route | Permission | Request / response |
|---|---|---|---|
| GET | `/api/v1/categories?page=1&size=20` | AD, NK, TN (`SanPhamXem`) | Existing paged category list; see category-list contract |
| GET | `/api/v1/categories/{id}` | AD (`SanPhamGiaDanhMuc`) | `id,ten,moTa,khongDoiTra,trangThai,phienBan` |
| POST | `/api/v1/categories` | AD | `{ten,moTa?,khongDoiTra}` → 201, Location, category details |
| PUT | `/api/v1/categories/{id}` | AD | `{ten,moTa?,khongDoiTra,phienBan}` → 200, category details |

Name is trimmed, required, maximum 100 characters. Description is trimmed, optional, maximum 255. Names use the existing database unique constraint/collation. New categories are HOAT_DONG, version 1. An actual edit increments the existing version through CuaHangDbContext; a no-op retains it. Clients must reload after 409 before retrying. Unknown fields cannot set status, ID or server-owned version.

400: malformed binding; 401: missing/invalid session; 403: insufficient permission, forced password change or missing XSRF; 404: category absent; 409: duplicate name or stale/concurrent edit; 422: semantic validation. Unexpected failures use the existing safe error handler.

Razor: `/Categories`, `/Categories/Create`, `/Categories/{id}/Edit`. AD alone receives create/edit links. Form POSTs require MVC antiforgery; validation and conflict errors are rendered with encoded values. Browser E2E has not been executed.

Pending business approval: SP-07 product-count scope and hidden-category visibility. Current management listing behavior is preserved. No API integration or acceptance on develop is claimed by local verification.

## GET /api/v1/products — SP-01

Permission: AD, NK, TN (`SanPhamXem`). Returns `items,total,page,size`. Each item contains `id,ma,tenSanPham,danhMucId,danhMuc:{id,ten},donViTinh,giaBan,tonKho,giaVon,trangThai,phienBan`. `giaVon` is null for NK/TN; TN can only retrieve DANG_KINH_DOANH products. No role query/header can override session permissions.

Parameters: `page` (positive, default 1, offset within Int32), `size` (positive, default 20, capped at 100), `keyword` (up to 200, literal case/accent-insensitive substring of code/name), `danhMucId` (positive), `trangThai` (runtime product enum), `giaBanTu`/`giaBanDen` (inclusive, 0..UInt32.MaxValue, ordered), `sort` (default tenSanPham).

Sort supports `ma,tenSanPham,giaBan,trangThai,ngayTao`; prefix `-` for descending, comma-separated unique fields, always followed by ID as a stable tie-breaker. Unknown filter/sort, invalid enum, impossible offset or invalid numeric range → 422. Malformed numeric binding → 400. `%`, `_` and `!` in keywords remain literal characters, not pattern syntax. Values are parameterized. An empty result is 200 with an empty items array. Effective price comes from the existing MySQL view and excludes future/cancelled history; stock/cost are read in one batch through IDichVuKho.

Razor management screen: `/Products`, with matching query parameters, escaped output, preserved filters in pagination links and an explicit empty/error state. Category suggestions are bounded to 100; the linked category list supplies IDs for other categories. Navbar visibility follows SanPhamXem. HTTP/Razor coverage is separate from browser E2E (NOT RUN).
