# Issue #62 — SP-07 category listing

> **Tài liệu lịch sử.** Nội dung ghi lại thời điểm hệ thống còn dùng MySQL (trước 24/11/2026). Từ PR #286 ứng dụng chạy PostgreSQL 17 (`docs/adr/ADR-003-chuyen-csdl-postgresql-supabase.md`); số liệu test, kiểu dữ liệu và câu lệnh MySQL trong tệp này không áp dụng cho bản hiện tại.

Issue #62 and its comment, PR #169 and the existing audit were inspected. PR #169 merged into an intermediate DB branch, not develop. Initially the local API/repository existed, while UI, navigation, AD-only empty guidance and the common validation error contract were missing. Plan: reuse the existing layers; write HTTP/MySQL tests first; add a server-rendered screen and scoped DI; run focused/regression/full gates; review an isolated staged export before a local commit.

## Acceptance criteria

| Requirement | Initial | Local verification |
|---|---|---|
| Open category listing; name/product count/status | PARTIAL | IMPLEMENTED; HTTP UI/API agreement and MYSQL projection tests PASS |
| E1 empty state; create guidance only for AD | MISSING | IMPLEMENTED; AD/NK/TN rendered HTML cases PASS |
| BR-SP-09 single level | IMPLEMENTED in schema | Preserved; no hierarchy, entity or schema change |
| Server permissions AD/NK/TN | API implemented; UI missing | IMPLEMENTED on both surfaces; session/role/state cases PASS |
| Migration when changing schema | Not applicable | No migration/context/entity/snapshot changes |
| Independent reviewer and CI | BLOCKED externally | PENDING; not performed in this local-only workflow |
| QA #4 acceptance | BLOCKED externally | Automated evidence supplied; independent QA sign-off pending |
| Staging | BLOCKED externally | NOT RUN; no remote publication authorized |
| SRS if deviation | No new use case | Policy choices from existing port documented for business review |

## Design and review

Production changes: Products DTO/service/repository/API plus CategoryPagesController, typed Razor view, module DI, Program.cs registration and permission-aware navbar. One CuaHangDbContext. Lists order by ID, cap size at 100, reject invalid/overflow pagination, project counts in SQL, use AsNoTracking, propagate cancellation and render encoded names. No writer; duplicate writes, rollback and stale-version mutations are N/A. No schema or unrelated POS/Returns code changed. The UI uses the same service as JSON rather than fetching itself over HTTP.

Review: session authentication and server authorization retained; forced-password check added to the page; no-store on both surfaces; SQL is parameterized through EF; no per-row queries; bounded page reads; generic read-error UI and shared API validation envelope; cancellation is not swallowed. All required focused gates PASS. Restore/build PASS with 0 warnings/errors. git diff --check and staged diff check PASS. TDD red-2: 17 FAIL/11 PASS before implementation; focused-1: 38 PASS; final focused: 45 PASS. Initial test-authoring/compiler mistakes are preserved in raw build logs and are not counted as executed tests. Existing mixed SP/NH audit tests are preserved uncommitted and excluded from this scoped commit.

All changes are local. No push, PR, merge or issue closure. Browser E2E: **NOT RUN**; HTTP tests execute Razor HTML and JSON over Kestrel with the production database-backed session handler. No fake identity is used for security acceptance. No new package/test framework.

## Executed gates

| Gate | PASS | FAIL | SKIP | Total |
|---|---:|---:|---:|---:|
| focused-1 | 38 | 0 | 0 | 38 |
| full | 461 | 38 | 0 | 499 |
| http | 28 | 0 | 0 | 28 |
| index-full | 417 | 38 | 0 | 455 |
| mysql | 1 | 0 | 0 | 1 |
| red | 0 | 0 | 0 | 0 |
| red-2 | 11 | 17 | 0 | 28 |
| regression | 156 | 1 | 0 | 157 |
| unit | 16 | 0 | 0 | 16 |

Filtered runs overlap full runs and must not be added. UNIT / HTTP / MYSQL focused gates are disjoint. Full working-tree run includes preserved uncommitted files from the previous audit. `index-full` tests a separate export of only the proposed commit, including the staged Program.cs without unrelated registrations. Both discovery sets match their full TRX case identities exactly.

Previous local audit comparison: {'removed': [], 'changed': [], 'newFailures': [], 'added': 35}. Staged tree versus baseline: {'removed': [], 'changed': [], 'newFailures': [], 'added': 45}. The original 38 failures remain; overall solution is **FAIL**. No valid assertion was removed or weakened. Failing TDD/build evidence remains under `artifacts/issues/62`.

## Test cases

