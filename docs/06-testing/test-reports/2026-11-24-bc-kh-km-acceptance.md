# BC/KH/KM — sửa lỗi và chuẩn bị nghiệm thu #266 / #288

> Cập nhật 25/11: kết quả này là lịch sử của bản ngày 24/11. Review #289 phát hiện cách chia tổng KM theo tỷ trọng vẫn sai snapshot từng dòng và đơn giá vốn bị làm tròn sớm. Bản sửa và các gate còn lại được ghi riêng trong [F289-01…06](2026-11-25-pr289-review.md); không dùng 819/819 cũ làm bằng chứng cho bản sửa mới.

Ngày 24/11/2026. Trạng thái: **819/819 PASS cục bộ / chờ review**, không phải biên bản nghiệm thu cuối kỳ. Build Release 0 lỗi/cảnh báo; publish PostgreSQL-only đạt. 15 ca regression mới; không cộng các lượt filtered vào tổng full suite.

## Phạm vi và phiên bản

- Base `develop`: `770550c9fe76a811ec5db871d57482582852917c`.
- Nhánh sửa: `fix/bc-kh-km-acceptance-20261124`.
- PR #288 được đọc tại `6b8213ad56d3068ee6d91b364898654ed7b7ef81`: chỉ thêm `final-acceptance-matrix.md`, không sửa runtime. Không tự sửa nhánh PR của Yến, merge PR hoặc đánh dấu reviewer đã đồng ý.
- Chỉ sửa các lỗi BC đã tái hiện; chạy hồi quy cả KH/KM đang có. Không thêm tính năng tuần 9; không đổi schema/migration hoặc quyền trên CSDL dùng chung.
- PostgreSQL 17 native riêng, loopback `127.0.0.1:55448`. Fixture chỉ tạo/xóa database ngẫu nhiên do chính fixture sở hữu; không nối Supabase/Aiven để chạy test ghi.

## Lỗi tái hiện và sửa

| Mã đợt sửa | Trước sửa | Sau sửa / test bảo vệ |
|---|---|---|
| BC-RETURN-VALUE | Phiếu đổi có `tien_hoan=0` làm BC-02–05/08 không trừ giá trị hàng trả, trong khi BC-09–11 vẫn trừ. Test PostgreSQL cho thấy mong 135.000 nhưng thực tế 0 | Giảm trừ đọc giá trị dòng trả `ct_phieu_doi_tra.thanh_tien` theo ngày hoàn tất. Tiền mặt hoàn vẫn riêng trong BC-09/quỹ. Test có hóa đơn thay thế trả bằng tín dụng hàng đổi, đối chiếu BC-02/03/04/05/08/09 |
| BC-ID64 | ID danh mục 5.000.000.101 gây `OverflowException` khi đọc bằng Int32 | Các ID danh mục/nhà cung cấp trong adapter BC-02–08 dùng Int64; test đọc danh mục lớn từ PostgreSQL |
| BC-CATEGORY-FILTER | BC-04 lọc danh mục chỉ áp vào hàng trả, không áp vào bán; mong 174, thực tế 220 | Cùng điều kiện danh mục/sản phẩm cho cả bán và trả; metric, bảng, chart dùng cùng tập đã lọc |
| BC-EMPTY-FILTER | Có giao dịch ở danh mục khác khiến bộ lọc không khớp vẫn không báo E3 | Xác định trạng thái rỗng theo kết quả sau lọc |
| BC-DETAIL | Chi tiết BC-02/03/04/05 bỏ phiếu trả, BC-03/04 cộng cả hóa đơn dù đang xem một sản phẩm/danh mục; 220 bị hiển thị 230 | Chi tiết có dòng bán và trả, dùng thời điểm riêng của từng chứng từ; tiền bán/giảm phân bổ theo dòng được chọn. Test cả hàng trả của hóa đơn thuộc kỳ trước |
| BC-DISCOUNT-REMAINDER | Sau lọc sản phẩm, BC-08 tính lại tỷ lệ và bỏ phần dư: BC-03 thuần 45 nhưng BC-08 thuần 46 | Dùng cùng hàm phân bổ toàn hóa đơn trước khi lọc, gồm phần dư ở dòng cuối |
| BC-NAVIGATION | Link chi tiết/quay lại bỏ danh mục, sản phẩm, NCC, trạng thái tồn hoặc nhóm/Top N/sắp xếp | Giữ bộ lọc ở cả hai chiều; test HTTP Razor thật kiểm tra link detail và back |

Giảm trừ giá trị hàng đổi và phân bổ phần dư được làm rõ trong [đề xuất nghiệp vụ](../../03-requirements/week8-bc-km-clarifications.md); reviewer phải xác nhận trước khi coi là SRS đã được chốt. Không thay giá trị tiền mặt trong hóa đơn hoặc quỹ để làm báo cáo khớp.

## Các lượt kiểm thử

