# SP regression summary

Tested source worktree at `06f70abceb7a9991537b98bcda6eb8219bf46edc`; manifest in evidence/final/source-manifest.json. Target baseline develop `babfe1edae507e5d94a95c422a76ffa344a23c00`, initial executable SP-07 tree `86fa911`. Current fetched develop `a024757b73e696b5f4285f070c642db0c3a467cd`. Isolation B only; original dirty worktrees/NH work preserved. Local commits only; no push/PR/merge/issue closure.

Fresh baseline: 455 total, 417 PASS, 38 FAIL, 0 SKIP. Final: 682 total, 644 PASS, 38 FAIL, 0 SKIP. New failures: 0; removed baseline cases: 0; baseline fixes: 0; changed failure causes: 0. Overall solution FAIL. Filtered runs overlap and are never summed.

Raw full run: `artifacts/sp-module/final/full.trx`, SHA256 `4aab66754719b89fcfb3229c6f886a8babe149b74a0eb4c42f7e71989841225d`. Unique full-run case count drives totals. Namespace/naming module classification below assigns each case exactly once.

| Module | PASS | FAIL | SKIP | Total |
|---|---:|---:|---:|---:|
| Auth | 93 | 0 | 0 | 93 |
| Customers | 19 | 0 | 0 | 19 |
| Database | 30 | 11 | 0 | 41 |
| Inventory / KHO | 40 | 1 | 0 | 41 |
| POS | 50 | 2 | 0 | 52 |
| Products / SP | 272 | 0 | 0 | 272 |
| Promotions | 66 | 0 | 0 | 66 |
| Reports | 15 | 0 | 0 | 15 |
| Returns | 59 | 24 | 0 | 83 |
| Purchasing / NH consumer | 0 | 0 | 0 | NOT RUN — module absent |

## Baseline failures reproduced

