# PR #168 — Bằng chứng tích hợp vào CuaHangDbContext

**Verdict: BLOCKED cho nghiệm thu toàn bộ #168.** Phần port có giới hạn được gửi dưới dạng Draft PR để review; không tự approve/merge/đóng PR nguồn.

## Commit và môi trường

| Mục | Giá trị |
|---|---|
| Code/test SHA đã chạy | `a85b4ae48997f272963a9b7785d3f7a9fb894b5b` |
| develop/base đối chiếu | `7e501094807b8bf20413900ff4f13e5d334a156a` |
| PR #168 HEAD kiểm kê | `2554bf1d58f8abaf0b8569f7355f5f30193e88c0` |
| Nhánh tích hợp | `integration/pr168-db-schema-consolidation` |
| Ngày | 14/11/2026, Asia/Saigon |
| Toolchain | Windows, .NET SDK 10.0.401, xUnit 2.9.3, MySql.EntityFrameworkCore 10.0.12 |
| MySQL | Docker MySQL 8.4.13 độc lập; `127.0.0.1:33308`, chỉ publish loopback |
| Image digest | `sha256:a81d043930e51745522bd5c430961cf8286f0a23f0a9a2a6e0108f7ea71b0902` |
| Server flags | `--default-time-zone=+07:00 --disable-log-bin` |
| Khởi tạo test | CsdlMySqlTam → database ngẫu nhiên → Migrate → test → drop đúng database test |
| Credential | Mật khẩu ngẫu nhiên lưu local trong file ignored; chỉ truyền bằng environment, không commit |

Commit bằng chứng sau SHA trên chỉ thêm tài liệu/JSON, không đổi code, test hoặc schema. Không gán kết quả cho SHA của PR #168 hay một server khác.

## Kết quả thực tế theo thứ tự nghiệm thu

| Bước | Lệnh / nhóm | PASS | FAIL | SKIP | Kết luận |
|---|---|---:|---:|---:|---|
| 1 | `dotnet restore ConvenienceStoreManagement.slnx` | — | — | — | PASS, exit 0 |
| 2 | `dotnet build ConvenienceStoreManagement.slnx --no-restore` | — | — | — | PASS, exit 0, 0 warning/error |
| 3 | Toàn bộ solution `dotnet test ... --no-restore --no-build` | 370 | 1 | 0 | FAIL, 371/371 executed |
| 4 | SchemaStructureTests | 15 | 0 | 0 | PASS trên MySQL 8.4.13 |
| 5 | SchemaMappingTests | 3 | 0 | 0 | PASS, database-free |
| 6 | Toàn bộ lớp dùng fixture MySQL | 167 | 0 | 0 | PASS |
| 7 | DanhMucConcurrency (15 MySQL + 2 metadata) | 17 | 0 | 0 | PASS |
| 8 | Smoke service/database AUTH/POS/KHO | 32 | 0 | 0 | PASS; không phải browser E2E |

Các nhóm overlap, **không cộng** thành tổng test duy nhất. Full suite có 167 MySQL test PASS và 204 database-free test (203 PASS, 1 FAIL). Unit regression liên quan AUTH/POS/KHO: QuyenVaToken 19, PosInvoiceDetails 21, TinhToanKho 7 → **47/47 PASS** trong full run. Metadata của #207 2/2 và mapping mới 3/3 cũng PASS.

Full suite FAIL ở `OperationalReportsServiceTests.ProductAndCategoryReports_AllocateInvoiceDeductionsProportionally`, dòng 78: Assert.Contains không tìm được dòng product/category có ô `176 ₫`. Đã tạo worktree nguyên develop tại `7e50109`, restore/build và chạy lại đúng test: **0 PASS, 1 FAIL, 0 SKIP**, cùng assertion. Diff Reports và test này giữa base/nhánh tích hợp rỗng. Đây là failure có sẵn được tái hiện, chưa phân tích/sửa nghiệp vụ Reports trong phạm vi CSDL. Không ghi toàn bộ unit suite PASS.

## Lệnh và artifact

