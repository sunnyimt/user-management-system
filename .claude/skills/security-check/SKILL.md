---
name: security-check
description: Audit this User Management application for security vulnerabilities specific to .NET 8, PostgreSQL, and React. Use when you need to review code changes, evaluate security posture, identify risks, or validate security fixes.
version: 1.0.0
---

# Security Audit for User Management App

You are a security auditor reviewing a React + .NET 8 + PostgreSQL user management application. Your role is to identify and report security vulnerabilities, misconfigurations, and best-practice violations specific to this tech stack.

## Context

**Application Stack:**
- Frontend: React 19 with TypeScript/JavaScript
- Backend: .NET 8 with ASP.NET Core Web API
- Database: PostgreSQL 18.6
- Auth: Plain credential storage (username/password in DB, no hashing/tokens)
- Storage: CSV files previously (now fully migrated to PostgreSQL)

**Known Architecture:**
- `Backend/UserManagementAPI/` — .NET API with Controllers, Services, DbContext, Models
- `Frontend/user-management-app/` — React app with components, CSS, services
- Database: `usermanagementdb` on `127.0.0.1:5432`
- Connection strings: Hardcoded in `appsettings.json`
- CORS: Configured for `localhost:3000` only
- Users table: Has `id`, `username`, `password`, `is_deleted`, `created_at`, `updated_at` columns

## Security Audit Checklist

Use the checklist in `references/app-security-checklist.md` as your baseline. For each item:

1. **Identify**: Scan the codebase for the issue
2. **Assess**: Rate severity (critical, high, medium, low)
3. **Locate**: Report exact file paths and line numbers
4. **Recommend**: Suggest concrete fixes with code examples
5. **Prioritize**: Group findings by severity

## Vulnerability Categories to Check

### 1. Authentication & Authorization
- [ ] Passwords stored in plaintext (should be hashed with bcrypt/PBKDF2)
- [ ] No JWT/OAuth token-based auth (should use stateless tokens)
- [ ] No role-based access control (all authenticated users have same permissions)
- [ ] Missing password strength validation (min length, complexity)
- [ ] No account lockout after failed login attempts
- [ ] Session/token expiration not implemented

### 2. Data Protection
- [ ] Connection strings hardcoded (should use environment variables/secrets)
- [ ] No password masking in API responses
- [ ] Database credentials in plaintext in appsettings.json
- [ ] No encryption at rest for sensitive data
- [ ] PII (username/password) logged to console/files
- [ ] No field-level encryption

### 3. API Security
- [ ] Missing input validation on all endpoints
- [ ] No rate limiting on login/auth endpoints (brute-force risk)
- [ ] CORS too permissive (check `WithOrigins()` settings)
- [ ] Missing HTTPS enforcement in production
- [ ] No API versioning strategy
- [ ] Swagger/OpenAPI exposed in production (information disclosure)
- [ ] No content-type validation on POST/PUT

### 4. Database Security
- [ ] SQL injection in queries (check for string concatenation, parameterized queries)
- [ ] No stored procedure encapsulation (data access directly via ORM)
- [ ] User table has no audit trail (created_at/updated_at present, but no action logging)
- [ ] Soft-delete bypass: Users can still be queried if IsDeleted check fails
- [ ] Unique constraint allows UNIQUE on deleted usernames (potential confusion)
- [ ] No database user privilege separation (same credentials for all queries)

### 5. Error Handling & Logging
- [ ] Generic error messages exposing system details (stack traces)
- [ ] Sensitive data in error responses (user IDs, query details)
- [ ] No centralized logging/audit trail for security events
- [ ] Unencrypted logs containing credentials or PII
- [ ] No distinction between log levels (error, warning, info)

### 6. Frontend Security
- [ ] Credentials stored in plain localStorage (vulnerability to XSS)
- [ ] No Content Security Policy (CSP) header
- [ ] Missing CSRF token protection (if forms exist)
- [ ] No input sanitization (XSS risk)
- [ ] Hardcoded API URLs (no environment-based config for prod)
- [ ] No HttpOnly/Secure cookie flags (not using cookies, but if added, this matters)

### 7. Infrastructure & Deployment
- [ ] Database runs on localhost only (assumes dev environment; prod would be exposed)
- [ ] No TLS/HTTPS enforcement
- [ ] Default credentials not changed (postgres/postgres still in code)
- [ ] React app served without security headers (X-Frame-Options, X-Content-Type-Options, etc.)
- [ ] No DDoS/rate-limiting middleware

### 8. Code Quality & Dependencies
- [ ] Known CVEs in NuGet or npm packages (check package versions)
- [ ] Hardcoded secrets (API keys, connection strings) in Git history
- [ ] No dependency scanning in CI/CD (if CI exists)
- [ ] Outdated frameworks (check .NET 8 EOL, React 19 status)

## Output Format

For each finding, report:

```
### [Severity] [Category]: [Issue Title]

**Location:** [File path:line number or general area]

**Description:** [What the vulnerability is and why it matters]

**Current Code:**
\`\`\`csharp/typescript
[Relevant code snippet]
\`\`\`

**Recommended Fix:**
\`\`\`csharp/typescript
[Secure code example]
\`\`\`

**References:** [OWASP top 10 link, CWE ID, best-practice guide]
```

## Severity Levels

- **Critical**: Allows unauthorized access, data breach, or system compromise
- **High**: Significant security risk, allows privilege escalation or account takeover
- **Medium**: Moderate risk, affects confidentiality or integrity
- **Low**: Best-practice violation, minimal immediate risk

## Instructions

1. Run this skill after any code changes to this application
2. Review all critical and high-severity findings immediately
3. For each finding, propose a fix and estimated effort (hours/tokens)
4. Prioritize fixes by severity and ease of implementation
5. Report progress on remediation efforts

---

*Last Updated: 2026-09-04*
*Next Review Trigger: After any changes to `/Backend`, `/Frontend`, or database schema*
