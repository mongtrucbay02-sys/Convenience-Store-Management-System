# SupabaseSmoke VerifyFull và EF mapping — 24/11/2026

> Review độc lập cuối: **ACCEPT WITH CONDITIONS**; kết quả chạy mới và findings nằm ở phần “Rà soát độc lập cuối” bên dưới. Phần đầu lưu nguyên bằng chứng lượt TLS trước, không phải kết quả chạy lại.

**Kết luận: PASS trong phạm vi task.** Build Release thành công, 33/33 test tập trung PASS và smoke Supabase thật PASS với VerifyFull. Không tạo commit, push, PR, đổi nhánh hoặc sửa schema/dữ liệu database.

## Preflight và nguyên nhân

Repository root thực: `D:/DEV/Projects/mongtrucbay02-sys/Convenience-Store-Management-System`. Nhánh đúng `integration/smartmart-postgresql-only`, HEAD `1e47747d5280fc2765bbf2ab50ba16ce4aaa0940`. Đầu lượt có một thay đổi riêng ở `.env.example` tại root; giữ nguyên hash byte, không sửa, không stage. Private `src/ConvenienceStore.Web/.env` tồn tại và bị Git ignore; chỉ đọc vào bộ nhớ, không in nội dung.

Code tại HEAD khác mô tả cũ một điểm: nó đã giữ nguyên VerifyFull nếu được cấu hình, nhưng vẫn tự chuyển remote Disable/Allow/Prefer thành Require và chấp nhận remote Require/VerifyCA mà không bắt buộc RootCertificate. Vì vậy chưa đáp ứng hợp đồng TLS mới. Theo [Npgsql](https://www.npgsql.org/doc/security.html), Require mã hóa nhưng không kiểm chứng danh tính server như VerifyFull.

## File thay đổi

| File | Giải thích |
|---|---|
| `tools/SupabaseSmoke/Program.cs` | Entry point mỏng, chỉ gọi runner; không khởi động ASP.NET. |
| `tools/SupabaseSmoke/SmokeTlsPolicy.cs` | Mới: từ chối remote không VerifyFull/thiếu CA; giữ RootCertificate, tắt GSS cho TLS; giữ timeout/pooling/read-only options. |
| `tools/SupabaseSmoke/SmokeRunner.cs` | Mới: chạy probe/guard/rollback, validate schema/count; JSON chỉ chứa metadata an toàn; tạo report mới, không ghi đè. |
| `tools/SupabaseSmoke/SupabaseSmoke.csproj` | Cho test truy cập internal types; không thêm package. |
| `tests/ConvenienceStore.Web.Tests/PostgreSql/SupabaseSmokeTests.cs` | Mới: 29 ca TLS, localhost, chặn query/rollback, diagnostics, mapping/query shape và bảo toàn evidence. |
| `tests/ConvenienceStore.Web.Tests/ConvenienceStore.Web.Tests.csproj` | Tham chiếu executable smoke để test trực tiếp; không tạo backend/test project mới. |
| `src/ConvenienceStore.Web/.env.example` | Chỉ thêm placeholder Root Certificate và hướng dẫn CA ngoài Git. |
| `docs/07-deployment/supabase-smoke-ci.md` | Thay ví dụ Require bằng VerifyFull + CA; nêu yêu cầu runner và tên report mới. |
| `docs/06-testing/test-reports/2026-11-24-supabase-smoke-verifyfull.md` | Báo cáo hiện tại, phân biệt test mô phỏng/offline và live smoke thật. |

Không thay entity, DbContext, migration, schema SQL, roles, grants, RLS, triggers, views hoặc indexes. Không sửa private `.env` hay thay đổi riêng `.env.example` tại root. Tất cả thay đổi còn ở working tree, index trống.

## Bảo vệ trước và sau

- Remote phải cấu hình rõ `SslMode.VerifyFull`, `RootCertificate` không trống và file CA tồn tại. Không tự nâng/hạ mode, không fallback; Require/Prefer/Disable/Allow/VerifyCA bị từ chối trước kết nối. Explicit Trust Server Certificate=true bị từ chối.
- Giữ nguyên đường dẫn CA cho Npgsql 10.0.3 kiểm tra chain/hostname; không tự parse hay tin cậy CA, không callback bỏ kiểm chứng. Tắt GSS remote để bắt buộc TLS với Npgsql 10. Localhost/địa chỉ loopback vẫn giữ mode test cục bộ, không buộc CA cho local Disable.
- Giữ timeout kết nối 25s, command 20s, pooling/multiplexing tắt, startup read-only option. Sau BEGIN vẫn chạy SET TRANSACTION READ ONLY và SHOW transaction_read_only; chỉ khi kết quả on mới tiếp cận model/query.
- Mỗi query vẫn dùng AsNoTracking/Take(1). Chỉ schema smartmart, đúng 31 table/3 view, không trùng tên. Rollback được gọi rõ khi thành công hoặc guard/mapping/query lỗi; dispose tài nguyên sau đó. Status passed chỉ được đặt sau rollback thành công.
- Không gọi Migrate/EnsureCreated/EnsureDeleted, seeder, SaveChanges hay câu lệnh ghi/DDL. Không khởi động ASP.NET. Probe không thay role/policy để vượt lỗi quyền.
- JSON/log không ghi connection string, password, CA path/nội dung, exception message/stack trace hoặc dữ liệu hàng. Chỉ error/cause type, SQLSTATE, reason code cố định và metadata đối tượng. FileMode.CreateNew từ chối đường dẫn report đã tồn tại trước kết nối; report cũ được giữ nguyên.

## Build và automated tests

| Kiểm tra | Kết quả thực |
|---|---|
| `dotnet build tools/SupabaseSmoke/SupabaseSmoke.csproj -c Release` | PASS, 0 error; có warning NU1900 do không truy cập NuGet vulnerability feed |
| Build cuối với `--no-restore` | PASS, 0 error, 2 warning NU1900; không phải cảnh báo compiler |
| SupabaseSmokeTests | 29 PASS, 0 FAIL, 0 skipped; dùng session giả lập, không kết nối DB |
| SupabaseModelMappingTests | 4 PASS, 0 FAIL, 0 skipped; đối chiếu catalog reference/offline model, không chạy migration |
| Tổng focused run cuối | **33 PASS / 0 FAIL / 0 skipped** |
| Full suite 763 trước đó / GitHub CI mới | Không chạy lại trong lượt này; không dùng làm kết quả fresh của thay đổi TLS |

Lệnh test cuối:

```powershell
dotnet test tests/ConvenienceStore.Web.Tests/ConvenienceStore.Web.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~SupabaseSmokeTests|FullyQualifiedName~SupabaseModelMappingTests" --logger "trx;LogFileName=tls-focused-final.trx" --results-directory artifacts/supabase-smoke/tls-review -v quiet
```

Test TLS kiểm tra VerifyFull với file tồn tại (file giả lập, không tuyên bố trust handshake), mode yếu, thiếu/path CA không tồn tại, bypass, host local/remote; test diagnostics dùng secret giả; test guard/mapping lỗi phải chặn mọi query và rollback. Query shape thực của cả 34 đối tượng có AsNoTracking/Take(1)/LIMIT và khớp catalog. Việc kiểm chứng CA/hostname thực được xác minh bằng live run bên dưới.

## Live Supabase — thực sự đã chạy

Connection được nạp từ private `.env` vào SUPABASE_DB_CONNECTION chỉ trong environment của process con. Không đổi SSL mode hay CA path của cấu hình. Lần sandbox lỗi NpgsqlException/SocketException trước transaction (0 checked objects); chạy lại với quyền mạng ngoài sandbox thành công. Một wrapper PowerShell ban đầu dừng khi gặp native stderr để lại report rỗng; report rỗng này không được dùng làm bằng chứng PASS. Các lần thử dùng tên mới, không ghi đè kết quả trước.

Lệnh live dùng Release đã build:

```powershell
dotnet run --project tools/SupabaseSmoke/SupabaseSmoke.csproj -c Release --no-build --no-restore -- <new-report-path>
```

| Trường | Giá trị đã xác minh |
|---|---|
| status / exit | passed / 0 |
| tlsMode | VerifyFull |
| certificateConfigured / certificateFileExists | true / true |
| readOnly | true (SHOW transaction_read_only = on) |
| tables / views | 31 / 3 |
| checkedObjects | 34, không trùng; tập tên khớp catalog contract |
| writes | 0 |
| rollback | Thành công; runner chỉ trả passed sau rollback |
| checkedAtUtc | 2026-11-24T11:54:45.6674284+00:00 |

PASS chỉ chứng minh kết nối TLS và khả năng SELECT/materialize tối đa một hàng qua EF cho 34 mapping. Hàng rỗng/RLS che vẫn có thể trả 0; không suy ra quyền ghi, chất lượng toàn bộ dữ liệu, đầy đủ grants/RLS, hoặc nghiệm thu UI/nghiệp vụ.

## Đường dẫn bằng chứng

- Live report mới: `artifacts/supabase-smoke/tls-review/live-verifyfull-20261124T115434404213Z-ce976b63.json`.
- Live log mới: `artifacts/supabase-smoke/tls-review/live-verifyfull-20261124T115434404213Z-ce976b63.log`.
- Live report SHA-256: `2a1dfcfd1bbf7303f7809d41361fb7d7b20d5bb661ad4edd77cd4393fef0ce52`.
- Focused TRX: `artifacts/supabase-smoke/tls-review/tls-focused-final.trx`; SHA-256 `eb1b9d9a5c318e36a2c9c251b3a762aab126a99b5190fd2684532dd5ff390205`.
- Test log: `artifacts/supabase-smoke/tls-review/tests-final.log`.
- Build logs: `artifacts/supabase-smoke/tls-review/build.log`, `build-final.log`.
- `artifacts/supabase-smoke/result.json` cũ không đổi: SHA-256 `6d8a1c8f618fde3d8039b114d8bf0d309130d4764b2e7ae9cda261e85ac30b54`.

Raw report/log/TRX và CA/config riêng đều ngoài Git. Chỉ báo cáo biên tập này thuộc thay đổi source/docs cần review.

## Giới hạn và việc còn lại

Không còn blocker cho acceptance gate của task TLS/EF smoke. GitHub workflow chưa chạy trên thay đổi này; runner cần file CA thực tại Root Certificate và secret phù hợp, trigger hiện tại cũng chưa tự chạy cho nhánh integration. Không thay secret hoặc cấu hình GitHub trong lượt này. NU1900 cho biết lần kiểm tra lỗ hổng package chưa hoàn tất do mạng; build/test vẫn PASS.

Các gate adoption Supabase đang có dữ liệu, backend role triển khai, SRS/DT/UAT và release của Issue #283 vẫn giữ nguyên trạng thái trước đó. Smoke chỉ đọc không giải quyết các gate này. Không áp initial baseline lên Supabase hiện hữu.


## Rà soát độc lập cuối — bằng chứng chạy mới

**Verdict: ACCEPT WITH CONDITIONS.** Không còn finding P0/P1 hoặc lỗi source đã xác nhận chưa sửa trong phạm vi tool. Code/test sẵn sàng cho review commit; CI GitHub và adoption database vẫn là gate riêng. Không stage, commit, push hoặc mở PR trong lượt review.

### Preflight và phạm vi

Root/branch/HEAD đã xác minh lại: repository nêu trên, `integration/smartmart-postgresql-only`, `1e47747d5280fc2765bbf2ab50ba16ce4aaa0940`. Đầu lượt review ngoài chín file có `.env.example` root và `ConvenienceStoreManagement.slnx` bị sửa; cả hai giữ nguyên hash byte. Trong lúc review xuất hiện thêm `D MYSQL-REPORTS-SETUP.md`; giữ nguyên trạng thái ngoài phạm vi, không khôi phục/stage. Workflow chỉ đọc, không sửa; hash giữ nguyên.

Đã đọc toàn bộ chín file gồm cả bốn file untracked, không chỉ xem diff. Trong lượt review này chỉ sửa thêm **SmokeRunner.cs, SupabaseSmokeTests.cs và báo cáo hiện tại**. Sáu file còn lại trong danh sách chín giữ nguyên nội dung so với lúc bắt đầu review. Không thay entity, migration, SQL schema hoặc code nghiệp vụ.

### Findings theo severity và remediation

| Severity / trạng thái | File / dòng | Bằng chứng và sửa tối thiểu | Xác minh |
|---|---|---|---|
| P0/P1 | Không phát hiện | Không thấy downgrade/bypass TLS hoặc đường thực thi ghi Supabase trong phạm vi đã rà soát | Review toàn file, tests và live smoke |
| **P2 — FIXED** | `tools/SupabaseSmoke/SmokeRunner.cs`: DisposeAsync đầu review dòng 187–192, bản sửa helper dòng 189 | Cleanup cũ tuần tự: context DisposeAsync ném lỗi thì transaction/connection không được thử dispose; transaction lỗi cũng bỏ qua connection. Dùng nested try/finally để luôn thử cả ba rồi truyền lỗi ra ngoài; không thay guard/TLS/query | `ResourceCleanup_AttemptsEveryDisposeEvenWhenEarlierCleanupFails`, test dòng 285: fault context, transaction và cả hai; cả ba tài nguyên đều được gọi đúng thứ tự |
| P2 — gate CI chưa xác minh, không phải kết luận runtime đã lỗi | `.github/workflows/supabase-smoke.yml`:41–44 | Không có step provision CA. Không đọc được giá trị secret nên chưa thể xác nhận Root Certificate trỏ đúng file runner; không giả định workflow chắc chắn PASS/FAIL | Giữ NOT RUN; cần owner cung cấp/kiểm chứng CA chính thức và đường dẫn secret rồi chạy thực tế |
| P3 — gate trigger | `.github/workflows/supabase-smoke.yml`:6 | Push trigger không có integration branch; có workflow_dispatch. Không tự đổi trigger hoặc push/merge để chạy | Review YAML thực, không thay workflow |

Không gọi việc thiếu test là lỗ hổng đã khai thác. Regression bổ sung cho các nhánh liên quan trực tiếp: dispose lỗi sau 34 query phải đổi kết quả thành failed; guard/query bị hủy phải rollback/dispose và failed; report là directory hoặc parent là file phải dừng trước factory connection; IPv4 ngắn/số nguyên loopback vẫn là local, địa chỉ mapped-remote/hostname giả loopback bị chặn. Không sửa TLS policy vốn đã đúng.

### Kiểm tra các đường lỗi và bảo vệ

- Remote giữ nguyên VerifyFull + RootCertificate và GSS Disable; mode yếu/missing file/bypass bị từ chối trước Open. Host list trộn local/remote và hostname gây nhầm không đi qua local exception. Việc nhận IP loopback dạng khác dựa trên IPAddress.TryParse/IsLoopback, không dựa prefix chuỗi.
- BEGIN/SET READ ONLY/SHOW luôn trước model/entity query. Guard sai hoặc khởi tạo lỗi không chạy entity query; finally thử rollback, await-using dispose. Rollback hoặc session dispose lỗi bị catch và status failed; cancellation exception ở guard/query cũng không trả PASS.
- EF query builder thực vẫn AsNoTracking/Take(1), schema smartmart; tên 34 đối tượng được kiểm tra với contract bằng test và report live. Không chỉ lấy số đếm 31/3 làm chứng minh mapping đầy đủ.
- CreateNew được thực hiện trước connection, từ chối file tồn tại/path không hợp lệ. DTO serialization chỉ có primitive metadata; lỗi serialization/flush/đóng report đi vào lỗi CLI nonzero. Report thiếu/rỗng/partial hoặc status passed nhưng process nonzero không được xem là bằng chứng thành công. Luôn kiểm tra cả exit và JSON. Test hiện không giả lập sự cố disk-full/OS kill; không tuyên bố đã fault-inject các tình huống đó.
- Console/JSON chỉ có loại lỗi/cause, reason cố định, SQLSTATE và metadata; không log exception message/stack, connection builder/password, CA path/nội dung hoặc dữ liệu hàng. Quét các file ứng viên và report/log mới với credential cục bộ bằng phép so sánh trong bộ nhớ; không in giá trị credential.

### Kết quả thực hiện trong lượt review này

| Hạng mục | Kết quả mới |
|---|---|
| Build tool Release, có restore | **PASS, 0 warning, 0 error**; NU1900 ở lần trước là bằng chứng lịch sử |
| Focused SupabaseSmokeTests + SupabaseModelMappingTests | **45/45 PASS, 0 FAIL, 0 skipped** (41 smoke + 4 mapping) |
| Full regression Release | **804/804 PASS, 0 FAIL, 0 skipped** trên PostgreSQL riêng; không tái sử dụng 763 cũ |
| Live Supabase VerifyFull | **PASS**, exit 0, readOnly=true, 31 tables/3 views, checkedObjects=34, writes=0, rollback thành công |
| Git diff/check, index, secret/generated-file review | PASS trong phạm vi chín file; index trống, không private env/artifact được stage |
| GitHub CI/adoption | NOT RUN / điều kiện còn chờ |

Full regression chỉ được chạy sau khi rà soát hai fixture và wrapper: host test cố định `127.0.0.1:55439`, PostgreSQL server version num `170011`, data_directory đúng cluster trong `artifacts/postgresql-integration/pgdata`. Wrapper bỏ các biến Supabase/app connection trước khi chạy; fixture tạo/drop DB tên ngẫu nhiên riêng. Đây là môi trường test local, có áp schema/migration/seed và HTTP host phục vụ test; không chạy chúng trên Supabase. So sánh tên DB trước/sau không có DB mới còn sót hoặc DB có sẵn bị xóa. Cluster vốn dừng đầu lượt được dừng lại sau test.

Focused chạy từ `2026-11-24T19:12:42.4671229+07:00` đến `2026-11-24T19:12:50.7945350+07:00`; full chạy từ `2026-11-24T19:13:20.4977898+07:00` đến `2026-11-24T19:17:35.1115446+07:00`. Không cộng focused vào tổng full. Live mới kiểm tra lúc `2026-11-24T12:13:51.7930017+00:00`, dùng credential từ private env và tên report mới; các bằng chứng cũ giữ nguyên hash.

### Evidence mới, ngoài Git

- Build: `artifacts/supabase-smoke/final-independent-review/build.log`.
- Focused: `artifacts/supabase-smoke/final-independent-review/focused-review.trx`, SHA-256 `4bc62f1e5f142611795f29a41632f1911c02ab7aa33bb2a9a626d626763331a3`.
- Full: `artifacts/supabase-smoke/final-independent-review/full-review.trx`, SHA-256 `c7771545b399f68da5fde99319569a012246e9cc67f6f483b6f3f95d69f2a3d4`; log `full.log`, kiểm tra isolation/cleanup `full-invocation.json`.
- Live JSON: `artifacts/supabase-smoke/final-independent-review/live-verifyfull-20261124T121333751828Z-b94f0f03.json`, SHA-256 `3a96512fcaba95048b207893c0b6aeb06e0841638d06c792e4a4958615d7a737`.
- Live log: `artifacts/supabase-smoke/final-independent-review/live-verifyfull-20261124T121333751828Z-b94f0f03.log`.
- Snapshot preflight/hash và kết quả kiểm tra cuối: `artifacts/supabase-smoke/final-independent-review/`.

### Git và lệnh stage đề xuất — KHÔNG thực thi trong lượt này

HEAD/branch không đổi; không có staged file. Chín file trong scope còn chờ stage; ngoài scope là `.env.example`, `ConvenienceStoreManagement.slnx` và deletion `MYSQL-REPORTS-SETUP.md`. Private env, CA, bin/obj/TRX/cluster/log/report artifacts không thuộc commit. Chỉ chạy lệnh sau sau khi người dùng/owner duyệt nội dung:

```powershell
git add -- `
  "tools/SupabaseSmoke/Program.cs" `
  "tools/SupabaseSmoke/SmokeTlsPolicy.cs" `
  "tools/SupabaseSmoke/SmokeRunner.cs" `
  "tools/SupabaseSmoke/SupabaseSmoke.csproj" `
  "tests/ConvenienceStore.Web.Tests/PostgreSql/SupabaseSmokeTests.cs" `
  "tests/ConvenienceStore.Web.Tests/ConvenienceStore.Web.Tests.csproj" `
  "src/ConvenienceStore.Web/.env.example" `
  "docs/07-deployment/supabase-smoke-ci.md" `
  "docs/06-testing/test-reports/2026-11-24-supabase-smoke-verifyfull.md"
```

Còn cần CI đúng commit trên runner có CA/secret phù hợp và trigger được owner quyết định; không chỉnh secrets/ruleset ở đây. Adoption/backup/catalog/backend-role của Supabase hiện có và scope DT/SRS/UAT vẫn là gate triển khai độc lập. Không áp initial baseline lên database hiện hữu.
