#!/usr/bin/env bash
# Chạy schema_v1.sql + kiểm thử trên một CSDL trống. Dùng: ./kiem_thu.sh "mysql -uroot -pMATKHAU -h127.0.0.1"
set -u
M=${1:-"mysql -uroot"}
DB=cuahang_kiemthu
$M -e "DROP DATABASE IF EXISTS $DB; CREATE DATABASE $DB CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;" || exit 1
$M --default-character-set=utf8mb4 $DB < schema_v1.sql || { echo "LỖI: schema_v1.sql"; exit 1; }
$M --default-character-set=utf8mb4 -t $DB < kiem_thu_v1.sql || { echo "LỖI: kiem_thu_v1.sql"; exit 1; }
$M $DB -e "INSERT INTO phieu_xuat_huy (ma_phieu, nguoi_lap_id) VALUES ('XH-20261018-001', 3)"
n=0; ok=0
while IFS= read -r q; do
  [ -z "$q" ] && continue; n=$((n+1))
  if $M --default-character-set=utf8mb4 $DB -e "SET time_zone='+07:00'; $q" >/dev/null 2>&1; then echo "SAI - không bị chặn: $q"; else ok=$((ok+1)); fi
done < phai_loi.sql
echo "$ok/$n thao tác sai bị chặn"
[ "$ok" -eq "$n" ]
