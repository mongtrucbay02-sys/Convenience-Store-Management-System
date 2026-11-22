# Source Traceability — Bước 3 → Bước 4

> **SIMULATED / DỮ LIỆU GIẢ LẬP — KHÔNG PHẢI KHẢO SÁT THỰC TẾ.** Tất cả nguồn phỏng vấn và quan sát là tình huống AI dựng. Mã E-SIM chỉ trỏ tới nội dung giả lập, không phải bằng chứng hiện trường, lời chứng hay kết quả nghiệm thu. Không đưa các kết luận dưới đây vào báo cáo như đã khảo sát cửa hàng thật.


**ID:** SM-SUR-TRACE-001 · **Status:** INTERNAL SIMULATION TRACEABILITY ONLY.

| Original source(s) | Finding | Problem candidate | Need candidate | As-is |
|---|---|---|---|---|
| INT-001 OWN-04/09/11/14 | F-SIM-001 | P-SIM-01 | N-SIM-07 | AS-SIM-04 |
| INT-002 CAS-02/06/10; OBS-001:2–3 | F-SIM-002 | P-SIM-02 | N-SIM-01 | AS-SIM-01 |
| INT-002 CAS-04, PROBE-01 | F-SIM-003 | P-SIM-08 | **Không chốt giải pháp** | Không có observation |
| INT-003 WH-04/06; OBS-002:1–7 | F-SIM-004 | P-SIM-03; P-SIM-04 | N-SIM-02; N-SIM-03 | AS-SIM-02 |
| INT-003 WH-09; INT-001 OWN-07; OBS-003:2–6 | F-SIM-005 | P-SIM-05 | N-SIM-04 | AS-SIM-03 |
| INT-001 OWN-12; INT-002 CAS-03/09 | F-SIM-006 | P-SIM-06 | N-SIM-06 | Nhiều luồng; chưa quan sát |
| INT-002 CAS-05/14 | F-SIM-007 | P-SIM-07 | N-SIM-05 | AS-SIM-01 (interview only for reprint) |
| INT-003 WH-15/17 | F-SIM-008 | P-SIM-04 | N-SIM-08 | AS-SIM-03 |
| INT-001 OWN-17 | F-SIM-009 | Constraint to validate | N-SIM-08 | Không có observation thiết bị |

## Độ tin cậy

| Level | Meaning | Hiện trạng |
|---|---|---|
| S0 | AI-created interview/observation scenario | Có |
| S1 | Interview người thật, chưa đối chiếu | Không |
| S2 | Observation thực địa/artefact thật, đã xin phép | Không |
| S3 | Finding được cửa hàng xác nhận | Không |

Không có căn cứ kết luận các findings đã đạt S1–S3.


---

# Problem–Evidence Matrix (Bước 5)

> **SIMULATED / DỮ LIỆU GIẢ LẬP — KHÔNG PHẢI KHẢO SÁT THỰC TẾ.** Các cuộc phỏng vấn, quan sát, findings và vấn đề đều là kịch bản AI dựng. Tài liệu này chỉ phục vụ luyện tập. Không trình bày như kết luận đã được xác minh tại cửa hàng thật.


**Document ID:** SM-SUR-PS-MATRIX-001 · **Status:** SIMULATED / ALL NOT REAL-WORLD VERIFIED.

| Statement ID | Vấn đề phát biểu | Finding(s) | Problem(s) | Nguồn Bước 3 | Tình trạng |
|---|---|---|---|---|---|
| PS-01a | Đối chiếu số giao / số nhận | F-SIM-004 | P-SIM-03 | INT-003; OBS-002 | S0 SIMULATED |
| PS-01b | Chênh lệch tồn và duyệt điều chỉnh | F-SIM-005 | P-SIM-05 | INT-001; INT-003; OBS-003 | S0 SIMULATED |
| PS-02a | Tra cứu khi quét mã thất bại | F-SIM-002 | P-SIM-02 | INT-002; OBS-001 | S0 SIMULATED |
| PS-02b | Tra cứu/in lại hóa đơn | F-SIM-007 | P-SIM-07 | INT-002 only | S0 SIMULATED |
| PS-03 | Đối soát cuối ngày nhiều bước | F-SIM-001 | P-SIM-01 | INT-001 only; AS-SIM-04 derived | S0 SIMULATED |
| PS-04 | Kiểm soát quyền xử lý ngoại lệ | F-SIM-006 | P-SIM-06 | INT-001; INT-002 only | S0 SIMULATED |
| PS-05 | Kiểm tra nhận chuyển khoản | F-SIM-003 | P-SIM-08 | INT-002 only | S0 SIMULATED |
| CON-01 | Đơn giản, chi phí và thiết bị | F-SIM-008/009 | P-SIM-04, constraint | INT-001; INT-003 only | S0 SIMULATED |

## Cách đọc
- `INT` là lời nhân vật giả lập, `OBS` là nhật ký quan sát giả lập.
- `S0 SIMULATED` = Không đạt bằng chứng khảo sát thực địa.
- Không có kết luận nào về tần suất, mức độ ảnh hưởng định lượng hoặc mức độ ưu tiên chính thức.

## Liên kết với tài liệu chính
- [`problem-statement.md`](problem-statement.md)
- [`../validation/problem-statement-review.md`](../validation/problem-statement-review.md)
