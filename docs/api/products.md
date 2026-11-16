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

## GET /api/v1/products/{id} — SP-02

Permission AD/NK/TN, with the same TN active-only restriction applied to direct IDs. Returns the SP-01 item fields plus `anhUrl,moTa,nguongToiThieu,ngayCapNhat,giaDaLenLich,lichSuGia`. Both price collections are paged envelopes (initial page 1, size 20); `lichSuGia` is null outside AD. `giaVon` is null outside AD. Unsafe image schemes are replaced with null. Dates include +07:00.

`giaDaLenLich` contains uncancelled history strictly after database NOW; current price is the latest uncancelled effective entry at/before NOW. AD history includes cancelled rows. Each price has `id,giaBan,hieuLucTu,nguoiTaoId,ngayTao,huyLuc,nguoiHuyId`; audit fields are null outside AD. Missing or TN-invisible resource → 404, message `Sản phẩm không còn tồn tại`.

## GET /api/v1/products/{id}/prices — SP-02

Parameters: `page=1`, `size=20` (positive, max 100), `daLenLich=true`. Stable order: effective date descending, then ID descending. `daLenLich=false` returns all price history and requires AD; other actors receive 403. Invalid pagination/unknown filter → 422; invalid numeric/bool binding → 400; missing/TN-invisible product → 404. Returns the common paged envelope. All reads are authenticated/no-store and never mutate state.

Razor details: `/Products/{id}`, reached from a product-name link on the management list. `/Products/{id}/Prices?page=1&size=20&daLenLich=true` provides previous/next navigation for all scheduled prices; `daLenLich=false` is AD-only history. Encodes names/descriptions, validates image URLs, shows an explicit missing/error state, and restricts cost/history sections to AD. Browser E2E NOT RUN.

## POST /api/v1/products — SP-03 (standalone verified; A1 blocked)

AD/NK (`SanPhamSua`), real session plus XSRF. Body: `{ma?,tenSanPham,danhMucId,donViTinh,giaBan,nguongToiThieu=5,moTa?}`. Code optional/generated, trimmed, max 20 and unique under the current database collation. Name required/max 200, active category required, unit required/max 20, price integral VND >0 and divisible by 100 within UInt32, threshold integer >=0, description optional/max 1000. Validation reports all invalid fields with 422; malformed numeric JSON → 400. Duplicate code → 409 `TRUNG_MA_SAN_PHAM`. TN → 403. Unknown stock/cost/status/owner/ID/history fields cannot be written through this DTO.

201 + `Location: /api/v1/products/{id}` and the role-filtered SP-02 response. One transaction inserts the product, initial price effective at database NOW and zero stock/cost through IDichVuKho. Initial version 1, active product, threshold default 5. Shared category lock keeps active selection consistent with concurrent category changes. No transaction-dependent history or stock survives rollback. NK initial-price permission cannot change an existing product's price.

Razor `/Products/Create` posts multipart with MVC antiforgery and field/binding validation. Optional `Anh` is validated before inserts. `_ProductFields.cshtml` is shared by the standard/compact (`?compact=true`) form; **NH-04 host/adding the new product into its draft row is absent and remains BLOCKED**. A passing standalone form is not evidence that A1 works. No completion/acceptance claim for #58.

## POST /api/v1/products/{id}/image — SP-03/SP-04

AD/NK plus XSRF. Multipart fields `image` (JPG/PNG, <=2 MiB) and `phienBan` (current positive version). MIME/header/trailer recognition, bounded reads and generated GUID filenames; original filename is never used. This does not perform full pixel decoding. The endpoint caps the total request at 3 MiB. A successful atomic version predicate updates only image URL/version and returns the SP-02 response (200). Invalid image/version → 422; missing product → 404; stale/concurrent version → 409. Image update cannot set price/stock or overwrite another metadata edit silently.

Uploaded files live under the existing static web root `/uploads/products/` and are ignored by Git. Exact newly-created files are removed on known pre-write conflicts; old/shared files are preserved. Unknown database commit outcomes preserve the candidate file to avoid removing a potentially committed image. Orphan-media cleanup is an operational task. No credentials or binaries from builds are tracked.

## PUT /api/v1/products/{id} — SP-04

