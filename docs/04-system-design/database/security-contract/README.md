# Security Contract — Issue #293

**Trạng thái: PROPOSED / CHỜ REVIEW.** Chỉ đề xuất thiết kế, chưa sửa backend, chưa áp migration hoặc thay đổi Supabase. Không đóng #293.

Baseline develop `cc20336566ff3ac12933e3c1c65c36a6553b2000`; audit source `0117d44a26489e6e607f126b46e23e37be35c509`. So sánh hai SHA chỉ có 6 file CI, Smoke, test và runbook thay đổi, không đổi source nghiệp vụ. B01: PR #299 đã merge, 4/4 workflow PASS; hai CI đều 877/877, 0 fail/skip; live smoke 31 bảng + 3 view, `roleVerified=true`, `writes=0`. B02: quét tĩnh 280 C#/SQL, 168 điểm bằng chứng (167 gốc + WR-10), 10 finding thiếu quyền và 3 finding bổ sung; chưa nghiệm thu ghi. Catalog AS-IS: 26/11/2026 17:48:50 UTC, cần refresh trước triển khai. Không coi RLS hiện có là phân quyền AD/TN/NK: chúng là vai trò ứng dụng, dùng chung login `smartmart_backend`.

## Phân công theo đề xuất của người phụ trách

| Người | Công việc | Đầu ra |
|---|---|---|
| Yến — @phanyen06ym-ien | B04–B08, WR-01–10, B02-10, B02-12: sửa backend, migration, ACL/RLS/trigger Supabase | PR theo module; migration và contract máy đọc được; phối hợp Huy xử lý lỗi test. Không phụ trách viết bộ test độc lập hoặc cập nhật tài liệu nghiệp vụ. |
| Huy — @tahuy280726-blip | B09–B11, B02-11: viết test tự động, fixture/CI, integration, rollback, concurrency, regression | Test code, kết quả CI/TRX, test cases và evidence kỹ thuật bàn giao Thăng. |
| Thăng — @zlatanofficial111-netizen | Tài liệu: SRS, use case, ADR, API, ERD/từ điển nếu có ảnh hưởng, runbook, tổng hợp test report từ Huy | PR tài liệu, traceability, ghi quyết định đã duyệt; review AUTH/lock protocol. |
| Hoài — @thuhoai08041986-creator | Review POS/DT/quỹ; xác nhận nghiệp vụ và transaction | Review độc lập phần module; không nhận triển khai thay Yến. |
| Trung — @mongtrucbay02-sys | Duyệt thay đổi yêu cầu SP/NH/KH, security contract; B12 nghiệm thu | Quyết định có căn cứ, review và merge độc lập. |

Đây là phân công đề xuất để nhóm xác nhận trong PR, không phải thay đổi assignee GitHub. Nếu UI cần đổi, chuyển người phụ trách giao diện; Yến chỉ sửa backend. Không tự merge PR của mình. PR này chỉ chứa tài liệu review, không thay thế PR code/test/docs sau đó.

## Danh sách sửa backend (Yến)

