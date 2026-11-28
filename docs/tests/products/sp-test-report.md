# SP module test report

Tested source worktree at `06f70abceb7a9991537b98bcda6eb8219bf46edc`; manifest in evidence/final/source-manifest.json. Target baseline develop `babfe1edae507e5d94a95c422a76ffa344a23c00`, initial executable SP-07 tree `86fa911`. Current fetched develop `a024757b73e696b5f4285f070c642db0c3a467cd`. Isolation B only; original dirty worktrees/NH work preserved. Local commits only; no push/PR/merge/issue closure.

Fresh baseline: 455 total, 417 PASS, 38 FAIL, 0 SKIP. Final: 682 total, 644 PASS, 38 FAIL, 0 SKIP. New failures: 0; removed baseline cases: 0; baseline fixes: 0; changed failure causes: 0. Overall solution FAIL. Filtered runs overlap and are never summed.

## Summary

| Issue | Priority | Code | Unit | HTTP/Razor | MySQL | UI E2E | Regression | Commit | Status |
|---|---|---|---:|---:|---:|---|---|---|---|
| #62 SP-07 | Must | Local | 16 PASS | 28 PASS | 1 PASS | NOT RUN | 0 new FAIL | `86fa911` | VERIFIED_LOCAL |
| #63 SP-08 | Must | Local | 5 PASS | 13 PASS | 1 PASS | NOT RUN | 0 new FAIL | `1a9d32b` | VERIFIED_LOCAL |
| #56 SP-01 | Must | Local | 14 PASS | 22 PASS | 1 PASS | NOT RUN | 0 new FAIL | `438c2c3` | VERIFIED_LOCAL |
| #57 SP-02 | Should | Local | 9 PASS | 10 PASS | 1 PASS | NOT RUN | 0 new FAIL | `bac65e8` | VERIFIED_LOCAL |
| #58 SP-03 | Must | Local | 9 PASS | 22 PASS | 2 PASS | NOT RUN | 0 new FAIL | `5831f2a` WIP | BLOCKED |
| #59 SP-04 | Must | Local | 5 PASS | 12 PASS | 3 PASS | NOT RUN | 0 new FAIL | `e910d26` | VERIFIED_LOCAL |
| #60 SP-05 | Must | Local | 10 PASS | 16 PASS | 5 PASS | NOT RUN | 0 new FAIL | `628aed5` | VERIFIED_LOCAL |
| #61 SP-06 | Should | Local | 10 PASS | 12 PASS | 11 PASS | NOT RUN | 0 new FAIL | `27e4b7c` | VERIFIED_LOCAL |
| #64 SP-09 | Could | Local | 5 PASS | 12 PASS | 7 PASS | NOT RUN | 0 new FAIL | `96e5af4` | VERIFIED_LOCAL |

Additional final runtime/legacy integration cases: 10 PASS, included once in full totals. HTTP/Razor uses real production session authentication; it is not browser E2E.

Final integration fix `06f70abceb7a9991537b98bcda6eb8219bf46edc` secures the legacy area landing controller, removes its obsolete welcome-service dependency, and adds the existing `SP` API tag to ten controllers. [Integration case matrix](sp-integration-test-cases.md) records RED/GREEN, exact role data and runtime assertions. Narrow/intermediate filter runs are retained as diagnostics; final filters use the class list from issue evidence. The final 83 UNIT, 157 HTTP and 32 MYSQL identities are disjoint and cover exactly the 272 Products cases in the full TRX. Baseline/focused/full totals are never added together.

## A. Implemented functionality and files

| Scope | Implemented local behavior |
|---|---|
| SP-07 | Reused category list, name/count/status, bounded pagination, role and empty-state behavior; policy questions remain pending |
| SP-08 | Admin category create/edit, normalized duplicate handling, required names, existing EF concurrency token and user-facing conflicts |
| SP-01 | Management list with literal search, category/status/price filters, stable sorting and pagination; batched stock/cost reads and role restrictions |
| SP-02 | Product details, category/current price, role-safe cost/history and paged price-history Razor workflow |
| SP-03 | Standalone validated product/image creation, unique code, active category, initial price and stock in one transaction; NH-04 compact-add consumer remains absent |
| SP-04 | Allowed metadata/image/threshold edits with version predicate, transaction rollback and unit/history protection |
| SP-05 | Admin effective-now/future prices, below-cost confirmation, future cancellation and atomic history/version updates; shared POS reads use the existing +07:00 price convention |
| SP-06 | Confirmed discontinue/reactivate, reference/stock/held/draft warnings, safe deletion of unused products and preserved historical data |
| SP-09 | Admin category deletion/hiding with version protection, FK/reference preservation, idempotent hide and existing POS active-category behavior |

