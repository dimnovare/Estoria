-- One-shot: align production data with the legal entity registered at
-- inforegister.ee — ESTORIA CAPITAL GROUP OÜ, registry code 17477775,
-- registered office at Katusepapi tn 6, Tallinn 11412.
--
-- Idempotent: only touches rows that still hold the placeholder Kotzebue
-- address. Re-running does nothing once data is migrated.
--
-- Required because:
--   * SiteSettings has per-key idempotent guards in DataSeeder, so the
--     existing contact.address row won't be overwritten by a seed run.
--   * PageContents.contact.info won't be re-seeded either — the seeder
--     short-circuits when homepage.hero exists.
--   * The two new legal.* keys are added by the seeder on next deploy,
--     but we INSERT them here too so this script alone is sufficient
--     even if you run it before deploying the new build.

BEGIN;

-- 1. Update the canonical contact.address setting.
UPDATE "SiteSettings"
SET "Value"     = 'Katusepapi 6, Tallinn 11412, Estonia',
    "UpdatedAt" = NOW()
WHERE "Key"   = 'contact.address'
  AND "Value" = 'Kotzebue 4, Tallinn 10412, Estonia';

-- 2. Insert legal.* keys if missing. ON CONFLICT covers the case where a
--    later seeder run already inserted them.
INSERT INTO "SiteSettings" ("Id", "Key", "Value", "ValueType", "CreatedAt", "UpdatedAt")
VALUES
    (gen_random_uuid(), 'legal.company_name',  'ESTORIA CAPITAL GROUP OÜ', 0, NOW(), NOW()),
    (gen_random_uuid(), 'legal.registry_code', '17477775',                 0, NOW(), NOW())
ON CONFLICT ("Key") DO NOTHING;

-- 3. Update the contact.info PageContent translations — only rows still
--    holding the Kotzebue address.
UPDATE "PageContentTranslations" t
SET "Body" = REPLACE(
                REPLACE(t."Body", 'Kotzebue 4, Tallinn 10412', 'Katusepapi 6, Tallinn 11412'),
                'Kotzebue 4, Таллин 10412', 'Катусепапи 6, Таллин 11412'
             ),
    "UpdatedAt" = NOW()
FROM "PageContents" p
WHERE t."PageContentId" = p."Id"
  AND p."PageKey"       = 'contact.info'
  AND t."Body" LIKE '%Kotzebue 4%';

-- Estonian "Kotzebue 4, Tallinn 10412, Eesti" is covered by the first
-- REPLACE — same prefix as English. RU has its own transliterated form
-- handled by the second REPLACE in the chain.

COMMIT;
