# Deployment Guide - User Management System

## Pre-Deployment Checklist

- [ ] All tests passing (`dotnet test`)
- [ ] Code reviewed and merged to main branch
- [ ] Environment variables configured
- [ ] Database backups created
- [ ] Git tags created for version tracking
- [ ] Secrets stored in secure vault (not in git)
- [ ] SSL/TLS certificates ready
- [ ] Monitoring/logging configured
- [ ] Rollback plan documented

---

## Environment Setup

### 1. Production Environment Variables

Create `.env` file or configure in deployment environment:

```bash
# Database
DB_HOST=prod-db-server.example.com
DB_PORT=5432
DB_NAME=usermanagementdb_prod
DB_USER=prod_user
DB_PASSWORD=<secure-password>

# JWT Configuration
JWT_SECRET=<generate-32-char-secure-key>
JWT_ISSUER=UserManagementApp
JWT_AUDIENCE=UserManagementAppUsers
JWT_EXPIRATION_MINUTES=60

# Ollama/LLM Configuration
USE_LOCAL_LLM=true
LOCAL_LLM_URL=http://ollama-server:11434/api/generate
LOCAL_LLM_MODEL=mistral

# Anthropic API (optional fallback)
ANTHROPIC_API_KEY=<api-key-if-using>

# CORS & Security
ALLOWED_ORIGINS=https://yourdomain.com,https://www.yourdomain.com
ENVIRONMENT=Production
```

### 2. Generate JWT Secret

```bash
# PowerShell
$key = [System.Convert]::ToBase64String([System.Security.Cryptography.SHA256]::Create().ComputeHash([System.Text.Encoding]::UTF8.GetBytes((Get-Random))))
Write-Host $key

# Or use online generator for 32+ character random string
```

---

## Backend Deployment

### Option A: Docker Deployment (Recommended)

#### 1. Create Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["UserManagementAPI/UserManagementAPI.csproj", "UserManagementAPI/"]
RUN dotnet restore "UserManagementAPI/UserManagementAPI.csproj"

COPY . .
RUN dotnet build "UserManagementAPI/UserManagementAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UserManagementAPI/UserManagementAPI.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 5278
ENV ASPNETCORE_URLS=http://+:5278
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "UserManagementAPI.dll"]
```

#### 2. Create docker-compose.yml

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:18.6
    container_name: usermanagement_db
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: ${DB_PASSWORD}
      POSTGRES_DB: usermanagementdb_prod
    ports:
      - "5432:5432"
    volumes:
      - db_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  ollama:
    image: ollama/ollama:latest
    container_name: ollama_server
    ports:
      - "11434:11434"
    environment:
      OLLAMA_HOST: 0.0.0.0:11434
    volumes:
      - ollama_data:/root/.ollama
    # Run once to pull model: docker exec ollama_server ollama pull mistral

  backend:
    build: ./Backend/UserManagementAPI
    container_name: usermanagement_api
    ports:
      - "5278:5278"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:5278
      ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}
      Jwt__Secret: ${JWT_SECRET}
      Jwt__Issuer: ${JWT_ISSUER}
      Jwt__Audience: ${JWT_AUDIENCE}
      Jwt__ExpirationMinutes: ${JWT_EXPIRATION_MINUTES}
      UseLocalLLM: ${USE_LOCAL_LLM}
      LocalLLM__Url: http://ollama:11434/api/generate
      LocalLLM__Model: ${LOCAL_LLM_MODEL}
    depends_on:
      postgres:
        condition: service_healthy
      ollama:
        condition: service_started
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5278/api/users"]
      interval: 30s
      timeout: 10s
      retries: 3

  frontend:
    image: node:18-alpine
    container_name: usermanagement_ui
    working_dir: /app
    volumes:
      - ./Frontend/user-management-app:/app
      - node_modules:/app/node_modules
    ports:
      - "3000:3000"
    command: npm start
    environment:
      REACT_APP_API_URL: http://localhost:5278
    depends_on:
      - backend

volumes:
  db_data:
  ollama_data:
  node_modules:

networks:
  default:
    name: usermanagement_network
```

#### 3. Deploy with Docker Compose

```bash
# Create .env file with all variables
cd /path/to/UserManagementApp
docker-compose up -d

# Pull Ollama model
docker exec ollama_server ollama pull mistral

# Check status
docker-compose logs -f backend
docker-compose ps
```

### Option B: Manual Linux Deployment

#### 1. Install Prerequisites

```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install -y \
  wget \
  curl \
  git \
  postgresql-client \
  dotnet-sdk-8.0

# Add dotnet to PATH if needed
export PATH=$PATH:/usr/share/dotnet
```

#### 2. Deploy Backend

