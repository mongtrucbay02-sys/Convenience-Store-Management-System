# SP integration readiness

Tested source worktree at `06f70abceb7a9991537b98bcda6eb8219bf46edc`; manifest in evidence/final/source-manifest.json. Target baseline develop `babfe1edae507e5d94a95c422a76ffa344a23c00`, initial executable SP-07 tree `86fa911`. Current fetched develop `a024757b73e696b5f4285f070c642db0c3a467cd`. Isolation B only; original dirty worktrees/NH work preserved. Local commits only; no push/PR/merge/issue closure.

Fresh baseline: 455 total, 417 PASS, 38 FAIL, 0 SKIP. Final: 682 total, 644 PASS, 38 FAIL, 0 SKIP. New failures: 0; removed baseline cases: 0; baseline fixes: 0; changed failure causes: 0. Overall solution FAIL. Filtered runs overlap and are never summed.

Disposition: NOT READY FOR FULL ACCEPTANCE / MERGE. Local source is reviewable; every completed scoped gate is documented. SP-03 remains BLOCKED/WIP. Browser E2E NOT RUN.

## Scope and ancestry

Fresh develop branch plus only SP-07 cherry-pick, followed by scoped local SP commits. No NH ancestry or unrelated dirty files imported. Runtime has one CuaHangDbContext; no ApplicationDbContext, extra product/category entities or historical migration changes. Final runtime/DI/route/documentation test and source diff provide evidence.

## Related PR merge destinations

| PR | Merged | Destination | Head SHA | Merge SHA | Merge ancestor of current develop |
|---|---|---|---|---|---|
| #169 | True | feature/db-codefirst-remaining-schema | `f6834abf79770ceb4dce8317b2f48fd98c927373` | `0c2fb776b83417cf6dcab1315a30b01096cdd094` | False |
| #196 | True | feature/db-codefirst-remaining-schema | `02f9db0333d6050ad5f0b306614ba3959170c0f5` | `2554bf1d58f8abaf0b8569f7355f5f30193e88c0` | False |
| #202 | True | feature/sp-08-category-create-update | `a036c38cbc6d9233d99f8c58493c8b97016af613` | `02f9db0333d6050ad5f0b306614ba3959170c0f5` | False |
| #204 | True | develop | `e25356ceffc7dd1dc36e0e83441ea9a134c15479` | `bd4b7b57f151b5774cae41035ed653e0fedbd50d` | True |
| #207 | True | develop | `5e718a3211ecbe310d5074a1ecbeb0610cd3d2da` | `7e501094807b8bf20413900ff4f13e5d334a156a` | True |
| #219 | True | develop | `024f55c04510bb21592c925026e0f8e22e4e59d7` | `391416a3be382f6816a1b6f56dd920eb1612c4c7` | True |
| #220 | True | develop | `d3604f8f97080c083fb3e0aee9807a010c948a9c` | `b8586b8ec1c8a0231becad93862325f6dddffa8e` | True |
| #168 | False | main | `2554bf1d58f8abaf0b8569f7355f5f30193e88c0` | `9ce38ece26cbb228e1b135fbf58769a88acd4267` | None |

Intermediate merge is not integration into develop. An unknown/missing local merge object is reported as unknown, not proof of integration. Local implementations remain local regardless of source PR state.

## Required remediation / pending gates

- SP-03 #58 A1: live NH-04 compact-create/add-to-draft consumer absent. Standalone flow verified; explicit WIP commit is not completion.
- SP-06 new-receipt consumer workflow NOT RUN; current active-filter provider and actual draft-table warning query verified.
- SP-07/SP-09 business approval pending: count scope, hidden-category roles/selection, restore and bulk move scope. Preserve current tested behavior; comments refreshed November16 without new approval.
- 38 independent baseline failures remain: Returns cash/schema and HTTP failures, schema expectations, KHO table count and POS service/auth contracts. Exact names/messages are in the regression report and machine evidence.
- Reviewer/CI/QA/staging and real browser rehearsal/E2E pending. Full Word SRS unavailable; exact issue-published use cases plus canonical schema/API/auth conventions are the available authority.
- No production-size performance benchmark/SLO evidence. Bounded/indexed queries and batch stock reads are verified, not a T7-07 performance acceptance.

Do not merge, push, create PRs or close issues from this audit. The local branch and ignored evidence are preserved for owner/reviewer use.

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