### #62 SP-07 — VERIFIED_LOCAL

Commit `86fa91117f1233ddc31e8c8f9af07176cce05a89` — feat(sp-07): complete category listing with automated tests

Production files:

- [src/ConvenienceStore.Web/Modules/Products/Controllers/CategoriesController.cs](../../../src/ConvenienceStore.Web/Modules/Products/Controllers/CategoriesController.cs)
- [src/ConvenienceStore.Web/Modules/Products/Controllers/CategoryPagesController.cs](../../../src/ConvenienceStore.Web/Modules/Products/Controllers/CategoryPagesController.cs)
- [src/ConvenienceStore.Web/Modules/Products/DTOs/CategoryListItemDto.cs](../../../src/ConvenienceStore.Web/Modules/Products/DTOs/CategoryListItemDto.cs)
- [src/ConvenienceStore.Web/Modules/Products/DTOs/CategoryListQuery.cs](../../../src/ConvenienceStore.Web/Modules/Products/DTOs/CategoryListQuery.cs)
- [src/ConvenienceStore.Web/Modules/Products/DTOs/PagedResultDto.cs](../../../src/ConvenienceStore.Web/Modules/Products/DTOs/PagedResultDto.cs)
- [src/ConvenienceStore.Web/Modules/Products/DangKyModuleProducts.cs](../../../src/ConvenienceStore.Web/Modules/Products/DangKyModuleProducts.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/CategoryService.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/CategoryService.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/EfCategoryReadRepository.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/EfCategoryReadRepository.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/ICategoryReadRepository.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/ICategoryReadRepository.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/ICategoryService.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/ICategoryService.cs)
- [src/ConvenienceStore.Web/Modules/Products/Views/Categories/Index.cshtml](../../../src/ConvenienceStore.Web/Modules/Products/Views/Categories/Index.cshtml)
- [src/ConvenienceStore.Web/Program.cs](../../../src/ConvenienceStore.Web/Program.cs)
- [src/ConvenienceStore.Web/Views/Shared/_Layout.cshtml](../../../src/ConvenienceStore.Web/Views/Shared/_Layout.cshtml)

Executed cases and exact scenarios: [SP-07](sp-07-test-cases.md).

### #63 SP-08 — VERIFIED_LOCAL

Commit `1a9d32b9be469a97d29f350021a883738d6aff78` — feat(sp-08): implement category create and update with tests

Production files:

- [src/ConvenienceStore.Web/Modules/Products/Controllers/CategoryEditorController.cs](../../../src/ConvenienceStore.Web/Modules/Products/Controllers/CategoryEditorController.cs)
- [src/ConvenienceStore.Web/Modules/Products/Controllers/CategoryMutationsController.cs](../../../src/ConvenienceStore.Web/Modules/Products/Controllers/CategoryMutationsController.cs)
- [src/ConvenienceStore.Web/Modules/Products/DTOs/CategoryMutationDtos.cs](../../../src/ConvenienceStore.Web/Modules/Products/DTOs/CategoryMutationDtos.cs)
- [src/ConvenienceStore.Web/Modules/Products/DangKyModuleProducts.cs](../../../src/ConvenienceStore.Web/Modules/Products/DangKyModuleProducts.cs)
- [src/ConvenienceStore.Web/Modules/Products/Models/CategoryEditModel.cs](../../../src/ConvenienceStore.Web/Modules/Products/Models/CategoryEditModel.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/CategoryMutationService.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/CategoryMutationService.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/EfCategoryMutationRepository.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/EfCategoryMutationRepository.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/ICategoryMutationRepository.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/ICategoryMutationRepository.cs)
- [src/ConvenienceStore.Web/Modules/Products/Views/Categories/Edit.cshtml](../../../src/ConvenienceStore.Web/Modules/Products/Views/Categories/Edit.cshtml)
- [src/ConvenienceStore.Web/Modules/Products/Views/Categories/Index.cshtml](../../../src/ConvenienceStore.Web/Modules/Products/Views/Categories/Index.cshtml)

