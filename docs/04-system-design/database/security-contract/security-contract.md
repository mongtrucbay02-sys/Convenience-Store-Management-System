# Ma trận quyền và thiết kế migration (PROPOSED)

S=SELECT, I=INSERT, U=UPDATE (bao gồm row lock), D=DELETE. AS-IS là snapshot catalog; NEED là B02 quan sát; TO-BE sau khi tất cả quyết định D1–D7 được duyệt. Không áp dụng tự động. Các bảng khác giữ ACL. Quyền D ứng viên trên SP, giá, tồn, NCC, KH và chi tiết phiếu nhập có điều kiện row policy/guard, không phải xóa mọi dòng.

| Bảng | AS-IS | NEED B02 | TO-BE | Lý do |
|---|---|---|---|---|
| bo_dem_chung_tu | SIU | SIU | SIU | Giữ quyền đã quan sát; kiểm thử lại bằng role thật |
| chinh_sach_doi_tra | SIU | SU | SU | B02-12: bỏ I nếu scope DT-08 được xác nhận; U giữ vì FOR SHARE |
| chuong_trinh_khuyen_mai | SIUD | SIUD | SIUD | Giữ quyền đã quan sát; kiểm thử lại bằng role thật |
| ct_gio_hang | SIUD | SIUD | SIUD | Giữ quyền đã quan sát; kiểm thử lại bằng role thật |
| ct_hoa_don | SI | SI | SI | Giữ quyền đã quan sát; kiểm thử lại bằng role thật |
| ct_kiem_ke | SIU | SIU | SIU | Giữ quyền đã quan sát; kiểm thử lại bằng role thật |
| ct_phieu_doi_tra | SI | SI | SI | Giữ quyền đã quan sát; kiểm thử lại bằng role thật |
| ct_phieu_nhap | SIU | SIUD | SIUD | WR-06: DELETE riêng dòng NHAP; guard I/U/D theo cha |
| ct_xuat_huy | SIU | SIU | SIU | Giữ quyền đã quan sát; kiểm thử lại bằng role thật |
| danh_muc | SIUD | SIUD | SIUD | Giữ quyền đã quan sát; kiểm thử lại bằng role thật |
| giao_dich_tien_mat | SI | SI | SI | Giữ quyền đã quan sát; kiểm thử lại bằng role thật |
| gio_hang | SIUD | SIUD | SIUD | Giữ quyền đã quan sát; kiểm thử lại bằng role thật |
| hoa_don | SIU | SIU | SIU | Giữ quyền đã quan sát; kiểm thử lại bằng role thật |
| hoan_tien_hoa_don | SI | SI | SI | Giữ quyền đã quan sát; kiểm thử lại bằng role thật |
| khach_hang | SIU | SIUD | SIUD | WR-05: DELETE khách không tham chiếu; policy hạn chế + FK |
| khuyen_mai_san_pham | SIUD | SIUD | SIUD | Giữ quyền đã quan sát; kiểm thử lại bằng role thật |
| lich_su_diem | SI | SI | SI | Giữ quyền đã quan sát; kiểm thử lại bằng role thật |
| lich_su_gia | SIU | SIUD | SIUD | WR-01: D ứng viên chỉ dọn giá của SP đủ điều kiện; guard lịch sử chờ duyệt |
| nha_cung_cap | SIU | SIUD | SIUD | WR-04: D ứng viên khi không có phiếu nhập |
| phien_dang_nhap | SIU | SIU | SIU | Giữ quyền đã quan sát; kiểm thử lại bằng role thật |
| phieu_doi_tra | SIU | SIU | SIU | Giữ quyền đã quan sát; kiểm thử lại bằng role thật |
| phieu_kiem_ke | SIU | SIU | SIU | Giữ quyền đã quan sát; kiểm thử lại bằng role thật |
| phieu_nhap | SIU | SIU | SIU | Giữ quyền đã quan sát; kiểm thử lại bằng role thật |
| phieu_xuat_huy | SIU | SIU | SIU | Giữ quyền đã quan sát; kiểm thử lại bằng role thật |
| quy_tac_tich_diem | SIU | S | S | B02-12: bỏ I/U nếu KH-07 xác nhận cắt |
| quy_tien_mat | SIU | SU | SIU | D5: giữ I tới khi seed/repair được xác minh |
| san_pham | SIU | SIUD | SIUD | WR-03: D ứng viên theo SP-06; FK/race guard |
| tai_khoan | SIU | SIU | SIU | Giữ quyền đã quan sát; kiểm thử lại bằng role thật |
| the_kho | SI | SI | SI | Giữ quyền đã quan sát; kiểm thử lại bằng role thật |
| ton_kho | SIU | SIUD | SIUD | WR-02: D ứng viên tồn 0 và SP đủ điều kiện; giữ thẻ kho |
| vai_tro | S | SU | S | WR-07–10: sửa khóa; không mở UPDATE |

