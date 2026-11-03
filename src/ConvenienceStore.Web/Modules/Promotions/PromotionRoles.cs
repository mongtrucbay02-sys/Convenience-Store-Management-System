namespace ConvenienceStore.Web.Modules.Promotions;

public static class PromotionRoles
{
    public const string Admin = "AD";
    public const string Cashier = "TN";
    public const string AdminOrCashier = Admin + "," + Cashier;
}
