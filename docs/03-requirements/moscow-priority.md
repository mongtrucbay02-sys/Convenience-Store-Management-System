# MoSCoW – Candidate Priority, not approved

> **SIMULATED / INTERNAL DRAFT / NOT CUSTOMER-APPROVED.** Nguồn khảo sát Giai đoạn 1 là mô phỏng (S0); ưu tiên/MVP Giai đoạn 2 chưa được phê duyệt. Các yêu cầu dưới đây là **candidate specifications**, không phải xác nhận hệ thống đã triển khai hay đã PASS.

Bảng này **kế thừa mức ưu tiên đề xuất** trong `02/feature-candidates.md`; việc phân rã thành FR chỉ là quyết định đặc tả nội bộ.

| Priority proposed | Candidate requirements | Lý do / ranh giới |
|---|---|---|
| Must (I0–I2) | FR-01…05, FR-07…10, FR-13 | Luồng bán, nhận hàng và tồn nhất quán theo D-01 (pending) |
| Should (I3 / review) | FR-06, FR-11, FR-12, FR-14, FR-15 | In lại, kiểm kê, báo cáo, tiện dụng thiết bị; chưa chốt thời điểm |
| Not prioritized | KH/KM/DT và PS-05 bank API | Có nguồn yêu cầu độc lập cần kiểm tra hoặc chưa có yêu cầu kết nối ngân hàng |

**Important:** không tự gán lớp `Could/Won't` cho chức năng mà chưa có quyết định. Mỗi thay đổi phải ghi trong `change-log.md` và liên kết issue/approval.
