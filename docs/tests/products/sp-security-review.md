# SP security review

Tested source `06f70abceb7a9991537b98bcda6eb8219bf46edc`; [executed cases](sp-test-report.md), [machine evidence](evidence/final/summary.json). Overall security/solution acceptance is not PASS while baseline service/auth tests and project gates remain.

| Surface | Executed evidence / result |
|---|---|
| Session | Real cookie/database sessions; anonymous/forged, locked, forced-password, expired/revoked guards; HTTP case counts are per test, not per assertion |
| Role matrix | Read AD/NK/TN; create/edit product AD/NK; category mutation, pricing and lifecycle AD only via existing permission policies |
| CSRF | Existing API session XSRF and MVC antiforgery tested on mutations; missing tokens refused |
| Overposting | Dedicated DTO/binding allowlists preserve owner, stock, cost, code, price/status outside assigned operations |
| XSS | Razor encoding tested for names/descriptions; unsafe image schemes removed; image MIME/header/trailer bounded validation and server-generated names tested |
| Injection | LINQ/parameterized SQL; sort whitelist and literal LIKE escaping; unknown filters rejected |
| Concurrency | Category existing EF token/hook; product explicit SQL version predicate; real concurrent requests/DB writes, no-op/duplicate behavior and stale conflicts |
| Transactions | Rollback after writes and FK races; no cascade over business references; historical snapshots preserved |
| API errors | Common safe error contract, correct semantic/malformed/conflict statuses and no-store endpoints; unexpected DB errors not relabeled as success |

Unresolved coverage/decisions:

- SP-03 #58 A1: live NH-04 compact-create/add-to-draft consumer absent. Standalone flow verified; explicit WIP commit is not completion.
- SP-06 new-receipt consumer workflow NOT RUN; current active-filter provider and actual draft-table warning query verified.
- SP-07/SP-09 business approval pending: count scope, hidden-category roles/selection, restore and bulk move scope. Preserve current tested behavior; comments refreshed November16 without new approval.
- 38 independent baseline failures remain: Returns cash/schema and HTTP failures, schema expectations, KHO table count and POS service/auth contracts. Exact names/messages are in the regression report and machine evidence.
- Reviewer/CI/QA/staging and real browser rehearsal/E2E pending. Full Word SRS unavailable; exact issue-published use cases plus canonical schema/API/auth conventions are the available authority.
- No production-size performance benchmark/SLO evidence. Bounded/indexed queries and batch stock reads are verified, not a T7-07 performance acceptance.
- Image recognition does not fully decode pixels; static uploaded media uses unpredictable names and original paths are discarded. Orphan cleanup is operational work.
- Tracked requirements evidence excludes raw PR diffs/connection configuration; ignored raw copy and hashes are retained. No credentials, build binaries or raw TRX are staged.
- Existing POS service/auth consistency failures remain; HTTP exploitability of those independent baseline failures is not asserted without proof. New module role/header-spoof guards passed.

## Latest develop compatibility verification

Latest fetched develop `a024757b73e696b5f4285f070c642db0c3a467cd` advanced through PR #216 while this isolated branch retained its frozen ancestry. Target changes affect six Returns-only files, with no overlap with SP source edits. A clean detached worktree at the exact target was restored and built before any patch application.

**BLOCKED: target does not compile.** The committed target object contains conflict markers in `src/ConvenienceStore.Web/Modules/Returns/Services/ReturnOrderService.cs` at lines 22, 28, 31, 307, 309, 390. The build rerun outside sandbox produced six CS8300 compiler errors and no directory-access error. This is independently present before applying SP changes. No Returns source was edited.

| Gate | Actual result |
|---|---|
| Target restore | PASS after network escalation; initial sandbox socket failure retained |
| Target baseline build | FAIL: six CS8300 errors |
| Target discovery/full suite | NOT RUN: build failed |
| SP source / target changed-file overlap | None |
| Exact SP patch git apply --check | PASS: textual applicability only |
| Patch application / merge / rebase | Not performed; detached target remains clean |
| Runtime verification of target + SP | BLOCKED / NOT RUN |
| Regression against latest target | UNKNOWN; frozen-baseline results must not be transferred to this target |

Patch SHA256 `dc350c698bb5c18a3aff211a5411f753323f878dcdb3f5b679c009a26c0bb774`. [Machine target evidence](evidence/target/summary.json); raw build/restore/check logs remain in ignored artifacts/sp-module/target-baseline/. Source commit and target object hashes establish provenance. No current-develop PASS, acceptance or integration claim is made.

Priority: the Returns owner must resolve committed markers and provide a buildable develop SHA; then rerun clean target baseline and patched-candidate gates sequentially. The local SP branch is preserved for scoped review. No push, PR, merge or issue closure occurred.