| Lượt | Kết quả | Giới hạn |
|---|---|---|
| Trước sửa, unit mới | 7 ca FAIL tái hiện chi tiết/lọc/E3 | `artifacts/acceptance-288/results/before.trx`; hai ca DB trong lượt này không chạy được do server không còn sống, không dùng làm bằng chứng lỗi nghiệp vụ |
| Trước sửa, PostgreSQL | 2/2 FAIL đúng lỗi giá trị hàng đổi và OverflowException | `before-postgresql.trx`; đã sửa fixture có người tạo sản phẩm và giữ server trong cùng tiến trình test |
| Sau sửa đợt đầu, BC/KH/KM | 134/134 PASS, không bỏ qua | `targeted.trx`; trước bổ sung test điều hướng, phần dư và trả kỳ trước; chưa dùng kết quả này chứng nhận bản cuối |
| Trước sửa phần dư | 1/1 FAIL: mong 45, thực tế 46 | `before-rounding.trx` |
| Full suite trước restore cuối | 819/819 PASS | `full.trx`; còn assets cũ, không dùng làm kết quả phát hành cuối |
| Restore + rebuild solution Release | PASS, 0 warning, 0 error | SDK 10.0.401, EF Core 10.0.12, Npgsql/provider 10.0.3; [build log](../evidence/2026-11-24/bc-kh-km/build-final.txt) |
| Publish sạch + runtime guard | PASS | `publish-final`: không có dependency/DLL MySQL/Pomelo hoặc cấu hình phát triển; `verify-postgresql-runtime.ps1` đạt |
| Full suite cuối, sau restore/rebuild/publish | **819/819 PASS, failed=0, skipped=0**, 5 phút 25 giây | `full-final.trx`; đã chạy `verify-trx.ps1`; nguồn 7 file src/tests giữ nguyên hash từ trước đến sau lượt chạy |

Khi đổi từ checkout cũ, lượt `--no-restore` ban đầu vẫn dùng assets có dependency MySQL; publish guard phát hiện và từ chối. Đã restore solution theo csproj mới. Chỉ dùng kết quả sau restore/publish PostgreSQL sạch làm kết luận cuối; không sửa guard hoặc bỏ assertion để đạt.

Các TRX/log đầy đủ nằm trong `artifacts/acceptance-288` (ignored, cục bộ). [Verification JSON](../evidence/2026-11-24/bc-kh-km/verification.json) ghi base SHA, hash source, hash TRX và gói runtime. Đây là working tree chưa commit, không gán kết quả cho base SHA chưa chứa bản sửa. Không cộng kết quả filtered vào tổng full suite.

Chạy lại trên PostgreSQL 17 riêng có quyền tạo/xóa database fixture, không dùng Supabase chung:

```powershell
$env:ConnectionStrings__CuaHangPostgreSqlTest = 'Host=127.0.0.1;Port=55448;Database=postgres;Username=postgres;Pooling=false'
dotnet restore ConvenienceStoreManagement.slnx
dotnet build ConvenienceStoreManagement.slnx -c Release --no-restore
dotnet test tests/ConvenienceStore.Web.Tests/ConvenienceStore.Web.Tests.csproj -c Release --no-build --no-restore --results-directory artifacts/acceptance-288/results --logger 'trx;LogFileName=rerun.trx'
./scripts/ci/verify-trx.ps1 -Path artifacts/acceptance-288/results/rerun.trx
```

Lệnh trên cần máy chủ test riêng đang chạy; cluster native của đợt này đã dừng sau khi kiểm thử. Connection không mật khẩu chỉ áp dụng cluster test loopback trust riêng đã tạo trong artifacts, không phải cấu hình triển khai.

## Review PR #288

Ma trận 28 issue có đủ 14 Backend, 6 Frontend, 2 Test và 6 DOC; các issue #246–250/#266 của Trung được tách khỏi phần DOC #251. Việc giữ PENDING khi chưa có evidence cuối là đúng.

GitHub tại lần kiểm tra 24/11: PR #288 đang mở, base `develop`, không có xung đột; HEAD vẫn `6b8213ad56d3068ee6d91b364898654ed7b7ef81`. Check [Build and test](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/45405734328/job/138485850543) đã đạt. Có một review APPROVED của `mongtrucbay02-sys` trên đúng HEAD này; chưa thấy review thứ hai trong danh sách reviews lúc kiểm tra. CI này thuộc tài liệu #288, không phải CI của code sửa #266 đang ở máy.

Các điểm cần chỉnh trước khi dùng ma trận nghiệm thu:

1. Quy tắc nói hai reviewer, nhưng #260/#176/#146/#236 ghi Yến vừa làm vừa tự kiểm. Tự kiểm không phải review độc lập. Đề xuất các dòng này dùng Thăng + Trung, chờ nhóm xác nhận; không tự gán reviewer trên GitHub.
2. Đổi cột `Review Yến` / `Review peer` thành `Reviewer 1` / `Reviewer 2` kèm tên thực tế, để không bắt tác giả tự approve. Gate PR REVIEW phải yêu cầu hai người khác tác giả nếu đó là quy ước được nhóm chốt.
3. Ghi rõ PENDING là **chưa nghiệm thu cuối**, không phải khẳng định chưa có code/test lịch sử. Có thể liên kết PR/CI nền đã xác minh nhưng không chuyển cả dòng sang PASS nếu thiếu review/UAT/SRS.
4. Báo cáo MySQL 23/11 là lịch sử; báo cáo PostgreSQL 24/11 có snapshot/SHA và giới hạn riêng. Không lấy số test hoặc ảnh của môi trường cũ làm evidence cho bản nộp mới. Đã cập nhật ERD KH/KM, sequence KH-06/BC-08 và báo cáo module sang nguồn PostgreSQL hiện tại.

## Gate còn lại

- Hai review độc lập, CI exact commit và hồi quy của Huy: PENDING; không tự đánh dấu đã xong.
- Supabase smoke với VerifyFull/CA, runtime role `smartmart_backend`, backup/adoption baseline: thuộc gate #283, không được thay bằng kết quả PostgreSQL loopback của báo cáo này.
- UAT/staging và bản DOCX tổng hợp #257: chưa xác nhận trong đợt sửa này. #251 mới có nguồn Markdown/sơ đồ/ảnh và báo cáo, không gọi đó là DOCX final.
- Mobile/responsive: chưa nghiệm thu; không tự cắt yêu cầu của nhóm.
- Không đóng #266 hoặc #246–250 chỉ vì các ca cục bộ đạt; ma trận #288 vẫn cần merge đúng quy trình.
