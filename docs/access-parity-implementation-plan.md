# Access Parity Implementation Plan

> Status: **planning draft.** Subject to collaborator review before adoption.
> Companion to `docs/access-parity-design.md` and
> `docs/access-parity-workflow.md`. Read those first.

## 1. Objective

Translate the migration-stage parity strategy into an executable plan.
Specifically:

- decide what gets done first,
- name the in-repo artifacts each phase must produce,
- pin the exit criteria that move work from one phase to the next, and
- pin what is deferred so scope creep does not block any phase.

This document does **not** restate the strategy. It assumes the reader
has read the design and workflow docs.

## 2. Starting Assumptions

- The four real Avalonia query modules in scope are already present:
  `EntryQueryService`, `StatusQueryService`, `OfficeQueryService`, and
  the Person Browser surfaces. Service-layer parity attaches to the
  `Cbdb.App.Data` services, not to UI windows.
- A working oracle harness exists in the sibling repo
  `cbdb-user-mdb-tests`. Capturing a probe is an existing capability
  there and is not part of this plan.
- Phase 1 starts inside the existing test project
  `Cbdb.App.Avalonia.Tests`. A dedicated parity test project may be
  introduced later if scale, runtime, or fixture-management costs
  justify it.
- No new third-party dependencies are required for parity work; .NET
  SDK 8.x and the existing test stack are sufficient.
- "Operational" means: probe captured, replay assertion green, every
  observed diff classified per the workflow doc.

## 3. Phase-by-Phase Implementation Table

| Phase | Module | In-scope filters | Out-of-scope (deferred) | "Done" means |
| --- | --- | --- | --- | --- |
| 1 | Entry Query | hierarchical entry category, place + subordinate-place toggle, index-year range, dynasty range | export families, person-browser handoff parity, columns flagged as Access-bug surfaces | At least 3 representative probes captured; service-layer replay assertions green; every diff classified; artifact contract (§4) validated by use. |
| 2 | Status Query (semantics only) | hierarchical status, place + subordinate, index-year, dynasty range | Pajek / Gephi / UCINet exports; person-browser handoff | Same shape as Phase 1, applied to Status. |
| 3 | Office Query | hierarchical office, place + subordinate, index-year, office-year, dynasty range | GIS / KML / Neo4j exports; richer scratch-table fields not yet present in Avalonia | Same shape as Phase 1, applied to Office. |
| 4 | Person Browser stable sub-surfaces | per-sub-surface, after curation | the whole Person Browser; sub-surfaces with non-reference Access behavior | Curated sub-surface list approved; per-sub-surface phase-1-shape exit applied. |

Phases run sequentially. Phase 1 doubles as the validation pass for the
artifact contract and the test layout. If Phase 1 surfaces a contract
problem, fix it in Phase 1 before opening Phase 2.

## 4. Artifact Contract Plan

The contract defines what a single Access oracle capture looks like, so
the same probe can be replayed against Avalonia deterministically.

### Required fields (proposal)

- `probe_id`: stable, repo-visible identifier (e.g. `entry-q-001`).
- `module`: one of `entry`, `status`, `office`, or a Person Browser
  sub-surface key.
- `mdb_version`: identifier of the `.mdb` capture this oracle came
  from (file hash or release tag from `cbdb-user-mdb-tests`).
- `inputs`: named filter values (no positional ordering).
- `expected.row_count`: integer.
- `expected.person_id_set`: sorted list of `c_personid` values.
- `expected.columns_subset`: a small representative slice of result
  rows for column-shape parity (not the whole result set).

### Storage and ownership

- Oracle artifacts are owned by `cbdb-user-mdb-tests` and committed
  there. They are not regenerated in this repo.
- This repo references them by `probe_id` and `mdb_version`. The
  concrete fetch / sync mechanism (committed copy vs. fetched at test
  time) is decided at the end of Phase 1 based on what worked.

### What this plan does **not** lock

- The serialization format (JSON vs. another structured format).
  Phase 1 picks one and validates it; later phases follow.
- Whether artifacts are mirrored into this repo or referenced
  externally. Same: Phase 1 decision.