| Test case | Current outcome / cause |
|---|---|
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.CancelledInvoiceIsRejected` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.PointsFailureAfterRestockRollsBackSlipAndStockTogether` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Integration.Database.Schema.SchemaStructureTests.DB08_NoPendingMigrations_AndReapplyingIsIdempotent` | FAIL — Assert.Single() Failure: The collection contained 2 items Collection: ["20261026120030_InitialCreate", "20261112160000_Pos12CancelInvoice"] |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.PendingReservesQuantity_RejectedReleasesIt_WithoutRestocking` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.StaleConfirmationAndDoubleSubmitCannotCreateDuplicateReturn` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.CompletesRefund_RestocksAtOriginalCost_AndPrintsImmutableSnapshot` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.ExactlyThresholdAndLastCalendarDay_CompleteWithoutApproval` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.PosHeldOrderTests.FakeCashierClaimCannotUseAdminDatabaseAccount` | FAIL — Assert.Throws() Failure: No exception was thrown Expected: typeof(System.UnauthorizedAccessException) |
| `ConvenienceStore.Web.Tests.Integration.Database.Schema.SchemaStructureTests.DB12_UniqueKeysAndIndexesMatchRuntimeModel` | FAIL — Assert.Equal() Failure: Collections differ ↓ (pos 27) Expected: OrderedIterator<string, string> [···, "hoa_don/uq_hoa_don_gio/0/gio_hang_id", "hoa_don/uq_hoa_don_ma/0/ma_hoa_don", "khach_hang/fk_kh_nguoi_tao/1/nguoi_tao_id", "khach_hang/idx_khach_hang_ten/1/ho_ten", "khach_hang/uq_khach_hang_sdt/0/so_dien_thoai", ···] Actual: string[] [···, "hoa_don/uq_hoa_don_gio/0/gio_hang_id", "hoa_don/uq_hoa_don_ma/0/ma_hoa_don", "hoan_tien_hoa_don/fk_hoan_tien_actor/1/nguoi_thuc_"···, "hoan_tien_hoa_don/idx |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.InvalidLinesAreRejectedWithoutWrites(invalid: "other-note")` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.CompletedDetailsCannotBeEditedOrDeleted` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.InsufficientCashFund_E1_RollsBackReturnAndStock` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Integration.Database.Schema.SchemaStructureTests.DB07_AppliedMigrationsMatchRuntimeLineage` | FAIL — Assert.Equal() Failure: Collections differ Expected: ["20261026120030_InitialCreate"] Actual: ["20261026120030_InitialCreate", "20261112160000_Pos12CancelInvoice"] ↑ (pos 1) |
| `ConvenienceStore.Web.Tests.Integration.Database.Schema.SchemaStructureTests.DB02_TableIdentitiesMatchReferenceAndRuntimeModel` | FAIL — Assert.Equal() Failure: Collections differ ↓ (pos 12) Expected: [···, "gio_hang", "hoa_don", "khach_hang", "khuyen_mai_san_pham", "lich_su_diem", ···] Actual: [···, "gio_hang", "hoa_don", "hoan_tien_hoa_don", "khach_hang", "khuyen_mai_san_pham", ···] ↑ (pos 12) |
| `ConvenienceStore.Web.Tests.Integration.Database.Schema.SchemaStructureTests.DB13_ColumnTypesPrecisionAndNullabilityMatchRuntimeModel` | FAIL — Assert.Equal() Failure: Collections differ ↓ (pos 114) Expected: OrderedIterator<string, string> [···, "hoa_don/tong_tien_hang/bigint unsigned/NO", "hoa_don/trang_thai/enum('da_thanh_toan','da_huy')/"···, "khach_hang/diem_hien_co/int unsigned/NO", "khach_hang/email/varchar(100)/YES", "khach_hang/gioi_tinh/enum('nam','nu','khac')/YES", ···] Actual: string[] [···, "hoa_don/tong_tien_hang/bigint unsigned/NO", "hoa_don/trang_thai/enum('da_thanh_toan','da_huy')/"···, "hoan_tien_hoa_don/diem_chua_thu_ |
| `ConvenienceStore.Web.Tests.Inventory.HaTangTestKhoTests.Migration_TaoDu28Bang_3View_10Trigger` | FAIL — Assert.Equal() Failure: Values differ Expected: 28 Actual: 29 |
| `ConvenienceStore.Web.Tests.HaTang.SchemaMappingTests.ExistingMigrationOwnsAllReferenceDdl_IncludingChecksViewsAndTriggers` | FAIL — Assert.Equal() Failure: Collections differ Expected: string[] ["20261026120030_InitialCreate"] Actual: KeyList<string, TypeInfo> ["20261026120030_InitialCreate", "20261112160000_Pos12CancelInvoice"] ↑ (pos 1) |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.DestroyedReturn_DoesNotIncreaseStock_AndCannotOverRefund` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.InvalidLinesAreRejectedWithoutWrites(invalid: "handling")` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.InvalidLinesAreRejectedWithoutWrites(invalid: "reason")` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Integration.Database.Schema.SchemaStructureTests.DB09_AllPrimaryKeysMatchRuntimeModel` | FAIL — Assert.Equal() Failure: Collections differ ↓ (pos 12) Expected: OrderedIterator<string, string> [···, "gio_hang/id", "hoa_don/id", "khach_hang/id", "khuyen_mai_san_pham/chuong_trinh_id,san_pham_id", "lich_su_diem/id", ···] Actual: string[] [···, "gio_hang/id", "hoa_don/id", "hoan_tien_hoa_don/id", "khach_hang/id", "khuyen_mai_san_pham/chuong_trinh_id,san_pham_id", ···] ↑ (pos 12) |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.InvalidLinesAreRejectedWithoutWrites(invalid: "condition")` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.MissingStockRollsBackWholeReturn` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.ExceptionsAndOverThreshold_CreatePendingWithoutEffects(age: 1, forbidden: False, price: 500100)` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Integration.Database.Schema.SchemaStructureTests.DB10_AllMappedColumnNamesMatchDatabase` | FAIL — Assert.Equal() Failure: Collections differ ↓ (pos 114) Expected: OrderedIterator<string, string> [···, "hoa_don/tong_tien_hang", "hoa_don/trang_thai", "khach_hang/diem_hien_co", "khach_hang/email", "khach_hang/gioi_tinh", ···] Actual: string[] [···, "hoa_don/tong_tien_hang", "hoa_don/trang_thai", "hoan_tien_hoa_don/diem_chua_thu_hoi", "hoan_tien_hoa_don/diem_da_thu_hoi", "hoan_tien_hoa_don/hoa_don_id", ···] ↑ (pos 114) |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.CompletedReturn_RecordsOneCashOutMovementAndReducesFund` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.MissingKh06BlocksMemberCompletionWithoutPartialRefundOrStock` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.ExceptionsAndOverThreshold_CreatePendingWithoutEffects(age: 8, forbidden: False, price: 25000)` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.RefundUsesSavedDiscountEvenWhenPromotionExpired` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.InvalidLinesAreRejectedWithoutWrites(invalid: "quantity")` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Integration.Database.Schema.SchemaStructureTests.DB05_AllTenTriggerNamesTargetsTimingsAndEventsMatchReference` | FAIL — Assert.Equal() Failure: Values differ Expected: 10 Actual: 12 |
| `ConvenienceStore.Web.Tests.PosHeldOrderTests.RequiresCashierAndPasswordChanged(role: "AD", changePassword: False)` | FAIL — Assert.Throws() Failure: No exception was thrown Expected: typeof(System.UnauthorizedAccessException) |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.InvalidLinesAreRejectedWithoutWrites(invalid: "duplicate")` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.ConcurrentReturnsCannotExceedPurchasedQuantity` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Integration.Database.Schema.SchemaStructureTests.DB01_Has28BusinessTablesPlusEfHistory` | FAIL — Assert.Equal() Failure: Values differ Expected: 29 Actual: 30 |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderMySqlTests.ExceptionsAndOverThreshold_CreatePendingWithoutEffects(age: 1, forbidden: True, price: 25000)` | FAIL — MySql.Data.MySqlClient.MySqlException : Table 'TEST_DB.quy_tien_mat' doesn't exist |
| `ConvenienceStore.Web.Tests.Integration.Database.Schema.SchemaStructureTests.DB11_ForeignKeysAndDeleteRulesMatchRuntimeModel` | FAIL — Assert.Equal() Failure: Collections differ ↓ (pos 23) Expected: [···, "hoa_don/fk_hd_nguoi_huy/nguoi_huy_id/tai_khoan/id/"···, "hoa_don/fk_hd_thu_ngan/thu_ngan_id/tai_khoan/id/NO"···, "khach_hang/fk_kh_nguoi_tao/nguoi_tao_id/tai_khoan/"···, "khuyen_mai_san_pham/fk_kmsp_ct/chuong_trinh_id/chu"···, "khuyen_mai_san_pham/fk_kmsp_sp/san_pham_id/san_pha"···, ···] Actual: [···, "hoa_don/fk_hd_nguoi_huy/nguoi_huy_id/tai_khoan/id/"···, "hoa_don/fk_hd_thu_ngan/thu_ngan_id/tai_khoan/id/NO"···, "hoan_tien_h |
| `ConvenienceStore.Web.Tests.Returns.ReturnOrderHttpTests.ApiPreviewCreateAndReceipt_UsesServerAmountsAndNoStore` | FAIL — Assert.Equal() Failure: Values differ Expected: Created Actual: InternalServerError |

## Reports baseline claim verification

- `ConvenienceStore.Web.Tests.OperationalReportsServiceTests.ProductAndCategoryReports_AllocateInvoiceDeductionsProportionally`: Passed in final full TRX.

No environment-aborted run is treated as PASS. Cold startup/build delays completed. The SP-09 stale-binary run after a harness build failure was invalidated and preserved; fresh build/discovery/focused/full gates replaced it. Existing failed tests were neither removed, skipped nor weakened.

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
