using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace ConvenienceStore.Web.Shared.Data;

public static class VietnameseSearch
{
    private const string Accents = "àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ";
    private static readonly string Plain = Fold(Accents);
    public static string Fold(string value) => new string(value.ToLowerInvariant().Normalize(NormalizationForm.FormD)
        .Where(c=>CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark).ToArray()).Replace('đ','d');

    public static string Column(bool postgres, string column) => postgres
        ? $"translate(lower({column}),'{Accents}','{Plain}')" : column;

    public static void Configure(ModelBuilder model)
    {
        var mapping=new StringTypeMapping("text",System.Data.DbType.String);
        model.HasDbFunction(typeof(VietnameseSearch).GetMethod(nameof(Fold))!).HasTranslation(arguments =>
            new SqlFunctionExpression("translate", [
                new SqlFunctionExpression("lower",arguments,true,[true],typeof(string),mapping),
                new SqlConstantExpression(Accents,mapping),new SqlConstantExpression(Plain,mapping)
            ],true,[true,false,false],typeof(string),mapping));
    }
}