| ID | Mức | File (trong src/ConvenienceStore.Web) | Phương án đề xuất |
|---|---|---|---|
| WR-01 | P1 | Modules/Products/Services/EfProductLifecycleRepository.cs:32 | Giữ SP-06: chỉ dọn giá thuộc sản phẩm đủ điều kiện xóa; guard lịch sử chưa chốt. |
| WR-02 | P1 | Modules/Products/Services/EfProductLifecycleRepository.cs:33 | Chỉ dọn dòng tồn bằng 0 thuộc sản phẩm đủ điều kiện xóa; không xóa thẻ kho. |
| WR-03 | P1 | Modules/Products/Services/EfProductLifecycleRepository.cs:34 | Giữ xóa cứng có điều kiện theo PA-SP-01.B; nếu có giao dịch chỉ ngừng kinh doanh. |
| WR-04 | P1 | Modules/Purchasing/SuppliersController.cs:124–143 | Giữ xóa cứng khi chưa có phiếu nhập; chặn nếu có phiếu nháp; giữ lịch sử. |
| WR-05 | P1 | Modules/Customers/Services/CustomerService.cs:138–157 | KH-04: xóa nếu không có bất kỳ FK tham chiếu, chặn đơn mở; nếu có lịch sử thì ẩn/null PII. Kiểm số dòng ảnh hưởng và race FK. |
| WR-06 | P0 | Modules/Purchasing/PurchaseReceiptService.cs:140,157–191 | Chỉnh sửa dòng phiếu NHAP, lock cha trước dòng; guard I/U/D, không DELETE phiếu cha. |
| WR-07 | P0 | Modules/Auth/Services/KhoiTaoAdminDauTien.cs:148–165 | Advisory transaction lock, namespace cố định; mọi writer Admin cạnh tranh phải có protocol/invariant thống nhất; đếm sau lock, không log bí mật. |
| WR-08 | P0 | Services/Pos/PosInvoiceCancellationService.cs:87–92 | FOR UPDATE OF t; giữ kiểm tra AD và giao tác hoàn kho/điểm/quỹ. |
| WR-09 | P0 | Modules/Returns/Services/CashLedgerService.cs:46–51 | FOR UPDATE OF t; giữ khóa quỹ, so sánh số dư và chốt một lần. |
| WR-10 | P0 | Services/Pos/PosHeldOrderService.cs:107–118 | FOR UPDATE OF t; giữ kiểm tra TN, ownership và transaction POS-08. |
| B02-10 | P1 | Data/KetNoiCsdl.cs:20–39; Shared/Data/SqlDialect.cs:13–18 | Guard EF/raw/bootstrapping: TLS VerifyFull, đúng current/session role và role flags, row_security; fail closed trước SQL nghiệp vụ, kiểm trên connection/transaction thực dùng (pooler), tách admin profile, không áp read-only smoke cho ghi. |
| B02-11 | P1 | CI, CsdlPostgreSqlTam.cs:28–35 | Huy: fixture đăng nhập thật bằng backend, không admin fallback; Yến hỗ trợ contract. |
| B02-12 | P2 | ACL ba bảng (xem contract) | Chỉ còn I của chinh_sach_doi_tra là ứng viên REVOKE có điều kiện; giữ I/U quy_tac_tich_diem và I quy_tien_mat. Chưa REVOKE quyền nào. |

## Cổng phê duyệt (chưa duyệt)

