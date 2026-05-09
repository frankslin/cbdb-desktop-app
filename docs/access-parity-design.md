# Access Parity Design

> Status: **planning draft.** Subject to collaborator review before adoption.
> Last touched: 2026-05-09.

## Purpose

This document defines what "Access parity" means in `cbdb-desktop-app`
during the Avalonia migration, and just as importantly what it does not
mean. It scopes how the Access app is used as a behavioral reference and
sets the boundaries that keep that use bounded in time and surface area.

## Role of Access During Migration

Access is a **migration-stage reference implementation**, not a permanent
authority.

- **Why we use it now.** Several Access query workflows are mature, in
  active scholarly use, and have predictable result shapes. Comparing
  Avalonia against them lets us catch regressions and recover missing
  semantics while the Avalonia code is still catching up.
- **Why we will not call it ground truth forever.** The Access app has
  known bugs, under-specified behaviors, and scope gaps. It is also
  effectively frozen in form factor; the Avalonia app is expected to
  grow beyond it.
- **What this framing leaves room for.**
  - Intentional divergence from Access where Avalonia behavior is
    deliberately better-defined.
  - Non-adoption of known Access bugs.
  - Future re-baselining once the Avalonia side is mature enough to
    define behavior on its own terms.

Matching Access behavior requires a concrete migration rationale, not
precedent alone. The reasoning must be visible in the design doc, the
workflow triage, or a pinned divergence.

## Repo Boundary

- `cbdb-desktop-app` (this repo) is the **primary home** for parity
  planning, implementation, and review. Design, workflow, session
  handoff, and the parity assertions themselves live here.
- `cbdb-user-mdb-tests` is the **oracle / probe source repo**. It owns
  the Access-side probes and the canonical capture format for
  ground-truth artifacts.
- Planning workflow stays here. We will not move parity strategy back
  into the Access-testing repo.

## Eligible Reference Surface

"Reference-worthy" is a curated subset of the Access app, not the whole
app.

A workflow is eligible as a migration reference only if:

- it is stable on the Access side,
- its expected behavior can be captured by a reproducible probe in
  `cbdb-user-mdb-tests`, and
- it is not currently flagged as a known Access bug surface.

Surfaces explicitly excluded from parity gating, at least for now:

- known Access bug surfaces (catalogued in `cbdb-user-mdb-tests`),
- workflows whose Access implementation is itself in flux,
- export families that depend on third-party tools (Pajek, Gephi,
  UCINet, KML, Neo4j) — see Phased Roadmap, deferred scope.

## Phased Roadmap

Parity work is phased. We do not open a new phase until the previous
one is operational: probes captured in `cbdb-user-mdb-tests`,
service-layer parity assertions running here, and triage policy applied
to all observed diffs.

| Phase | Module | Notes |
| --- | --- | --- |
| 1 | Entry Query | First module to land. Validates the workflow itself. |
| 2 | Status Query | **Query semantics only.** Pajek / Gephi / UCINet export parity is deferred. |
| 3 | Office Query | Includes the existing office picker and the people-place / office-place split. |
| 4 | Person Browser stable sub-surfaces | Curated sub-surfaces only; not the whole Person Browser. |

Explicitly deferred scope:

- export families (Pajek, Gephi, UCINet, KML, Neo4j) for any module,
- Person Browser sub-surfaces with unstable or non-reference Access
  behavior,
- known Access bug surfaces.

## Stop Rules

The following are out of scope on purpose:

- No full combinatorial parity ambition. We are not enumerating every
  Access input.
- No permanent "Access says so" framing. Access is a migration-stage
  reference, not a permanent authority.
- No re-creating the Access issue tracker inside this repo. Bug
  inventory stays in `cbdb-user-mdb-tests`.
- No expanding parity scope until the previous phase is operational.

## Open Design Questions

- Probe artifact format and where captured artifacts are stored
  (committed in `cbdb-user-mdb-tests`, fetched on demand, or referenced
  by ID).
- Concrete bar for "operational" when closing one phase and opening the
  next.
- Threshold for promoting an "open question" diff into either a fix or
  a pinned divergence.