Executed cases and exact scenarios: [SP-08](sp-08-test-cases.md).

### #56 SP-01 — VERIFIED_LOCAL

Commit `438c2c342e03e2a9c7600c6beb37d256c8a1206d` — feat(sp-01): implement product listing and filtering

Production files:

- [src/ConvenienceStore.Web/Modules/Products/Controllers/ProductListController.cs](../../../src/ConvenienceStore.Web/Modules/Products/Controllers/ProductListController.cs)
- [src/ConvenienceStore.Web/Modules/Products/Controllers/ProductPagesController.cs](../../../src/ConvenienceStore.Web/Modules/Products/Controllers/ProductPagesController.cs)
- [src/ConvenienceStore.Web/Modules/Products/DTOs/ProductListItemDto.cs](../../../src/ConvenienceStore.Web/Modules/Products/DTOs/ProductListItemDto.cs)
- [src/ConvenienceStore.Web/Modules/Products/DTOs/ProductListQuery.cs](../../../src/ConvenienceStore.Web/Modules/Products/DTOs/ProductListQuery.cs)
- [src/ConvenienceStore.Web/Modules/Products/DangKyModuleProducts.cs](../../../src/ConvenienceStore.Web/Modules/Products/DangKyModuleProducts.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/EfProductReadRepository.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/EfProductReadRepository.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/IProductReadRepository.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/IProductReadRepository.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/ProductReadService.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/ProductReadService.cs)
- [src/ConvenienceStore.Web/Modules/Products/Views/Management/Index.cshtml](../../../src/ConvenienceStore.Web/Modules/Products/Views/Management/Index.cshtml)
- [src/ConvenienceStore.Web/Views/Shared/_Layout.cshtml](../../../src/ConvenienceStore.Web/Views/Shared/_Layout.cshtml)

Executed cases and exact scenarios: [SP-01](sp-01-test-cases.md).

### #57 SP-02 — VERIFIED_LOCAL

Commit `bac65e8a7bda0c47c077209b08d8f4f00cf473d4` — feat(sp-02): implement product details and price history views

Production files:

- [src/ConvenienceStore.Web/Modules/Products/Controllers/ProductDetailsController.cs](../../../src/ConvenienceStore.Web/Modules/Products/Controllers/ProductDetailsController.cs)
- [src/ConvenienceStore.Web/Modules/Products/Controllers/ProductDetailsPageController.cs](../../../src/ConvenienceStore.Web/Modules/Products/Controllers/ProductDetailsPageController.cs)
- [src/ConvenienceStore.Web/Modules/Products/Controllers/ProductPricePagesController.cs](../../../src/ConvenienceStore.Web/Modules/Products/Controllers/ProductPricePagesController.cs)
- [src/ConvenienceStore.Web/Modules/Products/DTOs/ProductDetailsDto.cs](../../../src/ConvenienceStore.Web/Modules/Products/DTOs/ProductDetailsDto.cs)
- [src/ConvenienceStore.Web/Modules/Products/DTOs/ProductListItemDto.cs](../../../src/ConvenienceStore.Web/Modules/Products/DTOs/ProductListItemDto.cs)
- [src/ConvenienceStore.Web/Modules/Products/DangKyModuleProducts.cs](../../../src/ConvenienceStore.Web/Modules/Products/DangKyModuleProducts.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/EfProductDetailRepository.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/EfProductDetailRepository.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/IProductDetailRepository.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/IProductDetailRepository.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/ProductDetailService.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/ProductDetailService.cs)
- [src/ConvenienceStore.Web/Modules/Products/Views/Management/Details.cshtml](../../../src/ConvenienceStore.Web/Modules/Products/Views/Management/Details.cshtml)
- [src/ConvenienceStore.Web/Modules/Products/Views/Management/Index.cshtml](../../../src/ConvenienceStore.Web/Modules/Products/Views/Management/Index.cshtml)
- [src/ConvenienceStore.Web/Modules/Products/Views/Management/Prices.cshtml](../../../src/ConvenienceStore.Web/Modules/Products/Views/Management/Prices.cshtml)

Executed cases and exact scenarios: [SP-02](sp-02-test-cases.md).

### #58 SP-03 — BLOCKED

