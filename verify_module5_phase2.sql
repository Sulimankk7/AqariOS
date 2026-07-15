-- ==========================================
-- PROPERTYOS - MODULE 5 PHASE 2 VERIFICATION
-- ==========================================

-- 1. MIGRATION HISTORY
-- FIX: Single PostgreSQL identifier double-quotes (not doubled)
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20260706204019_Module5_Phase2_LeaseContracts_Foundation';

-- 2. TABLE INVENTORY
SELECT table_schema, table_name
FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name IN ('lease_contracts', 'contract_documents', 'contract_terminations', 'contract_status_history')
ORDER BY table_name;

-- 3a. MODULE 5 PHASE 2 ENUM IDENTITY VERIFICATION
-- Expected: exactly six enum types in schema = public with correct ordered labels.
SELECT
    n.nspname                                                AS schema_name,
    t.typname                                                AS type_name,
    string_agg(e.enumlabel, ', ' ORDER BY e.enumsortorder)  AS ordered_labels
FROM pg_type      t
JOIN pg_enum      e ON e.enumtypid = t.oid
JOIN pg_namespace n ON n.oid       = t.typnamespace
WHERE t.typname IN (
    'contract_document_type_enum',
    'contract_status_enum',
    'legal_regime_enum',
    'payment_frequency_enum',
    'tenant_type_enum',
    'termination_type_enum'
)
GROUP BY n.nspname, t.typname
ORDER BY t.typname;

-- 3b. MODULE 5 PHASE 2 MALFORMED IDENTITY SEARCH
-- Searches specifically for accidental identities for the six Module 5 enum families.
-- A clean Module 5 Phase 2 database returns ZERO rows from this query.
SELECT
    n.nspname   AS schema_name,
    t.typname   AS type_name,
    'MALFORMED_SCHEMA' AS finding_category
FROM pg_type      t
JOIN pg_namespace n ON n.oid = t.typnamespace
WHERE n.nspname IN (
    'contract_document_type_enum',
    'contract_status_enum',
    'legal_regime_enum',
    'payment_frequency_enum',
    'tenant_type_enum',
    'termination_type_enum'
)

UNION ALL

-- Enum types for Module 5 families missing the _enum suffix (stripped CLR-derived names)
SELECT
    n.nspname   AS schema_name,
    t.typname   AS type_name,
    'MALFORMED_TYPE_NAME' AS finding_category
FROM pg_type      t
JOIN pg_namespace n ON n.oid = t.typnamespace
WHERE t.typtype = 'e'
  AND t.typname IN (
    'contract_document_type',
    'contract_status',
    'legal_regime',
    'payment_frequency',
    'tenant_type',
    'termination_type'
  )

UNION ALL

-- Module 5 enum families duplicated outside public schema
SELECT
    n.nspname   AS schema_name,
    t.typname   AS type_name,
    'DUPLICATE_OUTSIDE_PUBLIC' AS finding_category
FROM pg_type      t
JOIN pg_namespace n ON n.oid = t.typnamespace
WHERE t.typtype = 'e'
  AND t.typname IN (
    'contract_document_type_enum',
    'contract_status_enum',
    'legal_regime_enum',
    'payment_frequency_enum',
    'tenant_type_enum',
    'termination_type_enum'
  )
  AND n.nspname <> 'public'

ORDER BY finding_category, schema_name, type_name;

-- 3c. LEGACY ENUM IDENTITY INVENTORY
-- DIAGNOSTIC ONLY, NOT MODULE 5 FAILURE
-- Reports accidental enum schema/type identities from Modules 1-4.
-- These are a pre-existing historical pattern from the old HasPostgresEnum positional-argument
-- misuse and do NOT indicate a Module 5 Phase 2 defect.
SELECT
    n.nspname   AS schema_name,
    t.typname   AS type_name,
    t.typtype   AS type_kind
