# User Management App — Security Checklist

Reference guide for the security-check skill. Use this to ensure comprehensive coverage of known risks.

## Backend (.NET 8) Security Checklist

### File: `Program.cs`
- [ ] No hardcoded secrets in dependency injection setup
- [ ] CORS policy restricted to known origins only
- [ ] HTTPS redirection enabled for production
- [ ] Swagger UI disabled in production
- [ ] Security headers middleware present (if applicable)
- [ ] Rate limiting middleware configured
- [ ] Exception handling doesn't expose stack traces

### File: `appsettings.json`
- [ ] No database credentials in plaintext
- [ ] Connection strings use environment variable substitution (if deployed)
- [ ] API version not hardcoded
- [ ] Logging level appropriate (not "Debug" in production)
- [ ] Third-party API keys not stored here (use secrets manager)

### File: `Models/User.cs`
- [ ] Password property is string, not a secure type (✗ should use hashed storage)
- [ ] No [Sensitive] data annotations (could help with logging frameworks)
- [ ] IsDeleted field present (soft-delete support)
- [ ] No plaintext password in response objects (strip before returning)

### File: `Data/ApplicationDbContext.cs`
- [ ] DbContext uses parameterized queries (EF Core does this by default ✓)
- [ ] No raw SQL strings (no string concatenation for WHERE clauses)
- [ ] Column names match DB schema exactly (is_deleted, created_at, updated_at)
- [ ] Unique constraints properly configured (username is UNIQUE ✓)
- [ ] No credentials hardcoded in OnModelCreating

### File: `Services/UserService.cs`
- [ ] All queries use async/await
- [ ] No SQL injection vectors (EF Core parameterizes, so ✓)
- [ ] Passwords not logged in error messages
- [ ] Soft-delete filter applied consistently (GetAllUsersAsync, GetUserByIdAsync filter on IsDeleted ✓)
- [ ] CreateUserAsync validates input before insert (should add field validation)
- [ ] DeleteUserAsync performs soft-delete, not hard-delete ✓

### File: `Controllers/AuthController.cs`
- [ ] Login validates both username AND password (not just username)
- [ ] Login checks IsDeleted flag (blocks deleted user login ✓)
- [ ] Failed login returns generic "Invalid username or password" (not "user not found")
- [ ] No password returned in LoginResponse (only Success, Message, User — check if User includes password)
- [ ] No brute-force protection (should add after N failed attempts)
- [ ] Password compared in plaintext (should be hashed comparison)