Commit `5831f2aab04ca45c200166c1055c6bdfff3fcb29` — wip(sp-03): preserve standalone creation; NH-04 flow blocked

Production files:

- [src/ConvenienceStore.Web/Modules/Products/Controllers/ProductCreationController.cs](../../../src/ConvenienceStore.Web/Modules/Products/Controllers/ProductCreationController.cs)
- [src/ConvenienceStore.Web/Modules/Products/Controllers/ProductCreationPageController.cs](../../../src/ConvenienceStore.Web/Modules/Products/Controllers/ProductCreationPageController.cs)
- [src/ConvenienceStore.Web/Modules/Products/Controllers/ProductImagesController.cs](../../../src/ConvenienceStore.Web/Modules/Products/Controllers/ProductImagesController.cs)
- [src/ConvenienceStore.Web/Modules/Products/DTOs/ProductCreateDto.cs](../../../src/ConvenienceStore.Web/Modules/Products/DTOs/ProductCreateDto.cs)
- [src/ConvenienceStore.Web/Modules/Products/DangKyModuleProducts.cs](../../../src/ConvenienceStore.Web/Modules/Products/DangKyModuleProducts.cs)
- [src/ConvenienceStore.Web/Modules/Products/Models/ProductCreateModel.cs](../../../src/ConvenienceStore.Web/Modules/Products/Models/ProductCreateModel.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/EfProductCreationRepository.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/EfProductCreationRepository.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/IProductCreationRepository.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/IProductCreationRepository.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/ProductCreationService.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/ProductCreationService.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/ProductImageService.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/ProductImageService.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/ProductImages.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/ProductImages.cs)
- [src/ConvenienceStore.Web/Modules/Products/Views/Management/Create.cshtml](../../../src/ConvenienceStore.Web/Modules/Products/Views/Management/Create.cshtml)
- [src/ConvenienceStore.Web/Modules/Products/Views/Management/Index.cshtml](../../../src/ConvenienceStore.Web/Modules/Products/Views/Management/Index.cshtml)
- [src/ConvenienceStore.Web/Modules/Products/Views/Management/_ProductFields.cshtml](../../../src/ConvenienceStore.Web/Modules/Products/Views/Management/_ProductFields.cshtml)

Executed cases and exact scenarios: [SP-03](sp-03-test-cases.md).

### #59 SP-04 — VERIFIED_LOCAL

Commit `e910d262f8674310c1f4b1955f8fc0a87ce39239` — feat(sp-04): implement product editing with concurrency checks

Production files:

- [src/ConvenienceStore.Web/Modules/Products/Controllers/ProductEditingController.cs](../../../src/ConvenienceStore.Web/Modules/Products/Controllers/ProductEditingController.cs)
- [src/ConvenienceStore.Web/Modules/Products/Controllers/ProductEditingPageController.cs](../../../src/ConvenienceStore.Web/Modules/Products/Controllers/ProductEditingPageController.cs)
- [src/ConvenienceStore.Web/Modules/Products/DTOs/ProductEditDto.cs](../../../src/ConvenienceStore.Web/Modules/Products/DTOs/ProductEditDto.cs)
- [src/ConvenienceStore.Web/Modules/Products/DangKyModuleProducts.cs](../../../src/ConvenienceStore.Web/Modules/Products/DangKyModuleProducts.cs)
- [src/ConvenienceStore.Web/Modules/Products/Models/ProductEditModel.cs](../../../src/ConvenienceStore.Web/Modules/Products/Models/ProductEditModel.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/EfProductEditRepository.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/EfProductEditRepository.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/IProductEditRepository.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/IProductEditRepository.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/ProductEditService.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/ProductEditService.cs)
- [src/ConvenienceStore.Web/Modules/Products/Views/Management/Details.cshtml](../../../src/ConvenienceStore.Web/Modules/Products/Views/Management/Details.cshtml)
- [src/ConvenienceStore.Web/Modules/Products/Views/Management/Edit.cshtml](../../../src/ConvenienceStore.Web/Modules/Products/Views/Management/Edit.cshtml)

Executed cases and exact scenarios: [SP-04](sp-04-test-cases.md).

### #60 SP-05 — VERIFIED_LOCAL

Commit `628aed5c56017b16b693464406357fcdb9ce8d2e` — feat(sp-05): implement price scheduling and history verification