FROM pg_type      t
JOIN pg_namespace n ON n.oid = t.typnamespace
WHERE
    -- Schemas that are themselves named after a known Module 1-4 _enum family
    (n.nspname LIKE '%_enum' AND n.nspname NOT IN ('pg_catalog', 'information_schema', 'public'))
    OR
    -- Enum types that carry CLR-stripped names for known Module 1-4 families
    (t.typtype = 'e'
     AND t.typname IN (
        'audit_action',   'audit_severity',   'audit_source',
        'billing_cycle',
        'building_type',
        'company_type',
        'floor_type',
        'governorate',
        'late_fee_type',
        'login_status',
        'membership_status',
        'mfa_type',
        'occupancy_status',
        'otp_purpose',
        'ownership_status',
        'parking_assignment_status',
        'parking_type',
        'revoke_reason',
        'subscription_status'
     )
    )
ORDER BY n.nspname, t.typname;

-- 4. COLUMN CONTRACT
SELECT
    c.table_name,
    c.column_name,
    c.data_type,
    c.udt_name,
    c.is_nullable,
    c.column_default
FROM information_schema.columns c
WHERE c.table_schema = 'public'
  AND c.table_name IN ('lease_contracts', 'contract_documents', 'contract_terminations', 'contract_status_history')
ORDER BY c.table_name, c.ordinal_position;

-- 5. CONSTRAINTS (OID-SAFE via pg_constraint.conrelid -> pg_class.oid -> pg_namespace.oid)
SELECT
    n.nspname                              AS schema_name,
    c.relname                              AS table_name,
    con.conname                            AS constraint_name,
    con.contype                            AS constraint_type,
    pg_get_constraintdef(con.oid, true)    AS constraint_definition,
    con.confupdtype                        AS fk_update_action,
    con.confdeltype                        AS fk_delete_action
FROM pg_constraint con
JOIN pg_class     c ON c.oid = con.conrelid
JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname = 'public'
  AND c.relname IN ('lease_contracts', 'contract_documents', 'contract_terminations', 'contract_status_history')
ORDER BY c.relname, con.contype, con.conname;

-- 6a. INDEX INVENTORY (pg_indexes for human-readable definitions)
SELECT
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'public'
  AND tablename IN ('lease_contracts', 'contract_documents', 'contract_terminations', 'contract_status_history')
ORDER BY tablename, indexname;

-- 6b. INDEX DETAIL (OID-SAFE: access method, operator class, predicate)
SELECT
    n.nspname                                      AS schema_name,
    t.relname                                      AS table_name,
    i.relname                                      AS index_name,
    am.amname                                      AS access_method,
    pg_get_indexdef(idx.indexrelid)                AS index_definition,
    pg_get_expr(idx.indpred, idx.indrelid)         AS index_predicate,
    (
        SELECT string_agg(opc.opcname, ', ' ORDER BY u.ord)
        FROM unnest(idx.indclass::int[]) WITH ORDINALITY AS u(opcoid, ord)
        JOIN pg_opclass opc ON opc.oid = u.opcoid
    )                                              AS operator_classes
FROM pg_index     idx
JOIN pg_class     i  ON i.oid  = idx.indexrelid
JOIN pg_class     t  ON t.oid  = idx.indrelid
JOIN pg_namespace n  ON n.oid  = t.relnamespace
JOIN pg_am        am ON am.oid = i.relam
WHERE n.nspname = 'public'
  AND t.relname IN ('lease_contracts', 'contract_documents', 'contract_terminations', 'contract_status_history')
ORDER BY t.relname, i.relname;

-- 7. OCCUPANCY TRIGGER (OID-SAFE via pg_trigger.tgfoid = pg_proc.oid)
-- Does NOT assume function name; discovers it from the trigger's tgfoid.
SELECT
    n.nspname                              AS schema_name,
    c.relname                              AS table_name,
    trg.tgname                             AS trigger_name,
    pg_get_triggerdef(trg.oid, true)       AS trigger_definition,
    pn.nspname                             AS function_schema,
    p.proname                              AS function_name,
    pg_get_functiondef(p.oid)              AS function_definition
FROM pg_trigger   trg
JOIN pg_class     c   ON c.oid  = trg.tgrelid
JOIN pg_namespace n   ON n.oid  = c.relnamespace
JOIN pg_proc      p   ON p.oid  = trg.tgfoid
JOIN pg_namespace pn  ON pn.oid = p.pronamespace
WHERE n.nspname = 'public'
  AND c.relname IN ('lease_contracts', 'contract_documents', 'contract_terminations', 'contract_status_history')
  AND NOT trg.tgisinternal;

