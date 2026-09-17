# User Management System Documentation

## Table of Contents
1. Architecture Overview
2. Authentication & Security
3. User Management
4. API Reference
5. Database Schema
6. Troubleshooting

---

## 1. Architecture Overview

### System Components

The User Management System consists of three main layers:

**Frontend Layer:**
- React application running on localhost:3000
- TypeScript for type safety
- Real-time chat interface with LLM integration
- Document upload and management
- Test runner dashboard

**Backend Layer:**
- ASP.NET Core 8.0 Web API on port 5278
- Entity Framework Core for database access
- JWT-based authentication
- RESTful API design
- Vector database for document search

**Data Layer:**
- PostgreSQL 18.6 database
- pgvector extension for embedding storage
- Soft-delete pattern for data integrity
- Automatic indexing for performance

### Technology Stack

- **Backend:** C# .NET 8.0, ASP.NET Core
- **Frontend:** React 18+, JavaScript/TypeScript
- **Database:** PostgreSQL 18.6 with pgvector
- **Authentication:** JWT (JSON Web Tokens)
- **Password Hashing:** bcrypt with salt
- **LLM Integration:** Anthropic Claude API & Ollama
- **Testing:** XUnit framework

---

## 2. Authentication & Security

### JWT Token Flow

1. User logs in with username/password
2. Backend validates credentials using bcrypt
3. Server generates JWT token (1-hour expiration)
4. Frontend stores token in localStorage
5. Token included in Authorization header for all requests
6. Server validates token on each request

### Password Security

- Passwords stored as bcrypt hashes (never plaintext)
- Bcrypt.Net-Core library for hashing
- Salt automatically included in hash
- Verification using BCrypt.Verify()

### CORS Configuration

- Allowed Origins: http://localhost:3000, https://localhost:3000
- All HTTP methods allowed
- All headers allowed
- Credentials supported

### Environment Variables

Store sensitive data in environment variables, NOT in code:

```
DB_HOST=127.0.0.1
DB_USER=postgres
DB_PASSWORD=your_password
ANTHROPIC_API_KEY=sk-ant-...
JWT_SECRET=your-secret-key-min-32-chars
```

---

## 3. User Management

### User Operations

#### Create User
- Endpoint: POST /api/users
- Requires: JWT authentication
- Parameters:
  - username (string, required)
  - password (string, required, hashed before storage)
- Returns: User object (ID, username)
- Password NOT returned in response

#### Get All Users
- Endpoint: GET /api/users
- Requires: JWT authentication
- Returns: Array of user objects
- Excludes soft-deleted users
- Passwords masked

#### Get User by ID
- Endpoint: GET /api/users/{id}
- Requires: JWT authentication
- Returns: Single user object

#### Update User
- Endpoint: PUT /api/users/{id}
- Requires: JWT authentication
- Parameters: username, password (optional)
- New password hashed before update
- Returns: Updated user object

#### Delete User (Soft Delete)
- Endpoint: DELETE /api/users/{id}
- Requires: JWT authentication
- Action: Sets IsDeleted=true, does NOT remove from DB
- User can be restored by admin

### Soft Delete Pattern

The system uses soft deletes instead of hard deletes:

Benefits:
- Recoverable if mistake made
- Maintains referential integrity
- Better audit trail
- Complies with data retention policies

Implementation:
- IsDeleted boolean column in users table
- Deleted_at timestamp tracks when deleted
- All queries filter WHERE is_deleted = false

---

## 4. API Reference

### Authentication Endpoints

#### Login
```
POST /api/auth/login
Content-Type: application/json

{
  "username": "user1",
  "password": "admin"
}

Response (200):
{
  "success": true,
  "message": "Login successful",
  "user": {
    "id": 1,
    "username": "user1"
  },
  "token": "eyJhbGciOiJIUzI1NiIs..."
}
```

### Users Endpoints

#### Create User
```
POST /api/users
Authorization: Bearer {token}
Content-Type: application/json

{
  "username": "newuser",
  "password": "securepass123"
}

Response (200):
{
  "id": 3,
  "username": "newuser"
}
```

#### Get All Users
```
GET /api/users
Authorization: Bearer {token}

Response (200):
[
  {"id": 1, "username": "user1"},
  {"id": 2, "username": "user2"}
]
```

### Chat Endpoints

#### Send Message
```
POST /api/chat/send
Authorization: Bearer {token}
Content-Type: application/json

{
  "message": "How do I create a user?"
}

Response (200):
{
  "success": true,
  "response": "To create a user, use the POST /api/users endpoint with username and password..."
}
```

#### Get Chat History
```
GET /api/chat/history
Authorization: Bearer {token}

Response (200):
[
  {"id": 1, "role": "user", "content": "Hello", "createdAt": "2026-09-07T10:00:00Z"},
  {"id": 2, "role": "assistant", "content": "Hi! How can I help?", "createdAt": "2026-09-07T10:00:05Z"}
]
```

### Document Endpoints

#### Upload Document
```
POST /api/document/upload
Authorization: Bearer {token}
Content-Type: multipart/form-data

file: document.pdf

Response (200):
{
  "success": true,
  "message": "Document 'document.pdf' uploaded successfully",
  "documentId": 1
}
```

