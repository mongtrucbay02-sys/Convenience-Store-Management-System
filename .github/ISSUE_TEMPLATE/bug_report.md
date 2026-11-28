---
name: "Bug report - QA regression"
about: "Báo lỗi có thể tái hiện, theo dõi trong #264"
title: "[BUG][MODULE] Mô tả triệu chứng"
labels: ["bug"]
assignees: []
---

## Phân loại
- Test case / UC chính thức (nếu có):
- Issue nguồn hoặc gate: #264
- Module: AUTH / SP / NH / KHO / POS / KH / KM / DT / BC / DATABASE / DEVOPS
- Severity đề xuất: Cao (P0/P1) / Trung bình (P2) / Thấp (P3) — người triage xác nhận
- Mức ảnh hưởng: Mất dữ liệu / Sai tiền-tồn / Sai quyền / Chức năng / UI / NFR
- Trạng thái: NEW / TRIAGED / FIXED / RETEST / VERIFIED / CLOSED

## Bối cảnh kiểm thử
- Commit SHA / PR:
- GitHub Actions run + TRX artifact (nếu tự động):
- Môi trường: PostgreSQL 17 **cô lập** / local staging (không test ghi Supabase chung)
- Actor ứng dụng: AD / TN / NK; DB runtime login: smartmart_backend (nếu liên quan security)
- Dataset synthetic / thời gian UTC+07 / trình duyệt:

## Bước tái hiện
1.
2.
3.

**Kỳ vọng theo SRS/contract (dẫn link và phiên bản):**

**Thực tế (HTTP status, SQLSTATE, ảnh/log đã che dữ liệu nhạy cảm):**

**Phạm vi ảnh hưởng / dữ liệu trước-sau:**

## Điều kiện sửa và kiểm chứng lại
- Người xử lý:
- PR sửa và commit:
- Regression test tự động / manual evidence:
- Kết quả retest tại SHA:
- Reviewer xác nhận:
- SLA: lỗi mức **cao** mục tiêu sửa trong **2 ngày** theo kế hoạch gốc #41; theo dõi thời điểm xác nhận severity, không cam kết khi thiếu owner/phê duyệt

**Không đăng** mật khẩu, access token, connection string, cookie phiên, tên/điện thoại khách hàng thật, database dump hoặc log chưa làm sạch. Negative test `42501` đúng expected không phải bug; business write bị từ chối ngoài dự kiến là lỗi cần báo.
