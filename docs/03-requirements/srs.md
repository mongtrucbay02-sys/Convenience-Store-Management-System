# Software Requirements Specification – Smart Mart (Candidate v0.1)

> **SIMULATED / INTERNAL DRAFT / NOT CUSTOMER-APPROVED.** Nguồn khảo sát Giai đoạn 1 là mô phỏng (S0); ưu tiên/MVP Giai đoạn 2 chưa được phê duyệt. Các yêu cầu dưới đây là **candidate specifications**, không phải xác nhận hệ thống đã triển khai hay đã PASS.

**ID:** SM-SRS-001 · **Status:** DRAFT · **Tài liệu nguồn:** [02/scope](../02-product-definition/scope.md), [02/scenarios](../02-product-definition/scenarios.md) và [01/problem](../01-survey/analysis/problem-statement.md).

## 1. Mục đích và phạm vi
Đặc tả ứng viên cho cửa hàng tiện lợi độc lập, gồm chủ cửa hàng P01, thu ngân P02, nhân viên kho P03 (có thể kiêm nhiệm). Ưu tiên bán hàng tại quầy, nhận hàng, ghi tồn nhất quán và kiểm soát quyền. Không mô tả chuỗi nhiều chi nhánh.

## 2. Nguồn yêu cầu và mức chắc chắn
`PS-01…05/CON-01 → N-SIM-01…08 → FC-SIM-01…08 → FR/US/AC` là truy vết từ *bằng chứng mô phỏng*, không phải xác nhận thực địa. `D-01` yêu cầu POS hoàn tất trừ tồn và phiếu nhận được xác nhận cộng tồn cùng sổ; quyết định này vẫn pending G2-03/G2-04. Những chính sách chưa có nguồn chi tiết giữ `TBD`.

## 3. Tác nhân và giả thuyết quyền
| Actor | Mục tiêu | Ranh giới chưa chốt |
|---|---|---|
| P01 Owner | Quản trị ngoại lệ, xem doanh thu, duyệt tồn | Ai thay khi vắng, phân quyền chi tiết |
| P02 Cashier | Chọn hàng, thanh toán tiền mặt, tìm hóa đơn | Chuyển khoản, hủy/hoàn tiền, tra cứu theo ca |
| P03 Warehouse | Nhận hàng, kiểm đếm, đề xuất kiểm kê | Duyệt phiếu thường, đơn vị thùng/chai |

## 4. Phạm vi chức năng
**I0–I2 (MVP proposal):** đăng nhập/role cơ bản, tra cứu sản phẩm, tiền mặt + hóa đơn cơ bản, POS→Inventory, nhận hàng thực tế + xác nhận + NH→Inventory, audit nghiệp vụ tối thiểu. **I3 candidate:** kiểm kê/approval, báo cáo doanh thu/đối soát, tối ưu trên thiết bị kho; có thể tách sau MVP tối thiểu. [Danh mục FR](functional-requirements.md).

## 5. Yêu cầu phi chức năng
[NFR candidate](non-functional-requirements.md) chỉ có tiêu chí/phương pháp đánh giá; các chỉ tiêu định lượng và ràng buộc vận hành cần nguồn và người phê duyệt.

## 6. Giao diện/luồng cần định nghĩa ở bước thiết kế
- UI tìm hàng, giỏ hàng, thanh toán tiền mặt, tra hóa đơn; UI phiếu nhận với ordered/received/variance; tồn và lịch sử.
- Phối hợp bán hàng→KHO và nhận hàng→KHO: phải xác định transaction, idempotency, chống ghi trùng, lỗi giữa chừng, cách bù/rollback trước implement.
- Không giả định bắt buộc API ngân hàng, offline, máy in/hệ điều hành hay giao thức cụ thể.

## 7. Dữ liệu ví dụ kiểm thử (không phải số liệu vận hành)
- POS: 2×12.000đ + 9.000đ = 33.000đ; nhận 50.000đ; thối 17.000đ.
- NH: chứng từ 12 chai, nhận 10, chênh −2; không được tăng tồn +12 khi chỉ chấp nhận 10.
- Kiểm kê: tồn ghi 18, đếm 17, chênh −1, chưa duyệt thì tồn chính thức chưa đổi.

## 8. Các điểm mở và tiêu chí nghiệm thu
[Open decisions](open-decisions.md); AC phải có nguồn, owner và test evidence khi hoàn thành. [M-10a/M-10b/M-11](../02-product-definition/success-metrics.md) phân biệt coverage với kết quả PASS. **Chưa có acceptance sign-off.**

## 9. Tài liệu trước đây được nhắc trong repo
README Giai đoạn 3 hiện nhắc SRS v1.0.1, 81 Use Case v1.1, NFR, MoSCoW **ở ngoài repo**. Bản này là draft kế thừa 01–02, **không thay thế** các bản gốc khi chưa đối chiếu từng yêu cầu và quyết định hợp nhất.