## Contract chung
- 31 bảng, 3 views, 20 sequences hiện hữu; không thay schema nghiệp vụ/PK/FK. Owner deployment postgres (không runtime), không object owned by backend. `smartmart_backend` LOGIN, NOINHERIT, NOSUPERUSER, NOBYPASSRLS, NOCREATEROLE, NOCREATEDB, NOREPLICATION; không role memberships outgoing, không grant option. Incoming postgres ADMIN true/SET false/INHERIT false giữ như snapshot, không đụng quản trị platform. Kiểm tra quyền hiệu lực qua PUBLIC, column ACL, memberships; không chỉ đọc relacl.
- Schema smartmart backend USAGE only; PUBLIC/anon/authenticated không USAGE/CREATE. DB CONNECT yes, CREATE no; TEMP giữ hiện trạng để review, không đổi global PUBLIC grant. Schema public và event trigger platform không thay.
- 31 tables ENABLE RLS, FORCE false (owner deployment trusted; không coi owner bypass là runtime protection). Mỗi bảng một permissive ALL `smartmart_backend_trusted_server` TO smartmart_backend USING/WITH CHECK `(CURRENT_USER = 'smartmart_backend'::name)`. Bổ sung restrictive DELETE policies trên KH và CT phiếu nhập; D1 cần guard bổ sung cho bốn bảng SP/NCC. Tổng policy/trigger/function chưa chốt. Không grant cho frontend, không TRUNCATE/REFERENCES/TRIGGER/MAINTAIN, không column grants hay grant option. Với KH policy USING NOT EXISTS 4 bảng tham chiếu (hoa_don, lich_su_diem, gio_hang, phieu_doi_tra); FK vẫn authoritative khi concurrent reference. Không nhận actor ID từ client làm quyền DB; controller/service xác minh AD/TN/NK.
- `ct_phieu_nhap` restrictive DELETE USING EXISTS parent `phieu_nhap.trang_thai='NHAP'`; thêm BEFORE INSERT/UPDATE/DELETE guard invoker, search_path rỗng, schema-qualified, khóa parent và kiểm trạng thái trong giao tác; chặn đổi khóa cha/sản phẩm bằng UPDATE, yêu cầu sửa nháp qua delete+insert nếu cần; không tự sửa phiếu đã nhập/hủy. Trigger thực hiện kiểm tra sau khóa để tránh chỉ dựa vào snapshot RLS; direct child-first có nguy cơ deadlock với app parent-first, abort/retry có giới hạn, test. Chính sách RLS có thể lọc DELETE thành 0 dòng, app phải kiểm row count/EF concurrency, không báo thành công giả. Guard bảo vệ chi tiết, không tự khẳng định header đã bất biến; test transition header và ghi nhận nếu cần guard bổ sung.
- Views v_chuong_trinh_khuyen_mai, v_gia_ban_hien_hanh, v_luot_dung_khuyen_mai: SELECT only, security_invoker=true. 20 sequences theo snapshot USAGE SELECT, no UPDATE/setval. 3 trigger functions hiện có invoker, search_path rỗng, EXECUTE chỉ owner; function mới guard NH cũng vậy. Không grant EXECUTE PUBLIC/anon/authenticated/backend. Trigger đã cài chạy theo cơ chế trigger, test bằng runtime. `public.rls_auto_enable` giữ nguyên (ngoài phạm vi).
- Default privileges: xác định role tạo object thực tế; thu hồi default PUBLIC EXECUTE cho role deployment chỉ nếu blast radius được duyệt (global default không thể đảo bằng per-schema revoke); ưu tiên REVOKE EXECUTE cụ thể trong từng migration cùng transaction. Không grant tự động future objects. Catalog contract được quản lý theo commit, fail on missing/extra object/privilege/policy/role flags, include defaults/grant options/column ACL; không hardcode OID.
- 6 bảng lịch sử SI-only: ct_hoa_don, ct_phieu_doi_tra, giao_dich_tien_mat, hoan_tien_hoa_don, lich_su_diem, the_kho. Giữ 6 append-only triggers; giữ guard hoa_don chỉ hủy hợp lệ. Không xóa bản ghi audit khi rollback.

