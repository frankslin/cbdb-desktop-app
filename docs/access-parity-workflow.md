# Access Parity Workflow

> Status: **draft skeleton, locally anchored**. Not yet circulated for review.
> Audience: anyone (human or agent) adding or updating an Access-parity
> assertion in this repo.

## When to Use This Workflow

Use it when you are:

- adding a new parity assertion for a query module,
- investigating a reported divergence between Avalonia output and the
  Access form, or
- updating an existing parity assertion because the Access ground truth
  itself moved (e.g. an `.mdb` update).

Do NOT use it for:

- pure UI work,
- pure performance work that does not change result sets,
- bug fixes that have no Access counterpart.

## Step 1: Capture Access Ground Truth

- The Access side is owned by the sibling repo `cbdb-user-mdb-tests`.
- Pick or extend a probe there; do not invent ad-hoc ground truth here.
- Record: filter inputs, expected row count, expected person-id set,
  expected representative columns.
- Artifact format: TBD (to be defined in the design doc).

## Step 2: Replay Against Avalonia

- Run the corresponding Avalonia query service or query window in the
  existing headless test harness (`Cbdb.App.Avalonia.Tests`).
- Use the same inputs as the Access probe.
- Capture the same shape of artifact.

## Step 3: Triage Diffs

For every difference, classify it as one of:

- **Avalonia bug** — fix here.
- **Access bug we will not mirror** — pin it in `Pinned Divergences`
  in the session-handoff doc, with reasoning.
- **Access ground-truth probe wrong** — fix in `cbdb-user-mdb-tests`,
  not here.
- **Open question** — park in the session-handoff doc, do not silently
  pick a side.

## Step 4: Land Changes

- One concern per branch/PR where practical.
- Reference the design doc and the relevant probe in the PR body.
- Update `docs/access-parity-session-handoff.md` in the same PR if the
  per-module state changed.
- Do not auto-merge. The project has external collaborator review.

## Branch and PR Conventions

- Branch prefix for parity work: TBD (suggest `parity/<module>-<short-topic>`).
- PR title prefix: TBD (suggest `parity:`).
- PR body must list:
  - which module,
  - which probe in `cbdb-user-mdb-tests`,
  - whether any pinned divergence was added or removed.

## What Lives Where

| Artifact | Repo |
| --- | --- |
| Access ground-truth probes | `cbdb-user-mdb-tests` |
| Canonical Access bug index | `cbdb-user-mdb-tests` |
| Avalonia query implementation | this repo |
| Parity assertions in headless tests | this repo |
| Parity design / handoff / workflow docs | this repo (`docs/`) |
