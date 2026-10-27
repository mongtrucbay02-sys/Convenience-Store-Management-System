using System.Security.Cryptography;

namespace ConvenienceStore.Web.Modules.Auth.Security;

public static class MatKhauTamNgauNhien
{
    private const string ChuCai = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ";
    private const string ChuSo = "23456789";
    private const int DoDai = 10;

    public static string Sinh()
    {
        var kyTu = new char[DoDai];
        kyTu[0] = ChuCai[RandomNumberGenerator.GetInt32(ChuCai.Length)];
        kyTu[1] = ChuSo[RandomNumberGenerator.GetInt32(ChuSo.Length)];
        const string tatCa = ChuCai + ChuSo;
        for (var i = 2; i < DoDai; i++)
            kyTu[i] = tatCa[RandomNumberGenerator.GetInt32(tatCa.Length)];
        RandomNumberGenerator.Shuffle(kyTu.AsSpan());
        return new string(kyTu);
    }
}
