# Database Role Provisioning Contract

This contract defines the mandatory PostgreSQL roles that must be provisioned by the Infrastructure-as-Code (IaC) layer prior to deploying PropertyOS migrations.

The EF Core migrations **do not** and **must not** create these roles, as embedding passwords or login credentials in migrations violates security policies.

## Required Roles

### 1. `propertyos_owner`
- **Type**: NOLOGIN
- **Purpose**: Owns all application schema objects (tables, functions, types).
- **Permissions**: Fully controls schema. Used during migration execution.
- **Grants**: `GRANT INSERT ON audit_logs TO propertyos_owner;` (assigned by migration for the `SECURITY DEFINER` function).

### 2. `propertyos_app`
- **Type**: LOGIN
- **Attributes**: NOSUPERUSER, NOBYPASSRLS
- **Purpose**: The primary runtime identity for the PropertyOS API.
- **Permissions**: Subject to Row-Level Security (RLS). Limited access to tenant data based on current session context.
- **Grants**: `GRANT EXECUTE ON FUNCTION insert_audit_log TO propertyos_app;` (assigned by migration).

### 3. `propertyos_auth`
- **Type**: LOGIN
- **Attributes**: NOSUPERUSER, NOBYPASSRLS
- **Purpose**: The dedicated runtime identity for authentication operations (login, MFA, token refresh).
- **Permissions**: Subject to RLS. Has explicit access to security/identity tables (e.g., `users`, `refresh_tokens`, `otp_challenges`, `login_history`).
- **Grants**: `GRANT EXECUTE ON FUNCTION insert_audit_log TO propertyos_auth;` (assigned by migration).

## Deployment Instructions

Prior to running `dotnet ef database update`, the following SQL must be executed by a database superuser or equivalent provisioning tool:

```sql
CREATE ROLE propertyos_owner WITH NOLOGIN;
CREATE ROLE propertyos_app WITH LOGIN PASSWORD 'secure_app_password' NOSUPERUSER NOBYPASSRLS;
CREATE ROLE propertyos_auth WITH LOGIN PASSWORD 'secure_auth_password' NOSUPERUSER NOBYPASSRLS;
```

These passwords should be injected via secure secrets management (e.g., AWS Secrets Manager) and consumed by the respective connection strings in the API configuration.
