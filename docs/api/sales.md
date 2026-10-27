[pos-dt.openapi.yaml](https://github.com/user-attachments/files/33556194/pos-dt.openapi.yaml)
openapi: 3.0.3[PHOI_HOP_AUTH_POS03_POS04.md](https://github.com/user-attachments/files/33556199/PHOI_HOP_AUTH_POS03_POS04.md)
[sales.md](https://github.com/user-attachments/files/33556198/sales.md)
[returns.md](https://github.com/user-attachments/files/33556196/returns.md)

info:
  title: T3-14 Hop dong API POS va DT
  version: 1.0.0-review
  description: 'Theo Issue #4 va phan hoi review T3-14. AUTH: #1 Thang (Issue #5); KH/KM: #5 Trung. POS-03 GET /customers
    cho Trung xac nhan. DT Sau MVP.'
servers:
- url: /api/v1
  description: Cung domain voi React; cookie phien cs_session
tags:
- name: POS
- name: DT
paths:
  /carts:
    post:
      tags:
      - POS
      summary: POS-01 Tạo giỏ
      description: 'Quyen: TN. Ma quyen: BAN_HANG. Uu tien: MVP. Can cu: POS-01; Issue #4 va phan hoi T3-14. '
      responses:
        '201':
          description: Thanh cong; Location header neu 201
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/GioHang'
          headers:
            Location:
              description: Duong dan tai nguyen moi
              schema:
                type: string
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '409':
          $ref: '#/components/responses/Conflict'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/YeuCauTaoGioHang'
      operationId: taoGioHang
      security:
      - cookieAuth: []
        xsrfHeader: []
    get:
      tags:
      - POS
      summary: POS-08 Danh sách giỏ
      description: 'Quyen: TN. Ma quyen: BAN_HANG. Uu tien: Tang 2. Can cu: POS-08; Issue #4 va phan hoi T3-14. '
      responses:
        '200':
          description: Thành công
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      operationId: lietKeGioHangTamGiu
  /carts/{id}/items:
    post:
      tags:
      - POS
      summary: POS-01 Thêm sản phẩm
      description: 'Quyen: TN. Ma quyen: BAN_HANG. Uu tien: MVP. Can cu: POS-01; Issue #4 va phan hoi T3-14. '
      responses:
        '201':
          description: Thanh cong; Location header neu 201
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/GioHang'
          headers:
            Location:
              description: Duong dan tai nguyen moi
              schema:
                type: string
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '409':
          $ref: '#/components/responses/Conflict'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      parameters:
      - name: id
        in: path
        required: true
        schema:
          type: integer
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/YeuCauThemSanPham'
      operationId: themSanPhamVaoGio
      security:
      - cookieAuth: []
        xsrfHeader: []
  /carts/{id}/items/{itemId}:
    put:
      tags:
      - POS
      summary: POS-02 Sửa dòng giỏ
      description: 'Quyen: TN. Ma quyen: BAN_HANG. Uu tien: MVP. Can cu: POS-02; Issue #4 va phan hoi T3-14. '
      responses:
        '200':
          description: Thanh cong; Location header neu 201
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/GioHang'
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '409':
          $ref: '#/components/responses/Conflict'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      parameters:
      - name: id
        in: path
        required: true
        schema:
          type: integer
      - name: itemId
        in: path
        required: true
        schema:
          type: integer
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/YeuCauSuaDongGio'
      operationId: suaDongGio
      security:
      - cookieAuth: []
        xsrfHeader: []
    delete:
      tags:
      - POS
      summary: POS-02 Xóa dòng giỏ
      description: 'Quyen: TN. Ma quyen: BAN_HANG. Uu tien: MVP. Can cu: POS-02; Issue #4 va phan hoi T3-14. '
      responses:
        '204':
          description: Thành công
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '409':
          $ref: '#/components/responses/Conflict'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      parameters:
      - name: id
        in: path
        required: true
        schema:
          type: integer
      - name: itemId
        in: path
        required: true
        schema:
          type: integer
      operationId: xoaDongGio
      security:
      - cookieAuth: []
        xsrfHeader: []
  /carts/{id}/customer:
    put:
      tags:
      - POS
      summary: POS-03 Gắn khách hàng
      description: 'Quyen: TN. Ma quyen: BAN_HANG. Uu tien: MVP. Can cu: POS-03; Issue #4 va phan hoi T3-14.  POS-03:
        GET /customers do #5 Trung so huu, cho xac nhan truong tim kiem.'
      responses:
        '200':
          description: Thanh cong; Location header neu 201
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/GioHang'
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '409':
          $ref: '#/components/responses/Conflict'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      parameters:
      - name: id
        in: path
        required: true
        schema:
          type: integer
      - name: X-XSRF-TOKEN
        in: header
        required: true
        schema:
          type: string
        description: Token chống CSRF từ cookie XSRF-TOKEN
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/YeuCauGanKhachHang'
      operationId: ganKhachHangVaoGio
      security:
      - cookieAuth: []
        xsrfHeader: []
  /carts/{id}/apply-promotions:
    post:
      tags:
      - POS
      summary: POS-04 Áp dụng KM
      description: 'Quyen: TN. Ma quyen: BAN_HANG. Uu tien: Tang 2. Can cu: POS-04; Issue #4 va phan hoi T3-14. '
      responses:
        '200':
          description: Giỏ đã tính lại
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/GioHangResponse'
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '409':
          $ref: '#/components/responses/Conflict'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      parameters:
      - name: id
        in: path
        required: true
        schema:
          type: integer
      - name: X-XSRF-TOKEN
        in: header
        required: true
        schema:
          type: string
        description: Token chống CSRF từ cookie XSRF-TOKEN
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/ApDungKhuyenMaiRequest'
      operationId: apDungKhuyenMai
      security:
      - cookieAuth: []
        xsrfHeader: []
  /carts/{id}/checkout:
    post:
      tags:
      - POS
      summary: POS-07 Hoàn tất đơn
      description: 'Quyen: TN. Ma quyen: BAN_HANG. Uu tien: MVP. Can cu: POS-07; Issue #4 va phan hoi T3-14.  POS-05
        tien thua tinh tam o frontend; backend tinh lai khi checkout. Transaction luu hoa don + bien dong kho, chong
        lap chung tu.'
      responses:
        '200':
          description: Thanh cong; Location header neu 201
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/HoaDon'
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '409':
          description: Xung dot hoac TON_KHONG_DU; details gom sanPhamId, tonHienTai, soLuongYeuCau
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Loi'
              example:
                code: TON_KHONG_DU
                message: Chi con 2 san pham.
                details:
                - sanPhamId: 15
                  tonHienTai: 2
                  soLuongYeuCau: 3
                traceId: 0HN:01
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      parameters:
      - name: id
        in: path
        required: true
        schema:
          type: integer
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/YeuCauHoanTatDon'
      operationId: hoanTatDon
      security:
      - cookieAuth: []
        xsrfHeader: []
  /invoices/{id}/print:
    get:
      tags:
      - POS
      summary: POS-07/11 Dữ liệu in hóa đơn
      description: 'Quyen: TN, AD. Ma quyen: HOA_DON_XEM_CUA_MINH / HOA_DON_XEM_TAT_CA. Uu tien: MVP. Can cu: POS-11;
        Issue #4 va phan hoi T3-14. TN chi xem hoa don do minh lap, lay nguoi dung tu cookie phien; truy cap hoa
        don nguoi khac tra 403. Tra JSON de frontend in K80, khong tra PDF.'
      responses:
        '200':
          description: Thanh cong; Location header neu 201
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/DuLieuInHoaDon'
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      parameters:
      - name: id
        in: path
        required: true
        schema:
          type: integer
      operationId: layDuLieuInHoaDon
  /carts/{id}/hold:
    post:
      tags:
      - POS
      summary: POS-08 Tạm giữ
      description: 'Quyen: TN. Ma quyen: BAN_HANG. Uu tien: Tang 2. Can cu: POS-08; Issue #4 va phan hoi T3-14. '
      responses:
        '200':
          description: Thành công
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '409':
          $ref: '#/components/responses/Conflict'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      parameters:
      - name: id
        in: path
        required: true
        schema:
          type: integer
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              description: DTO cụ thể trong docs/api/sales.md hoặc returns.md
      operationId: tamGiuGio
      security:
      - cookieAuth: []
        xsrfHeader: []
  /carts/{id}/resume:
    post:
      tags:
      - POS
      summary: POS-08 Gọi lại
      description: 'Quyen: TN. Ma quyen: BAN_HANG. Uu tien: Tang 2. Can cu: POS-08; Issue #4 va phan hoi T3-14. '
      responses:
        '200':
          description: Thành công
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '409':
          $ref: '#/components/responses/Conflict'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      parameters:
      - name: id
        in: path
        required: true
        schema:
          type: integer
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              description: DTO cụ thể trong docs/api/sales.md hoặc returns.md
      operationId: goiLaiGio
      security:
      - cookieAuth: []
        xsrfHeader: []
  /carts/{id}/cancel:
    post:
      tags:
      - POS
      summary: POS-09 Hủy giỏ
      description: 'Quyen: TN. Ma quyen: BAN_HANG. Uu tien: MVP. Can cu: POS-09; Issue #4 va phan hoi T3-14. '
      responses:
        '200':
          description: Thanh cong; Location header neu 201
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/GioHang'
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '409':
          $ref: '#/components/responses/Conflict'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      parameters:
      - name: id
        in: path
        required: true
        schema:
          type: integer
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/YeuCauHuyGio'
      operationId: huyGio
      security:
      - cookieAuth: []
        xsrfHeader: []
  /invoices:
    get:
      tags:
      - POS
      summary: POS-10 Tra cứu hóa đơn
      description: 'Quyen: TN, AD. Ma quyen: HOA_DON_XEM_CUA_MINH / HOA_DON_XEM_TAT_CA. Uu tien: MVP. Can cu: POS-10;
        Issue #4 va phan hoi T3-14. TN chi xem hoa don do minh lap, lay nguoi dung tu cookie phien; truy cap hoa
        don nguoi khac tra 403.'
      responses:
        '200':
          description: Thanh cong; Location header neu 201
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/TrangHoaDon'
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      operationId: traCuuHoaDon
      parameters:
      - name: tuNgay
        in: query
        required: false
        schema:
          type: string
          format: date
      - name: denNgay
        in: query
        required: false
        schema:
          type: string
          format: date
      - name: keyword
        in: query
        required: false
        schema:
          type: string
      - name: trangThai
        in: query
        required: false
        schema:
          type: string
      - name: sort
        in: query
        required: false
        schema:
          type: string
      - name: page
        in: query
        required: false
        schema:
          type: integer
          minimum: 1
          default: 1
      - name: size
        in: query
        required: false
        schema:
          type: integer
          minimum: 1
          maximum: 100
          default: 20
  /invoices/{id}:
    get:
      tags:
      - POS
      summary: POS-11 Chi tiết hóa đơn
      description: 'Quyen: TN, AD. Ma quyen: HOA_DON_XEM_CUA_MINH / HOA_DON_XEM_TAT_CA. Uu tien: MVP. Can cu: POS-11;
        Issue #4 va phan hoi T3-14. TN chi xem hoa don do minh lap, lay nguoi dung tu cookie phien; truy cap hoa
        don nguoi khac tra 403.'
      responses:
        '200':
          description: Thanh cong; Location header neu 201
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/HoaDon'
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      parameters:
      - name: id
        in: path
        required: true
        schema:
          type: integer
      operationId: xemChiTietHoaDon
  /invoices/{id}/cancel:
    post:
      tags:
      - POS
      summary: POS-12 Hủy hóa đơn
      description: 'Quyen: AD. Ma quyen: HOA_DON_HUY. Uu tien: Sau MVP. Can cu: POS-12; Issue #4 va phan hoi T3-14. '
      responses:
        '200':
          description: Thành công
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '409':
          $ref: '#/components/responses/Conflict'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      parameters:
      - name: id
        in: path
        required: true
        schema:
          type: integer
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              description: DTO cụ thể trong docs/api/sales.md hoặc returns.md
      operationId: huyHoaDon
      security:
      - cookieAuth: []
        xsrfHeader: []
  /returns:
    post:
      tags:
      - DT
      summary: DT-02 Tạo phiếu trả
      description: 'Quyen: TN. Ma quyen: DOI_TRA. Uu tien: Sau MVP. Can cu: DT-02; Issue #4 va phan hoi T3-14. '
      responses:
        '201':
          description: Thành công
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '409':
          $ref: '#/components/responses/Conflict'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              description: DTO cụ thể trong docs/api/sales.md hoặc returns.md
      operationId: taoPhieuTra
      security:
      - cookieAuth: []
        xsrfHeader: []
    get:
      tags:
      - DT
      summary: DT-07 Danh sách phiếu
      description: 'Quyen: TN, AD. Ma quyen: DOI_TRA / DOI_TRA_DUYET_CAU_HINH. Uu tien: Sau MVP. Can cu: DT-07;
        Issue #4 va phan hoi T3-14. '
      responses:
        '200':
          description: Thành công
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      operationId: lietKePhieuTra
  /returns/{id}/exchange:
    post:
      tags:
      - DT
      summary: DT-03 Đổi hàng
      description: 'Quyen: TN. Ma quyen: DOI_TRA. Uu tien: Sau MVP. Can cu: DT-03; Issue #4 va phan hoi T3-14. '
      responses:
        '200':
          description: Thành công
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '409':
          $ref: '#/components/responses/Conflict'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      parameters:
      - name: id
        in: path
        required: true
        schema:
          type: integer
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              description: DTO cụ thể trong docs/api/sales.md hoặc returns.md
      operationId: doiHang
      security:
      - cookieAuth: []
        xsrfHeader: []
  /returns/{id}/refund:
    post:
      tags:
      - DT
      summary: DT-04 Hoàn tiền
      description: 'Quyen: TN. Ma quyen: DOI_TRA. Uu tien: Sau MVP. Can cu: DT-04; Issue #4 va phan hoi T3-14. '
      responses:
        '200':
          description: Thành công
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '409':
          $ref: '#/components/responses/Conflict'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      parameters:
      - name: id
        in: path
        required: true
        schema:
          type: integer
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              description: DTO cụ thể trong docs/api/sales.md hoặc returns.md
      operationId: hoanTien
      security:
      - cookieAuth: []
        xsrfHeader: []
  /returns/{id}/process-stock:
    post:
      tags:
      - DT
      summary: DT-05 Xử lý hàng trả
      description: 'Quyen: TN. Ma quyen: DOI_TRA. Uu tien: Sau MVP. Can cu: DT-05; Issue #4 va phan hoi T3-14. '
      responses:
        '200':
          description: Thành công
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '409':
          $ref: '#/components/responses/Conflict'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      parameters:
      - name: id
        in: path
        required: true
        schema:
          type: integer
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              description: DTO cụ thể trong docs/api/sales.md hoặc returns.md
      operationId: xuLyTonHangTra
      security:
      - cookieAuth: []
        xsrfHeader: []
  /returns/{id}/approve:
    post:
      tags:
      - DT
      summary: DT-06 Duyệt ngoại lệ
      description: 'Quyen: AD. Ma quyen: DOI_TRA_DUYET_CAU_HINH. Uu tien: Sau MVP. Can cu: DT-06; Issue #4 va phan
        hoi T3-14. '
      responses:
        '200':
          description: Thành công
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '409':
          $ref: '#/components/responses/Conflict'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      parameters:
      - name: id
        in: path
        required: true
        schema:
          type: integer
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              description: DTO cụ thể trong docs/api/sales.md hoặc returns.md
      operationId: duyetDoiTraNgoaiLe
      security:
      - cookieAuth: []
        xsrfHeader: []
  /returns/{id}:
    get:
      tags:
      - DT
      summary: DT-07 Chi tiết phiếu
      description: 'Quyen: TN, AD. Ma quyen: DOI_TRA / DOI_TRA_DUYET_CAU_HINH. Uu tien: Sau MVP. Can cu: DT-07;
        Issue #4 va phan hoi T3-14. '
      responses:
        '200':
          description: Thành công
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      parameters:
      - name: id
        in: path
        required: true
        schema:
          type: integer
      operationId: xemPhieuTra
  /returns/{id}/print:
    get:
      tags:
      - DT
      summary: DT-07 Dữ liệu in
      description: 'Quyen: TN, AD. Ma quyen: DOI_TRA / DOI_TRA_DUYET_CAU_HINH. Uu tien: Sau MVP. Can cu: DT-07;
        Issue #4 va phan hoi T3-14. '
      responses:
        '200':
          description: Thành công
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      parameters:
      - name: id
        in: path
        required: true
        schema:
          type: integer
      operationId: layDuLieuInPhieuTra
  /return-policies:
    get:
      tags:
      - DT
      summary: DT-08 Chính sách
      description: 'Quyen: AD. Ma quyen: DOI_TRA_DUYET_CAU_HINH. Uu tien: Sau MVP. Can cu: DT-08; Issue #4 va phan
        hoi T3-14. '
      responses:
        '200':
          description: Thành công
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      operationId: lietKeChinhSachDoiTra
  /return-policies/{id}:
    put:
      tags:
      - DT
      summary: DT-08 Sửa chính sách
      description: 'Quyen: AD. Ma quyen: DOI_TRA_DUYET_CAU_HINH. Uu tien: Sau MVP. Can cu: DT-08; Issue #4 va phan
        hoi T3-14. '
      responses:
        '200':
          description: Thành công
        '401':
          $ref: '#/components/responses/Unauthorized'
        '403':
          $ref: '#/components/responses/Forbidden'
        '409':
          $ref: '#/components/responses/Conflict'
        '422':
          $ref: '#/components/responses/ValidationError'
        '400':
          $ref: '#/components/responses/BadRequest'
        '404':
          $ref: '#/components/responses/KhongTimThay'
        '500':
          $ref: '#/components/responses/LoiMayChu'
      parameters:
      - name: id
        in: path
        required: true
        schema:
          type: integer
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              description: DTO cụ thể trong docs/api/sales.md hoặc returns.md
      operationId: suaChinhSachDoiTra
      security:
      - cookieAuth: []
        xsrfHeader: []
components:
  securitySchemes:
    cookieAuth:
      type: apiKey
      in: cookie
      name: cs_session
      description: Cookie phien HttpOnly, Secure, SameSite=Strict; 30 phut khong hoat dong
    xsrfHeader:
      type: apiKey
      in: header
      name: X-XSRF-TOKEN
      description: Header cho POST/PUT/DELETE, lay tu cookie XSRF-TOKEN
  responses:
    Unauthorized:
      description: 401 Chưa đăng nhập/hết phiên
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/Loi'
    Forbidden:
      description: 403 Không đủ quyền/bắt buộc đổi mật khẩu
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/Loi'
    Conflict:
      description: 409 Xung đột
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/Loi'
    ValidationError:
      description: 422 Dữ liệu sai nghiệp vụ
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/Loi'
    KhongTimThay:
      description: 404 Khong tim thay
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/Loi'
    LoiMayChu:
      description: 500 Loi may chu
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/Loi'
    BadRequest:
      description: 400 Yeu cau sai cu phap
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/Loi'
  schemas:
    YeuCauGanKhachHang:
      type: object
      required:
      - khachHangId
      - phienBan
      properties:
        khachHangId:
          type: integer
          nullable: true
          example: 8
        phienBan:
          type: integer
          minimum: 1
          example: 3
    YeuCauApDungKhuyenMai:
      type: object
      required:
      - phienBan
      properties:
        phienBan:
          type: integer
          minimum: 1
          example: 4
    GioHang:
      type: object
      required:
      - id
      - chiTiet
      - tongTienHang
      - tienGiam
      - thanhTien
      - phienBan
      - trangThai
      - khachHangId
      properties:
        id:
          type: integer
        khachHangId:
          type: integer
          nullable: true
        chiTiet:
          type: array
          items:
            $ref: '#/components/schemas/DongGio'
        tongTienHang:
          type: integer
        tienGiam:
          type: integer
        thanhTien:
          type: integer
        khuyenMaiApDung:
          type: array
          items:
            type: object
        phienBan:
          type: integer
        trangThai:
          type: string
          description: Enum phai khop ERD T3-13; can doi chieu truoc merge
        thoiDiemTao:
          type: string
          format: date-time
          example: '2026-10-16T08:30:00+07:00'
    Loi:
      type: object
      required:
      - code
      - message
      - details
      - traceId
      properties:
        code:
          type: string
        message:
          type: string
        details:
          type: array
          nullable: true
          items:
            type: object
            additionalProperties: true
          description: null neu khong co chi tiet
        traceId:
          type: string
    LoiTruong:
      type: object
      required:
      - truong
      - loi
      properties:
        truong:
          type: string
          example: chiTiet[0].soLuong
        loi:
          type: string
          example: So luong phai lon hon 0.
    DongGio:
      type: object
      properties:
        id:
          type: integer
          minimum: 1
          example: 1
        sanPhamId:
          type: integer
          minimum: 1
          example: 1
        tenSanPham:
          type: string
          example: Sua tuoi 180ml
        soLuong:
          type: integer
          minimum: 1
          example: 1
        donGia:
          type: integer
          minimum: 0
          example: 1
        tienGiam:
          type: integer
          minimum: 0
          example: 1
        thanhTien:
          type: integer
          minimum: 0
          example: 1
      required:
      - id
      - sanPhamId
      - soLuong
      - donGia
      - thanhTien
    YeuCauTaoGioHang:
      type: object
      properties: {}
      description: Body JSON rong {}; nguoi tao lay tu phien, khong truyen userId
    YeuCauThemSanPham:
      type: object
      properties:
        sanPhamId:
          type: integer
          minimum: 1
          example: 1
        soLuong:
          type: integer
          minimum: 1
          example: 1
        phienBan:
          type: integer
          minimum: 1
          example: 1
      required:
      - sanPhamId
      - soLuong
      - phienBan
    YeuCauSuaDongGio:
      type: object
      properties:
        soLuong:
          type: integer
          minimum: 1
          example: 1
        phienBan:
          type: integer
          minimum: 1
          example: 1
      required:
      - soLuong
      - phienBan
    YeuCauHuyGio:
      type: object
      properties:
        lyDo:
          type: string
          nullable: true
          example: Khach doi y
        phienBan:
          type: integer
          minimum: 1
          example: 1
      required:
      - phienBan
    YeuCauHoanTatDon:
      type: object
      properties:
        tienKhachDua:
          type: integer
          minimum: 0
          example: 1
        phienBan:
          type: integer
          minimum: 1
          example: 1
      required:
      - tienKhachDua
      - phienBan
      description: 'POS-05 va POS-07: chi tien mat; may chu tinh lai thanhTien va tienThua. Khong nhan tienThua
        tu frontend.'
    ChiTietHoaDon:
      type: object
      properties:
        id:
          type: integer
          minimum: 1
          example: 1
        sanPhamId:
          type: integer
          minimum: 1
          example: 1
        tenSanPham:
          type: string
          example: Sua tuoi 180ml
        soLuong:
          type: integer
          minimum: 1
          example: 1
        donGia:
          type: integer
          minimum: 0
          example: 1
        tienGiam:
          type: integer
          minimum: 0
          example: 1
        thanhTien:
          type: integer
          minimum: 0
          example: 1
      required:
      - id
      - sanPhamId
      - soLuong
      - donGia
      - thanhTien
    HoaDon:
      type: object
      properties:
        id:
          type: integer
          minimum: 1
          example: 1
        ma:
          type: string
          example: HD-20261016-001
        trangThai:
          type: string
          example: DA_THANH_TOAN
        khachHangId:
          type: integer
          nullable: true
        nguoiTaoId:
          type: integer
          minimum: 1
          example: 1
        chiTiet:
          type: array
          items:
            $ref: '#/components/schemas/ChiTietHoaDon'
        tongTienHang:
          type: integer
          minimum: 0
          example: 1
        tienGiam:
          type: integer
          minimum: 0
          example: 1
        thanhTien:
          type: integer
          minimum: 0
          example: 1
        tienKhachDua:
          type: integer
          minimum: 0
          example: 1
        tienThua:
          type: integer
          minimum: 0
          example: 1
        thoiDiemTao:
          type: string
          format: date-time
          example: '2026-10-16T08:30:00+07:00'
      required:
      - id
      - ma
      - trangThai
      - nguoiTaoId
      - chiTiet
      - tongTienHang
      - tienGiam
      - thanhTien
      - tienKhachDua
      - tienThua
      - thoiDiemTao
    TrangHoaDon:
      type: object
      properties:
        items:
          type: array
          items:
            $ref: '#/components/schemas/HoaDon'
        total:
          type: integer
          minimum: 0
          example: 1
        page:
          type: integer
          minimum: 1
          example: 1
        size:
          type: integer
          minimum: 1
          example: 1
      required:
      - items
      - total
      - page
      - size
    DuLieuInHoaDon:
      type: object
      properties:
        hoaDon:
          $ref: '#/components/schemas/HoaDon'
        khoGiay:
          type: string
          enum:
          - K80
          example: K80
      required:
      - hoaDon
      - khoGiay
      description: JSON, frontend dung CSS K80 va window.print()
    TrangGioHang:
      type: object
      properties:
        items:
          type: array
          items:
            $ref: '#/components/schemas/GioHang'
        total:
          type: integer
          minimum: 0
          example: 1
        page:
          type: integer
          minimum: 1
          example: 1
        size:
          type: integer
          minimum: 1
          example: 1
      required:
      - items
      - total
      - page
      - size
security:
- cookieAuth: []

# T3-14 — Hợp đồng API DT (Đổi trả)

> **Trạng thái:** Đề xuất để review; chưa phải API đã triển khai. Căn cứ: Issue #10 (phạm vi), Issue #4 (quy ước chung, phiên bản 1.0 ngày 16/10/2026). Đường dẫn/DTO chưa có trong Issue #4 được đánh dấu **ĐỀ XUẤT** và cần người phụ trách duyệt. Không tạo API AUTH thay cho #1 Thăng (Issue #5); module KH/KM do #5 Trung phụ trách.

## Quy ước áp dụng

- REST + JSON UTF-8; prefix `/api/v1`; URL tiếng Anh số nhiều/kebab-case; không dùng PATCH.
- JSON camelCase tiếng Việt không dấu; `id` số nguyên, tiền VND số nguyên, thời điểm ISO 8601 `+07:00`, enum `CHU_HOA_KHONG_DAU`.
- Bản ghi đơn trả thẳng object; danh sách `{ "items": [], "total": 0, "page": 1, "size": 20 }`.
- Thành công: GET/PUT/POST chuyển trạng thái 200; POST tạo 201 + Location; DELETE xóa thật 204.
- Lỗi: `{ "code": "...", "message": "...", "details": null, "traceId": "..." }`; 400 cú pháp, 401 chưa đăng nhập/hết phiên, 403 không quyền, 404 không tìm thấy, 409 xung đột, 422 sai nghiệp vụ, 500 lỗi máy chủ.
- Cookie phiên `HttpOnly; Secure; SameSite=Strict`; hết hạn sau 30 phút không hoạt động; không dùng localStorage cho token. POST/PUT/DELETE bắt buộc `X-XSRF-TOKEN` từ cookie `XSRF-TOKEN`.
- Quyền theo ma trận SRS và phản hồi review: `TN` thu ngân, `AD` quản lý, `NK` kho. POS-01..09 chỉ TN (`BAN_HANG`); POS-10/11 TN xem của mình (`HOA_DON_XEM_CUA_MINH`), AD xem tất cả (`HOA_DON_XEM_TAT_CA`); POS-12 chỉ AD (`HOA_DON_HUY`). DT-01..05 TN (`DOI_TRA`), DT-06/08 AD (`DOI_TRA_DUYET_CAU_HINH`), DT-07 TN/AD (đề xuất của #1).
- Dữ liệu `phienBan` cần thiết khi bản ghi có nhiều người cùng sửa; nếu sai trả `409 XUNG_DOT_DU_LIEU`. Không cho tạo chứng từ trùng do gửi lại.
- Mọi thay đổi tồn kho phải gọi dịch vụ biến động kho trong **cùng giao tác CSDL** với lưu chứng từ; lỗi phải rollback toàn bộ.

## 1. Phạm vi

Issue #10 giao DT-01 đến DT-08, nhưng Issue #4 đánh dấu `/returns` **Sau MVP**. Vì vậy tài liệu này ghi danh sách endpoint và quy tắc nghiệp vụ để thống nhất trước; chưa coi là cam kết triển khai tuần 3. **ĐỀ XUẤT:** tài liệu đặt `docs/api/returns.md` (cần #1 xác nhận tên file vì Issue #4 chưa liệt kê `returns.md`).

## 2. Danh sách endpoint (đề xuất, sau MVP)

| UC | HTTP + đường dẫn (bỏ `/api/v1`) | Quyền đề xuất | Thành công | Ghi chú |
|---|---|---|---|---|
| DT-01 | `GET /invoices?keyword=...` | TN (`DOI_TRA`) | 200 | Tra hóa đơn gốc bằng mã; tái sử dụng POS |
| DT-01 | `GET /invoices/{id}` | TN (`DOI_TRA`) | 200 | Lấy dòng và số lượng đã trả |
| DT-02 | `POST /returns` | TN (`DOI_TRA`) | 201 | Tạo phiếu trả; Location `/api/v1/returns/{id}` |
| DT-03 | `POST /returns/{id}/exchange` | TN (`DOI_TRA`) | 200 | Đổi hàng, liên kết hóa đơn mới |
| DT-04 | `POST /returns/{id}/refund` | TN (`DOI_TRA`) | 200 | Ghi nhận hoàn tiền mặt |
| DT-05 | `POST /returns/{id}/process-stock` | TN (`DOI_TRA`) | 200 | Nhập lại hoặc xuất hủy qua dịch vụ kho |
| DT-06 | `POST /returns/{id}/approve` | AD (`DOI_TRA_DUYET_CAU_HINH`) | 200 | Duyệt ngoại lệ; cần thêm `/reject` nếu SRS yêu cầu |
| DT-07 | `GET /returns` | TN/AD (`DOI_TRA` / `DOI_TRA_DUYET_CAU_HINH`) | 200 | `{items,total,page,size}` |
| DT-07 | `GET /returns/{id}` | TN/AD (`DOI_TRA` / `DOI_TRA_DUYET_CAU_HINH`) | 200 | Chi tiết phiếu |
| DT-07 | `GET /returns/{id}/print` | TN/AD (`DOI_TRA` / `DOI_TRA_DUYET_CAU_HINH`) | 200 | **ĐỀ XUẤT** dữ liệu in |
| DT-08 | `GET /return-policies` | AD (`DOI_TRA_DUYET_CAU_HINH`) | 200 | Chính sách đổi trả, tên tài nguyên mới cần #1 duyệt |
| DT-08 | `PUT /return-policies/{id}` | AD (`DOI_TRA_DUYET_CAU_HINH`) | 200 | Cập nhật kèm `phienBan` |

## 3. Dữ liệu ví dụ (đề xuất để #1/#6 xác nhận)

### DT-02 `POST /api/v1/returns`

```json
{
  "hoaDonGocId": 90,
  "chiTiet": [{ "hoaDonChiTietId": 301, "soLuong": 1, "lyDo": "Hàng lỗi" }],
  "ghiChu": null
}
```

`201 Created`, Location `/api/v1/returns/12`:

```json
{
  "id": 12, "ma": "TH-20261016-001", "hoaDonGocId": 90,
  "chiTiet": [{ "hoaDonChiTietId": 301, "soLuong": 1, "soTienDuKienHoan": 8000 }],
  "tongTienHoan": 8000, "trangThai": "MOI_TAO", "phienBan": 1,
  "thoiDiemTao": "2026-10-16T09:00:00+07:00"
}
```

`MOI_TAO` là enum **đề xuất**, chưa xác nhận với ERD. Máy chủ kiểm tra hóa đơn gốc, thời hạn, số lượng trả lũy kế và giá trị hoàn theo quy tắc đã chốt. Lỗi: 404 `KHONG_TIM_THAY`; 409 `DA_DUOC_XU_LY`; 422 `DU_LIEU_KHONG_HOP_LE`. Nếu cần mã lỗi riêng, phải đăng ký Issue #4 mục 8.4 trước.

### DT-03 `POST /api/v1/returns/{id}/exchange`

```json
{ "sanPhamMoi": [{ "sanPhamId": 22, "soLuong": 1 }], "phienBan": 1 }
```

Máy chủ kiểm tra giá trị hàng mới không thấp hơn hàng trả theo quy tắc DT-03; tạo hóa đơn mới và biến động kho trong cùng giao tác. Cần chốt cách ghi nhận phần chênh lệch tiền.

### DT-04 `POST /api/v1/returns/{id}/refund`

```json
{ "hinhThucHoan": "TIEN_MAT", "phienBan": 2 }
```

Số tiền hoàn do máy chủ tính; chống hoàn hai lần (`409 DA_DUOC_XU_LY`); lưu giao dịch tiền mặt.

### DT-05 `POST /api/v1/returns/{id}/process-stock`

```json
{ "xuLy": "NHAP_LAI", "phienBan": 3 }
```

`NHAP_LAI`/`XUAT_HUY` là giá trị **đề xuất** cần chốt. Mọi thay đổi tồn qua dịch vụ kho, cùng transaction với phiếu trả; không ghi trực tiếp tồn kho.

### DT-06 `POST /api/v1/returns/{id}/approve`

```json
{ "lyDo": "Duyệt ngoại lệ quá hạn", "phienBan": 4 }
```

Chỉ AD; kiểm tra trạng thái `CHO_DUYET` (nếu enum này được ERD DT chốt). Trả `409 DA_DUOC_XU_LY` nếu phiếu đã duyệt/từ chối.

### DT-08 `PUT /api/v1/return-policies/{id}`

```json
{ "soNgayChoPhep": 7, "nguongCanDuyet": 500000, "phienBan": 1 }
```

`200 OK`: trả chính sách đã cập nhật, `phienBan` mới. Các giá trị 7 ngày và 500.000 VND là mặc định theo yêu cầu DT đã trao đổi, cần đối chiếu bản SRS/ERD chính thức trước khi triển khai.

## 4. Quy tắc nghiệp vụ và phối hợp

- Bắt buộc hóa đơn gốc; số lượng trả không vượt `soLuongDaMua - soLuongDaTraTruoc`.
- Áp dụng hạn đổi trả mặc định 7 ngày; ngoại lệ và ngưỡng phê duyệt 500.000 VND cần xác nhận cách tính với #1/#6.
- Không hoàn tiền hoặc nhập kho hai lần khi mạng gửi lại; dùng kiểm tra trạng thái + `phienBan`.
- Khi hoàn tiền, đổi hàng hoặc nhập lại tồn, ghi chứng từ và biến động liên quan trong cùng transaction.
- DT tái sử dụng `/invoices` của POS; không sao chép dữ liệu khách hàng hay viết API AUTH riêng.
- Lỗi, cookie phiên, CSRF và phân quyền giống Issue #4; không đưa token vào request body.

## 5. Cần trưởng nhóm xác nhận

1. DT-01 đến DT-08 có cần chi tiết ngay tuần 3 hay chỉ bảng do `/returns` là Sau MVP?
2. Tên bảng/enum DT theo ERD đã hợp nhất; ai có quyền duyệt ngoại lệ?
3. Thời hạn 7 ngày tính từ ngày mua hay thời điểm thanh toán; ngưỡng 500.000 áp dụng theo phiếu hay hóa đơn?
4. Cách xử lý hoàn tiền khi hóa đơn đã giảm giá, tích điểm, hoặc đã trả một phần.
5. Tên tài nguyên `/return-policies` và endpoint in có cần bổ sung vào Issue #4 không?

> Theo phản hồi #1 Thăng: toàn bộ DT thuộc **Sau MVP**; chỉ cần bảng endpoint, DTO chi tiết chưa bắt buộc. DT-07 TN/AD là đề xuất phân quyền của #1, chờ thống nhất ma trận.

# T3-14 — Hợp đồng API POS (Bán hàng)

> **Trạng thái:** Đề xuất để review; chưa phải API đã triển khai. Căn cứ: Issue #10 (phạm vi), Issue #4 (quy ước chung, phiên bản 1.0 ngày 16/10/2026). Đường dẫn/DTO chưa có trong Issue #4 được đánh dấu **ĐỀ XUẤT** và cần người phụ trách duyệt. Không tạo API AUTH thay cho #1 Thăng (Issue #5); module KH/KM do #5 Trung phụ trách.

## Quy ước áp dụng

- REST + JSON UTF-8; prefix `/api/v1`; URL tiếng Anh số nhiều/kebab-case; không dùng PATCH.
- JSON camelCase tiếng Việt không dấu; `id` số nguyên, tiền VND số nguyên, thời điểm ISO 8601 `+07:00`, enum `CHU_HOA_KHONG_DAU`.
- Bản ghi đơn trả thẳng object; danh sách `{ "items": [], "total": 0, "page": 1, "size": 20 }`.
- Thành công: GET/PUT/POST chuyển trạng thái 200; POST tạo 201 + Location; DELETE xóa thật 204.
- Lỗi: `{ "code": "...", "message": "...", "details": null, "traceId": "..." }`; 400 cú pháp, 401 chưa đăng nhập/hết phiên, 403 không quyền, 404 không tìm thấy, 409 xung đột, 422 sai nghiệp vụ, 500 lỗi máy chủ.
- Cookie phiên `HttpOnly; Secure; SameSite=Strict`; hết hạn sau 30 phút không hoạt động; không dùng localStorage cho token. POST/PUT/DELETE bắt buộc `X-XSRF-TOKEN` từ cookie `XSRF-TOKEN`.
- Quyền theo ma trận SRS và phản hồi review: `TN` thu ngân, `AD` quản lý, `NK` kho. POS-01..09 chỉ TN (`BAN_HANG`); POS-10/11 TN xem của mình (`HOA_DON_XEM_CUA_MINH`), AD xem tất cả (`HOA_DON_XEM_TAT_CA`); POS-12 chỉ AD (`HOA_DON_HUY`). DT-01..05 TN (`DOI_TRA`), DT-06/08 AD (`DOI_TRA_DUYET_CAU_HINH`), DT-07 TN/AD (đề xuất của #1).
- Dữ liệu `phienBan` cần thiết khi bản ghi có nhiều người cùng sửa; nếu sai trả `409 XUNG_DOT_DU_LIEU`. Không cho tạo chứng từ trùng do gửi lại.
- Mọi thay đổi tồn kho phải gọi dịch vụ biến động kho trong **cùng giao tác CSDL** với lưu chứng từ; lỗi phải rollback toàn bộ.

## 1. Quyết định thiết kế cần chốt

**ĐÃ ĐƯỢC #1 THĂNG XÁC NHẬN:** lưu giỏ hàng tại máy chủ (`/carts`) để hỗ trợ tạm giữ/gọi lại POS-08, một thu ngân có tối đa một giỏ đang bán. Trạng thái dự kiến `DANG_BAN`, `TAM_GIU`, `DA_HOAN_TAT`, `DA_HUY`; xác nhận tên enum với ERD trước khi code. Giỏ tạm giữ và giỏ hoàn tất phải có cơ chế tránh xử lý trùng.

**Phối hợp #5 Trung (KH/KM):** POS-03 chỉ tham chiếu `khachHangId`, tra cứu qua `GET /api/v1/customers?keyword=...` do #5 Trung sở hữu; POS-04 cần đầu vào/chính sách từ module khuyến mãi, nhưng không giả định #5 đã triển khai `/promotions` (Issue #4 ghi KM thuộc tầng 2). Cả hai cần xác nhận contract trước khi merge.

## 2. Bảng endpoint POS

| UC | HTTP + đường dẫn (bỏ prefix `/api/v1`) | Quyền đề xuất | Thành công | Ghi chú |
|---|---|---|---|---|
| POS-01 | `POST /carts` | TN (`BAN_HANG`) | 201 | Tạo giỏ; Location `/api/v1/carts/{id}` |
| POS-01 | `GET /products?keyword=...` | TN (`BAN_HANG`) | 200 | Dùng API SP của #2, không tạo API tìm sản phẩm trùng |
| POS-01 | `POST /carts/{id}/items` | TN (`BAN_HANG`) | 201 | Thêm dòng; Location của dòng |
| POS-02 | `PUT /carts/{id}/items/{itemId}` | TN (`BAN_HANG`) | 200 | Sửa số lượng; gửi `phienBan` |
| POS-02 | `DELETE /carts/{id}/items/{itemId}` | TN (`BAN_HANG`) | 204 | Xóa dòng thật |
| POS-03 | `PUT /carts/{id}/customer` | TN (`BAN_HANG`) | 200 | Gắn/bỏ khách hàng; tham chiếu #5 |
| POS-04 | `POST /carts/{id}/apply-promotions` | TN (`BAN_HANG`) | 200 | Tầng 2 —  Tính lại ưu đãi phía máy chủ |
| POS-07 | `POST /carts/{id}/checkout` | TN (`BAN_HANG`) | 200 | Chuyển trạng thái, tạo hóa đơn + biến động kho trong 1 transaction |
| POS-07 | `GET /invoices/{id}/print` | TN (`BAN_HANG`) | 200 | **ĐỀ XUẤT:** dữ liệu in (JSON); nếu xuất PDF cần chốt kiểu trả về |
| POS-08 | `POST /carts/{id}/hold` | TN (`BAN_HANG`) | 200 | Tầng 2 —  Tạm giữ |
| POS-08 | `POST /carts/{id}/resume` | TN (`BAN_HANG`) | 200 | Tầng 2 —  Gọi lại |
| POS-08 | `GET /carts?trangThai=TAM_GIU` | TN (`BAN_HANG`) | 200 | Tầng 2 —  Danh sách giỏ tạm giữ, phân trang |
| POS-09 | `POST /carts/{id}/cancel` | TN (`BAN_HANG`) | 200 | Hủy giỏ bằng chuyển trạng thái, không DELETE |
| POS-10 | `GET /invoices` | TN (của mình), AD (tất cả) | 200 | Phân trang, lọc ngày/trạng thái |
| POS-11 | `GET /invoices/{id}` | TN (của mình), AD (tất cả) | 200 | Xem chi tiết; in lại dùng endpoint in ở trên |
| POS-12 | `POST /invoices/{id}/cancel` | AD (`HOA_DON_HUY`) | 200 | Sau MVP —  Chỉ theo quyền/điều kiện SRS; đảo biến động kho trong transaction |

> POS-06 đã bỏ. Các đường dẫn thao tác (`cash-payment`, `checkout`, `hold`, `resume`, `apply-promotions`, `print`) là **đề xuất**, cần cập nhật bảng tài nguyên/hành động trong Issue #4 nếu trưởng nhóm yêu cầu.

## 3. DTO và ví dụ hợp đồng chính

### POS-01 — `POST /api/v1/carts`

Yêu cầu: `{}`. Phản hồi `201 Created`, header `Location: /api/v1/carts/21`:

```json
{
  "id": 21, "trangThai": "DANG_BAN", "khachHangId": null,
  "chiTiet": [], "tongTienHang": 0, "tienGiam": 0, "thanhTien": 0,
  "phienBan": 1, "thoiDiemTao": "2026-10-16T08:30:00+07:00"
}
```

### POS-01 — `POST /api/v1/carts/{id}/items`

```json
{ "sanPhamId": 15, "soLuong": 2, "phienBan": 1 }
```

`201 Created`: trả cả giỏ (`GioHang`) sau khi thêm dòng, gồm `chiTiet`, `thanhTien`, `phienBan` mới; trước đây trả dòng giỏ mới `{ "id": 32, "sanPhamId": 15, "soLuong": 2, "donGia": 8000, "thanhTien": 16000 }` và `Location: /api/v1/carts/21/items/32`. Máy chủ lấy giá hiện hành, không nhận `donGia` từ trình duyệt. Lỗi: 404 `KHONG_TIM_THAY`; 409 `TON_KHONG_DU`, `XUNG_DOT_DU_LIEU`; 422 `DU_LIEU_KHONG_HOP_LE`.

### POS-02 — `PUT /api/v1/carts/{id}/items/{itemId}`

```json
{ "soLuong": 3, "phienBan": 2 }
```

`200 OK`: trả giỏ mới đã tính lại toàn bộ tổng tiền, `phienBan` tăng 1. `soLuong` nguyên dương; muốn bỏ dòng dùng DELETE. Kiểm tra tồn tại thời điểm sửa và **kiểm tra lại khi checkout**. Lỗi: 409 `TON_KHONG_DU`, `XUNG_DOT_DU_LIEU`.

### POS-03 — `PUT /api/v1/carts/{id}/customer`

```json
{ "khachHangId": 8, "phienBan": 3 }
```

`khachHangId: null` để bỏ gắn khách hàng. Trước đó frontend dùng API **thuộc #5 Trung** `GET /api/v1/customers?keyword=090...` với cấu trúc danh sách chuẩn; backend POS kiểm tra khách hàng tồn tại và được phép dùng, rồi **tính lại ưu đãi** nếu phụ thuộc khách hàng. `200 OK`: trả giỏ mới, `khachHangId`, `thanhTien`, `phienBan` mới. Lỗi: 404 `KHONG_TIM_THAY`; 409 `XUNG_DOT_DU_LIEU`; 422 `DU_LIEU_KHONG_HOP_LE`.

**Chưa chốt với #5:** tìm bằng `keyword` có hỗ trợ số điện thoại không; tên trường trả về (`soDienThoai`, `tenKhachHang`); trường hợp khách chưa đăng ký; chính sách điểm thành viên. POS không tự tạo endpoint `/customers`.

### POS-04 — `POST /api/v1/carts/{id}/apply-promotions`

```json
{ "phienBan": 4 }
```

`200 OK` (ví dụ):

```json
{
  "id": 21, "khachHangId": 8,
  "chiTiet": [{ "id": 32, "sanPhamId": 15, "soLuong": 2, "donGia": 8000, "tienGiam": 1000, "thanhTien": 15000 }],
  "tongTienHang": 16000, "tienGiam": 1000, "thanhTien": 15000,
  "khuyenMaiApDung": [{ "id": 3, "ma": "KM-01", "soTienGiam": 1000 }],
  "phienBan": 5
}
```

Máy chủ xác định khuyến mãi hợp lệ theo thời điểm, khách hàng, sản phẩm và quy tắc ưu tiên; không tin `tienGiam` do client gửi. Không để tiền giảm vượt giá trị dòng hàng; không để `thanhTien` âm. Nếu không có KM hợp lệ: danh sách `khuyenMaiApDung=[]`, tiền giảm 0 (không coi là lỗi). **Cần #5 xác nhận** nguồn dữ liệu KM, nguyên tắc chọn ưu đãi tốt nhất, có cho cộng dồn không, và thời điểm tính lại (khi gắn khách, sửa giỏ, checkout).

### POS-05 — Thanh toán tiền mặt (trong checkout POS-07)

Không tạo endpoint `cash-payment`. Giao diện tính tạm `tienThua` để hiển thị; `POST /api/v1/carts/{id}/checkout` nhận `tienKhachDua` và `phienBan`, máy chủ kiểm tra số tiền, tính lại và trả hóa đơn.

### POS-07 — `POST /api/v1/carts/{id}/checkout`

```json
{ "hinhThucThanhToan": "TIEN_MAT", "tienKhachDua": 20000, "phienBan": 5 }
```

`200 OK` (hành động chuyển trạng thái theo Issue #4):

```json
{
  "id": 90, "ma": "HD-20261016-001", "gioHangId": 21,
  "khachHangId": 8, "tongTienHang": 16000, "tienGiam": 1000,
  "thanhTien": 15000, "tienKhachDua": 20000, "tienThua": 5000,
  "trangThai": "DA_THANH_TOAN", "thoiDiemTao": "2026-10-16T08:40:00+07:00"
}
```

Trong **một transaction**: khóa/kiểm tra trạng thái giỏ, kiểm tra tồn, tính lại giá/khuyến mãi, tạo hóa đơn + chi tiết + thanh toán, ghi biến động kho `BAN_HANG`, cập nhật điểm khách hàng nếu có; commit khi tất cả thành công. Nếu bất kỳ bước nào lỗi: rollback. Lỗi: 409 `TON_KHONG_DU`, `DA_DUOC_XU_LY`, `XUNG_DOT_DU_LIEU`; 422 `DU_LIEU_KHONG_HOP_LE`. Không tạo 2 hóa đơn khi nhấn nút hai lần.

### POS-08/09 — Giữ, gọi lại, hủy giỏ

- `POST /carts/{id}/hold` với `{ "phienBan": 6 }`: `DANG_BAN` → `TAM_GIU`.
- `POST /carts/{id}/resume` với `{ "phienBan": 7 }`: `TAM_GIU` → `DANG_BAN`; không vi phạm giới hạn một giỏ đang bán/thu ngân.
- `POST /carts/{id}/cancel` với `{ "phienBan": 8, "lyDo": "Khách không mua" }`: → `DA_HUY`; không sinh hóa đơn, không đổi tồn.
- `409 DA_DUOC_XU_LY` nếu giỏ không ở trạng thái cho phép.

### POS-10/11 — Tra cứu/in lại

`GET /api/v1/invoices?page=1&size=20&tuNgay=2026-10-01&denNgay=2026-10-16&sort=-thoiDiemTao` trả `{items,total,page,size}`. `GET /invoices/{id}` trả hóa đơn và `chiTiet`; `GET /invoices/{id}/print` trả dữ liệu cần in (đề xuất, không tạo chứng từ mới). Ngày lọc tính cả hai đầu theo +07:00.

### POS-12 — `POST /api/v1/invoices/{id}/cancel`

```json
{ "lyDo": "Hóa đơn nhập sai", "phienBan": 1 }
```

`200 OK`: hóa đơn trạng thái `DA_HUY` và phiên bản mới. Quyền AD **đề xuất**, phải đối chiếu SRS. Nếu đã đổi/trả liên quan, cần quy tắc chặn hoặc xử lý bù trừ được trưởng nhóm xác nhận. Đảo tồn qua dịch vụ kho với loại `HUY_HOA_DON` trong cùng transaction; không xóa hóa đơn vật lý. Lỗi 409 `DA_DUOC_XU_LY`, `XUNG_DOT_DU_LIEU`.

## 4. Quy tắc frontend phối hợp AUTH (#5)

- Dùng `fetch('/api/v1/...', { credentials: 'same-origin', ... })` cùng domain; không thêm Bearer token.
- Với POST/PUT/DELETE, đọc cookie `XSRF-TOKEN` và gắn header `X-XSRF-TOKEN`; cookie phiên `HttpOnly` không đọc bằng JS.
- `401 HET_PHIEN` → chuyển trang đăng nhập, giữ URL quay lại; `403 PHAI_DOI_MAT_KHAU` → màn đổi mật khẩu; `403 KHONG_DU_QUYEN` → thông báo không có quyền.
- `409 TON_KHONG_DU` → hiển thị tồn mới; `409 XUNG_DOT_DU_LIEU` → tải lại giỏ; `422` → hiển thị lỗi theo `details[].truong`.
- Khóa nút thanh toán ngay khi gửi; server vẫn bắt buộc chống trùng. Không cho frontend tự tính số tiền cuối cùng làm nguồn sự thật.

## 5. Những việc phải xác nhận trước khi chốt PR

1. #1: xác nhận lưu giỏ trên server, tên enum, endpoint chuyển trạng thái và quyền từng UC.
2. #5: xác nhận `GET /customers` hỗ trợ tìm theo điện thoại, DTO khách hàng, xử lý khách không tồn tại (POS-03).
3. #5 + #1: xác nhận ai sở hữu chính sách KM, công thức/ưu tiên/cộng dồn và thời điểm tính lại (POS-04). Issue #4 đang ghi `/promotions` là tầng 2.
4. #1/#2: thống nhất biến động kho, lỗi tồn và giao tác checkout.
5. #1: chốt dữ liệu in, nghiệp vụ hủy hóa đơn đã thanh toán, phân quyền và phạm vi MVP.

## 6. Checklist kiểm thử API (sau khi code)

- Tạo giỏ/thêm sản phẩm/sửa/xóa dòng; tìm sản phẩm qua API SP.
- Gắn khách hàng hợp lệ, bỏ gắn khách, khách không tồn tại; tính lại KM sau khi đổi khách.
- Áp dụng KM hợp lệ, không có KM, nhiều KM cạnh tranh, tiền giảm không vượt giá.
- Tiền khách đưa nhỏ hơn tổng tiền; checkout thành công; checkout lặp không tạo 2 hóa đơn.
- Thiếu tồn khi checkout → rollback hóa đơn, thanh toán và biến động kho.
- Giữ/gọi lại/hủy giỏ; tra cứu, in lại, hủy hóa đơn theo quyền.
- Hết phiên, thiếu CSRF, sai vai trò, sửa với `phienBan` cũ.

## 4. Cập nhật theo phản hồi review #1 Thăng (16/10/2026)

- **Phân quyền:** POS-01,02,03,05,07,09 chỉ TN / `BAN_HANG`; POS-10,11 AD xem mọi hóa đơn (`HOA_DON_XEM_TAT_CA`), TN chỉ hóa đơn mình lập (`HOA_DON_XEM_CUA_MINH`); POS-12 chỉ AD / `HOA_DON_HUY`. Thu ngân truy cập hóa đơn người khác: 403 `KHONG_DU_QUYEN`.
- **Phạm vi:** MVP: POS-01,02,03,05,07,09,10,11. Tầng 2: POS-04,08. Sau MVP: POS-12 và DT.
- **POS-05:** bỏ endpoint `cash-payment`; frontend chỉ tính tạm tiền thừa để hiển thị, backend kiểm tra `tienKhachDua >= thanhTien` và tính lại `tienThua` tại `POST /carts/{id}/checkout`.
- **POS-07:** request `{ "tienKhachDua": 50000, "phienBan": 2 }`, chỉ tiền mặt; response `HoaDon` có `chiTiet`, `tienThua`; cập nhật tồn qua dịch vụ kho trong cùng transaction; lỗi tồn 409 `TON_KHONG_DU`.
- **POS-11:** `GET /invoices/{id}/print` trả JSON, frontend in K80 bằng CSS.
- **Mã enum:** cần so sánh với ERD T3-13 trước khi merge, không tự chốt enum mới trong hợp đồng.

### Bảng lỗi đối chiếu nghiệp vụ (cần so câu chữ với đặc tả gốc)

| Endpoint MVP | HTTP | Mã lỗi | Tình huống |
|---|---|---|---|
| Tạo/sửa giỏ | 409 | `XUNG_DOT_DU_LIEU` | `phienBan` đã thay đổi |
| Thêm/sửa sản phẩm | 409 | `TON_KHONG_DU` | Số lượng vượt tồn khả dụng |
| Gắn khách POS-03 | 404 | `KHONG_TIM_THAY` | Không có khách hàng |
| Checkout POS-07 | 409 | `TON_KHONG_DU` | Không đủ tồn tại thời điểm chốt |
| Checkout POS-07 | 409 | `DA_DUOC_XU_LY` | Đơn đã hoàn tất, không được tạo hóa đơn thứ hai |
| Checkout POS-07 | 422 | `DU_LIEU_KHONG_HOP_LE` | Tiền khách đưa nhỏ hơn tiền phải trả |
| Xem hóa đơn POS-10/11 | 403 | `KHONG_DU_QUYEN` | TN truy cập hóa đơn của TN khác |

Các mã và tình huống trên bám quy ước Issue #4; câu thông báo cuối cùng cần đối chiếu đúng phần ngoại lệ E1/E2 của đặc tả Use Case.

# Phối hợp AUTH – POS-03 – POS-04 (cập nhật theo review)

- **AUTH:** #1 Thăng, Issue #5; cookie `cs_session`, header `X-XSRF-TOKEN`, vai trò/quyền từ phiên.
- **KH:** #5 Trung, T3-25; POS-03 tái sử dụng `GET /api/v1/customers`, chờ Trung xác nhận `keyword`, trường trả về và quy tắc khách chưa đăng ký.
- **KM:** #5 Trung, tầng 2; POS-04 không triển khai MVP, chưa chốt nguồn chính sách.
- **POS-03:** chỉ TN / `BAN_HANG`, `PUT /api/v1/carts/{id}/customer` với `khachHangId` và `phienBan`. POS kiểm tra ID phía máy chủ; không tự định nghĩa API AUTH hoặc KH.
- **POS-04:** chỉ TN / `BAN_HANG`, tầng 2; server quyết định tiền giảm.
- **Các điểm mở AUTH-01/06/09:** là phần AUTH do #1 Thăng quyết định; ý kiến POS không thay đổi đặc tả AUTH.

## Nội dung gốc và lịch sử phối hợp

# T3-14 — Thống nhất POS-03, POS-04 với AUTH/KH/KM (#5)

**Trạng thái: ĐỀ XUẤT CHỜ REVIEW.** Căn cứ Issue #4 (quy ước API), Issue #10 (POS/DT), các nhận xét AUTH-01/06/09 được cung cấp. **Không tự sửa API AUTH hoặc ERD** vì thuộc người phụ trách khác.

## 1. Phân định quyền sở hữu

| Chức năng | Chủ sở hữu | API / trách nhiệm |
|---|---|---|
| Đăng nhập, phiên, quyền | AUTH của #1 Thăng (Issue #5) / theo phân công nhóm | `POST /api/v1/auth/login`, cookie phiên 30 phút; `GET /api/v1/me`; xử lý `401`, `403` |
| Tra cứu khách hàng | KH #5 (theo bảng tài nguyên Issue #4) | `GET /api/v1/customers?keyword=...` **cần #5 xác nhận hỗ trợ SĐT** |
| Gắn khách vào đơn | POS #3 | `PUT /api/v1/carts/{id}/customer` nhận `khachHangId`, `phienBan` |
| Áp dụng KM cho giỏ | POS #3 | `POST /api/v1/carts/{id}/apply-promotions` nhận `phienBan`, server tự tính |
| Nguồn chính sách KM | Chưa chốt | `/promotions` là tầng 2 theo Issue #4; **không giả định #5 đã có API KM** |

**Lưu ý quan trọng:** `soDienThoai` của **tài khoản nhân viên AUTH-06** và `soDienThoai` của **khách hàng POS-03** là hai nghiệp vụ/bảng khác nhau. Không suy diễn việc số điện thoại tài khoản là bắt buộc thì số điện thoại khách hàng cũng bắt buộc.

## 2. Hợp đồng POS-03 — gắn khách hàng

1. Thu ngân đăng nhập một lần; browser tự gửi cookie phiên trong các request cùng domain; không gửi Bearer token, không lưu token trong localStorage.
2. Frontend tìm khách: `GET /api/v1/customers?keyword=0901234567&page=1&size=20`. #5 xác nhận tìm được theo số điện thoại, và DTO tối thiểu `{id, tenKhachHang, soDienThoai, trangThai}` (tên trường cần đối chiếu ERD).
3. Thu ngân chọn khách; frontend gửi `PUT /api/v1/carts/21/customer`, header `X-XSRF-TOKEN`, body `{ "khachHangId": 8, "phienBan": 3 }`. Bỏ khách: `khachHangId: null`.
4. Backend POS kiểm tra phiên, quyền TN/AD, giỏ thuộc phiên/thu ngân được phép thao tác, `phienBan`, khách tồn tại/hợp lệ; gắn khách và tính lại ưu đãi, trả giỏ mới `200`.
5. Lỗi: `401 HET_PHIEN` -> login; `403 PHAI_DOI_MAT_KHAU` -> đổi mật khẩu; `403 KHONG_DU_QUYEN` -> chặn; `404 KHONG_TIM_THAY` -> khách/giỏ không tồn tại; `409 XUNG_DOT_DU_LIEU` -> tải lại giỏ; `422 DU_LIEU_KHONG_HOP_LE` -> hiển thị lỗi trường.

**Cần #5 xác nhận:** tên trường DTO khách, lọc theo điện thoại, trạng thái hợp lệ, khách không có SĐT, có cho khách vãng lai không, và cập nhật điểm tại POS-07.

## 3. Hợp đồng POS-04 — áp dụng khuyến mãi

1. Frontend gửi `POST /api/v1/carts/21/apply-promotions`, header `X-XSRF-TOKEN`, body `{ "phienBan": 4 }`; **không** gửi số tiền giảm tự tính.
2. Backend POS lấy giỏ/khách/chính sách KM từ nguồn được thống nhất, kiểm tra điều kiện và tự tính. Mỗi dòng không giảm quá thành tiền dòng, tổng không âm.
3. `200` trả giỏ đã cập nhật gồm `chiTiet`, `tongTienHang`, `tienGiam`, `thanhTien`, `khuyenMaiApDung`, `phienBan`.
4. Khi thay đổi khách, hàng hoặc số lượng và khi checkout, server phải tính lại KM theo quy tắc được chốt. Nếu không có KM hợp lệ: `khuyenMaiApDung: []`, `tienGiam: 0`.
5. Lỗi `409 XUNG_DOT_DU_LIEU`, `409 DA_DUOC_XU_LY` nếu giỏ đã hoàn tất, `422 DU_LIEU_KHONG_HOP_LE` nếu request không hợp lệ.

**Cần #1/#5 chốt:** ai sở hữu bảng/chính sách KM; có cộng dồn không; cách chọn KM tốt nhất và hòa điểm; tính theo từng dòng hay cả hóa đơn; thời điểm tính lại; API/service dùng để đọc KM. `/promotions` đang được Issue #4 xếp tầng 2.

## 4. Ba vấn đề AUTH được nêu trong review (không thuộc quyền tự chốt của POS)

| Vấn đề | Điều đang mâu thuẫn / chưa chốt | Đề xuất để nhóm duyệt | Tác động đến POS |
|---|---|---|---|
| AUTH-06 số điện thoại tài khoản | Đặc tả ngầm hiểu bắt buộc; ERD/YAML cho phép null | **Ưu tiên giữ nullable theo ERD/YAML**, chỉnh AUTH-06 nói rõ `soDienThoai` không bắt buộc; nếu nghiệp vụ bắt buộc thì sửa ERD và YAML ngược lại | POS-03 dùng khách hàng, không phụ thuộc trường SĐT của tài khoản |
| AUTH-09 đặt lại mật khẩu khi khóa tạm | E1 chỉ nêu Admin khóa | **Đề xuất** reset thành công thì xóa khóa tạm do sai mật khẩu, đưa bộ đếm sai về 0; không tự gỡ khóa do Admin | Thu ngân đăng nhập lại được sau khi Admin reset, không cần đợi khóa tạm; phải có kiểm soát quyền và audit |
| AUTH-01 thứ tự kiểm tra khóa tạm | YAML kiểm tra khóa tạm trước mật khẩu, đặc tả bước 4–5 khác | **Đề xuất** kiểm tra tài khoản/khóa tạm trước xác thực mật khẩu; trả lỗi công khai không làm lộ trạng thái tài khoản theo quyết định AUTH | POS chỉ dựa mã lỗi đăng nhập/phiên được chốt; không tự xử lý khóa tài khoản |

Ba đề xuất này **chưa phải quyết định chính thức**. Người phụ trách AUTH/BA phải thống nhất rồi sửa SRS, đặc tả AUTH, ERD và YAML tương ứng trong PR của họ.

## 5. Hành vi frontend thống nhất với AUTH

- Cookie phiên: `HttpOnly; Secure; SameSite=Strict`, 30 phút không hoạt động; browser tự gửi cookie cùng domain.
- `POST/PUT/DELETE` gửi header `X-XSRF-TOKEN` lấy từ cookie `XSRF-TOKEN` (khác cookie phiên HttpOnly).
- `401 HET_PHIEN`: chuyển login và giữ return URL; `403 PHAI_DOI_MAT_KHAU`: trang đổi mật khẩu; `403 KHONG_DU_QUYEN`: thông báo không đủ quyền.
- Không tự tính giá cuối cùng, tồn kho, khuyến mãi ở frontend; server quyết định.

## 6. Checklist xác nhận

- [ ] #5 xác nhận contract GET /customers và DTO khách cho POS-03.
- [ ] #1/#5 xác nhận nguồn chính sách và cách tính KM cho POS-04.
- [ ] #1 xác nhận lưu giỏ server (`/carts`) và enum trạng thái.
- [ ] AUTH owner chốt ba điểm AUTH-01/06/09, đồng bộ SRS/ERD/YAML.
- [ ] #1 xác nhận quyền AD/TN theo ma trận SRS, DT là sau MVP.
- [ ] Review file `docs/api/sales.md`, `docs/api/returns.md`, YAML; tạo PR base `develop`.