AD/NK (`SanPhamSua`) + XSRF. Full editable body: `{tenSanPham,danhMucId,donViTinh,moTa?,nguongToiThieu,phienBan}`. Name required/max200; existing category; unit required/max20; description max1000; nonnegative threshold; positive current version. Trim text values. Server-owned code/price/stock/cost/status/owner fields cannot be overposted. Separate image endpoint remains available. Successful save → 200 SP-02 response and version +1.

Malformed binding → 400; invalid fields/category → 422; missing product → 404; stale version → 409 XUNG_DOT_DU_LIEU, `Dữ liệu đã được người khác thay đổi, vui lòng tải lại.`; forbidden unit change → 409 KHONG_DOI_DON_VI_TINH. Unit changes are refused with positive stock or any stock-ledger, invoice, receipt, count, disposal or cart reference. Pricing configuration alone is not a stock/business transaction. Name/description edits never rewrite historical snapshots. A missing stock row triggers reconciliation conflict rather than an invented zero balance.

Metadata/version/image URL and threshold use one MySQL transaction. Lock order follows the shared stock writer; the explicit version predicate protects SanPham without modifying EF token configuration or migration history. Price/history/quantity/cost are preserved. Category existence is enforced; hidden-category reassignment policy remains pending business clarification, with no new hidden-state rule introduced here.

Razor `/Products/{id}/Edit`: AD/NK links from details, antiforgery, bounded category suggestions, optional validated `Anh`, current hidden `PhienBan`, field errors and a reload link on conflict. Metadata plus optional image URL commit as one version change. Browser E2E NOT RUN.

## SP-05 — selling price history and scheduling

| Method | Route | Permission | Success |
|---|---|---|---|
| POST | `/api/v1/products/{id}/prices` | AD / SanPhamGiaDanhMuc + XSRF | 201, Location to the new history row |
| GET | `/api/v1/products/{id}/prices/{priceId}` | AD | 200, history row plus current product version |
| POST | `/api/v1/products/{id}/prices/{priceId}/cancel` | AD + XSRF | 200, cancelled row plus product version |

Create: `{giaBan,hieuLucTu?,phienBan,xacNhanDuoiGiaVon=false}`. Integral positive VND, multiple of100, up to UInt32; VAT included. Missing/null/blank time means database NOW. Explicit time is ISO 8601 with `Z` or numeric offset, converted to +07:00 at existing DATETIME second precision; no timestamp in the past. Use null for immediate changes rather than sending a client-clock approximation of NOW. No invented mandatory change reason or scheduled task.

Cancellation: `{phienBan}`. Only an uncancelled row belonging to that product with effective time strictly after database NOW can be cancelled. Both operations require an active product/current version, hold stock then product locks, and change history plus product version in one transaction. Cancellation preserves price/effective date/creator and sets both cancellation time and authenticated actor. Both return `{id,sanPhamId,giaBan,hieuLucTu,nguoiTaoId,ngayTao,huyLuc,nguoiHuyId,phienBan}`, dates with +07:00.

400: malformed money/body. 401/403: session/permission/XSRF. 404: missing product/price or wrong parent-price pairing. 422: invalid price/time/version. 409: stale/concurrent version, inactive product, already-cancelled/effective row, missing stock requiring reconciliation, or `CAN_XAC_NHAN_GIA_DUOI_VON`. The latter exposes current cost only to AD and requires explicitly setting `xacNhanDuoiGiaVon=true` before retry; it does not write history or increment version. Double submissions with the old version cannot append twice.

The shared current-price contract is the existing `v_gia_ban_hien_hanh`: uncancelled history at/before NOW, latest effective timestamp, then greatest ID. Both EF Products readers and `MySqlPosCatalog.FindAsync/SearchAsync` use it. SP-05 fixes those catalog read sessions to +07:00 to match EF; RED evidence reproduced a 10100 versus 10000 discrepancy before the fix. Existing cart `don_gia` snapshots remain unchanged; new catalog lookups see the new effective price. Existing POS cart/checkout/auth business code is otherwise unchanged.

Razor `/Products/{id}/Price`: AD current price/cost/history, immediate/future form with explicit Vietnamese local time and below-cost confirmation. `/Products/{id}/Prices/{priceId}/Cancel` is a read-only confirmation GET followed by antiforgery POST; repeated/stale submissions show a reload link. Scheduled-price lists link to this confirmation, including later pages. Browser E2E NOT RUN.