Preconditions: isolated MySQL 8.4.13 at localhost:33341; fresh fixture database built from existing migrations. HTTP cases create real sessions for the actor named in the case; expired/revoked/locked/forced/forged variants are seeded as shown in the test source. UNIT uses the existing xUnit project and explicit repository doubles. MYSQL runs real EF queries and database assertions.

Steps: UNIT constructs the service/controller and calls the named method with its inline boundary data. HTTP starts Kestrel with production auth, seeds the named account/session, requests the UI/API route and asserts status/body/visibility. MYSQL migrates a temporary database, seeds rows, executes the repository and asserts projections/order/constraints/cancellation. Each row links its precise method, inline test data and assertions through the source files below.

| ID | Level | Scenario / test data | Expected | Actual | Status | Evidence |
|---|---|---|---|---|---|---|
| 62-UNIT-001 | UNIT | `Modules.Products.CategoryServiceTests.GetCategories_ForwardsCancellationToken` | All assertions in the linked method hold. | Assertions passed | PASS | `artifacts/issues/62/unit.trx` |
| 62-UNIT-002 | UNIT | `Modules.Products.CategoriesControllerTests.EmptyCategories_ReturnsEmptyPagedResult` | All assertions in the linked method hold. | Assertions passed | PASS | `artifacts/issues/62/unit.trx` |
| 62-UNIT-003 | UNIT | `Modules.Products.CategoryServiceTests.InvalidPaginationDoesNotCallRepository(page: -1, size: 20)` | All assertions in the linked method hold. | Assertions passed | PASS | `artifacts/issues/62/unit.trx` |
| 62-UNIT-004 | UNIT | `Modules.Products.CategoriesControllerTests.ValidQuery_ReturnsOkWithCategories` | All assertions in the linked method hold. | Assertions passed | PASS | `artifacts/issues/62/unit.trx` |
| 62-UNIT-005 | UNIT | `Modules.Products.CategoryServiceTests.InvalidPaginationDoesNotCallRepository(page: 1, size: 0)` | All assertions in the linked method hold. | Assertions passed | PASS | `artifacts/issues/62/unit.trx` |
| 62-UNIT-006 | UNIT | `Modules.Products.CategoryServiceTests.InvalidPaginationDoesNotCallRepository(page: 2147483647, size: 100)` | All assertions in the linked method hold. | Assertions passed | PASS | `artifacts/issues/62/unit.trx` |
| 62-UNIT-007 | UNIT | `Modules.Products.CategoriesControllerTests.SizeOver100_ForwardsNormalizedSizeToService` | All assertions in the linked method hold. | Assertions passed | PASS | `artifacts/issues/62/unit.trx` |
| 62-UNIT-008 | UNIT | `Modules.Products.CategoriesControllerTests.SizeZero_ReturnsBadRequestWithoutCallingService` | All assertions in the linked method hold. | Assertions passed | PASS | `artifacts/issues/62/unit.trx` |
| 62-UNIT-009 | UNIT | `Modules.Products.CategoryServiceTests.InvalidPaginationDoesNotCallRepository(page: 0, size: 20)` | All assertions in the linked method hold. | Assertions passed | PASS | `artifacts/issues/62/unit.trx` |
| 62-UNIT-010 | UNIT | `Modules.Products.CategoriesControllerTests.PageZero_ReturnsBadRequestWithoutCallingService` | All assertions in the linked method hold. | Assertions passed | PASS | `artifacts/issues/62/unit.trx` |
| 62-UNIT-011 | UNIT | `Modules.Products.CategoriesControllerTests.ValidQuery_ForwardsQueryAndCancellationToken` | All assertions in the linked method hold. | Assertions passed | PASS | `artifacts/issues/62/unit.trx` |
| 62-UNIT-012 | UNIT | `Modules.Products.CategoryServiceTests.GetCategories_ForwardsQueryToRepository` | All assertions in the linked method hold. | Assertions passed | PASS | `artifacts/issues/62/unit.trx` |
| 62-UNIT-013 | UNIT | `Modules.Products.CategoriesControllerTests.SizeOver100_NormalizedSizeIs100` | All assertions in the linked method hold. | Assertions passed | PASS | `artifacts/issues/62/unit.trx` |
| 62-UNIT-014 | UNIT | `Modules.Products.CategoryServiceTests.NullQueryDoesNotCallRepository` | All assertions in the linked method hold. | Assertions passed | PASS | `artifacts/issues/62/unit.trx` |
| 62-UNIT-015 | UNIT | `Modules.Products.CategoryServiceTests.GetCategories_ReturnsRepositoryResult` | All assertions in the linked method hold. | Assertions passed | PASS | `artifacts/issues/62/unit.trx` |
| 62-UNIT-016 | UNIT | `Modules.Products.CategoryServiceTests.InvalidPaginationDoesNotCallRepository(page: 1, size: -1)` | All assertions in the linked method hold. | Assertions passed | PASS | `artifacts/issues/62/unit.trx` |
| 62-HTTP-001 | HTTP | `Modules.Products.CategoryHttpFeatureTests.BothSurfacesUseRealSessions(path: "/Categories", actor: "LOCKED", status: 401)` | Status equals the listed real-session actor/state matrix; successful reads are no-store. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-002 | HTTP | `Modules.Products.CategoryHttpFeatureTests.BothSurfacesUseRealSessions(path: "/api/v1/categories", actor: "TN", status: 200)` | Status equals the listed real-session actor/state matrix; successful reads are no-store. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-003 | HTTP | `Modules.Products.CategoryHttpFeatureTests.BothSurfacesUseRealSessions(path: "/api/v1/categories", actor: null, status: 401)` | Status equals the listed real-session actor/state matrix; successful reads are no-store. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-004 | HTTP | `Modules.Products.CategoryHttpFeatureTests.BothSurfacesUseRealSessions(path: "/Categories", actor: "FORCED", status: 403)` | Status equals the listed real-session actor/state matrix; successful reads are no-store. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-005 | HTTP | `Modules.Products.CategoryHttpFeatureTests.BothSurfacesUseRealSessions(path: "/api/v1/categories", actor: "EXPIRED", status: 401)` | Status equals the listed real-session actor/state matrix; successful reads are no-store. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-006 | HTTP | `Modules.Products.CategoryHttpFeatureTests.BothSurfacesUseRealSessions(path: "/api/v1/categories", actor: "AD", status: 200)` | Status equals the listed real-session actor/state matrix; successful reads are no-store. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-007 | HTTP | `Modules.Products.CategoryHttpFeatureTests.BothSurfacesUseRealSessions(path: "/Categories", actor: "AD", status: 200)` | Status equals the listed real-session actor/state matrix; successful reads are no-store. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-008 | HTTP | `Modules.Products.CategoryHttpFeatureTests.BothSurfacesUseRealSessions(path: "/api/v1/categories", actor: "NK", status: 200)` | Status equals the listed real-session actor/state matrix; successful reads are no-store. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-009 | HTTP | `Modules.Products.CategoryHttpFeatureTests.EmptyPageOffersCreationGuidanceOnlyToAdmin(role: "NK", guidance: False)` | Empty state and navigation/paging form render; creation guidance is present only for AD. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-010 | HTTP | `Modules.Products.CategoryHttpFeatureTests.ReadFailureShowsRetryWithoutLeakingDetails` | 503 with a retry link; database exception detail is absent from HTML. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-011 | HTTP | `Modules.Products.CategoryHttpFeatureTests.EmptyPageOffersCreationGuidanceOnlyToAdmin(role: "AD", guidance: True)` | Empty state and navigation/paging form render; creation guidance is present only for AD. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-012 | HTTP | `Modules.Products.CategoryHttpFeatureTests.PopulatedPageAndApiAgreeAndEncodeUntrustedCategoryNames` | Razor encodes script markup; API returns the same category/count; populated state is displayed. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-013 | HTTP | `Modules.Products.CategoryHttpFeatureTests.BothSurfacesUseRealSessions(path: "/api/v1/categories", actor: "REVOKED", status: 401)` | Status equals the listed real-session actor/state matrix; successful reads are no-store. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-014 | HTTP | `Modules.Products.CategoryHttpFeatureTests.InvalidQueryHasCommonErrorContract(query: "page=0")` | 400 with code YEU_CAU_KHONG_HOP_LE and nonempty traceId. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-015 | HTTP | `Modules.Products.CategoryHttpFeatureTests.BothSurfacesUseRealSessions(path: "/Categories", actor: null, status: 401)` | Status equals the listed real-session actor/state matrix; successful reads are no-store. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-016 | HTTP | `Modules.Products.CategoryHttpFeatureTests.BothSurfacesUseRealSessions(path: "/Categories", actor: "EXPIRED", status: 401)` | Status equals the listed real-session actor/state matrix; successful reads are no-store. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-017 | HTTP | `Modules.Products.CategoryHttpFeatureTests.InvalidQueryHasCommonErrorContract(query: "page=-1")` | 400 with code YEU_CAU_KHONG_HOP_LE and nonempty traceId. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-018 | HTTP | `Modules.Products.CategoryHttpFeatureTests.BothSurfacesUseRealSessions(path: "/api/v1/categories", actor: "FORCED", status: 403)` | Status equals the listed real-session actor/state matrix; successful reads are no-store. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-019 | HTTP | `Modules.Products.CategoryHttpFeatureTests.BothSurfacesUseRealSessions(path: "/Categories", actor: "TN", status: 200)` | Status equals the listed real-session actor/state matrix; successful reads are no-store. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-020 | HTTP | `Modules.Products.CategoryHttpFeatureTests.BothSurfacesUseRealSessions(path: "/api/v1/categories", actor: "LOCKED", status: 401)` | Status equals the listed real-session actor/state matrix; successful reads are no-store. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-021 | HTTP | `Modules.Products.CategoryHttpFeatureTests.BothSurfacesUseRealSessions(path: "/Categories", actor: "NK", status: 200)` | Status equals the listed real-session actor/state matrix; successful reads are no-store. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-022 | HTTP | `Modules.Products.CategoryHttpFeatureTests.InvalidQueryHasCommonErrorContract(query: "page=2147483647&size=100")` | 400 with code YEU_CAU_KHONG_HOP_LE and nonempty traceId. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-023 | HTTP | `Modules.Products.CategoryHttpFeatureTests.InvalidQueryHasCommonErrorContract(query: "size=0")` | 400 with code YEU_CAU_KHONG_HOP_LE and nonempty traceId. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-024 | HTTP | `Modules.Products.CategoryHttpFeatureTests.BothSurfacesUseRealSessions(path: "/Categories", actor: "FORGED", status: 401)` | Status equals the listed real-session actor/state matrix; successful reads are no-store. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-025 | HTTP | `Modules.Products.CategoryHttpFeatureTests.EmptyPageOffersCreationGuidanceOnlyToAdmin(role: "TN", guidance: False)` | Empty state and navigation/paging form render; creation guidance is present only for AD. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-026 | HTTP | `Modules.Products.CategoryHttpFeatureTests.BothSurfacesUseRealSessions(path: "/api/v1/categories", actor: "FORGED", status: 401)` | Status equals the listed real-session actor/state matrix; successful reads are no-store. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-027 | HTTP | `Modules.Products.CategoryHttpFeatureTests.BothSurfacesUseRealSessions(path: "/Categories", actor: "REVOKED", status: 401)` | Status equals the listed real-session actor/state matrix; successful reads are no-store. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-HTTP-028 | HTTP | `Modules.Products.CategoryHttpFeatureTests.InvalidQueryHasCommonErrorContract(query: "page=abc")` | 400 with code YEU_CAU_KHONG_HOP_LE and nonempty traceId. | Assertions passed | PASS | `artifacts/issues/62/http.trx` |
| 62-MYSQL-001 | MYSQL | `Modules.Products.CategoryMySqlTests.StablePagesCountAllAssignedProductsAndDoNotTrackEntities` | Stable ID pages, counts including discontinued products, hidden/empty categories, cap 100, empty out-of-range page, no tracking and cancellation. | Assertions passed | PASS | `artifacts/issues/62/mysql.trx` |
| 62-UI-E2E-001 | UI-E2E | Browser navigation/forms/visibility | Browser automation executes user workflow | No browser E2E runner in repository; no package added | NOT RUN | HTTP evidence is separate |

