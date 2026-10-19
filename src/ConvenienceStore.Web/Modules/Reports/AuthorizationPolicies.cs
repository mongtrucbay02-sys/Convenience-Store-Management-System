namespace ConvenienceStore.Web.Modules.Reports;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";

    // PENDING #1: tên vai trò chốt theo module xác thực.
    public const string AdminRole = "Admin";
}
