-- 1. Migration History
SELECT "MigrationId"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%Module5_Leasing_Tenant_Foundation%';

-- 2. Tables, Columns, Nullability, UUID Defaults
SELECT table_name, column_name, data_type, is_nullable, column_default 
FROM information_schema.columns 
WHERE table_schema = 'public'
  AND table_name IN ('tenants', 'tenant_family_members', 'tenant_emergency_contacts', 'tenant_vehicles')
ORDER BY table_name, ordinal_position;

-- 3. Constraints (Candidate Keys, CHECK constraints, FKs, Delete Actions)
SELECT
    t.relname AS table_name,
    c.conname AS constraint_name,
    c.contype AS constraint_type,
    pg_get_constraintdef(c.oid) AS constraint_definition
FROM pg_constraint c
JOIN pg_class t ON c.conrelid = t.oid
JOIN pg_namespace n ON t.relnamespace = n.oid
WHERE n.nspname = 'public'
  AND t.relname IN ('tenants', 'tenant_family_members', 'tenant_emergency_contacts', 'tenant_vehicles')
ORDER BY t.relname, c.contype, c.conname;

-- 4. Indexes and Partial Predicates
SELECT
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'public'
  AND tablename IN ('tenants', 'tenant_family_members', 'tenant_emergency_contacts', 'tenant_vehicles')
ORDER BY tablename, indexname;

-- 5. RLS Enabled and Forced
SELECT
    t.relname AS table_name,
    t.relrowsecurity AS rls_enabled,
    t.relforcerowsecurity AS rls_forced
FROM pg_class t
JOIN pg_namespace n ON t.relnamespace = n.oid
WHERE n.nspname = 'public'
  AND t.relname IN ('tenants', 'tenant_family_members', 'tenant_emergency_contacts', 'tenant_vehicles')
ORDER BY t.relname;

-- 6. Exact RLS Policies
SELECT
    cls.relname AS table_name,
    pol.polname AS policy_name,
    CASE pol.polcmd
        WHEN 'r' THEN 'SELECT'
        WHEN 'a' THEN 'INSERT'
        WHEN 'w' THEN 'UPDATE'
        WHEN 'd' THEN 'DELETE'
        WHEN '*' THEN 'ALL'
    END AS command,
    (SELECT string_agg(pg_get_userbyid(r), ',') FROM unnest(pol.polroles) AS r) AS roles,
    pg_get_expr(pol.polqual, pol.polrelid) AS using_expression,
    pg_get_expr(pol.polwithcheck, pol.polrelid) AS with_check_expression
FROM pg_policy pol
JOIN pg_class cls ON pol.polrelid = cls.oid
JOIN pg_namespace n ON cls.relnamespace = n.oid
WHERE n.nspname = 'public'
  AND cls.relname IN ('tenants', 'tenant_family_members', 'tenant_emergency_contacts', 'tenant_vehicles')
ORDER BY cls.relname, pol.polname;

-- 7. Table Ownership
SELECT
    t.relname AS table_name,
    pg_get_userbyid(t.relowner) AS owner
FROM pg_class t
JOIN pg_namespace n ON t.relnamespace = n.oid
WHERE n.nspname = 'public'
  AND t.relname IN ('tenants', 'tenant_family_members', 'tenant_emergency_contacts', 'tenant_vehicles')
ORDER BY t.relname;

-- 8. Table Privileges (propertyos_app, propertyos_auth)
SELECT
    table_name,
    grantee,
    privilege_type
FROM information_schema.role_table_grants
WHERE table_schema = 'public'
  AND table_name IN ('tenants', 'tenant_family_members', 'tenant_emergency_contacts', 'tenant_vehicles')
  AND grantee IN ('propertyos_app', 'propertyos_auth')
ORDER BY table_name, grantee, privilege_type;