```bash
# Clone repository
git clone https://github.com/your-org/usermanagement-system.git
cd usermanagement-system/Backend/UserManagementAPI

# Publish application
dotnet publish -c Release -o /opt/usermanagement/api

# Create systemd service file
sudo tee /etc/systemd/system/usermanagement-api.service > /dev/null <<EOF
[Unit]
Description=User Management API
After=network.target

[Service]
Type=notify
User=www-data
WorkingDirectory=/opt/usermanagement/api
ExecStart=/usr/bin/dotnet /opt/usermanagement/api/UserManagementAPI.dll
Restart=on-failure
RestartSec=10
Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="ASPNETCORE_URLS=http://+:5278"
# Add other environment variables here
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
EOF

# Enable and start service
sudo systemctl daemon-reload
sudo systemctl enable usermanagement-api
sudo systemctl start usermanagement-api
sudo systemctl status usermanagement-api
```

#### 3. Configure Nginx Reverse Proxy

```nginx
# /etc/nginx/sites-available/usermanagement
upstream backend {
    server localhost:5278;
}

upstream frontend {
    server localhost:3000;
}

server {
    listen 80;
    server_name yourdomain.com www.yourdomain.com;
    
    # Redirect to HTTPS
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name yourdomain.com www.yourdomain.com;
    
    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    
    # Frontend
    location / {
        proxy_pass http://frontend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
    
    # API
    location /api/ {
        proxy_pass http://backend;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # CORS headers
        add_header 'Access-Control-Allow-Origin' '$http_origin' always;
        add_header 'Access-Control-Allow-Methods' 'GET, POST, PUT, DELETE, OPTIONS' always;
        add_header 'Access-Control-Allow-Headers' 'DNT,User-Agent,X-Requested-With,If-Modified-Since,Cache-Control,Content-Type,Range,Authorization' always;
    }
}
```

#### 4. Enable Nginx Site

```bash
sudo ln -s /etc/nginx/sites-available/usermanagement /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

---

## Frontend Deployment

### Option A: Docker (Recommended)

```bash
# Included in docker-compose.yml above
# Frontend automatically built and deployed
```

### Option B: Manual Deployment

```bash
# Build frontend
cd Frontend/user-management-app
npm install
npm run build

