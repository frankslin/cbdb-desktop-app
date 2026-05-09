# Access Parity Design

> Status: **draft skeleton, locally anchored**. Not yet circulated for review.
> Owner: TBD. Last touched: 2026-05-09.

## Purpose

Why this document exists, and what "Access parity" means for this repo.

- The Avalonia app's query modules (`Status`, `Entry`, `Office`, …) are
  re-implementations of long-established Access `.mdb` query forms.
- "Parity" here means: for the same user-visible inputs, the Avalonia app
  produces the same record set and person set as the canonical Access form,
  with documented and intentional exceptions.

## Scope and Non-Goals

- **In scope:** query semantics, result row shape, person-id set,
  edge-case behavior (empty inputs, subordinate places, dynasty range,
  index-year range), and how known Access bugs should be handled.
- **Out of scope:** UI layout, font choices, localization, packaging,
  release pipeline, performance tuning that does not change result sets.

## Ground Truth Source

- The canonical reference is the Access app driven by `CBDB_BJ_User.mdb`.
- Probes against that ground truth live in a sibling repo:
  `cbdb-user-mdb-tests` (differential testing harness, real VBA via
  pywinauto vs Python replay).
- This repo MUST NOT redefine ground truth locally; it consumes it.

## Parity Dimensions

To be fleshed out. Likely sections:

- Input shape parity (filters that exist on both sides)
- Result row parity (columns and types)
- Result set parity (which rows appear, ordering tolerance)
- Person-set parity (the unique-people rollup)
- Edge-case parity (empty filter, subordinate place toggle, dynasty range
  semantics, NULL handling)
- Intentional divergences from Access (e.g. known Access bugs we will
  not reproduce)

## Architecture and Cross-Repo Layout

To be fleshed out. Likely:

- What lives here: the Avalonia-side query service, parity-oriented
  fixtures, parity assertions that compare against captured ground-truth
  artifacts.
- What lives in `cbdb-user-mdb-tests`: the Access ground-truth probes and
  the canonical artifact format.
- Artifact handoff format (file shape, columns, encoding, where it is
  checked in or fetched from).

## Open Design Questions

- TBD. Use this section to park unresolved choices instead of letting them
  drift into code.
