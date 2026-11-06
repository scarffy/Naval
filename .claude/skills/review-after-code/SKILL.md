---
name: review-after-code
description: Review every code change in this project before it is committed. Priorities are correctness first, then pragmatism, then style. Use after writing or changing any C# or tooling code, and before committing.
---

# Review after code

Every code change gets a review before it's committed: changes written in this session, by a helper agent, or by anyone else. Review the diff, fix what the review finds, then commit.

## Priorities, in this order

1. **Correctness.** Does it do what it's meant to, in every case, without crashing?
   - It compiles: run `Tools/compile-check/check.sh` (Unity 2021.3 reference assemblies; write C# 7.3).
   - Null references: Inspector fields left empty, `GetComponent` returning nothing, missing data rows.
   - Off-by-one errors, wrong units (days vs seconds, gold vs crates), integer division, rounding.
   - Cases not handled: empty lists, zero or negative values, a ship sinking mid-voyage, gold going negative, the last day of the mission.
   - State left inconsistent: cargo, gold or ownership changed in one place but not another; events fired twice or never.
   - Unity lifecycle and serialisation: Awake, OnEnable and Start ordering; event handlers unsubscribed in OnDisable or OnDestroy; renamed serialised fields losing their data (use `FormerlySerializedAs`); assets referenced by path or GUID.
   - Data: a missing or misspelled CSV row or column must fail loudly with a clear message, not silently use 0.
   - Randomness: game rules use a seeded random source so tests and bugs can be reproduced.
2. **Pragmatism.** Is it the simplest thing that works for the vertical slice?
   - No speculative features, extra options or abstractions the slice doesn't need (see `Design/SliceScope.md` for what's in and what's parked).
   - Reuse what exists before adding something new.
   - Prefer a small, clear change over a clever one.
3. **Style.** Only after the above:
   - consistent names and folders
   - short comments where a rule isn't obvious
   - no dead code or leftover debug logging

## How to run the review

1. Run `Tools/compile-check/check.sh`. It must pass.
2. Run the built-in code review on the change (the `code-review` skill at high effort), or read the full diff yourself if that skill isn't available.
3. Sort findings by the priorities above.
   - Fix every correctness finding.
   - Fix pragmatism findings unless the fix costs more than it saves.
   - Fix style last, or leave it if the change is urgent.
4. Re-run the compile check after fixing.
5. Summarise for the user: what was found, what was fixed, what was left and why.

## Project rules that always apply

- No AI or assistant attribution anywhere: commit messages, trailers, authors, code comments or files.
- Commit as the repository's own git identity (`scarffy`), on the working branch.
- Unity behaviour can't be run here. Say plainly what was only compile-checked and what still needs a test in the Unity editor.