-- 8. ROW-LEVEL SECURITY
SELECT
    c.relname                                       AS table_name,
    c.relrowsecurity,
    c.relforcerowsecurity,
    pol.polname                                     AS policy_name,
    pol.polcmd                                      AS command,
    r.rolname                                       AS role,
    pg_get_expr(pol.polqual,      pol.polrelid)     AS using_expression,
    pg_get_expr(pol.polwithcheck, pol.polrelid)     AS with_check_expression
FROM pg_class     c
JOIN pg_namespace n   ON n.oid = c.relnamespace
LEFT JOIN pg_policy pol ON pol.polrelid = c.oid
LEFT JOIN pg_roles  r   ON r.oid = ANY(pol.polroles)
WHERE n.nspname = 'public'
  AND c.relname IN ('lease_contracts', 'contract_documents', 'contract_terminations', 'contract_status_history')
ORDER BY c.relname, pol.polname;

-- 9. TABLE OWNERSHIP
SELECT
    c.relname   AS table_name,
    a.rolname   AS owner
FROM pg_class     c
JOIN pg_roles     a ON a.oid = c.relowner
JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname = 'public'
  AND c.relname IN ('lease_contracts', 'contract_documents', 'contract_terminations', 'contract_status_history')
ORDER BY c.relname;

-- 10a. TABLE PRIVILEGE INVENTORY (information_schema.role_table_grants)
SELECT
    grantee,
    table_name,
    privilege_type
FROM information_schema.role_table_grants
WHERE table_schema = 'public'
  AND table_name IN ('lease_contracts', 'contract_documents', 'contract_terminations', 'contract_status_history')
  AND grantee IN ('propertyos_app', 'propertyos_auth')
ORDER BY grantee, table_name, privilege_type;

-- 10b. TABLE PRIVILEGE ACL AUDIT (has_table_privilege)
WITH roles(role_name) AS (
    VALUES ('propertyos_app'::text), ('propertyos_auth'::text)
),
tbls(table_name) AS (
    VALUES
        ('lease_contracts'::text),
        ('contract_documents'::text),
        ('contract_terminations'::text),
        ('contract_status_history'::text)
),
privs(priv) AS (
    VALUES
        ('SELECT'::text),
        ('INSERT'::text),
        ('UPDATE'::text),
        ('DELETE'::text),
        ('TRUNCATE'::text),
        ('REFERENCES'::text),
        ('TRIGGER'::text)
)
SELECT
    r.role_name,
    tb.table_name,
    p.priv,
    has_table_privilege(r.role_name, tb.table_name, p.priv) AS has_privilege
FROM roles r
CROSS JOIN tbls  tb
CROSS JOIN privs p
ORDER BY r.role_name, tb.table_name, p.priv;

-- 10c. COLUMN-LEVEL UPDATE PRIVILEGE AUDIT (OID-SAFE via pg_class + pg_attribute + attnum)
-- FIX: Uses has_column_privilege(role, table_oid, attnum, privilege) overload.
-- Does NOT construct 'table.column' text strings. Excludes system and dropped columns.
SELECT
    r.rolname                                                        AS role_name,
    cls.relname                                                      AS table_name,
    att.attname                                                      AS column_name,
    att.attnum                                                       AS attnum,
    has_column_privilege(r.rolname, cls.oid, att.attnum, 'UPDATE')   AS has_update_privilege
FROM pg_class     cls
JOIN pg_namespace ns  ON ns.oid       = cls.relnamespace
JOIN pg_attribute att ON att.attrelid = cls.oid
CROSS JOIN pg_roles r
WHERE ns.nspname = 'public'
  AND cls.relname IN ('lease_contracts', 'contract_documents', 'contract_terminations', 'contract_status_history')
  AND att.attnum > 0
  AND NOT att.attisdropped
  AND r.rolname IN ('propertyos_app', 'propertyos_auth')
ORDER BY r.rolname, cls.relname, att.attnum;

-- 11. PG_TRGM EXTENSION
SELECT
    e.extname   AS extension_name,
    n.nspname   AS schema_name
FROM pg_extension e
JOIN pg_namespace n ON n.oid = e.extnamespace
WHERE e.extname = 'pg_trgm';