## Delta đề xuất
23 bảng giữ ACL, 6 bảng thêm DELETE ứng viên, 2 bảng thu hẹp tổng 3 quyền. S/I/U/D: 31/30/24/5 → 31/28/23/11 nếu duyệt đầy đủ. Không mở UPDATE vai_tro. D1 giữ SP-06/NH-03; các D ứng viên chưa được cấp. ACL UPDATE lịch sử giá chưa chứng minh DB chặn sửa giá đã hiệu lực; Huy test trực tiếp, Yến bổ sung guard/column privilege chỉ sau review nếu cần. Header phiếu nhập phải không quay lại NHAP sau xác nhận/hủy; nếu guard chưa có, cần quyết định riêng, cập nhật số policy/trigger/function dự kiến; không tuyên bố bất biến DB khi chưa test.

## Migration, adoption và rollback

**Không phải script SQL thực thi.** Không chạy trên Supabase ở B03. Không sửa PostgreSqlBaseline.sql đã phát hành. Migration mới B08 sau khi PR nghiệp vụ và contract được duyệt; người phụ trách schema duy nhất thực hiện. Không tạo migration ID giả. Môi trường isolated PG17 trước, sau đó backup/restore drill, rollout có maintenance window.

### Preflight
1. Freeze HEAD mới; xác minh B04–B07 và guard B02-10/11 đã review; D1–D7 accepted. Kiểm tra catalog fresh read-only: role, memberships, owner, privileges incl PUBLIC/column/default, policies, schema objects, triggers, view options; compare exact AS-IS; nếu khác hoặc role không tồn tại, FAIL/stop, không grant rộng. Không tự create role login production, không đưa credential vào migration.
2. Kiểm tra adoption EF history và schema contract; live có schema sẵn, không chạy baseline CREATE lại; xác nhận migration ledger/owner bằng thủ tục được duyệt. Role bootstrap tách khỏi EF migration. Backup schema/ACL/role metadata và dữ liệu theo quyền, restore test isolated; lưu hash + phiên bản, không chứa secret. Kiểm tra active writers, drain app/pool, dừng concurrent schema changes. Xác nhận deploy role thực sự có quyền owner.
3. Trên isolated clone, test cả fresh baseline (rộng quyền) và live-like AS-IS (hẹp quyền); delta phải hội tụ cùng target, không sửa migration lịch sử. Không dùng global REVOKE có tác động platform. Kiểm tra thay đổi dữ liệu bằng snapshot row counts, hash chọn lọc không xuất PII, invariants.

### Up — trình tự logic (transactional)
- BEGIN; lock_timeout/statement_timeout có giới hạn; preflight assert version, đúng schema, đúng owner/role, whitelist 31 tables/3 views/20 sequences/functions; fail closed nếu object lạ hoặc grantor/column ACL ngoài contract. Migration chạy bằng owner/deploy role, không runtime.
- Chuẩn hóa policy ALL tên `smartmart_backend_trusted_server`; đối với baseline bỏ `backend_access`, với live replace theo kiểm tra định nghĩa, không để duplicate permissive policies. Bật RLS cho 31 bảng. Thêm restrictive DELETE cho KH/CT phiếu nhập và guard D1 đã duyệt; không cấp D nếu guard chưa hoàn thành. Điều kiện KH NOT EXISTS mọi FK thực tế (4 bảng), kiểm lại FK catalog trước chốt. CT phiếu nhập EXISTS parent NHAP; trigger invoker check parent lock cho I/U/D, reject key mutation, reject non-NHAP; SQLSTATE nghiệp vụ ổn định (23514 hoặc mã đã chọn), test EF mapping.
- Cài function guard trong smartmart, revoke EXECUTE khỏi PUBLIC/anon/authenticated/backend cùng transaction; create trigger bằng owner, không SECURITY DEFINER. Giữ 17 trigger hiện có và 3 function hiện có; số tăng chưa chốt, validate exact names/definitions ở B08.
- Chuẩn hóa ACL bằng explicit object allowlist: thu hồi quyền backend trên từng object đã biết rồi cấp đúng matrix; kiểm tra không còn inherited/PUBLIC/column ACL, quyền grant option. Grant schema USAGE only, views SELECT only, sequences USAGE SELECT only; revoke function EXECUTE khỏi các role không được phép, chỉ trong smartmart. Không đổi role flags/membership production bằng migration khi khác expected: stop và yêu cầu riêng.
- Chỉ revoke ba candidate excess sau D5; giữ I quỹ; nếu chưa duyệt, giữ AS-IS và cập nhật contract tương ứng (không gọi final target PASS). Không thay quyền TEMP, public event trigger, global platform ACL. Không dùng GRANT ON ALL TABLES như target.
- Verify catalog inside transaction, check 31/3/20, expected policies, guards, effective grants, owner, roles, function ACL; rollback nếu mismatch; COMMIT nếu tất cả pass. Đăng ký EF migration history đúng quy trình adoption, không tự insert giả record. Bản code mới và contract cùng bản phát hành tương thích; deploy sau commit, restart pool, smoke read-only then canary app theo quy định. Không chạy write tests trên Supabase dùng chung.

