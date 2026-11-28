# NFR verification — Issue #264 (canonical SRS codes)

**Status: NOT VERIFIED for performance thresholds.** Updated after reviews [QA-04 by Yến](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/pull/304#pullrequestreview-6092820134) and [QA-02/04 by Thăng](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/pull/304#pullrequestreview-6094976252). Source: [official SRS v1.0.3 traceability draft on PR #301 at ff68536](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/ff68536fd6e8fcae4fed20975c3fcd2487b66101/docs/03-requirements/ma-tran-truy-vet-27-must_SRS-chinh-thuc.md). PR #301 is OPEN; final SRS/release review is pending.

**Do not use** simulated candidate `NFR-02…07` from `docs/03-requirements/non-functional-requirements.md` as official acceptance thresholds. Do not infer response latency from total CI duration. All performance tests use **isolated PG17**, not shared Supabase.

| Official ID | Request / action | SRS threshold | Measurement method | Actual |
|---|---|---|---|---|
| NFR-HN-01 | POS-01 product suggestions while typing | ≤ 1 second | Browser/API measured from input to results rendered; 30 warmed requests, p50/p95/max, errors | NOT RUN |
| NFR-HN-02 | POS-07 complete payment, persist invoice/stock/points as applicable | ≤ 2 seconds | Browser transaction start to confirmed receipt, reconcile DB, measure failures | NOT RUN |
| NFR-HN-03 | BC-01…03 dashboard and one-month reports | ≤ 3 seconds | 30 requests after warm-up with one-month filter; record p50/p95/max and status | NOT RUN |
| NFR-HN-04 | paginated AUTH-05/SP-01/NH-07/POS-10 lists | ≤ 2 seconds per page | Initial and subsequent pages, 30 requests each, page size recorded | NOT RUN |
| NFR-HN-05 | 5 simultaneous users (3 TN, 1 NK, 1 AD) | ≥ 5 concurrently while HN-01…04 hold | Five isolated sessions with separate synthetic accounts; common workload and per-user latency/error rates | NOT RUN |

### Required dataset and repeatable procedure
- 1,000 synthetic products and 10,000 synthetic invoices covering **six months** (approved SRS scale). No production PII or real shared credentials.
- Seed only disposable PG17 using owner/admin profile; run business and list/report traffic under real `smartmart_backend` login with app AD/TN/NK authentication/authorization. Verify object counts, indexes, migrations/ACL, data distribution and six-month period.
- Document exact commit SHA, SRS revision, PostgreSQL version, CPU/memory, runner, browser, backend configuration, date/time UTC+07, dataset seed revision and hash, cache warm-up, concurrency model, sampling intervals, network path, raw request timings (ms), HTTP/SQLSTATE and errors.
- Capture per-request CSV/JSON without secrets, report p50/p95/max and PASS/FAIL against the approved threshold only for the metric defined; do not label PASS just because a sample average passes. If percentile acceptance convention has not been approved, report statistics and **BLOCKED pending interpretation**.
- Separate security `NFR-BM-01…09` (roles, sessions, CSRF, bcrypt/password hashing design review, denied write, TLS), integrity `NFR-TV-01…08`, calculations `NFR-TT-01…04`, usability `NFR-SD`, compatibility `NFR-TC`, maintenance `NFR-BT`, limits `NFR-GH`, legal `NFR-PL`. Map only exact source criteria before claiming PASS; some prior xUnit code covers sub-behaviors, not whole NFR.
- HTTPS of deployed website (NFR-BM-09) is **different** from PostgreSQL TLS VerifyFull. Deployment decision/URL #242/#257 remains unverified; never mark HTTPS PASS solely because database TLS works.

### Scope and gates
PR #304 may be merged only after its own tested diff, required review and accurate evidence; system release remains HOLD until official SRS acceptance, TO-BE role writes/WR-06–10, browser #244, NFR samples, full-schema backup/restore and deployment gates. Store final measurements in the **single rolling QA report** under `docs/06-testing/test-reports/2026-11-27-qa-regression.md`; no parallel final report.
