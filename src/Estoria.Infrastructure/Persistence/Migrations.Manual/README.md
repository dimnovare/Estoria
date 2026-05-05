# Manual migrations

SQL files in this folder are **NOT** auto-applied. Run them once, by hand,
against the target environment when described by the matching task in the
plan doc.

These exist for one-shot data fixes that don't fit the EF migration model —
typically because the seeder short-circuits on existing data, so a code-only
change can't reach rows already in production.