#### Get My Documents
```
GET /api/document/my-documents
Authorization: Bearer {token}

Response (200):
{
  "success": true,
  "documents": [
    {
      "id": 1,
      "fileName": "API_Guide.pdf",
      "fileType": "pdf",
      "uploadedAt": "2026-09-07T10:00:00Z",
      "uploadedByUserId": 1
    }
  ]
}
```

#### Delete Document
```
DELETE /api/document/{id}
Authorization: Bearer {token}

Response (200):
{
  "success": true,
  "message": "Document deleted successfully"
}
```

---

## 5. Database Schema

### Users Table

```sql
CREATE TABLE users (
  id SERIAL PRIMARY KEY,
  username VARCHAR(255) UNIQUE NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  is_deleted BOOLEAN DEFAULT FALSE,
  UNIQUE(username)
);

CREATE INDEX idx_users_username ON users(username);
```

### Chat Messages Table

```sql
CREATE TABLE chat_messages (
  id SERIAL PRIMARY KEY,
  user_id INTEGER NOT NULL,
  role VARCHAR(20) NOT NULL,
  content TEXT NOT NULL,
  created_at TIMESTAMP WITH TIME ZONE NOT NULL,
  FOREIGN KEY (user_id) REFERENCES users(id)
);

CREATE INDEX idx_chat_messages_user_id ON chat_messages(user_id);
```

### Documents Table

```sql
CREATE TABLE documents (
  id SERIAL PRIMARY KEY,
  file_name VARCHAR(500) NOT NULL,
  file_type VARCHAR(50) NOT NULL,
  original_text TEXT NOT NULL,
  uploaded_at TIMESTAMP WITH TIME ZONE NOT NULL,
  uploaded_by_user_id INTEGER,
  is_deleted BOOLEAN DEFAULT FALSE,
  deleted_at TIMESTAMP WITH TIME ZONE,
  FOREIGN KEY (uploaded_by_user_id) REFERENCES users(id)
);

CREATE INDEX idx_documents_is_deleted ON documents(is_deleted);
```

### Document Chunks Table (with Vector Embeddings)

```sql
CREATE TABLE document_chunks (
  id SERIAL PRIMARY KEY,
  document_id INTEGER NOT NULL,
  content TEXT NOT NULL,
  chunk_index INTEGER NOT NULL,
  embedding vector(1536),
  created_at TIMESTAMP WITH TIME ZONE NOT NULL,
  is_deleted BOOLEAN DEFAULT FALSE,
  FOREIGN KEY (document_id) REFERENCES documents(id)
);

CREATE INDEX idx_document_chunks_document_id ON document_chunks(document_id);
CREATE INDEX idx_document_chunks_embedding ON document_chunks 
  USING IVFFLAT (embedding vector_cosine_ops) WITH (lists = 100);
```

### Data Relationships

```
users (1) ──→ (many) chat_messages
users (1) ──→ (many) documents
documents (1) ──→ (many) document_chunks
```

---

## 6. Troubleshooting

### Common Issues & Solutions

#### Issue: "Unauthorized (401)" Error

**Cause:** Missing or invalid JWT token

**Solution:**
1. Verify token is in Authorization header: `Authorization: Bearer {token}`
2. Check token hasn't expired (1-hour expiration)
3. Login again to get new token

#### Issue: "Database connection failed"

**Cause:** PostgreSQL not running or wrong credentials

**Solution:**
```powershell
# Check PostgreSQL is running
Get-Service postgresql*

# Verify connection string in appsettings.json:
"DefaultConnection": "Host=127.0.0.1;Port=5432;Database=usermanagementdb;Username=postgres;Password=postgres;"
```

#### Issue: "Uncaught ReferenceError: createPopper is undefined"

**Cause:** Frontend using wrong backend URL

**Solution:**
1. Check API_BASE_URL in src/services/api.js
2. Verify backend is running on http://localhost:5278
3. Check CORS configuration in Program.cs

#### Issue: "Password verification failed" after update

**Cause:** Old plaintext passwords in database

**Solution:**
- Clear existing users: Delete from users table
- Re-create test users through UI (they'll be bcrypt hashed)

#### Issue: "Document upload fails"

**Cause:** pgvector extension not enabled

**Solution:**
```sql
-- In PostgreSQL:
CREATE EXTENSION IF NOT EXISTS vector;
```

#### Issue: "Chat gives generic responses without document context"

**Cause:** No documents uploaded yet

**Solution:**
1. Upload PDF documents through Documents tab
2. System automatically extracts and indexes text
3. Future queries will include relevant document context

#### Issue: "Ollama not responding" error

**Cause:** Ollama service not running

**Solution:**
```powershell
# Start Ollama
ollama serve

# In another terminal, download Mistral:
ollama pull mistral

# Run Mistral:
ollama run mistral
```

Or use Anthropic API by setting UseLocalLLM=false

---

## Best Practices

### Security
- Always use HTTPS in production
- Rotate JWT secret regularly
- Never commit credentials to git
- Use environment variables for sensitive data
- Keep dependencies updated

### Performance
- Upload documents during off-peak hours
- Batch test runs
- Monitor database query performance
- Use vector indexes for fast similarity search

### Maintenance
- Regular database backups
- Monitor disk space for documents
- Archive old documents periodically
- Review logs for errors

---

## Support & Help

For issues or questions:
1. Check Troubleshooting section above
2. Review API Reference for endpoint details
3. Check application logs in console
4. Verify all services are running

Default Credentials (Development Only):
- Username: user1
- Password: admin

---

End of Documentation
