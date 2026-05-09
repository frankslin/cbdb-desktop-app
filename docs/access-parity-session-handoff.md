# Access Parity Session Handoff

> Status: **planning draft.** Subject to collaborator review before adoption.
> Purpose: a short, regularly-updated note that lets the next session
> resume Access-parity work without re-discovering context.

## How to Use This Document

- One section per active parity workstream (usually one per module).
- Update at the **end** of any parity session, not the start.
- Keep entries terse. Detail belongs in commits, PRs, and the design
  doc.

## Active Phase

See Phased Roadmap in `docs/access-parity-design.md`.

- Current planned phase: **Phase 1 (Entry Query)**.
- Implementation not yet in flight; this entry tracks the planning
  target only.
- Phase exit criteria: probes captured in `cbdb-user-mdb-tests`,
  Avalonia-side service-layer parity assertions running, triage policy
  applied to all observed diffs.

## Per-Module Parity State

### Phase 1: Entry Query
- Avalonia coverage: not started under the parity workflow yet.
- Access oracle probe: TBD in `cbdb-user-mdb-tests`.
- Last compared on: —
- Known diffs: —

### Phase 2: Status Query
- Status: not started; query semantics only, export families deferred.

### Phase 3: Office Query
- Status: not started.

### Phase 4: Person Browser stable sub-surfaces
- Status: not started; sub-surface curation pending.

## Pinned Divergences

Intentional differences from Access we will not "fix". Each entry must
answer: what is different, why, and what would change the decision.

- (none yet)

## Open Questions

Items blocked on a decision from the project owner or on more evidence
from the Access side.

- (none yet)

## Pointers to Access-Side Artifacts

- Sibling repo with ground-truth probes:
  `cbdb-user-mdb-tests`
- Known Access-side bug index: TBD collaborator-readable link in
  `cbdb-user-mdb-tests` (do not point this doc at any non-repo source).
- Probe naming convention and artifact location: TBD.

## How to Resume

1. Read this file.
2. If scope or framing feels unclear, read
   `docs/access-parity-design.md` (especially "Role of Access During
   Migration" and "Phased Roadmap").
3. Before changing any parity assertion, read
   `docs/access-parity-workflow.md`.
4. Pick the in-phase module with the oldest "Last compared on" date
   and run the workflow there.
5. Update this file before ending the session.
