namespace SmartMart.SupabaseSmoke;

internal sealed record SmokeRoleEvidence(string CurrentUser, string SessionUser,
    bool IsSuperuser, bool BypassRls, bool CanCreateRole, bool CanCreateDatabase,
    bool RowSecurity, bool ReadOnly);

internal static class SmokeRoleGuard
{
    internal const string ExpectedRole = "smartmart_backend";
    internal const string Query = """
        SELECT current_user::text, session_user::text, r.rolsuper, r.rolbypassrls,
               r.rolcreaterole, r.rolcreatedb,
               pg_catalog.current_setting('row_security'), pg_catalog.current_setting('transaction_read_only')
        FROM pg_catalog.pg_roles AS r
        WHERE r.rolname = current_user
        """;

    internal static void Verify(SmokeRoleEvidence evidence, SmokeReport report)
    {
        // Do not disclose unexpected role names in logs or artifacts.
        report.CurrentUser = evidence.CurrentUser == ExpectedRole ? ExpectedRole : null;
        report.SessionUser = evidence.SessionUser == ExpectedRole ? ExpectedRole : null;
        report.IsSuperuser = evidence.IsSuperuser;
        report.BypassRls = evidence.BypassRls;
        report.CanCreateRole = evidence.CanCreateRole;
        report.CanCreateDatabase = evidence.CanCreateDatabase;
        report.RowSecurity = evidence.RowSecurity;
        report.ReadOnly = evidence.ReadOnly;
        report.RoleVerified = false;
        if (!evidence.ReadOnly) throw new SmokeValidationException("ReadOnlyGuardNotApplied");
        if (evidence.CurrentUser != ExpectedRole) throw new SmokeValidationException("UnexpectedDatabaseRole");
        if (evidence.SessionUser != ExpectedRole) throw new SmokeValidationException("UnexpectedSessionRole");
        if (evidence.IsSuperuser) throw new SmokeValidationException("SuperuserNotAllowed");
        if (evidence.BypassRls) throw new SmokeValidationException("BypassRlsNotAllowed");
        if (evidence.CanCreateRole) throw new SmokeValidationException("CreateRoleNotAllowed");
        if (evidence.CanCreateDatabase) throw new SmokeValidationException("CreateDatabaseNotAllowed");
        if (!evidence.RowSecurity) throw new SmokeValidationException("RowSecurityNotEnabled");
        report.RoleVerified = true;
    }
}