## 5. Test Project Plan

### Where parity tests live

- Phase 1 starts inside `Cbdb.App.Avalonia.Tests` under a new
  `AccessParity/` subfolder.
- A dedicated parity test project may be introduced later if scale,
  runtime, or fixture-management costs justify it. This is a
  later-phase decision, not a Phase-1 commitment.
- One file per module: `EntryQueryParityTests.cs`,
  `StatusQueryParityTests.cs`, `OfficeQueryParityTests.cs`, and
  later `PersonBrowser<SubSurface>ParityTests.cs`.
- These are **separate** from the existing `*QueryServiceTests.cs`
  files, which keep their current scope of fixture-backed unit checks.

### Test shape

- Service-layer parity tests target the `Cbdb.App.Data` query services
  directly. They do not open Avalonia windows.
- Each test loads a probe artifact, calls the service with the probe
  inputs, and asserts row count, person-id set, and the
  representative column slice.
- Real-SQLite integration is used only when a probe specifically
  requires it; default is fixture-backed where the fixture can
  faithfully represent the case.

### What is **not** in this project plan

- No new test runner, framework, or CI lane.
- No window-level parity tests in Phase 1. Thin UI verification, if
  needed, is added per phase, not preemptively.

## 6. Triage and Ownership Rules

Every parity diff is classified under the workflow doc's five
categories. This section pins **who** classifies and **who** signs off.

| Action | Owner |
| --- | --- |
| Initial classification of a diff | Author of the parity test |
| Confirmation of classification | Reviewer on the parity PR |
| Adding a **pinned divergence** | Project owner sign-off required |
| Fixing an **outdated / wrong oracle capture** | Owner of `cbdb-user-mdb-tests`; this repo's PR waits |
| Promoting an **open question** to a fix or pin | Project owner |
| Closing a phase | Project owner, against the exit criteria in §3 |

Rules:

- An unclassified parity diff blocks merge for parity-significant PRs
  (parity resolution PRs and PRs with parity-significant behavior
  changes). It does not block pure documentation, refactoring, or
  test-harness PRs.
- An open question may not be silently resolved by code; it must move
  to fix, pin, or stay parked with a written reason.
- Classification disagreement escalates to the project owner, not to
  ad-hoc resolution in PR comments.

## 7. Exit From Migration-Reference Mode

This is the part the strategy doc deliberately leaves open. The
implementation plan pins concrete signals.

### Per-module exit

A module exits migration-reference mode when **all** of:

- its phase exit criteria (§3) have been met,
- no high-priority diffs remain unclassified,
- no pinned divergence is older than one full `.mdb` release without
  re-review, and
- the project owner records the module as `post-reference` in the
  session-handoff doc.

After per-module exit **and explicit team acceptance**, Avalonia
becomes the working reference for that module. Access remains
available as a sanity check, not as a gate. Per-module exit is not
automatic; meeting the checklist surfaces a candidate, the team
acceptance step closes it.

### Project-wide exit

The project as a whole exits migration-reference mode only when:

- all four phases are complete,
- every module has an explicit `post-reference` entry in
  session-handoff,
- a written re-baselining note is added to
  `docs/access-parity-design.md` (§"Role of Access During Migration")
  confirming the shift, and
- the workflow doc is updated to reflect that parity assertions are
  now regression checks, not migration gates.

Project-wide exit does **not** require retiring Access. It means
Access-reference parity is no longer the default gate for ongoing
development. Access can still be consulted for cross-checks,
historical comparison, or ad-hoc questions after exit.

Until both conditions are met, parity reference remains active and
this plan governs.

## Open Implementation Questions

- Whether oracle artifacts should be mirrored into this repo (e.g.
  under a new top-level `parity-fixtures/` directory) or referenced
  by ID and fetched at test time. Phase 1 decides.
- Whether artifact mirroring, if chosen, lives in this repo or in a
  third "artifacts" repo to keep this repo's history small.
- How `mdb_version` is communicated when the oracle repo updates and
  this repo's tests have not yet caught up (skip, fail, warn).
