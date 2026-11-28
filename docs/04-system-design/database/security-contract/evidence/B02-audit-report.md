# B02 — Audit quyền ghi SmartMart / Issue #293

- Develop SHA: `0117d44a26489e6e607f126b46e23e37be35c509`
- ZIP SHA256: `02f5a62be273d91483d9e50048cfc77412ce3a1f31f6fe95167f29a827f30b6c`
- CRC: PASS; 280 C#/SQL files; archive comment matches frozen SHA.
- Git comparison: FNV-1a64 over sorted path + Git blob SHA1 = `2e68fcf5f09883f9` on both remote and local after CRLF→LF except ReportsController.cs (raw CRLF matches remote). Aggregate fingerprint, not independent per-file cryptographic verification. .github/tests/tools tree hashes match after newline normalization. Full src subtree including non-C#/SQL assets not established.
- ACL source: `issue-293-security-contract-observed.json`, previous read-only catalog snapshot 2026-11-26T17:48:50.701658+00:00. No new live query or changes in this turn.

## Coverage
All 280 C#/SQL files scanned; 458 heuristic candidates (including comments/helpers/false positives); 167 curated operation anchors. This is static pattern-based coverage, not compiler call-graph or executed SQL trace. Razor/JS and deployment scripts not fully audited; no claim of absolute completeness.

## Findings