Production files:

- [src/ConvenienceStore.Web/Modules/Products/Controllers/ProductPricingController.cs](../../../src/ConvenienceStore.Web/Modules/Products/Controllers/ProductPricingController.cs)
- [src/ConvenienceStore.Web/Modules/Products/Controllers/ProductPricingPageController.cs](../../../src/ConvenienceStore.Web/Modules/Products/Controllers/ProductPricingPageController.cs)
- [src/ConvenienceStore.Web/Modules/Products/DTOs/ProductPricingDtos.cs](../../../src/ConvenienceStore.Web/Modules/Products/DTOs/ProductPricingDtos.cs)
- [src/ConvenienceStore.Web/Modules/Products/DangKyModuleProducts.cs](../../../src/ConvenienceStore.Web/Modules/Products/DangKyModuleProducts.cs)
- [src/ConvenienceStore.Web/Modules/Products/Models/ProductPriceModel.cs](../../../src/ConvenienceStore.Web/Modules/Products/Models/ProductPriceModel.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/EfProductPricingRepository.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/EfProductPricingRepository.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/IProductPricingRepository.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/IProductPricingRepository.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/ProductPricingService.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/ProductPricingService.cs)
- [src/ConvenienceStore.Web/Modules/Products/Views/Management/CancelPrice.cshtml](../../../src/ConvenienceStore.Web/Modules/Products/Views/Management/CancelPrice.cshtml)
- [src/ConvenienceStore.Web/Modules/Products/Views/Management/Details.cshtml](../../../src/ConvenienceStore.Web/Modules/Products/Views/Management/Details.cshtml)
- [src/ConvenienceStore.Web/Modules/Products/Views/Management/Price.cshtml](../../../src/ConvenienceStore.Web/Modules/Products/Views/Management/Price.cshtml)
- [src/ConvenienceStore.Web/Modules/Products/Views/Management/Prices.cshtml](../../../src/ConvenienceStore.Web/Modules/Products/Views/Management/Prices.cshtml)
- [src/ConvenienceStore.Web/Services/Pos/MySqlPosCatalog.cs](../../../src/ConvenienceStore.Web/Services/Pos/MySqlPosCatalog.cs)

Executed cases and exact scenarios: [SP-05](sp-05-test-cases.md).

### #61 SP-06 — VERIFIED_LOCAL

Commit `27e4b7c45cffcf70bbbdf62ed1020a1fc42ae79b` — feat(sp-06): implement guarded product lifecycle with tests

Production files:

- [src/ConvenienceStore.Web/Modules/Products/Controllers/ProductLifecycleController.cs](../../../src/ConvenienceStore.Web/Modules/Products/Controllers/ProductLifecycleController.cs)
- [src/ConvenienceStore.Web/Modules/Products/Controllers/ProductLifecyclePageController.cs](../../../src/ConvenienceStore.Web/Modules/Products/Controllers/ProductLifecyclePageController.cs)
- [src/ConvenienceStore.Web/Modules/Products/DTOs/ProductLifecycleDtos.cs](../../../src/ConvenienceStore.Web/Modules/Products/DTOs/ProductLifecycleDtos.cs)
- [src/ConvenienceStore.Web/Modules/Products/DangKyModuleProducts.cs](../../../src/ConvenienceStore.Web/Modules/Products/DangKyModuleProducts.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/EfProductLifecycleRepository.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/EfProductLifecycleRepository.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/IProductLifecycleRepository.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/IProductLifecycleRepository.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/ProductLifecycleService.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/ProductLifecycleService.cs)
- [src/ConvenienceStore.Web/Modules/Products/Views/Management/Details.cshtml](../../../src/ConvenienceStore.Web/Modules/Products/Views/Management/Details.cshtml)
- [src/ConvenienceStore.Web/Modules/Products/Views/Management/Lifecycle.cshtml](../../../src/ConvenienceStore.Web/Modules/Products/Views/Management/Lifecycle.cshtml)

Executed cases and exact scenarios: [SP-06](sp-06-test-cases.md).

### #64 SP-09 — VERIFIED_LOCAL

Commit `96e5af478d7209887c3349b5168b765e57b94c2d` — feat(sp-09): implement category removal and hiding with tests

Production files:

