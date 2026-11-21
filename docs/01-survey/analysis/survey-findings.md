# Survey Findings Register — Bước 4

> **SIMULATED / DỮ LIỆU GIẢ LẬP — KHÔNG PHẢI KHẢO SÁT THỰC TẾ.** Tất cả nguồn phỏng vấn và quan sát là tình huống AI dựng. Mã E-SIM chỉ trỏ tới nội dung giả lập, không phải bằng chứng hiện trường, lời chứng hay kết quả nghiệm thu. Không đưa các kết luận dưới đây vào báo cáo như đã khảo sát cửa hàng thật.


**ID:** SM-SUR-FIND-001 · **Status:** SIMULATED FINDINGS / NO FIELD VALIDATION.

## Phương pháp

Mỗi `F-SIM-*` là phát hiện *trong kịch bản mô phỏng*, có nguồn `INT` (lời vai diễn) hoặc `OBS` (bước quan sát diễn tập). `INT` và `OBS` ở đây không độc lập chứng minh hiện tượng ngoài đời: cả hai đều được AI tạo.

| Finding ID | Business area | Phát hiện trong mô phỏng | Nguồn trực tiếp | Evidence ID | Xác minh thực địa |
|---|---|---|---|---|---|
| F-SIM-001 | Reporting | P01 so báo cáo quầy với tiền mặt, chuyển khoản rồi ghi tổng kết vào bảng tính; có thể phải tra lại giao dịch khi ghi chú thiếu | [INT-001](../interviews/records/INT-001-owner.md) `OWN-04`, `OWN-09`, `OWN-11`, `OWN-14` | E-SIM-INT-01 | NOT VERIFIED |
| F-SIM-002 | Sales/Product | P02 quét mã, khi mã không nhận có thể tìm theo tên; diễn tập một lần quét không thành công rồi tìm tên | [INT-002](../interviews/records/INT-002-cashier.md) `CAS-02`, `CAS-06`, `CAS-10`; [OBS-001](../observations/records/OBS-001-sales.md) bước 2–3 | E-SIM-INT-02, E-SIM-OBS-001-02..03 | NOT VERIFIED |
| F-SIM-003 | Payment | P02 muốn xác minh đã nhận chuyển khoản thực tế, không chỉ dựa vào ảnh khách đưa; **không có observation chuyển khoản** | INT-002 `CAS-04`, `CAS-10`, `PROBE-01` | E-SIM-INT-02 | NOT VERIFIED |
| F-SIM-004 | Purchasing | P03 cần đối chiếu số giao/số thực nhận; kịch bản ghi phiếu 12 chai, kiểm được 10, chủ đồng ý chỉ ghi nhận 10 | INT-003 `WH-02`, `WH-04`, `WH-06`, `PROBE-01`; [OBS-002](../observations/records/OBS-002-purchasing.md) bước 1–7 | E-SIM-INT-03, E-SIM-OBS-002-01..07 | NOT VERIFIED |
| F-SIM-005 | Inventory | P03 nói phải đếm lại, đối chiếu chứng từ và hỏi P01 trước điều chỉnh; diễn tập tồn sổ 18, đếm hai lượt 17, ghi -1 và chờ duyệt | INT-003 `WH-07`, `WH-09`, `WH-14`; INT-001 `OWN-07`; [OBS-003](../observations/records/OBS-003-inventory.md) bước 1–7 | E-SIM-INT-03, E-SIM-INT-01, E-SIM-OBS-003-01..07 | NOT VERIFIED |
| F-SIM-006 | Authorization | P01 nói quyền đổi giá, hủy hóa đơn đã thanh toán và duyệt điều chỉnh tồn thuộc chủ cửa hàng; P02 nói không tự sửa giá | INT-001 `OWN-05`, `OWN-12`; INT-002 `CAS-03`, `CAS-08`, `CAS-09` | E-SIM-INT-01, E-SIM-INT-02 | NOT VERIFIED |
| F-SIM-007 | Sales/Receipt | P02 nói ứng dụng quầy lưu giao dịch và có thể in lại khi máy in kẹt, cần tìm giao dịch; không có tình huống in lại thực tế | INT-002 `CAS-05`, `CAS-14`; INT-001 `OWN-16`, `PROBE-02` | E-SIM-INT-02, E-SIM-INT-01 | NOT VERIFIED |
| F-SIM-008 | Inventory UX | P03 cần nhìn mã/tên/đơn vị tính/tồn ghi/tồn đếm/chênh lệch, mong dễ nhập trên điện thoại hoặc máy gần kho | INT-003 `WH-15`, `WH-17` | E-SIM-INT-03 | NOT VERIFIED |
| F-SIM-009 | Operational constraint | P01 mong triển khai đơn giản trên máy quầy, có thể xem bằng điện thoại, tránh chi phí duy trì phức tạp | INT-001 `OWN-17` | E-SIM-INT-01 | NOT VERIFIED |

## Các số liệu có trong tình huống, không phải thống kê nghiên cứu

- [OBS-001](../observations/records/OBS-001-sales.md): 2 chai nước × 12.000đ + 1 snack × 9.000đ = **33.000đ**; nhận **50.000đ** tiền mặt; thối **17.000đ**. Đây là 1 ví dụ tính tiền, không phải doanh thu quan sát được.
- [OBS-002](../observations/records/OBS-002-purchasing.md): số trên phiếu **12**, thực nhận **10**, thiếu **2** (một tình huống dựng sẵn).
- [OBS-003](../observations/records/OBS-003-inventory.md): tồn bảng tính **18**, kiểm đếm **17**, chênh lệch **-1** (một tình huống dựng sẵn).
- Không có thống kê số giao dịch/ngày, tỷ lệ quét lỗi, thời gian chốt ca, tần suất sai lệch hoặc tác động tài chính thực tế.

## Findings cần đối chiếu thực địa

1. Xác định tên/phương thức hoạt động của công cụ POS, bảng tính và chứng từ thật.
2. Đo thời gian thao tác khi tra cứu mã không quét được hoặc đối chiếu cuối ngày, nếu được phép.
3. Xác định cơ chế xác nhận chuyển khoản và phạm vi quyền người dùng.
4. Kiểm tra đơn vị tính thùng/chai và chính sách phê duyệt chênh lệch.
5. Xác minh thời điểm cập nhật tồn kho sau bán, nhận và kiểm kê.
