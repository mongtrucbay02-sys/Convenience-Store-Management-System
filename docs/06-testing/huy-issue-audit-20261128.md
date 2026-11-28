# Audit 28 Issue được giao cho Huy — 28/11/2026

Đã đọc nội dung và comment của 28 Issue giao cho tài khoản tahuy280726-blip. Giữ nguyên lịch sử, assignee, điểm công, hạn, trạng thái và checklist gốc. Không coi hợp nhất backlog hoặc CI xanh là nghiệm thu.

**Bất nhất cần owner xác minh:** 28/28 Issue truy xuất đang hiển thị OPEN, nhiều issue có state_reason=reopened, trong khi comment 28/11 của Yến ghi đã đóng NOT PLANNED để hợp nhất. Không tự đóng hoặc mở lại hàng loạt; Trung cần đọc timeline và chốt trạng thái.

| Issue nguồn | Theo dõi tại | Kết luận rà soát / phần còn thiếu |
|---|---|---|
| [#13](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/13) | #254 | Bàn giao thiết kế giao diện; cần Trang xác nhận file và quyết định, chưa có evidence |
| [#14](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/14) | #242 | CI build PASS; required checks main chưa được xác nhận |
| [#15](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/15) | #264 | Một test plan trên PR304; reviewer approval còn thiếu |
| [#16](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/16) | #264 | 27 Must theo PR301, không thay thế per-use-case evidence |
| [#17](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/17) | #293 | Migration/adoption/rollback cần owner duyệt; không chạy tự động trên shared Supabase |
| [#39](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/39) | #257 | Không tạo GitHub Projects hồi tố; tracking qua Issues/PR |
| [#40](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/40) | #264 | CI/fixture cũ có evidence; seed demo cuối theo #103 chưa nghiệm thu |
| [#41](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/41) | #264 | Mẫu bug, severity cao/trung bình/thấp, SLA cao 2 ngày; cần đối chiếu template |
| [#42](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/42) | #264 | AUTH/SP/NH nhà cung cấp/POS: trace đúng official UC, role và CI |
| [#43](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/43) | #264 | API AUTH/SP cần gắn test ID và kết quả CI tại SHA |
| [#44](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/44) | #242 | Secret guard không thay scan lịch sử Git, runtime TLS, bootstrap Admin |
| [#76](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/76) | #257 | ADR-001 là lịch sử; runtime .NET 10 MVC/Razor + Npgsql/PostgreSQL 17 theo ADR-002/003 |
| [#101](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/101) | #264 | Backup sprint 2 không xác minh; full PG17 restore chưa xong |
| [#102](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/102) | #264 | NH-KHO-POS giá vốn/thẻ kho/concurrency và defect evidence |
| [#103](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/103) | #103 | 3 checkbox lịch sử không thay PG17 demo seed/BC-02,03,06 verification |
| [#104](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/104) | #264 | NH/POS API và test tồn; reviewer chỉ ra thiếu NH-04/POS-02 |
| [#115](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/115) | #257 | Demo sprint 2 lịch sử, không tạo tag/biên bản hồi tố; seed theo #103 |
| [#151](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/151) | #264 | Regression chuyển #264; staging hiện tại #242, không nhận PASS giữa kỳ hồi tố |
| [#164](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/164) | #257 | Báo cáo giữa kỳ là mốc cũ; không tái tạo sprint và tag |
| [#181](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/181) | #264 | mysqldump cũ không hợp runtime PG17; pg_dump/pg_restore isolated |
| [#182](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/182) | #264 | KH/KM/DT/POS; KH-06 disabled, KH-08 locked, KH-07 cut theo #257 |
| [#183](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/183) | #264 | NFR-HN-01…05 chính thức; 1000 sản phẩm/10000 hóa đơn, samples NOT RUN |
| [#242](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/242) | #242 | HTTPS/deploy, README, required checks, secrets, migration vẫn NOT VERIFIED |
| [#243](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/243) | #264 | DT/report hợp nhất, browser giữ #244; not planned khác PASS |
| [#244](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/244) | #244 | Browser automation chưa chứng minh, HTTP test không thay Playwright |
| [#245](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/245) | #245 | Chương DOC mới draft, chờ Thăng/Yến ghép theo #257, chưa final |
| [#264](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/264) | #264 | Source of truth test plan/report, exact-role, 27 Must, browser, NFR, full backup, review |
| [#265](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/265) | #265 | Slide outline draft không thay PPTX final; #265 cần scope và bàn giao |

## Nhóm ưu tiên

1. P0 Test và security: #264 báo cáo/test plan cuối; #244 browser E2E; #293 + PR300 thiết kế ACL TO-BE/WR-06–10. Test bằng login thật smartmart_backend trên PG17 cô lập, không dùng postgres cho nghiệp vụ và không ghi Supabase chung.
2. P0 Coverage: 27 official Must theo bảng Thăng tại PR301 còn OPEN; bốn nhóm có gap NH-02, POS-02, KHO-01, NH-04. Test bổ sung không ghi PASS trước khi chạy.
3. P0 Data/recovery: #103 seed/đối soát sản phẩm, phiếu nhập, hóa đơn, tồn và BC-02/03/06; #264 restore đầy đủ schema thực, không lấy synthetic stock drill làm bằng chứng đủ.
4. P1 NFR: HN-01 tối đa 1s, HN-02 tối đa 2s, HN-03 tối đa 3s, HN-04 tối đa 2s, HN-05 tối thiểu 5 người; yêu cầu quy mô 1000 sản phẩm, 10000 hóa đơn/6 tháng. Measurements NOT RUN.
5. P1 Release & secrets: #44, #242, #283 phải phân biệt PostgreSQL TLS VerifyFull và HTTPS web, kiểm Git secrets history và required checks. Không deploy hoặc apply migration trên Supabase shared nếu chưa có approval.
6. P2 DOC/slides: #245/#265 đợi report/test dữ liệu thật và review, không tuyên bố Word/PPTX đã hoàn thành.

## Căn cứ review của thành viên

- Yến yêu cầu QA-01 đến QA-07 trong [review PR304](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/pull/304#pullrequestreview-6092820134), sau đó [REQUEST CHANGES](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/pull/304#pullrequestreview-6092830408).
- Thăng cung cấp [27 Must và ngưỡng NFR](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/pull/304#pullrequestreview-6094976252); source [PR301](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/pull/301) chưa merge.
- PR300 đã merge tài liệu security proposal, không tự coi D1–D7/WR-06–10 đã triển khai. #257 đã ghi quyết định scope DISABLE/FREEZE loyalty, không đồng nghĩa code đã nghiệm thu.
- Các comments backlog ngày 28/11 của Yến nói đóng issue lịch sử theo NOT PLANNED; thực trạng GitHub hiện OPEN/reopened cần nhóm trưởng xác nhận. Không tự ghi DONE/PASS hồi tố.

## Gate

Test plan thống nhất trong docs/06-testing/test-plan.md và report duy nhất trong docs/06-testing/test-reports/2026-11-27-qa-regression.md. Release HOLD tới khi CI/TRL đúng SHA, test theo official Must, browser, security TO-BE, NFR, full-schema backup/restore, và reviewer signoff. Chỉ sửa trên PR304 hướng develop, không chạm main hoặc Supabase chung.
