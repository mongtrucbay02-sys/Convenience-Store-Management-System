# F289-03 — danh mục lịch sử của báo cáo BC-03/04/08

Ngày 25/11/2026. **OPEN / design gap; chờ quyết định SRS và duyệt CSDL qua #283.**

## Hiện trạng đã đối chiếu

`ct_hoa_don` lưu tên sản phẩm, giá, giảm giá và giá vốn, nhưng không lưu danh mục lúc bán. Entity `CtHoaDon`, mapping EF và baseline PostgreSQL đều không có trường đó. Adapter hiện nối `san_pham.danh_muc_id` với `danh_muc` để phân loại cả bán và trả. Catalog bao gồm cả sản phẩm lưu để tra cứu lịch sử; không có dữ liệu để khôi phục danh mục cũ từ snapshot hóa đơn.

Test `HistoricalCategoryCurrentlyFollowsCatalogChangeAndHasNoInvoiceSnapshot` chứng minh hành vi hiện tại: sau khi sản phẩm chuyển từ A sang B, số bán kỳ cũ được phân nhóm ở B trong BC-03/04/08. PASS của test này chỉ chứng minh hiện trạng/khoảng thiếu, **không nghiệm thu yêu cầu giữ danh mục lúc bán**.

## Đề xuất cần nhóm chốt

Ưu tiên **danh mục lúc bán** để báo cáo kỳ cũ ổn định khi SP chuyển danh mục. Phương án dùng danh mục hiện tại cũng có thể hợp lệ nếu nhóm chủ đích muốn tái phân loại, nhưng phải ghi rõ trong SRS và trên báo cáo. Bản sửa này giữ hành vi hiện tại, có chú thích trên UI; chưa tự chọn thay nhóm và không thêm migration.

Phân biệt ID danh mục và nhãn: nhóm cần quyết định đổi tên danh mục có thay nhãn lịch sử hay không; lưu ID riêng chưa đảm bảo nhãn bất biến. Hàng trả cần theo phân loại của dòng hóa đơn gốc nếu chọn lịch sử, không theo phân loại sản phẩm ở ngày trả.

## Owner và acceptance gate đề xuất tại #283

- Trung (#266/#289): công thức BC-03/04/08, bộ lọc, detail, test và đối soát.
- Yến / module SP-CSDL: quy tắc chuyển/đổi tên/ẩn danh mục, đánh giá entity, DDL và dữ liệu cũ.
- Thăng / người phụ trách #283: điều phối quyết định SRS, baseline/adoption và gate tích hợp. Việc nhận từng đầu việc cần xác nhận trong issue; tài liệu không tự gán người hay ghi approval.

Chỉ đóng F289-03 khi:

1. Có quyết định dùng danh mục lúc bán hay hiện tại, kèm người/ngày/phạm vi hóa đơn cũ và hàng trả.
2. Nếu dùng lịch sử: thiết kế snapshot ID/nhãn và luồng POS lưu snapshot cùng transaction hóa đơn được review với SP.
3. Migration mới và cập nhật entity/schema được duyệt theo #283 trước khi apply; không sửa migration đã phát hành.
4. Có kế hoạch cho hóa đơn cũ không có snapshot. Không lấy danh mục hiện tại backfill rồi gọi đó là lịch sử đã được xác minh; không thể suy ra dữ liệu cũ nếu thiếu audit nguồn.
5. Regression tạo hóa đơn → chuyển/đổi tên danh mục → xem BC-03/04/08/detail → trả hàng kỳ sau cho kết quả đúng quyết định, CI exact merge snapshot và UAT được xác nhận.

Theo dõi: [#283](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/283), [#289](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/pull/289). Không tự đóng issue hoặc chạy DDL trên Supabase chung trong đợt sửa này.
