namespace ConvenienceStore.Web.Modules.Customers;

public static class CustomerRoles
{
    public const string Admin = "AD";
    public const string Cashier = "TN";
    public const string AdminOrCashier = Admin + "," + Cashier;
}