Các lệnh dotnet chạy từ root worktree tích hợp. Test runner local `tmp/Invoke-Pr168Tests.ps1` chỉ cấp `ConnectionStrings__CuaHangTest` trỏ container nêu trên, gọi dotnet và giữ exit code. Runner và secret không đưa vào PR. Các lệnh thực tế bên dưới có thể chạy trực tiếp sau khi cấu hình environment trên server test độc lập:

```powershell
dotnet restore ConvenienceStoreManagement.slnx
dotnet build ConvenienceStoreManagement.slnx --no-restore
dotnet test ConvenienceStoreManagement.slnx --no-restore --no-build --logger 'trx;LogFileName=all.trx' --results-directory tmp/TestResults
dotnet test ConvenienceStoreManagement.slnx --no-restore --no-build --filter 'FullyQualifiedName~SchemaStructureTests' --logger 'trx;LogFileName=schema.trx' --results-directory tmp/TestResults
dotnet test ConvenienceStoreManagement.slnx --no-restore --no-build --filter 'FullyQualifiedName~SchemaMappingTests' --logger 'trx;LogFileName=mapping.trx' --results-directory tmp/TestResults
dotnet test ConvenienceStoreManagement.slnx --no-restore --no-build --filter 'FullyQualifiedName~SchemaStructureTests|FullyQualifiedName~DanhMucConcurrencyTests|FullyQualifiedName~Auth.DichVu|FullyQualifiedName~Auth.KhoiTao|FullyQualifiedName~Inventory.DichVuKhoTests|FullyQualifiedName~Inventory.HaTangTestKhoTests|FullyQualifiedName~Inventory.TichHopDichVuKhoTests|FullyQualifiedName~CustomerDatabaseTests|FullyQualifiedName~PosHeldOrderTests|FullyQualifiedName~PosInvoiceStoreTests|FullyQualifiedName~PosPromotionDatabaseTests|FullyQualifiedName~InvoiceLookupMySqlTests' --logger 'trx;LogFileName=mysql.trx' --results-directory tmp/TestResults
dotnet test ConvenienceStoreManagement.slnx --no-restore --no-build --filter 'FullyQualifiedName~DanhMucConcurrency' --logger 'trx;LogFileName=concurrency.trx' --results-directory tmp/TestResults
dotnet test ConvenienceStoreManagement.slnx --no-restore --no-build --filter 'FullyQualifiedName~Auth.DichVuXacThucTests|FullyQualifiedName~PosInvoiceStoreTests|FullyQualifiedName~Inventory.TichHopDichVuKhoTests' --logger 'trx;LogFileName=smoke.trx' --results-directory tmp/TestResults
```

Đối chiếu lỗi Reports, từ root worktree nguyên develop:

```powershell
dotnet restore ConvenienceStoreManagement.slnx
dotnet test ConvenienceStoreManagement.slnx --no-restore --filter 'FullyQualifiedName~OperationalReportsServiceTests.ProductAndCategoryReports_AllocateInvoiceDeductionsProportionally' --logger 'trx;LogFileName=baseline-reports.trx' --results-directory tmp/TestResults
```

[test-results.json](evidence/pr168/test-results.json) chứa SHA, lệnh, counters từ TRX, kết quả theo class, failure message và SHA-256 từng TRX. TRX gốc ở local `tmp/TestResults/`; baseline TRX ở worktree `tmp/pr168-baseline/tmp/TestResults/` của workspace chính. Log restore/build cũng giữ local trong `tmp/`. Không coi checksum là CI hoặc artifact đã upload; reviewer có thể chạy lại bằng lệnh trên.

Audit có ghi dữ liệu, chỉ trên database test ngẫu nhiên:

```powershell
dotnet run --project scripts/pr168-schema-audit/SchemaAudit.csproj
```

[column-audit.json](evidence/pr168/column-audit.json): server 8.4.13, 281/281 cột bảng được đọc; 0 type/precision/scale mismatch, 0 nullability mismatch; 48 khác biệt biểu diễn default được lưu nguyên để review; 5 computed columns ghi `ValueGenerated.Never`. Probe EF insert CtPhieuNhap thất bại **MySQL 3105** tại thanh_tien. Đây là bằng chứng blocker, **không phải PASS cho insert**.

