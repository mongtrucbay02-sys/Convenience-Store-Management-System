# OBS-001 — Quan sát giả lập quy trình bán hàng

> **SIMULATED / DỮ LIỆU GIẢ LẬP — KHÔNG PHẢI KHẢO SÁT THỰC TẾ.** Tất cả tên, lời nói, quy trình, số liệu, thời gian và tình huống ở đây là do AI dựng để luyện tập. Không được trình bày như bằng chứng khảo sát thật trong báo cáo.

**Observation ID:** OBS-001 · **Activity:** Sales at Counter · **Scenario slot:** 15:05–15:30  
**Observer:** ENG-01 · **Participant:** P02  
**Status:** SIMULATED OBSERVATION ONLY — NO REAL STORE WAS OBSERVED.  
**Permission:** Fictional scenario consent, not consent from a real business.

## Nhật ký thao tác giả lập

| Step | Actor | Action (simulated) | Input / Tool | Observable result | Evidence |
|---|---|---|---|---|---|
| 1 | P02 | Nhận 2 nước đóng chai và 1 gói snack từ khách giả lập | Nước 12.000đ/chai, snack 9.000đ | Ba món đặt tại quầy | E-SIM-OBS-001-01 |
| 2 | P02 | Quét mã của chai thứ nhất; lần quét thứ hai được mô phỏng lỗi | Máy quét mã giả định | Một món không nhận dạng từ mã | E-SIM-OBS-001-02 |
| 3 | P02 | Tìm mặt hàng theo tên trên giao diện quầy | Ứng dụng POS mô phỏng | Tìm đúng sản phẩm | E-SIM-OBS-001-03 |
| 4 | P02 | Chọn số lượng 2 cho nước và 1 cho snack | Giỏ hàng mô phỏng | Tổng tiền được hiển thị 33.000đ | E-SIM-OBS-001-04 |
| 5 | P02 | Xác nhận hình thức tiền mặt; nhận 50.000đ | Tiền mặt mô phỏng | Tiền thừa dự kiến 17.000đ | E-SIM-OBS-001-05 |
| 6 | P02 | Hoàn tất giao dịch và in phiếu mô phỏng | Máy in giả lập | Phiếu in mô phỏng có ba đơn vị hàng | E-SIM-OBS-001-06 |
| 7 | ENG-01 | Ghi lại thời điểm tra cứu thủ công vì quét mã lỗi | Ghi chép diễn tập | Tình huống cần quan sát lại tại cửa hàng thật | E-SIM-OBS-001-07 |

## So với phỏng vấn

Lời P02 về khó quét mã tại CAS-02/CAS-11 được tái hiện trong kịch bản, nhưng đây không phải bằng chứng xác nhận lỗi thiết bị thật.

## Nội dung chưa kiểm chứng

- Chưa biết phần mềm quầy thực tế cho phép tìm tên ra sao.
- Chưa đo thời gian xử lý mã quét lỗi.
- Chưa đối chiếu phiếu in và dữ liệu tồn với hệ thống thật.

## Phạm vi bằng chứng

Không có ảnh chụp, hóa đơn thật, đồng hồ đo thời gian thực, log ứng dụng hay chứng từ của một cửa hàng có thật. Các mã E-SIM chỉ là con trỏ tới bước diễn tập trong bảng trên.
