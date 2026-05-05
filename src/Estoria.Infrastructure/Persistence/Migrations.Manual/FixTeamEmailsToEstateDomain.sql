-- One-shot: rewrite team member emails from the legacy .ee domain to
-- the canonical .estate domain. Safe to re-run; affects only rows that
-- still match the old pattern. Existing prod data won't be touched by
-- the seeder because it short-circuits when PageContents already exist.
UPDATE "TeamMembers"
SET "Email" = REPLACE("Email", '@estoria.ee', '@estoria.estate'),
    "UpdatedAt" = NOW()
WHERE "Email" LIKE '%@estoria.ee';
