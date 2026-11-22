# Product Vision — Smart Mart

> **SIMULATED / INTERNAL DRAFT — NOT FIELD VALIDATED.**

**ID:** SM-PD-VISION-001 · **Version:** 0.1 · **Approval:** PENDING.

## 1. Định vị sản phẩm
- **Tên:** Smart Mart — Convenience Store Management System.
- **Đối tượng:** Một cửa hàng tiện lợi độc lập; Chủ cửa hàng (P01), Thu ngân (P02), Nhân viên kho (P03). Một người có thể kiêm nhiệm vai trò.
- **Vấn đề cốt lõi:** Cần đối chiếu, tra cứu và kiểm soát trạng thái thông tin giữa giao dịch bán hàng, số thực nhận khi nhập hàng, kiểm kê và báo cáo cuối ngày (`PS-01…04`); xác minh chuyển khoản `PS-05` còn mở.
- **Hạn chế:** Thiết bị/công sức vận hành phù hợp quy mô nhỏ (`CON-01`), không có số liệu thời gian, doanh thu, ngân sách thực tế.

## 2. Vision Statement (đề xuất)
> Smart Mart hướng tới một hệ thống quản lý nghiệp vụ trên nền tảng web, gọn và phù hợp với một cửa hàng tiện lợi độc lập, giúp nhân viên bán hàng, người quản lý kho và chủ cửa hàng phối hợp việc tra cứu sản phẩm, ghi nhận giao dịch, nhận hàng, kiểm kê và đối soát trên những dữ liệu có trạng thái và trách nhiệm rõ ràng; ưu tiên tính nhất quán, khả năng tra cứu và sự dễ sử dụng hơn sự phức tạp của hệ thống chuỗi bán lẻ.

## 3. Giá trị kỳ vọng, chưa phải kết quả đo
| Value | Người hưởng lợi | Trace | Cách xác minh về sau |
|---|---|---|---|
| Tìm sản phẩm theo mã hoặc tên | P02 | PS-02 / N-SIM-01 | Thử tình huống không quét được mã |
| Ghi nhận số thực nhận, tách số giao | P03/P01 | PS-01 / N-SIM-02,03 | Thử phiếu 12/10, kiểm chứng biến động tồn |
| Kiểm kê và duyệt chênh lệch | P03/P01 | PS-01,04 / N-SIM-04,06 | Thử tồn 18/đếm 17, chưa được duyệt thì không chỉnh |
| Tìm hóa đơn và đối soát ngày | P02/P01 | PS-02,03 / N-SIM-05,07 | Thử tra cứu và báo cáo bằng dữ liệu test |
| Vận hành đơn giản | P01/P03 | CON-01 / N-SIM-08 | Usability walkthrough trên thiết bị mục tiêu |

## 4. Các nguyên tắc sản phẩm
1. **Single store first**: không thiết kế như hệ thống chuỗi nhiều chi nhánh.
2. **Verified actions**: trạng thái thanh toán, nhận hàng và điều chỉnh tồn không được tự suy diễn từ hành động chưa xác nhận.
3. **Least privilege**: thay đổi giá, hủy hóa đơn đã thanh toán, phê duyệt điều chỉnh cần kiểm soát theo vai trò (chi tiết còn phải xác minh).
4. **Traceable**: mỗi Feature ứng viên có liên kết nhu cầu, và quyết định bổ sung không có nguồn khảo sát phải đánh dấu riêng.
5. **Incremental**: xây luồng end-to-end hữu ích trước, không tự động gom mọi module hiện có vào MVP.

## 5. Ranh giới và chưa quyết định
**Trong phạm vi sản phẩm dự kiến:** sản phẩm và tra cứu, bán hàng tại quầy, nhập hàng thực nhận, quản lý tồn, kiểm kê, quyền cơ bản, hóa đơn và báo cáo đơn giản.  
**Ngoài phạm vi định hướng:** chuỗi đa chi nhánh, điều phối kho phân tán, e-commerce toàn diện, enterprise BI, ngân hàng API chưa có căn cứ.  
**Chưa quyết định:** MoSCoW được khách hàng xác nhận; bộ tính năng MVP; thiết bị cụ thể; yêu cầu offline; quy tắc trả/hoàn tiền; chính sách đơn vị tính; cơ chế thanh toán chuyển khoản.

## 6. Nguồn và trạng thái
- [Problem Statement](../01-survey/analysis/problem-statement.md)
- [Handoff N-SIM-01…08](../01-survey/analysis/transition-to-product-definition.md)
- [Feature Candidates](feature-candidates.md)

**Review:** Owner/lecturer/team pending. Không dùng Vision này như lời cam kết khách hàng.
