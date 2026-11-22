# Problem Analysis Register — Bước 4

> **SIMULATED / DỮ LIỆU GIẢ LẬP — KHÔNG PHẢI KHẢO SÁT THỰC TẾ.** Tất cả nguồn phỏng vấn và quan sát là tình huống AI dựng. Mã E-SIM chỉ trỏ tới nội dung giả lập, không phải bằng chứng hiện trường, lời chứng hay kết quả nghiệm thu. Không đưa các kết luận dưới đây vào báo cáo như đã khảo sát cửa hàng thật.


**ID:** SM-SUR-PROB-001 · **Status:** CANDIDATE PROBLEMS / ALL NOT FIELD VERIFIED.

## Quy tắc phân tích

- `Problem` là khó khăn được gợi ra bởi *nguồn giả lập*, không phải kết luận ngoài đời.
- `Possible cause` là giả thuyết của nhóm, không được nhầm với nguyên nhân gốc đã chứng minh.
- `Impact` có thể là lời kể hoặc nguy cơ hợp lý; **không gắn số lượng/tỷ lệ** khi không có dữ liệu đo.
- `Proposed need` chưa phải yêu cầu SRS được chấp nhận.

| Problem ID | Related findings | Problem candidate | Possible cause, NOT proven | Possible impact | Source | Status |
|---|---|---|---|---|---|---|
| P-SIM-01 | F-SIM-001 | Đối chiếu doanh thu cuối ngày và tìm giao dịch phải qua nhiều bước | Báo cáo quầy, xác nhận tiền và bảng tính tách nhau | Chậm tổng hợp, cần tra cứu thủ công | OWN-03/04/09/11/14 | HYPOTHESIS |
| P-SIM-02 | F-SIM-002 | Khi mã không nhận, thu ngân phải chuyển sang tìm tên/hỏi chủ | Nhãn/mã quét không nhận; đặc tính thiết bị chưa rõ | Có thể trì hoãn giao dịch | CAS-02/06/10, OBS-001:2–3 | HYPOTHESIS |
| P-SIM-03 | F-SIM-004 | Số trên chứng từ khác hàng thực nhận; có nguy cơ ghi sai số tồn | Chưa phân biệt số giao và số thực nhận ở bước ghi chép | Tồn ghi cao hơn hàng thực tế nếu nhập sai | WH-04/06, OBS-002:1–7 | HYPOTHESIS |
| P-SIM-04 | F-SIM-004, F-SIM-008 | Việc ghi lặp và phân quyền xử lý chênh lệch kho có thể thiếu rõ ràng | Biểu mẫu giấy/bảng tính, kiểm duyệt thủ công | Tăng thao tác, khó đối chiếu | WH-12/14/15/17, OWN-07 | HYPOTHESIS |
| P-SIM-05 | F-SIM-005 | Chênh lệch giữa tồn ghi và đếm thực tế cần được điều tra, không thể sửa ngay | **UNKNOWN** — tài liệu không chứng minh nguyên nhân chênh -1 | Nguy cơ sai lệch nếu điều chỉnh thiếu kiểm soát | OBS-003:2–6, WH-09, OWN-07 | HYPOTHESIS |
| P-SIM-06 | F-SIM-006 | Các thao tác đổi giá/hủy hóa đơn/duyệt tồn cần kiểm soát theo vai trò | Cơ chế truy vết hiện tại chưa rõ | Khó xác minh trách nhiệm khi có sai sót | OWN-12/13, CAS-03/08/09 | HYPOTHESIS |
| P-SIM-07 | F-SIM-007 | Tình huống máy in trục trặc đòi hỏi khả năng tìm/in lại hóa đơn | Thiết bị in đôi khi hết giấy hoặc kẹt theo P02 | Gây gián đoạn phục vụ nếu không truy hồi nhanh | CAS-05/11/14 | HYPOTHESIS |
| P-SIM-08 | F-SIM-003 | Giao dịch chuyển khoản cần xác minh đã nhận tiền | Ảnh giao dịch không chứng minh tiền đã đến | Nguy cơ ghi nhận thanh toán khi chưa nhận tiền | CAS-04/10, PROBE-01 | HYPOTHESIS |

## Tình huống số học để luyện phân tích — KHÔNG phải mức độ ảnh hưởng đo được

- **Nhập hàng:** phiếu giao 12, nhận 10; chênh lệch **2**. Nếu ghi tồn +12 thay vì +10 thì sai **+2**. Trong kịch bản P01 đã yêu cầu ghi 10, **không có bằng chứng đã cộng sai 12**.
- **Kiểm kê:** sổ 18, thực đếm 17, chênh **-1**. Chưa điều chỉnh; nguyên nhân `UNKNOWN`.
- **Bán hàng:** tổng 33.000đ, nhận 50.000đ, thối 17.000đ. Phép tính đúng trong tình huống; **không phải lỗi doanh thu**.

## Ưu tiên từ lời kể, không phải thứ tự nghiệm thu

- P01 `OWN-16`: tồn chính xác sau bán/nhập → tra cứu hóa đơn → báo cáo cuối ngày gọn hơn.
- P02 `CAS-14`: tìm sản phẩm theo tên → in lại hóa đơn.
- P03 `WH-16`: ghi số thực nhận một nơi → chênh lệch rõ ràng → lịch sử biến động.

Không tổng hợp thành xếp hạng toàn cửa hàng hoặc gán số điểm vì chưa có khảo sát độc lập và không có quy tắc đo được xác nhận.

## Chưa thể kết luận

Không thể kết luận rằng phần mềm hiện tại gây thất thoát, xảy ra thất bại thanh toán, mã vạch lỗi thường xuyên, hoặc dữ liệu tồn đã bị sửa sai. Không có phép đo thực địa, chứng từ thật hay xác nhận người thật.