### File: `Controllers/UsersController.cs`
- [ ] CreateUser prevents duplicate usernames ✓
- [ ] CreateUser validates username/password not empty ✓
- [ ] UpdateUser checks ownership (only admins or self can update — currently no role check)
- [ ] DeleteUser returns 204 No Content (✓, doesn't expose user data)
- [ ] All endpoints require authentication (currently none do — potential issue)
- [ ] Input validation on username length/format

### Database (`usermanagementdb`)
- [ ] `users` table has `is_deleted` column ✓
- [ ] `username` column is UNIQUE ✓
- [ ] `password` column stores plaintext (should be hashed)
- [ ] No sensitive data in `created_at`/`updated_at` — these are audit fields ✓
- [ ] PostgreSQL user has minimal privileges (currently using shared postgres/postgres)
- [ ] Backups encrypted and access-controlled
- [ ] Connection requires credentials (postgres/postgres still hardcoded ✗)

---

## Frontend (React) Security Checklist

### File: `src/services/api.js`
- [ ] API base URL not hardcoded (should read from .env)
- [ ] No credentials in request headers (currently not, but if JWT added, ensure HttpOnly)
- [ ] No sensitive data in URL query strings
- [ ] Credentials not sent in localStorage (currently not storing session)
- [ ] All API calls use HTTPS (localhost:5278 is HTTP in dev ✗)
- [ ] Timeout on network requests
- [ ] Request logging doesn't expose sensitive data

### File: `src/components/Login.js`
- [ ] No password pre-filled in production (currently prefilled with "admin" in demo code ✗)
- [ ] Password field uses `type="password"` (✓)
- [ ] Login attempt throttling (no current protection against brute-force)
- [ ] Failed login shows generic message (✓)
- [ ] No credentials stored after login (currently not, but if storing, must use HttpOnly cookies)

### File: `src/components/UserManagement.js`
- [ ] Delete confirmation dialog present (✓)
- [ ] No sensitive data in console.log statements
- [ ] Passwords masked in table display (✓ shows dots instead of plaintext)
- [ ] Only logged-in users see the component (no auth guard implemented)
- [ ] Role-based visibility (currently all users see all users, no admin-only views)

### `.env` file
- [ ] No plaintext secrets in .env.example
- [ ] .env file excluded from Git (.gitignore)
- [ ] Environment-specific configs for dev/staging/prod
- [ ] No hardcoded API URLs (should use env var REACT_APP_API_URL)

### General React
- [ ] No eval() or dangerouslySetInnerHTML
- [ ] Input fields sanitized before display (no XSS risk)
- [ ] No sensitive data in component state (if debugging in dev tools)
- [ ] CSP headers configured (likely missing)
- [ ] X-Frame-Options header configured (likely missing)

---

## Known Issues & To-Do

### Critical (Fix Immediately)
- [ ] **Plaintext passwords**: Migrate to bcrypt/PBKDF2 hashing
- [ ] **No authentication on API**: Add JWT or session-based auth to all endpoints
- [ ] **Hardcoded DB credentials**: Move postgres/postgres to environment variables
- [ ] **Admin prefill in login form**: Remove "admin" autofill from login demo

### High (Fix Soon)
- [ ] **No rate limiting**: Add brute-force protection on login endpoint
- [ ] **No role-based access**: Implement admin vs. regular user roles
- [ ] **Password in API response**: Strip password field before returning User objects
- [ ] **HTTP in dev (localhost)**: Use HTTPS even locally; generate self-signed cert

### Medium (Fix Before Production)
- [ ] **No audit logging**: Log all security events (login, delete, admin actions)
- [ ] **Missing input validation**: Add regex/format checks for username, password length
- [ ] **Swagger exposed**: Disable Swagger UI in production builds
- [ ] **CORS open to localhost**: Narrow if deploying to separate domain

### Low (Best Practice)
- [ ] **No HSTS header**: Add Strict-Transport-Security in production
- [ ] **No CSP header**: Implement Content-Security-Policy
- [ ] **Dependency scanning**: Run `npm audit` and `dotnet list package --vulnerable`
- [ ] **API versioning**: Add `/api/v1/` versioning prefix

---

## Testing & Validation

### Manual Security Tests
1. **Test 1**: Try to login with SQL injection payload in username field → Should fail cleanly
2. **Test 2**: Delete a user, then try to login as that user → Should return 401
3. **Test 3**: Try to create a new user with existing username → Should return 400
4. **Test 4**: Inspect network traffic (F12 DevTools) → Credentials should not appear in URLs or headers
5. **Test 5**: Try to access API endpoints without logging in (if auth is added) → Should return 401
6. **Test 6**: Attempt to update/delete another user's data (if implemented) → Should return 403

### Automated Checks
```bash
# Check for known CVEs in dependencies
npm audit
dotnet list package --vulnerable

# Check for hardcoded secrets
grep -r "postgres" --include="*.cs" --include="*.json" Backend/
grep -r "admin" --include="*.js" Frontend/
```

---

## Reference Links

- OWASP Top 10 (2021): https://owasp.org/www-project-top-ten/
- OWASP .NET Security Cheat Sheet: https://cheatsheetseries.owasp.org/cheatsheets/Dotnet_Security_Cheat_Sheet.html
- PostgreSQL Security: https://www.postgresql.org/docs/current/sql-syntax.html#SQL-SYNTAX-IDENTIFIERS
- React Security Best Practices: https://snyk.io/blog/10-react-security-best-practices/
- NIST Password Guidelines: https://pages.nist.gov/800-63-3/sp800-63b.html

---

**Last Updated**: 2026-09-04  
**Maintainer**: Security Audit Skill  
**Next Review**: When database schema changes or new dependencies added