# Deploy to web server
scp -r build/* user@server:/var/www/usermanagement/

# Or use PM2 for Node.js
npm install -g pm2
pm2 start npm --name "usermanagement-ui" -- start
pm2 save
pm2 startup
```

---

## Database Migration

### PostgreSQL Setup

```bash
# Connect to database
psql -h localhost -U postgres -d usermanagementdb_prod

# Verify tables exist
\dt

# Check schema
\d users
\d chat_messages
\d documents
\d document_chunks

# Backup before deployment
pg_dump -h localhost -U postgres usermanagementdb_prod > backup_$(date +%Y%m%d_%H%M%S).sql
```

### Data Migration (if upgrading)

```bash
# The application creates tables automatically via Program.cs
# If tables exist, raw SQL execution in Program.cs skips creation

# For major schema changes:
# 1. Create migration file
# 2. Test on staging environment
# 3. Deploy with downtime window
# 4. Verify data integrity after migration
```

---

## Verification Steps

### 1. Backend Health Check

```bash
# Check API is responding
curl -X GET http://localhost:5278/api/users \
  -H "Authorization: Bearer <valid-jwt-token>"

# Expected: 200 OK with users array

# Check specific endpoints
curl -X GET http://localhost:5278/api/users
# Expected: 401 Unauthorized (without token)

curl -X POST http://localhost:5278/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"user1","password":"admin"}'
# Expected: 200 OK with JWT token
```

### 2. Frontend Health Check

```bash
# Open in browser
https://yourdomain.com

# Check:
# - Login page loads
# - Login with user1/admin works
# - Chat feature loads
# - Document upload works
# - Can see uploaded documents
```

### 3. Database Connectivity

```bash
# Test connection
psql -h prod-db.example.com -U prod_user -d usermanagementdb_prod -c "SELECT * FROM users LIMIT 5;"

# Verify soft-delete filtering
psql -h prod-db.example.com -U prod_user -d usermanagementdb_prod -c "SELECT * FROM users WHERE is_deleted = false;"
```

### 4. Ollama Connectivity

```bash
# Test Ollama endpoint
curl -X POST http://localhost:11434/api/generate \
  -H "Content-Type: application/json" \
  -d '{"model":"mistral","prompt":"hello","stream":false}'

# Should respond with AI generated text
```

### 5. Monitor Logs

```bash
# Docker logs
docker-compose logs -f backend
docker-compose logs -f frontend
docker-compose logs -f postgres

# Systemd logs
sudo journalctl -u usermanagement-api -f

# Nginx logs
sudo tail -f /var/log/nginx/access.log
sudo tail -f /var/log/nginx/error.log
```

---

## Production Security Checklist

- [ ] HTTPS enabled with valid SSL certificate
- [ ] JWT Secret is cryptographically secure (32+ chars)
- [ ] Database password is strong and stored securely
- [ ] CORS configured to specific origins only
- [ ] Secrets not in git repository or docker images
- [ ] API rate limiting configured
- [ ] SQL injection prevented (EF Core parameterized queries)
- [ ] XSS protection headers set
- [ ] CSRF tokens validated
- [ ] Admin credentials changed from default
- [ ] Database backups automated daily
- [ ] Monitoring and alerting configured

---

## Rollback Procedure

### If Deployment Fails

#### Docker Rollback

```bash
# Stop current containers
docker-compose down

# Restore previous version from git tag
git checkout v1.0.0  # Previous stable version

# Rebuild and restart
docker-compose up -d

# Verify services
docker-compose ps
docker-compose logs backend
```

#### Manual Deployment Rollback

```bash
# Stop service
sudo systemctl stop usermanagement-api

# Restore previous application version
cd /opt/usermanagement/api
sudo git checkout v1.0.0
sudo dotnet publish -c Release -o /opt/usermanagement/api

# Start service
sudo systemctl start usermanagement-api
```

#### Database Rollback

```bash
# Restore from backup
psql -h localhost -U postgres usermanagementdb_prod < backup_YYYYMMDD_HHMMSS.sql

# Verify restoration
psql -h localhost -U postgres -c "SELECT COUNT(*) FROM users;"
```

---

## Monitoring & Maintenance

### Application Performance

```bash
# Monitor API response times
# Use tools like: New Relic, DataDog, or Prometheus

# Check error rates in logs
grep -i "error\|exception" /var/log/usermanagement-api.log

# Monitor database connections
# Max: 100 connections (adjust in appsettings)
```

### Database Maintenance

```bash
# Weekly: VACUUM and ANALYZE
sudo -u postgres psql -d usermanagementdb_prod -c "VACUUM ANALYZE;"

# Monthly: Reindex
sudo -u postgres psql -d usermanagementdb_prod -c "REINDEX DATABASE usermanagementdb_prod;"

# Check disk space
sudo du -sh /var/lib/postgresql/data
```

### Log Rotation

```bash
# Configure logrotate for application logs
cat << 'EOF' | sudo tee /etc/logrotate.d/usermanagement
/var/log/usermanagement-api.log {
    daily
    rotate 30
    compress
    delaycompress
    missingok
    notifempty
}
EOF
```

### Backup Schedule

```bash
# Daily automated backup (cron)
0 2 * * * pg_dump -h localhost -U postgres usermanagementdb_prod | gzip > /backups/usermanagement_$(date +\%Y\%m\%d).sql.gz

# Weekly full backup to cloud storage
0 3 * * 0 aws s3 sync /backups/ s3://your-backup-bucket/usermanagement/
```

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Database connection refused | Verify DB_HOST, DB_PORT, credentials. Check PostgreSQL is running |
| JWT token errors | Verify JWT_SECRET matches between frontend and backend |
| CORS errors on frontend | Update ALLOWED_ORIGINS to include frontend domain |
| Ollama timeouts | Increase timeout to 180s, verify Ollama service running |
| High memory usage | Reduce document chunk limits, implement caching |
| Slow API responses | Check database indexes, enable query caching |
| 502 Bad Gateway | Check upstream services, review nginx config |

---

## Version Control & Deployment Tags

```bash
# Create release tag
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0

# Deploy specific version
git checkout v1.0.0
docker-compose up -d --build

# List all releases
git tag -l
```

---

## Support & Monitoring Contacts

- **Database Admin**: Check PostgreSQL logs
- **Application Owner**: Review application logs
- **DevOps**: Monitor infrastructure and containers
- **Security**: Review and rotate secrets quarterly

---

## Deployment Checklist Summary

```bash
# Pre-deployment
- [ ] Run all tests: dotnet test
- [ ] Create git tag: git tag v1.0.0
- [ ] Backup database: pg_dump > backup.sql
- [ ] Configure environment variables
- [ ] Generate new JWT secret

# Deployment
- [ ] Build Docker images
- [ ] Push to registry
- [ ] Deploy with docker-compose
- [ ] Pull Ollama model if needed

# Post-deployment
- [ ] Verify API endpoints responding
- [ ] Test authentication (login)
- [ ] Test document upload
- [ ] Test chat with Ollama
- [ ] Check logs for errors
- [ ] Verify monitoring alerts

# Final
- [ ] Update status page
- [ ] Notify team of successful deployment
- [ ] Document any issues encountered
```

---

**Last Updated**: 2026-09-09  
**Version**: 1.0  
**Maintained by**: DevOps Team