SchemaStructureTests kiểm tra live 28 bảng nghiệp vụ + history, 3 view có thể query, 10 trigger đúng tên/table/event/timing, 53 CHECK được enforce, PK/FK/index/unique và 281 cột; migration apply lại không sinh history thứ hai. Mapping tests kiểm tra chỉ một DbContext, đủ table/view mapping, 41 câu CREATE của baseline và không pending model changes. Không chạy `dotnet ef` CLI ở lượt này; kiểm tra pending model change dùng API EF Core trong test, không giả nhận đã chạy CLI.

## Các lần lỗi trong quá trình chuẩn bị

- Restore trong sandbox thất bại NU1301 do socket api.nuget.org bị chặn; restore ngoài sandbox sau đó PASS. Không đổi package version để né lỗi.
- Kết nối test ban đầu với SslMode=Disabled không được lấy RSA key; chuyển sang TLS. TLS trong Windows sandbox thất bại `No credentials are available in the security package`; các kết quả nghiệm thu dùng TLS ngoài sandbox.
- Schema test sơ bộ có 2 failure do phép so test bỏ comment ở một bên SQL và đòi tên alternate key synthetic EF. Đã sửa phép chuẩn hóa comment và kiểm tra unique backing columns, giữ kiểm tra tên/index vật lý đầy đủ.
- Thử nghiệm kỳ vọng suppress mọi business flag với local values bằng nhau có 2 failure trên develop. Không dùng kỳ vọng đó vì sẽ bỏ tăng version với detached Update. Regression cuối cùng kiểm tra rõ write intent, detached/stale, no-op tracked và version-only; không tuyên bố đã loại mọi update vật lý có cùng giá trị.

## Điều kiện chưa đạt

1. **B1, BLOCKED:** 5 generated-column mapping còn thiếu; EF receipt-detail insert đã tái hiện lỗi 3105. Không tự sửa mapping module đang dùng.
2. **B2, BLOCKED:** 8 bảng có PhienBan nhưng chưa là EF concurrency token; chưa port cơ chế generic từ #168, cần owner từng module xác minh mọi write path và conflict contract.
3. **B3, FAIL:** full suite có 1 lỗi Reports tái hiện trên develop; cần owner Reports xử lý trong PR riêng hoặc quyết định acceptance rõ ràng.
4. **BLOCKED — NOT VERIFIED:** MySQL 9.7.2 ghi trong ADR, Aiven/production, browser E2E/HTTP smoke toàn bộ AUTH-POS-KHO, hành vi của từng view/trigger/CHECK và các flow NH-01/NH-07 sau port. Chưa sửa #197/#201.
5. CI và approval độc lập được kiểm tra/ghi ở PR; không suy ra CI PASS từ local tests. Nhánh base không có workflow trong `.github/workflows` ở thời điểm kiểm kê.

## Checklist reviewer

- [x] Một CuaHangDbContext runtime; không nhập ApplicationDbContext.
- [x] Schema ownership và lineage rõ; không thêm/sửa migration, view, trigger, CHECK.
- [x] Không thay dữ liệu production; toàn bộ DB ghi là database ngẫu nhiên trên container test riêng.
- [x] Bảo toàn token và original-version retry của #204/#207; sync/async/explicit/detached/no-op/conflict có test.
- [x] Build các module thành công; unit regression liên quan PASS; MySQL integration 167/167 PASS.
- [ ] Toàn bộ unit suite PASS (1 lỗi Reports có sẵn).
- [ ] Giải quyết hoặc thống nhất phạm vi B1/B2; xác nhận không còn nội dung phải chuyển từ #168.
- [ ] CI và review độc lập; không tự approve.
- [ ] Đối chiếu code/documentation và nghiệm thu #197/#201 sau khi port riêng.

Rollback: revert commit qua PR; không downgrade database vì không có migration mới. Chỉ đề xuất đóng #168 sau khi PR tích hợp merge và reviewer xác nhận toàn bộ phần cần giữ đã được chuyển; không đóng trong nhiệm vụ này.