Test sources: [CategoryServiceTests.cs](../../../tests/ConvenienceStore.Web.Tests/Modules/Products/CategoryServiceTests.cs), [CategoriesControllerTests.cs](../../../tests/ConvenienceStore.Web.Tests/Modules/Products/CategoriesControllerTests.cs), [CategoryHttpFeatureTests.cs](../../../tests/ConvenienceStore.Web.Tests/Modules/Products/CategoryHttpFeatureTests.cs), [CategoryMySqlTests.cs](../../../tests/ConvenienceStore.Web.Tests/Modules/Products/CategoryMySqlTests.cs), [ListFeatureHost.cs](../../../tests/ConvenienceStore.Web.Tests/HaTang/ListFeatureHost.cs).

Machine evidence: [results](../evidence/issues/62/results.json), [tested staged-source manifest](../evidence/issues/62/source-manifest.json), [raw evidence hashes](../evidence/issues/62/raw-manifest.json). Manifest fixes source content before the commit exists; the commit SHA is reported after committing.

## Remaining acceptance and next issue

Local implementation is verified for the chosen audited semantics (all assigned products counted; hidden categories visible to AD/NK/TN). Those two policy choices still await business review; the user was asked and the existing behavior was retained as a documented assumption. External independent review/CI/QA/staging and browser E2E are not claimed PASS. No technical local blocker or newly introduced regression failure was found. Overall release acceptance remains pending. Next: #65 after this scoped local implementation commit; no issue is closed.
