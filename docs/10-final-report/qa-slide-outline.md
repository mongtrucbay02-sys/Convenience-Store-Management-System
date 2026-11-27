# Dàn ý slide kiểm thử và triển khai — #265
Bản nội dung chờ mẫu #6; không phải file PPTX đã nghiệm thu.
1. Mục tiêu: kiểm thử 9 module, tính toàn vẹn và an toàn dữ liệu.
2. Chiến lược: unit, integration PG17, API, UI/E2E, NFR; phân biệt tự động và thủ công.
3. CI: PR → restore → build → publish → PG17 test → verify TRX → artifact.
4. Kết quả thực tế 27/11: 877 PASS, 0 FAIL, 0 SKIP, 73 giây; head cc203365, merge affe1c4. Không gọi đây là 27 Must đã nghiệm thu.
5. Luồng nghiệp vụ: POS → hóa đơn → kho/điểm/quỹ → đổi trả → báo cáo; lịch sử test giao dịch và concurrency, chưa xác minh từng TRX mới.
6. Triển khai: HTTPS reverse proxy → .NET 10 → PG17; backup/restore, schema adoption #283, staging chưa nghiệm thu.
7. Hạn chế và bước tiếp: E2E browser, NFR, UAT, backup, review, F289-03.
## Hỏi đáp
- Vì sao PG17 cô lập? Không ảnh hưởng dữ liệu chung, tái lập giao dịch.
- 100% có nghĩa không lỗi? Không; chỉ 877 test đã thực thi trên checkout xác định.
- CI xanh có tự deploy? Không; workflow hiện build/test, cần gate deploy riêng.
- Rollback? Gói ứng dụng trước đó; dữ liệu theo kế hoạch owner duyệt.
