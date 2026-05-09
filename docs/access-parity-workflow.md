# Access Parity Workflow

> Status: **planning draft.** Subject to collaborator review before adoption.
> Audience: anyone (human or agent) adding or updating an Access-parity
> assertion in `cbdb-desktop-app`.

## When to Use This Workflow

Use it when you are:

- adding a new parity assertion for an **in-phase** query module
  (see Phased Roadmap in `docs/access-parity-design.md`),
- investigating a reported divergence between Avalonia output and the
  Access form, or
- updating an existing parity assertion because the Access oracle
  itself moved (e.g. a `.mdb` update reflected in
  `cbdb-user-mdb-tests`).

Do NOT use it for:

- pure UI work,
- pure performance work that does not change result sets,
- modules outside the current phase,
- known Access bug surfaces or deferred export families.

## Test Layering

Parity is verified in this order. Earlier layers are cheaper, more
deterministic, and catch most regressions. Later layers are reserved
for what cannot be expressed at an earlier layer.

1. **Service-layer parity (primary).** Compare Avalonia query-service
   output against the captured Access oracle output for the same
   inputs. This is where most parity assertions live.
2. **Thin UI verification (secondary).** Confirm the relevant query
   window passes service-layer inputs through correctly and surfaces
   results without losing fields. Layout, styling, and visual fidelity
   are not in scope at this layer.
3. **Real-SQLite integration checks (tertiary).** Used only when an
   issue is suspected at the data layer or when a fixture cannot
   reasonably represent the case.

UI-to-UI comparison against the Access app is **not** the primary
method and must not be used as a parity gate.

## Step 1: Capture Access Ground Truth

- Pick or extend a probe in `cbdb-user-mdb-tests`.
- Record: filter inputs, expected row count, expected person-id set,
  representative result columns.
- Do not invent ad-hoc oracles in this repo.

## Step 2: Replay Against Avalonia

- Run the corresponding Avalonia query service from
  `Cbdb.App.Avalonia.Tests` with the same inputs.
- Capture the same artifact shape used by the probe.
- Prefer service-layer assertions; reserve window-level assertions for
  cases the service layer cannot express.

## Step 3: Triage Diffs

Every mismatch must be classified as exactly one of:

- **Avalonia bug** — fix it here.
- **Access bug we will not mirror** — pin it in
  `docs/access-parity-session-handoff.md`, with reasoning.
- **Outdated / wrong oracle capture** — fix the probe in
  `cbdb-user-mdb-tests`. Do not patch around it here.
- **Intentional divergence** — record it as a pinned divergence in
  session handoff, with reasoning and a "what would change this
  decision" line.
- **Open question** — park in session handoff. Do not silently pick
  a side.

A diff that has not been classified is not resolved.

## Step 4: Land Changes

- One concern per branch / PR where practical.
- PR body must reference the design doc, the relevant probe in
  `cbdb-user-mdb-tests`, and the triage classification.
- Update `docs/access-parity-session-handoff.md` in the same PR if the
  per-module state changed.
- Do not auto-merge. External collaborator review is expected.

## Branch and PR Conventions

- Branch prefix for parity work: `parity/<module>-<short-topic>`.
- PR title prefix: `parity:`.
- PR body must list:
  - which module and which roadmap phase,
  - which probe in `cbdb-user-mdb-tests`,
  - the triage classification of any mismatch resolved,
  - whether any pinned divergence was added or removed.

## What Lives Where

| Artifact | Repo |
| --- | --- |
| Access ground-truth probes | `cbdb-user-mdb-tests` |
| Canonical Access bug index | `cbdb-user-mdb-tests` |
| Avalonia query implementation | this repo |
| Service-layer parity assertions | this repo |
| Parity design / workflow / handoff docs | this repo (`docs/`) |
