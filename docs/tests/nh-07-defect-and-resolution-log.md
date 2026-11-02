# NH-07 — Defect and Resolution Journal

## ENV-NH07-01

- Feature: NH-07; date: 2026-11-02; branch `feature/nh-07-receipt-list`; starting SHA `d03a30c51cd5fb4543653041841a403ff8707ef4`.
- Command: PowerShell write of test-first file. Environment: new worktree from schema branch.
- Expected: create test file; actual: `DirectoryNotFoundException` because `tests/.../Modules/Purchasing/` did not yet exist. No file was changed by the failed write.
- Root cause: schema branch has no Purchasing test directory. Correction: create that directory and write the exact test source again. Retest: file exists; targeted test-first run reached compilation. Regression: later build PASS, focused 4/4 and non-integration 12/12. Remaining risk: repository behavior not tested with MySQL.

## TEST-FIRST-NH07-02

- Feature: NH-07; date: 2026-11-02; branch `feature/nh-07-receipt-list`; starting SHA `d03a30c51cd5fb4543653041841a403ff8707ef4` plus uncommitted test.
- Command: `dotnet test .\ConvenienceStoreManagement.slnx --no-restore --filter FullyQualifiedName~ReceiptListServiceTests`.
- Expected red phase: DTO/service are not yet defined. Actual: exit 1, CS0234 and CS0246 in the new test file; no test case executed.
- Root cause: production types intentionally absent before implementation. Correction: implement DTO, service, repository and DI without changing assertions. Retest: targeted 4/4 PASS, non-integration 12/12 PASS, build 0 warning/error at code SHA `aeba11c781281b9af8bef568036fc1b5cb0ed49e`. Remaining risk: MySQL and HTTP tests BLOCKED.
