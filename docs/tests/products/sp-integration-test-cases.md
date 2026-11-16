# Products integration test cases

These ten HTTP cases were executed against the real application cookie/session and isolated MySQL fixture. Raw RED and GREEN evidence: `artifacts/sp-module/final/integration-{red,green}.trx`; RED 0 PASS / 10 FAIL, GREEN 10 PASS / 0 FAIL / 0 SKIP. Final full-run identities and source hashes are recorded in [final evidence](evidence/final/summary.json). These are HTTP/Razor tests; browser E2E was NOT RUN.

Source: [ProductsIntegrationHttpTests.cs](../../../tests/ConvenienceStore.Web.Tests/Modules/Products/ProductsIntegrationHttpTests.cs). Tests use the same area/default route templates as `Program.cs`.

| Test ID | Requirement | Level | Scenario / data | Preconditions | Steps | Expected | Actual | Status | Evidence |
|---|---|---|---|---|---|---|---|---|---|
| SP-INT-H-001 | Existing management session policy | HTTP | Anonymous | No cookie | GET /Products/Products/Index | 401 | 401 | PASS | integration-green.trx; LegacyAreaLandingEnforcesRealSessionAndRedirectsToManagement, actor null |
| SP-INT-H-002 | Existing management session policy | HTTP | FORGED | Forged identity headers, no valid session | Same GET | 401 | 401 | PASS | Same theory, FORGED |
| SP-INT-H-003 | Forced-password restriction | HTTP | FORCED | Real session with password change required | Same GET | 403 | 403 | PASS | Same theory, FORCED |
| SP-INT-H-004 | Locked-account restriction | HTTP | LOCKED | Real session, locked account | Same GET | 401 | 401 | PASS | Same theory, LOCKED |
| SP-INT-H-005 | Session expiry | HTTP | EXPIRED | Expired real session | Same GET | 401 | 401 | PASS | Same theory, EXPIRED |
| SP-INT-H-006 | Session revocation | HTTP | REVOKED | Revoked real session | Same GET | 401 | 401 | PASS | Same theory, REVOKED |
| SP-INT-H-007 | SP read permission | HTTP | AD | Valid admin session | Same GET | 302, Location /Products | Matches | PASS | Same theory, AD |
| SP-INT-H-008 | SP read permission | HTTP | NK | Valid warehouse session | Same GET | 302, Location /Products | Matches | PASS | Same theory, NK |
| SP-INT-H-009 | SP read permission | HTTP | TN | Valid cashier session | Same GET | 302, Location /Products | Matches | PASS | Same theory, TN |
| SP-INT-H-010 | Runtime architecture/API conventions | HTTP | AD; actual Products controller endpoints and DI | Current migrations; Products and Inventory registrations | GET /api/v1/products, activate every Products controller, inspect policies/tags/routes/docs, resolve contexts and inspect token/registrations | 200; controllers resolve; policies present; SP tags; unique attribute route/method pairs; APIs documented; one runtime context type, scoped identity, category concurrency token, no duplicate module service registrations | All assertions passed | PASS | RuntimeControllersRoutesDocumentationAndSingleContextAreConsistent; route-inventory.json |

The RED run exposed a legacy welcome-controller dependency/permission gap and missing SP API tags. The fix preserves old bookmarks by redirecting through the authenticated management workflow. No migration or global database mapping changed. Existing independent POS service/auth failures remain outside this fix; these tests do not establish their HTTP exploitability.
