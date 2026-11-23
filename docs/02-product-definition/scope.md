# Product Scope & Proposed MVP — Smart Mart

> **SIMULATED / NOT APPROVED.** Đây là phương án chia phạm vi nội bộ dựa trên hồ sơ Giai đoạn 1 (PR #227), **không phải backlog/MVP khách hàng đã duyệt hoặc mô tả chức năng repo đã chạy**.

**ID:** SM-PD-SCOPE-001 · **Version:** 0.1 · **Status:** DRAFT.

## 1. In scope / Out of scope
- **In scope candidates:** danh mục và tra cứu sản phẩm; bán hàng; nhập hàng với số thực nhận; tồn kho và kiểm kê; role controls; hóa đơn; báo cáo đơn giản; giao diện phù hợp cửa hàng nhỏ.
- **Out of scope định hướng:** đa chi nhánh, nhiều kho phân tán, e-commerce toàn diện, enterprise analytics, tích hợp ngân hàng tự động khi chưa có cơ sở.
- **Không đồng nghĩa cam kết:** có trong phạm vi nghiên cứu ≠ thuộc MVP.

## 2. MVP đề xuất theo luồng dọc (vertical slices)
| Increment | Giá trị người dùng | Feature(s) đề xuất | Điều kiện cắt phạm vi |
|---|---|---|---|
| I0 — Foundation | Đăng nhập/role cơ bản, dữ liệu sản phẩm mẫu | FC-SIM-01,06 | Không có báo cáo nâng cao |
| I1 — Sales Golden Path | Tìm sản phẩm, bán tiền mặt, lưu & tìm hóa đơn, **trừ tồn chính thức khi giao dịch hoàn tất** | FC-SIM-01,03,04,06 | Chưa chốt kiểm chứng chuyển khoản |
| I2 — Receiving-to-Stock | Nhập số thực nhận, xác nhận và tăng tồn theo nghiệp vụ | FC-SIM-02,03,06 | Cần chốt quyền duyệt và đơn vị tính |
| I3 — Stocktake & Simple Reporting | Lệch kiểm kê chờ duyệt, xem báo cáo cơ bản | FC-SIM-05,07,08 | Có thể chia sau MVP tối thiểu nếu thiếu thời gian |

**Đề xuất MVP tối thiểu:** I0+I1+I2 với một đường đi end-to-end cho bán hàng và nhận hàng, có quyền căn bản, dữ liệu và kiểm thử chứng minh. **I3 chưa chốt**, tùy lịch và ưu tiên được người đại diện xác nhận. Tính nhất quán POS→Inventory không được coi là đã bảo đảm khi thiếu kiểm thử tích hợp.

### Quyết định D-01 — Ranh giới cập nhật tồn kho trong MVP
| Mục | Nội dung |
|---|---|
| Quyết định | **MVP gồm bán hàng và cập nhật tồn nhất quán.** Giao dịch bán hoàn tất phải trừ tồn kho chính thức; phiếu nhận hàng được xác nhận phải cộng tồn theo số thực nhận. Cả hai cùng ghi vào một sổ tồn chính thức. |
| Hệ quả | Kiểm thử tích hợp POS→KHO và NH→KHO là điều kiện bắt buộc để công bố MVP (xem `M-04`, `M-11` trong [success-metrics.md](success-metrics.md)). Nếu đến hạn chưa có bằng chứng tích hợp, MVP phải ghi rõ là **chưa hoàn thành**, không được trình bày như đã bảo đảm nhất quán tồn. |
| Còn mở | Xử lý khi thiếu tồn lúc bán; hủy hóa đơn/trả hàng làm thay đổi tồn (DT, POS-12) thuộc phạm vi riêng. |
| Căn cứ | Product Vision §3–4 (tính nhất quán, verified actions); review PR #228 — Finding 02. |
| Người đề xuất / ngày | Phan Hai Yen · 2026-11-22 |
| Trạng thái | ĐỀ XUẤT NỘI BỘ — chờ nhóm/Product Owner duyệt (G2-03, G2-04) |

**Đầu vào cho Giai đoạn 3:** D-01 phải được chuyển thành business rule và acceptance criteria trong SRS, kèm test case tích hợp.

## 3. Scope theo module dự án
| Module | MVP proposal | Giai đoạn tiếp | Nguồn/nhận xét |
|---|---|---|---|
| SP | Core | Mở rộng quản trị | N-SIM-01 + yêu cầu repo độc lập |
| POS | Core (tiền mặt, hóa đơn cơ bản) | Thanh toán phức tạp, in lại tùy scope | N-SIM-01,05 |
| NH | Core (nhận theo số thực) | Quy tắc ngoại lệ chi tiết | N-SIM-02,03 |
| KHO | Core ledger / tồn cơ bản | Kiểm kê và approval đầy đủ (I3) | N-SIM-03,04 |
| AUTH | Core roles | Granular permissions | N-SIM-06 |
| BC | Sau I0–I2, review | Báo cáo/đối soát | N-SIM-07 |
| KH | Not selected | Separate review | Không có nguồn N-SIM trực tiếp |
| KM | Not selected | Separate review | Không có nguồn N-SIM trực tiếp |
| DT | Not selected | Separate review | Cần nguồn yêu cầu độc lập; không có bản mô phỏng đổi trả E2E |

## 4. Không đồng nhất kế hoạch với trạng thái code
- Các module nói trên là danh mục quyết định phạm vi **đề xuất**, không xác nhận hiện đã code/test đủ.
- Tài liệu thiết kế hiện có ghi Đổi trả là *Sau MVP* (Issue #4) và khuyến mãi POS-04 ở tầng 2 (xem [`04-system-design/api/`](../04-system-design/api/)); cần kiểm lại Issues/PR/Decisions liên quan trước khi phê duyệt chính thức.
- Nếu nhóm chọn giữ KH/KM/DT trong bản demo hoặc rubric yêu cầu, phải nêu nguồn yêu cầu riêng và quyết định thay đổi phạm vi (scope change).

## 5. Quyết định cần phê duyệt
| Gate ID | Quyết định | Người quyết định | Ngày review | Kết luận | Trạng thái |
|---|---|---|---|---|---|
| G2-01 | Chốt đối tượng và Vision | Chưa chỉ định (Product Owner/nhóm) | 2026-11-22 (review PR #228) | Nội dung Vision giữ nguyên; chưa có người duyệt | PENDING |
| G2-02 | Xác nhận Must/Should từ FC | Chưa chỉ định | 2026-11-22 (review PR #228) | MoSCoW vẫn là đề xuất nội bộ | PENDING |
| G2-03 | Chọn I0–I2 hoặc I0–I3 làm MVP | Chưa chỉ định | 2026-11-22 (review PR #228) | Đề xuất I0+I1+I2, kèm quyết định D-01 về tồn kho | PENDING — có đề xuất |
| G2-04 | Xác minh quyền, tồn kho và trạng thái thanh toán | Chưa chỉ định | 2026-11-22 (review PR #228) | Tồn kho: đề xuất theo D-01. Quyền và chuyển khoản (PS-05): còn mở | PENDING |
| G2-05 | Tách rõ yêu cầu khảo sát và yêu cầu repo đã có | Chưa chỉ định | 2026-11-22 (review PR #228) | Đã tách trong feature-candidates §2 (KH, KM, DT); chờ xác nhận | PENDING |

**Trạng thái Giai đoạn 2:** `SIMULATED / INTERNAL PROPOSAL`. Chưa có gate nào được phê duyệt; không ghi `Customer Approved` khi chưa có khảo sát thực địa và người đại diện cửa hàng xác nhận. Khi có người duyệt, điền tên, ngày và kết luận vào bảng trên.

**Đầu ra cho Giai đoạn 3:** US candidates, AC, business rules, NFR và requirement traceability cho những FC thực sự được chấp thuận.
