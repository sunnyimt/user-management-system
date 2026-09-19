# GitHub to Azure Deployment Setup

This document explains how to configure your GitHub repository for automated deployment to Azure.

## Prerequisites

- GitHub account with repository created
- Azure account with subscription
- Azure App Service instances (dev, staging, production)
- Azure PostgreSQL Flexible Server instance
- Azure Static Web Apps for frontend (optional)

---

## Step 1: Create Azure Service Principal

Azure needs credentials to deploy from GitHub. Create a service principal:

```bash
# Login to Azure
az login

# Create service principal for GitHub
az ad sp create-for-rbac \
  --name "github-deployment-sp" \
  --role contributor \
  --scopes /subscriptions/{SUBSCRIPTION_ID}/resourceGroups/{RESOURCE_GROUP}

# Output will look like:
# {
#   "clientId": "...",
#   "clientSecret": "...",
#   "subscriptionId": "...",
#   "tenantId": "..."
# }
```

**Save this JSON output** - you'll need it for GitHub secrets.

---

## Step 2: Configure GitHub Secrets

In your GitHub repository: **Settings → Secrets and variables → Actions → New repository secret**

### Required Secrets:

```
AZURE_CREDENTIALS
{
  "clientId": "...",
  "clientSecret": "...",
  "subscriptionId": "...",
  "tenantId": "..."
}

AZURE_SUBSCRIPTION_ID
{your-subscription-id}

AZURE_RESOURCE_GROUP
usermanagement-rg

AZURE_STORAGE_ACCOUNT
{storage-account-name}

LOCAL_DB_HOST
{local-or-ssh-tunnel-host}

LOCAL_DB_PORT
5432

LOCAL_DB_NAME
usermanagementdb

LOCAL_DB_USER
postgres

LOCAL_DB_PASSWORD
{secure-password}

AZURE_DB_HOST
usermanagement-db.postgres.database.azure.com

AZURE_DB_PORT
5432

AZURE_DB_NAME
usermanagementdb_prod

AZURE_DB_USER
azuredbadmin

AZURE_DB_PASSWORD
{secure-password}

AZURE_STATIC_WEB_APPS_API_TOKEN
{azure-static-web-apps-token}
```

### Environment-Specific Secrets:

In **Settings → Environments → Create new environment**

**Development Environment:**
- `AZURE_WEBAPP_NAME`: usermanagement-app-dev
- `ASPNETCORE_ENVIRONMENT`: Development
- `UseLocalLLM`: true

**Staging Environment:**
- `AZURE_WEBAPP_NAME`: usermanagement-app-staging
- `ASPNETCORE_ENVIRONMENT`: Staging
- `UseLocalLLM`: true

**Production Environment:**
- `AZURE_WEBAPP_NAME`: usermanagement-app-prod
- `ASPNETCORE_ENVIRONMENT`: Production
- `UseLocalLLM`: false

---

## Step 3: Create Azure App Service Instances

```bash
# For each environment (dev, staging, prod)
az webapp create \
  --resource-group usermanagement-rg \
  --plan usermanagement-plan \
  --name usermanagement-app-{env} \
  --runtime "DOTNETCORE:8.0"

# Configure connection string
az webapp config appsettings set \
  --resource-group usermanagement-rg \
  --name usermanagement-app-{env} \
  --settings \
    ConnectionStrings__DefaultConnection="Host=usermanagement-db.postgres.database.azure.com;Port=5432;Database=usermanagementdb_prod;Username=azuredbadmin;Password={password};SSL Mode=Require;" \
    Jwt__Secret="{jwt-secret}" \
    Jwt__Issuer="UserManagementApp" \
    Jwt__Audience="UserManagementAppUsers" \
    Jwt__ExpirationMinutes=60 \
    UseLocalLLM=true \
    LocalLLM__Url="http://ollama:11434/api/generate" \
    LocalLLM__Model="mistral"

# Enable continuous deployment
az webapp deployment github-actions \
  --resource-group usermanagement-rg \
  --name usermanagement-app-{env} \
  --repo {github-username}/{repo-name} \
  --branch {branch}
```

---

## Step 4: Configure GitHub Actions Secrets

Add these secrets for each workflow:

### In GitHub repo → Settings → Secrets and variables → Actions

**AZURE_CREDENTIALS:**
```json
{
  "clientId": "your-client-id",
  "clientSecret": "your-client-secret",
  "subscriptionId": "your-subscription-id",
  "tenantId": "your-tenant-id"
}
```

---

## Step 5: Branch Deployment Strategy

The CI/CD pipeline automatically deploys based on branch:

```
main → Production Azure App Service
  ↓
  Direct deployment

staging → Staging Azure App Service
  ↓
  Direct deployment

develop → Development Azure App Service
  ↓
  Direct deployment
```

