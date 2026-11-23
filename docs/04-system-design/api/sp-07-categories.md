# SP-07 — category listing

Issue #62. Runtime owner: `CuaHangDbContext`; no schema or migration changes.

`GET /Categories` renders the Razor/Bootstrap screen through `ICategoryService`.
The navbar exposes it to users with `SAN_PHAM_XEM`. The screen shows category
name, assigned product count and status, plus page-size and previous/next controls.
An empty database shows creation guidance only to AD, referring to SP-08 without
advertising a create endpoint that does not exist. Database read errors return
503 with a retry link and a generic message.

`GET /api/v1/categories?page=1&size=20` returns JSON:

```json
{"items":[{"id":1,"ten":"Đồ uống","khongDoiTra":false,"soSanPham":2,"trangThai":"HOAT_DONG","phienBan":1}],"total":1,"page":1,"size":20}
```

Page and size must be positive integers; size is capped at 100. Offsets above
Int32.MaxValue return 400. Invalid query input uses the shared error contract
`{code,message,details,traceId}`. Empty pages return 200 and retain total count.
Ordering is by ID ascending. Reads use AsNoTracking and project product counts
in SQL, without per-row database calls.

AD, NK and TN need a valid database-backed session and `SAN_PHAM_XEM` on both
surfaces. Anonymous/invalid/expired/revoked/locked sessions receive 401. Forced
password changes receive 403. Responses disable caching. No writer, transaction
or stale-version mutation is introduced by this issue.

The local implementation retains the audited PR #169 semantics: counts include
all assigned products and the management list includes hidden categories for all
three actors. These two policy choices remain subject to business review; the
POS catalog's existing active-only filtering is unchanged. Razor encodes names.
The UI renders on the server through the same service as the JSON API; it does
not introduce a second client-side data store or a new frontend framework.

HTTP feature tests execute the rendered UI, session/role matrix, empty state,
error state and JSON contracts. They are not browser E2E tests. External PR
review, CI, QA sign-off and staging acceptance remain pending; no remote action
is performed by the local implementation workflow.
