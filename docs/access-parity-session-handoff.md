# Access Parity Session Handoff

> Status: **draft skeleton, locally anchored**. Not yet circulated for review.
> Purpose: a short, regularly-updated note that lets the next session pick
> up Access-parity work without re-discovering context.

## How to Use This Document

- One section per active parity workstream (usually one per query module).
- Update this file at the **end** of any parity session, not the start.
- Keep entries terse. Detail belongs in commits, PRs, and the design doc.

## Current Parity State (per module)

### Status Query
- Avalonia coverage: TBD
- Access ground-truth probe: TBD
- Last compared on: TBD
- Known diffs: TBD

### Entry Query
- Avalonia coverage: TBD
- Access ground-truth probe: TBD
- Last compared on: TBD
- Known diffs: TBD

### Office Query
- Avalonia coverage: TBD
- Access ground-truth probe: TBD
- Last compared on: TBD
- Known diffs: TBD

## Pinned Divergences

Intentional differences from Access that should NOT be "fixed". Each entry
should answer: what is different, why, and what would change that decision.

- (none yet)

## Open Questions

Things blocked on a decision from the project owner or on more evidence
from the Access side.

- (none yet)

## Pointers to Access-Side Artifacts

- Sibling repo with ground-truth probes:
  `cbdb-user-mdb-tests`
- Known Access-side bug index: TBD collaborator-readable link in
  `cbdb-user-mdb-tests` (do not point this doc at any non-repo source).
- Probe naming convention and artifact location: TBD.

## How to Resume

A short checklist for the next session:

1. Read this file.
2. Read `docs/access-parity-design.md` if scope or definitions feel unclear.
3. Read `docs/access-parity-workflow.md` before changing parity tests.
4. Pick the module with the oldest "Last compared on" date and run the
   workflow there.
