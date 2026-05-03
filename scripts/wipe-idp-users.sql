-- ============================================================================
-- wipe-idp-users.sql — delete AspNetUsers from HmmIdp + every dependent record.
--
-- Use cases:
--   * Test cleanup — drop fchy/fchy2 etc. so you can re-register with the same
--     email/username.
--   * Reset a stuck account whose verification flow was interrupted.
--
-- DOES NOT touch IdentityServer config (Clients, ApiResources, Scopes…) or the
-- HmmNotes API database.
--
-- Run:
--   docker exec -i hmm-idp su postgres -c 'psql -h 127.0.0.1 -d HmmIdp' \
--     < scripts/wipe-idp-users.sql
--
-- Or interactively (lets you eyeball the row counts before COMMIT):
--   docker exec -it hmm-idp su postgres -c 'psql -h 127.0.0.1 -d HmmIdp'
--   \i /tmp/wipe-idp-users.sql
-- ============================================================================

\echo '================================================================'
\echo '  Hmm.Idp user wipe'
\echo '================================================================'

BEGIN;

-- ----------------------------------------------------------------------------
-- 1.  Pick which users to delete.
--
-- Adjust the WHERE clause below to whatever you need. Examples (commented):
--
--   * Wipe a single user by username:
--       WHERE u."UserName" = 'fchy2'
--
--   * Wipe a single user by email:
--       WHERE u."Email" = 'fchy@outlook.com'
--
--   * Wipe several users:
--       WHERE u."UserName" IN ('fchy', 'fchy2', 'fchy3')
--
--   * Wipe every unconfirmed account:
--       WHERE u."EmailConfirmed" = false
--
--   * Wipe everyone EXCEPT the SeedDataService accounts (DEFAULT):
--       WHERE u."UserName" NOT IN
--             ('admin@hmm.local','testuser@hmm.local','alice','bob',
--              'serviceapi@hmm.local')
--
--   * Wipe absolutely everyone (including seeds — re-seeded on next IDP start):
--       WHERE 1=1
-- ----------------------------------------------------------------------------

-- Materialize the target user-id list once so we don't re-evaluate the
-- predicate against a moving target as we cascade through the child tables.
CREATE TEMP TABLE _victims (id text PRIMARY KEY) ON COMMIT DROP;

INSERT INTO _victims (id)
SELECT u."Id"
FROM "AspNetUsers" u
WHERE u."UserName" NOT IN
      ('admin@hmm.local',
       'testuser@hmm.local',
       'alice',
       'bob',
       'serviceapi@hmm.local');
-- ^^^ EDIT THIS WHERE CLAUSE if you want a different selection.

\echo ''
\echo 'Targeting users (will be deleted at COMMIT):'
SELECT u."UserName", u."Email", u."EmailConfirmed"
  FROM "AspNetUsers" u
  JOIN _victims v ON u."Id" = v.id
 ORDER BY u."UserName";

-- ----------------------------------------------------------------------------
-- 2.  Cascade through children.  Order matters: dependent rows first,
--     parents last.
--
-- AspNetUserClaims, AspNetUserLogins, AspNetUserRoles, AspNetUserTokens, and
-- UserClaims all have FK constraints to AspNetUsers.Id — Postgres would refuse
-- the AspNetUsers DELETE otherwise.
--
-- PersistedGrants and ServerSideSessions store the user's SubjectId as a plain
-- string with NO FK constraint, so they wouldn't block the delete — but
-- leaving stale refresh tokens / sessions around for a now-nonexistent user
-- is gross.  Clear them too.
-- ----------------------------------------------------------------------------

\echo ''
\echo 'Cascading dependent rows...'

DELETE FROM "AspNetUserClaims"  WHERE "UserId" IN (SELECT id FROM _victims);
DELETE FROM "AspNetUserLogins"  WHERE "UserId" IN (SELECT id FROM _victims);
DELETE FROM "AspNetUserRoles"   WHERE "UserId" IN (SELECT id FROM _victims);
DELETE FROM "AspNetUserTokens"  WHERE "UserId" IN (SELECT id FROM _victims);
DELETE FROM "UserClaims"        WHERE "UserId" IN (SELECT id FROM _victims);

-- IdentityServer state keyed by SubjectId (no FK; safe to clean).
DELETE FROM "PersistedGrants"   WHERE "SubjectId" IN (SELECT id FROM _victims);
DELETE FROM "ServerSideSessions" WHERE "SubjectId" IN (SELECT id FROM _victims);

-- ----------------------------------------------------------------------------
-- 3.  Finally, the parent.
-- ----------------------------------------------------------------------------

DELETE FROM "AspNetUsers" WHERE "Id" IN (SELECT id FROM _victims);

\echo ''
\echo 'Remaining users:'
SELECT "UserName", "Email", "EmailConfirmed"
  FROM "AspNetUsers"
 ORDER BY "UserName";

-- ----------------------------------------------------------------------------
-- 4.  Commit (or ROLLBACK if you opened this interactively and don't like
--     what you see above).
-- ----------------------------------------------------------------------------
COMMIT;

\echo ''
\echo 'Wipe complete.'
