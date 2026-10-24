namespace ConvenienceStore.Web.Modules.Promotions;

public static class PromotionRoles
{
    // PENDING #1: tên vai trò chốt theo module xác thực.
    public const string Admin = "Admin";
    public const string Cashier = "Cashier";
    public const string AdminOrCashier = Admin + "," + Cashier;
}
