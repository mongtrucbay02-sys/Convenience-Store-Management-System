# Bước 2 — Survey Plan (Kế hoạch khảo sát)

> **SIMULATED / DỮ LIỆU GIẢ LẬP — KHÔNG PHẢI KHẢO SÁT THỰC TẾ.** Tất cả tên, lời nói, quy trình, số liệu, thời gian và tình huống ở đây là do AI dựng để luyện tập. Không được trình bày như bằng chứng khảo sát thật trong báo cáo.

**ID:** SM-SUR-PLAN-001 · **Version:** 1.0 · **Status:** SIMULATED PLAN — not executed in a real store.  
**Location:** Cửa hàng giả định `S-01` (không có địa chỉ thật).  
**Scenario date:** 21/11/2026, **chỉ là mốc thời gian của kịch bản**.

## 1. Purpose

Tìm hiểu quy trình As-is của một cửa hàng tiện lợi độc lập; nhận diện vấn đề và nhu cầu có căn cứ để chuẩn bị Persona, Scenario, User Stories và Product Vision. Không áp đặt giải pháp trước khi khảo sát.

## 2. Objectives

| Objective ID | Mục tiêu |
|---|---|
| O-01 | Xác định trách nhiệm của ba vai trò và các công việc hàng ngày |
| O-02 | Ghi nhận As-is bán hàng, nhập hàng, kiểm kê |
| O-03 | Xác định công cụ đang sử dụng và nơi lưu thông tin |
| O-04 | Hỏi về ngoại lệ, thao tác lặp và tác động của sai sót |
| O-05 | Tìm nhu cầu/ưu tiên và ràng buộc về thiết bị, kỹ năng, kinh phí |
| O-06 | Phân biệt phát biểu, quan sát và suy luận; ghi câu hỏi chưa rõ |

## 3. Approach

- **Interview:** câu hỏi bán cấu trúc; câu hỏi mở trước, làm rõ sau, không dẫn dắt.
- **Observation:** quan sát công việc tự nhiên; không gây lỗi giả trên thiết bị đang phục vụ khách hàng.
- **Cross-check:** kiểm tra phát biểu giữa các vai trò và giữa lời kể–quan sát.
- **Sampling:** ba vai trò, tương ứng ba người trong diễn tập; với khảo sát thật phải ghi rõ có ai kiêm nhiệm.

## 4. Người tham gia, thời lượng diễn tập

| Mốc giả lập | Hoạt động | Người tham gia |
|---|---|---|
| 13:30–13:40 | Opening / xin phép giả định | ENG-01, P01–P03 |
| 13:40–14:10 | Phỏng vấn chủ cửa hàng | ENG-01, P01 |
| 14:10–14:35 | Phỏng vấn thu ngân | ENG-01, P02 |
| 14:35–15:05 | Phỏng vấn nhân viên kho | ENG-01, P03 |
| 15:05–15:30 | Diễn tập quan sát bán hàng | ENG-01, P02 |
| 15:30–15:55 | Diễn tập quan sát nhập hàng | ENG-01, P03 |
| 15:55–16:15 | Diễn tập quan sát kiểm kê | ENG-01, P03 |
| 16:15–16:30 | Debrief / đối chiếu điểm chưa rõ | ENG-01, P01–P03 |

Thời lượng là thông số diễn tập, không phải số đo của buổi khảo sát đã diễn ra.

## 5. Phân vai thực hiện

| Mã | Trách nhiệm trong diễn tập |
|---|---|
| ENG-01 | Interviewer, observer, note-taker (một người đóng vai kỹ sư phần mềm) |
| P01 | Chủ cửa hàng giả định |
| P02 | Thu ngân giả định |
| P03 | Nhân viên kho giả định |

Trong khảo sát thật, nên phân công thêm một người ghi chép để giảm bỏ sót.

## 6. Công cụ cần chuẩn bị

- [`survey-questionnaire.md`](survey-questionnaire.md)
- [`interviews/interview-template.md`](interviews/interview-template.md)
- [`observations/observation-checklist.md`](observations/observation-checklist.md)
- Đồng hồ, sổ ghi chú, mẫu xin phép; phương tiện ghi âm/chụp ảnh chỉ khi có sự đồng ý riêng.

## 7. Quy tắc đạo đức và dữ liệu

- Xin phép trước khi hỏi/quan sát; có quyền từ chối hoặc dừng bất kỳ lúc nào.
- Không yêu cầu mật khẩu, thông tin thẻ thanh toán, danh tính khách hay thông tin tài chính nhạy cảm.
- Thông tin riêng tư được ẩn danh; chỉ đẩy bản an toàn lên repository công khai.
- `E-SIM-*` **không phải bằng chứng thực địa**; không dùng để nghiệm thu khảo sát thật.

## 8. Rủi ro, phương án xử lý

| Rủi ro | Ứng phó |
|---|---|
| Cửa hàng bận, phỏng vấn bị gián đoạn | Chia thành nhiều buổi, ghi phần chưa hỏi |
| Không có đợt giao hàng đúng thời điểm | Interview-only, không ghi Observed |
| Người tham gia kiêm nhiều vai trò | Tách câu trả lời theo vai trò, ghi nhận kiêm nhiệm |
| Lời kể khác quan sát | Ghi cả hai; xác minh lại chứ không tự chọn |
| Chụp ảnh chứa dữ liệu khách hàng | Không công bố, loại bỏ/ẩn dữ liệu |

## 9. Exit criteria và deliverables

| Deliverable | File |
|---|---|
| Scope | `survey-scope.md` |
| Survey plan | `survey-plan.md` |
| Interview questionnaire | `survey-questionnaire.md` |
| Interview records | `interviews/records/INT-*.md` |
| Observation records | `observations/records/OBS-*.md` |
| Survey execution summary | `survey-execution-record.md` |
| Evidence register | `evidence-register.md` |

**Kết luận:** Diễn tập có thể hoàn tất với dữ liệu giả lập; Exit Criteria khảo sát thực địa **chưa đạt** nếu chưa có hồ sơ và sự đồng ý thực tế.
