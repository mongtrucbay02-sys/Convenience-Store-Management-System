# Smart Mart — Ma trận phân công và nghiệm thu cuối kỳ

> Trạng thái: KẾ HOẠCH / chưa thay cho bằng chứng PASS. Mốc: 24–29/11/2026. Baseline: nhánh `develop` sau PR #286. Không chạy test phá hủy trên Supabase dùng chung.

## Quy tắc

- **Mỗi issue chỉ có 01 assignee/implementer**. Không chuyển toàn bộ người review thành assignees.
- **Hai người review được ghi rõ trong issue**: Yến (`phanyen06ym-ien`) và một thành viên theo module. Đây là người *được đề xuất review*, chưa phải xác nhận chấp thuận.
- **Lưu ý độc lập:** issue do Yến làm có 'Yến (tự kiểm)' + người duyệt thứ hai độc lập; một người không thể vừa làm vừa review độc lập. Nếu yêu cầu hai review độc lập, cần cử thêm reviewer thứ ba.
- Không đóng issue chỉ vì code đã có. Cần PR, review, build/test, evidence và đối chiếu phạm vi/scope.
- Không tạo 28 file nghiệm thu: **duy nhất file này** là ma trận tổng, liên kết các báo cáo kiểm thử và DOC có sẵn.

## Ma trận theo nhóm