- [src/ConvenienceStore.Web/Modules/Products/Controllers/CategoryRemovalController.cs](../../../src/ConvenienceStore.Web/Modules/Products/Controllers/CategoryRemovalController.cs)
- [src/ConvenienceStore.Web/Modules/Products/Controllers/CategoryRemovalPageController.cs](../../../src/ConvenienceStore.Web/Modules/Products/Controllers/CategoryRemovalPageController.cs)
- [src/ConvenienceStore.Web/Modules/Products/DTOs/CategoryRemovalResult.cs](../../../src/ConvenienceStore.Web/Modules/Products/DTOs/CategoryRemovalResult.cs)
- [src/ConvenienceStore.Web/Modules/Products/DangKyModuleProducts.cs](../../../src/ConvenienceStore.Web/Modules/Products/DangKyModuleProducts.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/CategoryRemovalService.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/CategoryRemovalService.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/EfCategoryRemovalRepository.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/EfCategoryRemovalRepository.cs)
- [src/ConvenienceStore.Web/Modules/Products/Services/ICategoryRemovalRepository.cs](../../../src/ConvenienceStore.Web/Modules/Products/Services/ICategoryRemovalRepository.cs)
- [src/ConvenienceStore.Web/Modules/Products/Views/Categories/Index.cshtml](../../../src/ConvenienceStore.Web/Modules/Products/Views/Categories/Index.cshtml)
- [src/ConvenienceStore.Web/Modules/Products/Views/Categories/Removal.cshtml](../../../src/ConvenienceStore.Web/Modules/Products/Views/Categories/Removal.cshtml)

Executed cases and exact scenarios: [SP-09](sp-09-test-cases.md).

## B. Executed test evidence

| Final gate | PASS | FAIL | SKIP | Total |
|---|---:|---:|---:|---:|
| full | 644 | 38 | 0 | 682 |
| http | 157 | 0 | 0 | 157 |
| integration-green | 10 | 0 | 0 | 10 |
| inventory | 40 | 1 | 0 | 41 |
| mysql | 32 | 0 | 0 | 32 |
| pos | 73 | 2 | 0 | 75 |
| security | 202 | 2 | 0 | 204 |
| unit | 83 | 0 | 0 | 83 |

Raw TRX/logs: ignored artifacts/sp-module/final/. Machine counters, failure identities, source hashes and route inventory: [final evidence](evidence/final/summary.json). Issue RED/fix logs remain under artifacts/sp-module/sp-XX/. SP-09 core run after build failure was explicitly INVALID/stale binary; it is not verification. No valid assertion was removed or skipped.

## C. Regression

Comparison by fully qualified case name and normalized failure cause: 0 new FAIL, 0 removed, 0 previously failing now fixed, 0 changed baseline causes. [Detailed regression](sp-regression-summary.md). Reports allocation regression: Passed.

## D. Local Git commits

- `86fa91117f1233ddc31e8c8f9af07176cce05a89` — feat(sp-07): complete category listing with automated tests
- `1a9d32b9be469a97d29f350021a883738d6aff78` — feat(sp-08): implement category create and update with tests
- `438c2c342e03e2a9c7600c6beb37d256c8a1206d` — feat(sp-01): implement product listing and filtering
- `bac65e8a7bda0c47c077209b08d8f4f00cf473d4` — feat(sp-02): implement product details and price history views
- `5831f2aab04ca45c200166c1055c6bdfff3fcb29` — wip(sp-03): preserve standalone creation; NH-04 flow blocked
- `e910d262f8674310c1f4b1955f8fc0a87ce39239` — feat(sp-04): implement product editing with concurrency checks
- `628aed5c56017b16b693464406357fcdb9ce8d2e` — feat(sp-05): implement price scheduling and history verification
- `27e4b7c45cffcf70bbbdf62ed1020a1fc42ae79b` — feat(sp-06): implement guarded product lifecycle with tests
- `96e5af478d7209887c3349b5168b765e57b94c2d` — feat(sp-09): implement category removal and hiding with tests
- `06f70abceb7a9991537b98bcda6eb8219bf46edc` — fix(products): secure legacy landing and verify integration surface

## E. Remaining blockers