| ID | Table | File/line | Status |
|---|---|---|---|
| WR-07; conditional valid bootstrap config | vai_tro | [Modules/Auth/Services/KhoiTaoAdminDauTien.cs:152](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/0117d44a26489e6e607f126b46e23e37be35c509/src/ConvenienceStore.Web/Modules/Auth/Services/KhoiTaoAdminDauTien.cs#L152) | UPDATE missing |
| WR-01 | lich_su_gia | [Modules/Products/Services/EfProductLifecycleRepository.cs:32](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/0117d44a26489e6e607f126b46e23e37be35c509/src/ConvenienceStore.Web/Modules/Products/Services/EfProductLifecycleRepository.cs#L32) | DELETE missing |
| WR-02 | ton_kho | [Modules/Products/Services/EfProductLifecycleRepository.cs:33](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/0117d44a26489e6e607f126b46e23e37be35c509/src/ConvenienceStore.Web/Modules/Products/Services/EfProductLifecycleRepository.cs#L33) | DELETE missing |
| WR-03 | san_pham | [Modules/Products/Services/EfProductLifecycleRepository.cs:34](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/0117d44a26489e6e607f126b46e23e37be35c509/src/ConvenienceStore.Web/Modules/Products/Services/EfProductLifecycleRepository.cs#L34) | DELETE missing |
| WR-04 | nha_cung_cap | [Modules/Purchasing/SuppliersController.cs:134-141](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/0117d44a26489e6e607f126b46e23e37be35c509/src/ConvenienceStore.Web/Modules/Purchasing/SuppliersController.cs#L134) | DELETE missing |
| WR-06; only existing removed lines | ct_phieu_nhap | [Modules/Purchasing/PurchaseReceiptService.cs:140](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/0117d44a26489e6e607f126b46e23e37be35c509/src/ConvenienceStore.Web/Modules/Purchasing/PurchaseReceiptService.cs#L140) | DELETE missing |
| WR-05 | khach_hang | [Modules/Customers/Services/CustomerService.cs:151](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/0117d44a26489e6e607f126b46e23e37be35c509/src/ConvenienceStore.Web/Modules/Customers/Services/CustomerService.cs#L151) | DELETE missing |
| WR-08 | vai_tro | [Services/Pos/PosInvoiceCancellationService.cs:87-92](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/0117d44a26489e6e607f126b46e23e37be35c509/src/ConvenienceStore.Web/Services/Pos/PosInvoiceCancellationService.cs#L87) | UPDATE missing |
| WR-09 | vai_tro | [Modules/Returns/Services/CashLedgerService.cs:46-51](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/0117d44a26489e6e607f126b46e23e37be35c509/src/ConvenienceStore.Web/Modules/Returns/Services/CashLedgerService.cs#L46) | UPDATE missing |

## Additional findings
- B02-10 (P1, configuration boundary): Data/KetNoiCsdl.cs:20–25, 38–39 and Shared/Data/SqlDialect.cs:13–18 normalize search path/time zone, but do not enforce runtime role or VerifyFull. PR #299 guards Smoke, not application connections. No evidence production uses wrong role; configuration-risk finding, not an observed incident. Verify production config and design guard without breaking local/test bootstrap.
- B02-11 (P1, test gap): .github/workflows/ci.yml:65 supplies postgres; CsdlPostgreSqlTam.cs:28–35 constructs runtime options from this admin connection. PostgreSqlSchemaTests.cs:34–43 tests SET ROLE reads, not actual backend write parity. Need isolated real-login tests.
- B02-12 (P2, candidate excess): chinh_sach_doi_tra INSERT, quy_tac_tich_diem INSERT/UPDATE, quy_tien_mat INSERT not observed in scanned runtime write paths. Policy table UPDATE is required by FOR SHARE. khuyen_mai_san_pham UPDATE required by POS FOR SHARE. Do not revoke until scope and tests approved.

## Table matrix
| Table | Live ACL | Observed need | Missing | Candidate excess | Status |
|---|---|---|---|---|---|
| bo_dem_chung_tu | INSERT,SELECT,UPDATE | INSERT,SELECT,UPDATE |  |  | STATIC_PASS |
| chinh_sach_doi_tra | INSERT,SELECT,UPDATE | SELECT,UPDATE |  | INSERT | REVIEW_EXCESS |
| chuong_trinh_khuyen_mai | DELETE,INSERT,SELECT,UPDATE | DELETE,INSERT,SELECT,UPDATE |  |  | STATIC_PASS |
| ct_gio_hang | DELETE,INSERT,SELECT,UPDATE | DELETE,INSERT,SELECT,UPDATE |  |  | STATIC_PASS |
| ct_hoa_don | INSERT,SELECT | INSERT,SELECT |  |  | STATIC_PASS |
| ct_kiem_ke | INSERT,SELECT,UPDATE | INSERT,SELECT,UPDATE |  |  | STATIC_PASS |
| ct_phieu_doi_tra | INSERT,SELECT | INSERT,SELECT |  |  | STATIC_PASS |
| ct_phieu_nhap | INSERT,SELECT,UPDATE | DELETE,INSERT,SELECT,UPDATE | DELETE |  | FAIL |
| ct_xuat_huy | INSERT,SELECT,UPDATE | INSERT,SELECT,UPDATE |  |  | STATIC_PASS |
| danh_muc | DELETE,INSERT,SELECT,UPDATE | DELETE,INSERT,SELECT,UPDATE |  |  | STATIC_PASS |
| giao_dich_tien_mat | INSERT,SELECT | INSERT,SELECT |  |  | STATIC_PASS |
| gio_hang | DELETE,INSERT,SELECT,UPDATE | DELETE,INSERT,SELECT,UPDATE |  |  | STATIC_PASS |
| hoa_don | INSERT,SELECT,UPDATE | INSERT,SELECT,UPDATE |  |  | STATIC_PASS |
| hoan_tien_hoa_don | INSERT,SELECT | INSERT,SELECT |  |  | STATIC_PASS |
| khach_hang | INSERT,SELECT,UPDATE | DELETE,INSERT,SELECT,UPDATE | DELETE |  | FAIL |
| khuyen_mai_san_pham | DELETE,INSERT,SELECT,UPDATE | DELETE,INSERT,SELECT,UPDATE |  |  | STATIC_PASS |
| lich_su_diem | INSERT,SELECT | INSERT,SELECT |  |  | STATIC_PASS |
| lich_su_gia | INSERT,SELECT,UPDATE | DELETE,INSERT,SELECT,UPDATE | DELETE |  | FAIL |
| nha_cung_cap | INSERT,SELECT,UPDATE | DELETE,INSERT,SELECT,UPDATE | DELETE |  | FAIL |
| phien_dang_nhap | INSERT,SELECT,UPDATE | INSERT,SELECT,UPDATE |  |  | STATIC_PASS |
| phieu_doi_tra | INSERT,SELECT,UPDATE | INSERT,SELECT,UPDATE |  |  | STATIC_PASS |
| phieu_kiem_ke | INSERT,SELECT,UPDATE | INSERT,SELECT,UPDATE |  |  | STATIC_PASS |
| phieu_nhap | INSERT,SELECT,UPDATE | INSERT,SELECT,UPDATE |  |  | STATIC_PASS |
| phieu_xuat_huy | INSERT,SELECT,UPDATE | INSERT,SELECT,UPDATE |  |  | STATIC_PASS |
| quy_tac_tich_diem | INSERT,SELECT,UPDATE | SELECT |  | INSERT,UPDATE | REVIEW_EXCESS |
| quy_tien_mat | INSERT,SELECT,UPDATE | SELECT,UPDATE |  | INSERT | REVIEW_EXCESS |
| san_pham | INSERT,SELECT,UPDATE | DELETE,INSERT,SELECT,UPDATE | DELETE |  | FAIL |
| tai_khoan | INSERT,SELECT,UPDATE | INSERT,SELECT,UPDATE |  |  | STATIC_PASS |
| the_kho | INSERT,SELECT | INSERT,SELECT |  |  | STATIC_PASS |
| ton_kho | INSERT,SELECT,UPDATE | DELETE,INSERT,SELECT,UPDATE | DELETE |  | FAIL |
| vai_tro | SELECT | SELECT,UPDATE | UPDATE |  | FAIL |

## Conclusion
Original scan identified 9 static mismatch findings; re-review adds WR-10 on the same table vai_tro (10 total). Additional configuration/test risks and candidate excess privileges above. B01 technical CI/live smoke pass previously verified; independent review pending. B02 source scan complete for C#/SQL, semantic completeness and runtime verification pending. No merge, no Supabase changes, no Git commits.

## Re-review bổ sung 28/11/2026
WR-10 — P0, `Services/Pos/PosHeldOrderService.cs:107–118`, JOIN tai_khoan t/vai_tro v FOR UPDATE thiếu OF t; vai_tro chỉ SELECT theo snapshot gốc. [Source develop](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/cc20336566ff3ac12933e3c1c65c36a6553b2000/src/ConvenienceStore.Web/Services/Pos/PosHeldOrderService.cs#L107-L118). Static mismatch, runtime not run. 168 anchors including appended WR-10; original 167 preserved. No new table mismatch.