### Down / rollback
- Trước commit: transaction rollback toàn bộ ACL/policy/trigger DDL. Không phải rollback dữ liệu sequence do DML vì migration không chạy DML nghiệp vụ.
- Sau commit: không tự dùng EF Down để quay về baseline CRUD rộng. Tạo rollback delta có review từ snapshot *đúng môi trường* (fresh baseline khác live). Restore ACL/policy/trigger/function definition đã ghi, chỉ nếu compatible với code rollback và invariant; không restore quyền rộng lên live. Nếu không tương thích, giữ maintenance/read-only và forward fix. Xóa trigger mới chỉ sau khi code cũ và bảo vệ nghiệp vụ được đánh giá; không xóa history, không reset/seed, không DROP schema. Không restore DB production tự động; backup restore là phương án sự cố được owner/lead duyệt, đánh giá RPO/RTO và dữ liệu phát sinh sau backup.
- Test Up → verify → Down isolated → verify exact pre-state → re-Up; check seed/data unchanged và migration ledger; simulate lỗi giữa transaction, lock timeout, catalog drift, role missing. Dữ liệu do ứng dụng ghi sau rollout không được đảo bằng DELETE. Nếu rollback code quay lại hard delete/lock cũ không có guard, phải disable route hoặc duy trì compatible ACL được duyệt; không cho production chạy sai contract.

### Gates
B08 blocked tới khi D1–D7 review và B04–B07 code-ready; B09/B10/B11 phải pass trên isolated trước rollout production. Kết quả B03 không cho phép áp migration. Nếu deployment cần thứ tự khác, ghi ADR mới và test compatibility.

## D1 — điều kiện thiết kế, chưa được duyệt
- SP-06/PA-SP-01.B: AD xóa SP chưa có giao dịch; SP-03 tạo sẵn giá đầu và tồn 0. Không coi tồn 0 là đủ. Kiểm toàn bộ FK và tham chiếu nghiệp vụ, khóa theo thứ tự thống nhất với writer, kiểm lại điều kiện và row count, xóa giá/tồn/SP nguyên tử. Giữ thẻ kho và chứng từ. Nếu guard không bảo đảm xóa đúng phạm vi, giữ ACL cũ và chặn luồng xóa; không vô hiệu trigger lịch sử.
- Không cho phép DELETE giá độc lập chỉ vì SP chưa có giao dịch: cần thiết kế phân biệt cleanup SP và lịch sử giá/giá đã hiệu lực; xem xét command giới hạn quyền thay vì D trực tiếp nếu cần. D trên matrix là ứng viên tối đa, không phải quyết định cuối.
- NCC: lock parent, không phiếu nhập ở mọi trạng thái, FK restrict, race với tạo phiếu; chỉ AD qua app.
- D4: WR-08–10 dùng FOR UPDATE OF t; WR-07 advisory xact lock trước COUNT. Audit writer AUTH-06/08/10 và bootstrap, thống nhất protocol hoặc chứng minh invariant; tránh deadlock; không chỉ dựa vào lock bootstrap.