- SP-03 #58 A1: live NH-04 compact-create/add-to-draft consumer absent. Standalone flow verified; explicit WIP commit is not completion.
- SP-06 new-receipt consumer workflow NOT RUN; current active-filter provider and actual draft-table warning query verified.
- SP-07/SP-09 business approval pending: count scope, hidden-category roles/selection, restore and bulk move scope. Preserve current tested behavior; comments refreshed November16 without new approval.
- 38 independent baseline failures remain: Returns cash/schema and HTTP failures, schema expectations, KHO table count and POS service/auth contracts. Exact names/messages are in the regression report and machine evidence.
- Reviewer/CI/QA/staging and real browser rehearsal/E2E pending. Full Word SRS unavailable; exact issue-published use cases plus canonical schema/API/auth conventions are the available authority.
- No production-size performance benchmark/SLO evidence. Bounded/indexed queries and batch stock reads are verified, not a T7-07 performance acceptance.

## F. PR / integration readiness

Local source is reviewable with scope/limitations above. The complete SP branch is not accepted or ready to merge as a fully accepted module. It includes SP-03 WIP and pending business/consumer gates; global baseline remains FAIL. No claim that intermediate PR merges reached develop. No external review/CI/staging was run.

## G. November 17 demo preparation

HTTP/Razor exercised: /Categories list/create/edit/delete/hide; /Products list/search/filter/pagination/details/create/edit/price scheduling/cancellation/discontinue/reactivate/unused-delete. AD/NK/TN visibility, warnings, validation and conflicts are tested. A seeded isolated demo database, browser walkthrough and staging rehearsal still need execution. NH-04 live add-to-draft cannot be demonstrated on this branch.

## H. Incomplete / untested items

- SP-03 #58 A1: live NH-04 compact-create/add-to-draft consumer absent. Standalone flow verified; explicit WIP commit is not completion.
- SP-06 new-receipt consumer workflow NOT RUN; current active-filter provider and actual draft-table warning query verified.
- SP-07/SP-09 business approval pending: count scope, hidden-category roles/selection, restore and bulk move scope. Preserve current tested behavior; comments refreshed November16 without new approval.
- 38 independent baseline failures remain: Returns cash/schema and HTTP failures, schema expectations, KHO table count and POS service/auth contracts. Exact names/messages are in the regression report and machine evidence.
- Reviewer/CI/QA/staging and real browser rehearsal/E2E pending. Full Word SRS unavailable; exact issue-published use cases plus canonical schema/API/auth conventions are the available authority.
- No production-size performance benchmark/SLO evidence. Bounded/indexed queries and batch stock reads are verified, not a T7-07 performance acceptance.

## Latest develop compatibility verification

Latest fetched develop `a024757b73e696b5f4285f070c642db0c3a467cd` advanced through PR #216 while this isolated branch retained its frozen ancestry. Target changes affect six Returns-only files, with no overlap with SP source edits. A clean detached worktree at the exact target was restored and built before any patch application.

**BLOCKED: target does not compile.** The committed target object contains conflict markers in `src/ConvenienceStore.Web/Modules/Returns/Services/ReturnOrderService.cs` at lines 22, 28, 31, 307, 309, 390. The build rerun outside sandbox produced six CS8300 compiler errors and no directory-access error. This is independently present before applying SP changes. No Returns source was edited.

| Gate | Actual result |
|---|---|
| Target restore | PASS after network escalation; initial sandbox socket failure retained |
| Target baseline build | FAIL: six CS8300 errors |
| Target discovery/full suite | NOT RUN: build failed |
| SP source / target changed-file overlap | None |
| Exact SP patch git apply --check | PASS: textual applicability only |
| Patch application / merge / rebase | Not performed; detached target remains clean |
| Runtime verification of target + SP | BLOCKED / NOT RUN |
| Regression against latest target | UNKNOWN; frozen-baseline results must not be transferred to this target |

Patch SHA256 `dc350c698bb5c18a3aff211a5411f753323f878dcdb3f5b679c009a26c0bb774`. [Machine target evidence](evidence/target/summary.json); raw build/restore/check logs remain in ignored artifacts/sp-module/target-baseline/. Source commit and target object hashes establish provenance. No current-develop PASS, acceptance or integration claim is made.

Priority: the Returns owner must resolve committed markers and provide a buildable develop SHA; then rerun clean target baseline and patched-candidate gates sequentially. The local SP branch is preserved for scoped review. No push, PR, merge or issue closure occurred.
