# SP-07 - Test Cases: Xem danh sach danh muc

**Issue:** #62
**Trang thai:** Draft
**Loai:** Thiet ke test case, chua chay kiem thu

## 1. Pham vi

- GET /api/v1/categories
- Vai tro: AD, TN, NK
- Danh sach phan trang theo Issue #4
- Hien thi ten, so san pham va trang thai danh muc

## 2. Test cases

| ID         | Dieu kien / Dau vao            | Ket qua mong doi             |
| ---------- | ------------------------------ | ---------------------------- |
| TC-SP07-01 | Dang nhap AD, co danh muc      | HTTP 200, tra danh sach      |
| TC-SP07-02 | Dang nhap TN, co danh muc      | HTTP 200, du lieu theo quyen |
| TC-SP07-03 | Dang nhap NK, co danh muc      | HTTP 200, tra danh sach      |
| TC-SP07-04 | Khong co danh muc              | HTTP 200, items=[], total=0  |
| TC-SP07-05 | Khong truyen page/size         | page=1, size=20              |
| TC-SP07-06 | size=150                       | size=100                     |
| TC-SP07-07 | Chua dang nhap                 | HTTP 401                     |
| TC-SP07-08 | Danh muc co nhieu san pham     | soSanPham dung theo du lieu  |
| TC-SP07-09 | Co danh muc an/ngung hoat dong | Can xac nhan quy tac loc     |

## 3. Test data can chuan bi

- Mot danh muc khong co san pham.
- Mot danh muc co 1 san pham.
- Mot danh muc co nhieu san pham.
- Danh muc o cac trang thai duoc SRS/ERD cho phep.
- Tai khoan AD, TN, NK.

## 4. Phu thuoc

- PR #74: Backend Foundation.
- Issue #76: ADR-001.
- Issue #4: Quy uoc API.
- Issue #16: Test case chung do #4 phu trach.

## 5. Pending

- Quy tac loc danh muc theo trang thai.
- Cach xu ly page <= 0 va size <= 0.
- Test framework va vi tri test project chung.
- Schema va Entity Category sau khi hop nhat ERD.

## 6. Ket qua

Chua thuc thi test. Khong danh dau PASS/FAIL cho den khi co API hoat dong.