| Quyết định | Cần xác nhận | Reviewer |
|---|---|---|
| D1 WR-01–04 | Giữ SRS SP-06/NH-03; chốt guard, lịch sử giá và FK/race trước khi cấp D. | Trung, Thăng (SRS) |
| D2 WR-05 | KH-04: mọi FK, ẩn PII, phản hồi API và race. | Trung |
| D3 WR-06 | NHAP guard, khóa cha, header không quay lại NHAP; chốt DB guard header nếu cần. | Trung, Thăng |
| D4 WR-07–10 | Advisory lock mọi writer Admin; khóa t, vai trò không đổi ngoài quy trình owner. | Thăng, Hoài |
| D5 B02-12 | DT-08 cắt; KH-07 chờ quyết định (#189 mở lại). Giữ I/U quy_tac_tich_diem và I quỹ; xác nhận seed/repair trước mọi thu hẹp. | Trung, Hoài, Thăng |
| D6 B02-10/11 | Guard mọi đường kết nối, test real-login, pooler. | Huy, Trung |
| D7 B08 | ACL/RLS/trigger, adoption, rollback, backup restore. | Trung, Thăng; Yến xác nhận kỹ thuật |

## Căn cứ

- [SRS tổng hợp](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/cc20336566ff3ac12933e3c1c65c36a6553b2000/docs/srs/SRS_ban-cuoi_tong-hop-thay-doi.md#L20-L54): S3, CP-01–03, R3, X7; [business rules](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/cc20336566ff3ac12933e3c1c65c36a6553b2000/docs/03-requirements/business-rules.md#L16-L21).
- [SP/NH design](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/cc20336566ff3ac12933e3c1c65c36a6553b2000/docs/04-system-design/database/product-purchase/design-notes.md#L38-L76): lịch sử giá, phiếu nháp; là đề xuất, chưa chứng minh bỏ xóa cứng được duyệt. [KH-04](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/cc20336566ff3ac12933e3c1c65c36a6553b2000/docs/development/customers-pos03-implementation.md#L11-L20): xóa/ẩn theo FK; [KH/KM ERD](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/cc20336566ff3ac12933e3c1c65c36a6553b2000/docs/04-system-design/database/customers-promotions/README.md#L68-L74).
- [ERD SP/NH](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/cc20336566ff3ac12933e3c1c65c36a6553b2000/docs/04-system-design/database/product-purchase/erd-product-purchase.mmd), [ERD POS/DT](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/cc20336566ff3ac12933e3c1c65c36a6553b2000/docs/04-system-design/database/invoice-returns/erd-invoice-returns.mmd), [ERD bổ sung](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/cc20336566ff3ac12933e3c1c65c36a6553b2000/docs/04-system-design/database/reconciliation-20261123/erd-additions.mmd): ERD khái niệm, tên/kiểu MySQL cũ không thay schema vật lý. [Quỹ](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/cc20336566ff3ac12933e3c1c65c36a6553b2000/docs/04-system-design/database/cash-promotion-20261123.md#L7-L19).
- [Baseline SQL](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/cc20336566ff3ac12933e3c1c65c36a6553b2000/src/ConvenienceStore.Web/Data/Migrations/Sql/PostgreSqlBaseline.sql#L1018-L1047): quyền CRUD rộng khác live; không sửa migration đã phát hành.
- [Word SRS dự thảo #301](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/428e101061407c993e3b4245038c81d46f76b577/docs/srs/SRS_ban-cuoi_du-thao_27-11-2026.docx): SP-06 trang 36, PA-SP-01.B trang 38; NH-03/NH-05, KH-04, POS-08; mục 3.2 ghi scope. Đã đọc bản Word cung cấp, không coi PR #301 OPEN là SRS cuối được duyệt.
- [Quy trình đóng góp](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/cc20336566ff3ac12933e3c1c65c36a6553b2000/CONTRIBUTING.md), [ma trận nghiệm thu](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/cc20336566ff3ac12933e3c1c65c36a6553b2000/docs/06-testing/test-reports/final-acceptance-matrix.md#L58-L59). Không tự merge; ghi nhận B01 tác giả tự merge là deviation.
- Evidence: [audit B02](evidence/B02-audit-report.md), [operation anchors](evidence/B02-operation-evidence.csv), [catalog](evidence/observed-contract.json), [live smoke](evidence/post-merge-smoke.json). [PR #299](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/pull/299), [CI push](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/45874022269), [CI PR](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/45874035651), [guard](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/45874036168), [smoke](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/45874022388).

## Thứ tự
B03 review → B04 SP/NH/KHO, B05 AUTH, B06 POS/DT/quỹ, B07 KH (Yến; ưu tiên WR-06–10 khi quyết định tương ứng được duyệt) → B08 migration/contract trên isolated → B09–B11 Huy kiểm thử độc lập, Thăng cập nhật tài liệu song song → rollout chỉ sau tất cả gate và phê duyệt → B12 Trung nghiệm thu. Không đóng issue khi chỉ mới pass smoke đọc. Audit tĩnh không phải compiler call graph hay runtime đầy đủ; Razor/JS/scripts chưa audit đầy đủ.

## Re-review 28/11/2026
- CI 877/877 không chứng minh quyền ghi Supabase: baseline grant CRUD rộng, fixture admin; cần B02-11 real-login với ACL live-like.
- Đề xuất trước 04/12: WR-06–10 và test real-login/negative/rollback/race tương ứng. WR-06 cần cả DELETE có guard và migration tối thiểu đã duyệt, không thể sửa code đơn thuần. Các mục khác chỉ hoãn khi Trung/Thăng xác nhận phạm vi, vô hiệu hóa hoặc ghi rõ luồng chưa đạt; không tuyên bố nghiệm thu #293.
- D1–D7 vẫn là thiết kế đề xuất; ý kiến đồng ý của reviewer không thay thế phê duyệt cuối của Trung. Review lại HEAD mới; không tự merge.
- [SRS tổng hợp #301 tại ff68536](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/ff68536fd6e8fcae4fed20975c3fcd2487b66101/docs/srs/SRS_ban-cuoi_tong-hop-thay-doi.md): KH-07 pending, DT-08 CUT; bản Word/PDF mới chưa được reviewer kiểm chứng toàn văn. D5 giữ I/U quy_tac_tich_diem và I quy_tien_mat.
- [Re-review của Thăng](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/pull/300#pullrequestreview-6094976022) đồng ý D1/D3/D4 về hướng nghiệp vụ; guard, lock protocol và test vẫn cần xác minh. [REC baseline](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/pull/300#issuecomment-7236995868) chốt kỹ thuật đề xuất lúc 16h, không phải approval. [Phản hồi SRS REC-08–10](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/pull/300#issuecomment-7238059808) sau mốc: BR-BC-02 làm rõ, CP-03/R3 phù hợp, KHO-04 E2 bổ sung yêu cầu cần #5 duyệt.
- WR-06 V2 đang thiết kế, xem security-contract.md; không coi V1 hoặc V2 là migration đã nghiệm thu. Phân công triển khai chi tiết cần phối hợp owner #260/#262/#266; không tự đổi assignee.
- Merge tài liệu đề xuất chỉ kết thúc vòng tổng hợp B03; không đóng #293, không cấp quyền thực tế, không phê duyệt release.