> **Note:** Deployment uses direct publish (no slot swap). Azure deployment
> slots require the Standard (S1) tier or higher; this setup targets the
> B1 Basic tier for cost, which doesn't support slots. If you later
> upgrade the App Service Plan to Standard+, you can reintroduce a
> staging slot and blue-green swap for zero-downtime production releases.

---

## Step 6: First Deployment

1. **Push to develop branch:**
   ```bash
   git checkout develop
   git push origin develop
   ```

2. **Watch GitHub Actions:**
   - Go to **Actions** tab in your GitHub repo
   - Select **"Build and Deploy to Azure"** workflow
   - See real-time build and deployment logs

3. **Verify deployment:**
   ```bash
   # Check if app is running
   curl https://usermanagement-app-dev.azurewebsites.net/api/users
   # Should return 401 (requires JWT token) or actual data if authenticated
   ```

---

## Step 7: Database Migration

1. **First time setup - Full migration:**
   ```
   Go to Actions → Database Migration to Azure
   ↓
   Click "Run workflow"
   ↓
   Select:
     - Environment: staging (or production)
     - Migration type: full_migration
   ↓
   This will backup local DB and restore to Azure
   ```

2. **Regular backups:**
   ```
   Scheduled daily via GitHub Actions
   Stored in Azure Storage
   ```

---

## Workflow: Development to Production

### 1. Feature Development
```bash
# Create feature branch
git checkout -b feature/your-feature develop

# Make changes
git add .
git commit -m "Add your feature"

# Push to GitHub
git push origin feature/your-feature

# Create Pull Request → develop
```

### 2. Code Review
```
PR → Automatically runs:
  ✓ Build backend
  ✓ Build frontend
  ✓ Run tests
  ✓ Code review with agent
```

### 3. Merge to Develop
```
After approval:
  ↓
  GitHub Actions automatically:
    • Builds both frontend and backend
    • Runs tests
    • Deploys to dev Azure App Service
    • Runs health checks
```

### 4. Staging Release
```bash
# Create PR: develop → staging
git checkout staging
git pull origin staging
git merge origin/develop
git push origin staging
```

```
Deploys to:
  ✓ Staging Azure App Service
  ✓ All smoke tests
  ✓ Database verification
```

### 5. Production Release
```bash
# Create PR: staging → main
# After review and approval:
git checkout main
git pull origin main
git merge origin/staging
git push origin main
```

```
Deployment process:
  1. Build and test
  2. Deploy directly to production App Service
  3. Run health checks
  4. Monitor for issues
```

---

## Configuration Files Reference

### .github/workflows/build-and-deploy.yml
- Triggered on push to main, develop, staging
- Builds backend (.NET)
- Builds frontend (React)
- Runs tests
- Deploys to appropriate Azure environment
- Health checks after deployment

### .github/workflows/database-migration.yml
- Manual trigger via Actions tab
- Options:
  - `backup_only`: Backup local to Azure Storage
  - `backup_and_verify`: Backup + verify Azure connection
  - `full_migration`: Backup local + restore to Azure
  - `verify_only`: Check Azure database

### .github/CODEOWNERS
- Specifies who should review changes
- Automatically requests reviews based on files changed

### .github/pull_request_template.md
- Standardizes PR descriptions
- Enforces security checklist
- Documents deployment implications

---

## Monitoring & Troubleshooting

### View Deployment Logs
```
GitHub → Actions → {workflow run} → Jobs → {job} → Logs
```

### Azure App Service Logs
```bash
# Stream logs
az webapp log tail \
  --resource-group usermanagement-rg \
  --name usermanagement-app-{env}
```

### Common Issues

| Issue | Solution |
|-------|----------|
| Build fails | Check GitHub Actions logs, verify NuGet packages available |
| Deployment fails | Verify Azure credentials and environment variables |
| App won't start | Check Azure App Service logs, verify connection strings |
| Database connection fails | Verify firewall rules, connection string, Azure DB status |

---

## Environment Variables for Azure

Set in Azure App Service → Configuration → Application settings:

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80
ConnectionStrings__DefaultConnection=...
Jwt__Secret=...
UseLocalLLM=false (or true for dev)
LocalLLM__Url=...
LocalLLM__Model=mistral
```

---

## Rolling Back a Deployment

If production deployment has issues:

```bash
# Redeploy the previous known-good version
git checkout previous-commit-hash
git push origin main --force-with-lease
# GitHub Actions will redeploy that version
```

---

## Next Steps

1. ✅ Create Azure Service Principal
2. ✅ Add GitHub Secrets
3. ✅ Create Azure resources
4. ✅ Push code to GitHub
5. ✅ Trigger first deployment
6. ✅ Monitor deployment logs
7. ✅ Verify application running on Azure
8. ✅ Run smoke tests

---

**Documentation Last Updated:** 2026-09-16  
**Relevant:** GitHub Actions, Azure Deployment, CI/CD Pipeline