| Nhóm | Issue | Một người làm | Hai người review | Phạm vi |
|---|---|---|---|---|
| Backend | [#283](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/283) | Thăng | Yến, Trung | DB integration, quyền, migration, CI release |
| Backend | [#266](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/266) | Trung | Yến, Thăng | Bugfix BC/KH/KM |
| Backend | [#262](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/262) | Hoài | Yến, Trung | Bugfix POS/DT |
| Backend | [#260](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/260) | Yến | Yến (tự kiểm), Thăng | Bugfix SP/NH/KHO |
| Backend | [#250](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/250) | Trung | Yến, Thăng | BC-11 hiệu quả KM |
| Backend | [#249](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/249) | Trung | Yến, Thăng | BC-10 khách hàng/điểm |
| Backend | [#248](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/248) | Trung | Yến, Hoài | BC-09 đổi trả |
| Backend | [#247](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/247) | Trung | Yến, Thăng | KM-05 xóa KM |
| Backend | [#246](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/246) | Trung | Yến, Thăng | KM-06 lịch sử KM |
| Backend | [#239](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/239) | Hoài | Yến, Thăng | DT-06 duyệt ngoại lệ |
| Backend | [#238](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/238) | Hoài | Yến, Thăng | DT-03 đổi hàng |
| Backend | [#237](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/237) | Hoài | Yến, Thăng | DT-07 danh sách phiếu |
| Backend | [#176](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/176) | Yến | Yến (tự kiểm), Trung | Index và hiệu năng |
| Backend | [#146](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/146) | Yến | Yến (tự kiểm), Thăng | NH-09 tra cứu |
| Frontend | [#253](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/253) | Trang | Yến, Hoài | DT-08 giao diện cấu hình |
| Frontend | [#193](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/193) | Trang | Yến, Trung | Responsive tồn, cảnh báo, báo cáo |
| Frontend | [#189](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/189) | Trang | Yến, Trung | KH-07 giao diện cấu hình điểm |
| Frontend | [#114](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/114) | Trang | Yến, Hoài | Mẫu in phiếu/hóa đơn |
| Frontend | [#112](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/112) | Trang | Yến, Hoài | Giao diện POS |
| Frontend | [#69](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/69) | Trang | Yến, Trung | Shared UI components |
| Test | [#244](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/244) | Huy | Yến, Hoài | E2E automation POS |
| Test | [#264](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/264) | Huy | Yến, Thăng | Full regression & report cuối |
| DOC | [#257](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/257) | Thăng | Yến, Trung | Báo cáo DOC + SRS final |
| DOC | [#254](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/254) | Trang | Yến, Thăng | Khảo sát/giao diện |
| DOC | [#251](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/251) | Trung | Yến, Thăng | BC/KH/KM |
| DOC | [#245](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/245) | Huy | Yến, Thăng | Kiểm thử/triển khai |
| DOC | [#240](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/240) | Hoài | Yến, Thăng | POS/DT |
| DOC | [#236](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/236) | Yến | Yến (tự kiểm), Thăng | SP/NH/KHO |

## File bằng chứng / nghiệm thu

- **Backend**: `docs/06-testing/test-reports/2026-11-24-postgresql-integration.md` (mốc đã có); cập nhật bằng chứng cuối vào **file ma trận này** và liên kết report/PR/test thực tế theo issue.
- **Frontend**: `docs/06-testing/test-reports/2026-11-22-integration-ui-e2e.md` (mốc hiện hữu), liên kết screenshot và test browser ở PR, cập nhật kết quả cuối trong ma trận.
- **Test**: `docs/06-testing/test-reports/` chứa test reports có sẵn; #264 bổ sung báo cáo cuối vào thư mục này sau chạy kiểm thử, chưa được xem là PASS hiện tại.
- **DOC**: `docs/10-final-report/` chứa bản nháp; đầu ra **một báo cáo DOCX cuối**, SRS đối chiếu source ở #257. Chưa mặc định đã có bản final.

## Gate xuyên suốt áp dụng cho mỗi issue

1. **OPEN / SCOPE LOCKED** — chốt tiêu chí, người làm, reviewer, liên kết SRS và base commit.
2. **IN PROGRESS** — chỉ 01 assignee sửa trên feature branch từ `develop`; không sửa Supabase dùng chung.
3. **SELF TEST** — build + test mục tiêu; bằng chứng lệnh, môi trường, số lượng PASS/FAIL, SHA.
4. **PR REVIEW** — mở PR vào `develop`, người làm không tự merge; hai reviewer được mời trên PR. Yến làm author thì chỉ có một reviewer độc lập trong cặp bắt buộc.
5. **INTEGRATION** — merge sau review và CI xanh; test regression PostgreSQL 17 độc lập + check Razor route/permission nếu liên quan.
6. **ACCEPTANCE** — cập nhật ma trận issue→PR→SHA→test report→screenshot (nếu UI)→đánh giá PASS/FAIL/PENDING.
7. **FREEZE / RELEASE** — chốt code trên `develop`; release `main` chỉ khi gate #283 và báo cáo #264 đáp ứng; DOC cuối #257 đồng bộ đúng SHA.
8. **CLOSED** — chỉ đóng issue sau khi đủ bằng chứng và reviewer xác nhận. Nếu hủy phạm vi ghi rõ NOT PLANNED, không nói đã PASS.

## Mẫu ghi nghiệm thu cho từng issue (điền sau, không giả định PASS)

| Issue | PR | Merge SHA | Build/CI | Test evidence | Review Yến | Review peer | Quyết định |
|---|---|---|---|---|---|---|---|
| #283 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #266 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #262 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #260 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #250 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #249 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #248 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #247 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #246 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #239 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #238 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #237 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #176 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #146 | [#294](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/pull/294) → develop | PENDING | Local Release 0 lỗi/0 cảnh báo; CI xem PR | [NH09.md](../../05-implementation/NH09.md), [evidence](../evidence/2026-11-26/nh09/verification.json): source `254e9d4`, 31/31 NH-09, 857/857 full PostgreSQL 17.11 riêng | SELF TEST, chưa review độc lập | Đã mời Thăng, PENDING | Chờ CI/review và UI trực quan/staging; chưa nghiệm thu |
| #253 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #193 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #189 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #114 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #112 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #69 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #244 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #264 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #257 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #254 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #251 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #245 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #240 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |
| #236 | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING | PENDING |

## Tiêu chí kết thúc

- **Backend:** database/routing/role/transaction/negative cases đúng SRS, không có bug cao, SQL và privilege an toàn.
- **Frontend:** Razor UI hiển thị và thao tác đúng, quyền, responsive, error states và màn in.
- **Test:** bài test tự động có thể chạy lại, CI pass, báo cáo case/defect rõ ràng.
- **DOC:** một bản cuối đầy đủ chương, bảng phân công, hình minh họa, số liệu trùng log/test và mã nguồn release.

**Phạm vi review:** phân công được cập nhật trong issue, nhưng GitHub không hỗ trợ 'issue reviewers' như PR reviewers. Chỉ có thể yêu cầu review chính thức khi PR đã mở.
