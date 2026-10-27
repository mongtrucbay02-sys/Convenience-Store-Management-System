using ConvenienceStore.Web.Modules.Auth.Services;
using Microsoft.AspNetCore.Identity;

namespace ConvenienceStore.Web.Modules.Auth.Security;

public static class BamMatKhau
{
    private static readonly object NguoiDungBatKy = new();
    private static readonly Lazy<string> BamGia = new(() => Bam("khong-co-tai-khoan-nay-1"));

    public static string Bam(string matKhau) =>
        KhoiTaoAdminDauTien.BoBamMatKhau.HashPassword(NguoiDungBatKy, matKhau);

    public static PasswordVerificationResult KiemTra(string chuoiBam, string matKhau) =>
        KhoiTaoAdminDauTien.BoBamMatKhau.VerifyHashedPassword(NguoiDungBatKy, chuoiBam, matKhau);

    public static bool Dung(string chuoiBam, string matKhau) =>
        KiemTra(chuoiBam, matKhau) != PasswordVerificationResult.Failed;

    public static void KiemTraGia(string matKhau) => KiemTra(BamGia.Value, matKhau);
}
