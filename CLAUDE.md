# Claude.md - User Management System

## Project Overview

**User Management System** is a full-stack application for managing users with AI-powered chat assistance, document management, and local LLM integration.

### Tech Stack

**Backend:**
- ASP.NET Core 10.0 (.NET 10)
- Entity Framework Core with PostgreSQL 18.6
- JWT Authentication
- Ollama (local LLM - Mistral model)
- iText7 (PDF processing)

**Frontend:**
- React with TypeScript/JavaScript
- NPM for package management
- Bootstrap/custom CSS

**Database:**
- PostgreSQL 18.6
- Tables: users, chat_messages, documents, document_chunks
- Soft-delete pattern (IsDeleted column instead of hard delete)

**AI/LLM:**
- Ollama (local) - Mistral 7B model
- Anthropic Claude API (optional fallback)

---

## Project Structure

```
C:\Users\L\UserManagementApp\
├── Backend\
│   ├── UserManagementAPI\
│   │   ├── Program.cs                 (startup, database seeding)
│   │   ├── appsettings.json           (production config)
│   │   ├── appsettings.Development.json (dev config)
│   │   ├── Controllers\
│   │   │   ├── AuthController.cs      (login, JWT generation)
│   │   │   ├── UsersController.cs     (CRUD operations)
│   │   │   ├── ChatController.cs      (chat endpoints, local search)
│   │   │   ├── DocumentController.cs  (document upload, delete)
│   │   │   └── TestController.cs      (test execution)
│   │   ├── Services\
│   │   │   ├── UserService.cs         (user management, BCrypt hashing)
│   │   │   ├── ChatService.cs         (chat logic, Ollama integration)
│   │   │   ├── DocumentService.cs     (PDF processing, embeddings)
│   │   │   ├── ConfigurationService.cs (chunking strategies)
│   │   │   └── *Service.cs interfaces
│   │   ├── Models\
│   │   │   ├── User.cs                (User entity, LoginResponse)
│   │   │   ├── ChatMessage.cs         (chat history)
│   │   │   └── Document.cs            (Document, DocumentChunk entities)
│   │   ├── Data\
│   │   │   ├── ApplicationDbContext.cs (EF Core context)
│   │   │   └── DbSeeder.cs            (seed initial data)
│   │   └── UserManagementAPI.Tests\
│   │       └── AuthorizationTests.cs  (JWT, authorization tests)
│   └── START_APPLICATION.ps1          (startup script)
├── Frontend\
│   └── user-management-app\
│       ├── public\
│       ├── src\
│       │   ├── components\
│       │   │   ├── Login.js
│       │   │   ├── UserManagement.js  (main container)
│       │   │   ├── Chat.js            (two-step chat workflow)
│       │   │   ├── DocumentUpload.js  (PDF upload)
│       │   │   ├── AdminPanel.js
│       │   │   ├── Tests.js           (Section E tests)
│       │   │   └── *.css
│       │   ├── services\
│       │   │   └── api.js             (API calls, JWT token management)
│       │   └── App.js
│       └── package.json
└── CLAUDE.md (this file)
```

---

## Getting Started

### Prerequisites
- .NET 10 SDK installed
- Node.js and NPM installed
- PostgreSQL 18.6 running (connection: localhost:5432)
- Ollama running locally (for AI features): `ollama run mistral`

### Database Setup

PostgreSQL connection string:
```
Host=127.0.0.1;Port=5432;Database=usermanagementdb;Username=postgres;Password=postgres;
```

Tables are created automatically on application startup via `Program.cs` raw SQL.

### Starting the Application

**Option 1: PowerShell Script (Recommended)**
```powershell
& "C:\Users\L\UserManagementApp\START_APPLICATION.ps1"
```

**Option 2: Manual Start**
```powershell
# Terminal 1 - Backend
cd "C:\Users\L\UserManagementApp\Backend\UserManagementAPI"
dotnet run

# Terminal 2 - Frontend
cd "C:\Users\L\UserManagementApp\Frontend\user-management-app"
npm start
```

### URLs
- Frontend: http://localhost:3000
- Backend API: http://localhost:5278
- Swagger Docs: http://localhost:5278/swagger (when running in Development)

### Default Credentials
```
Username: user1
Password: admin
```

---

## Key Features & Implementation

### 1. Authentication & Authorization
- **JWT Tokens**: Generated on login, valid for 60 minutes
- **Protected Endpoints**: All endpoints except `/api/auth/login` require `[Authorize]` attribute
- **Token Storage**: Frontend stores JWT in localStorage, includes in all API requests
- **Header Format**: `Authorization: Bearer <token>`

### 2. Password Security
- **Hashing**: BCrypt.Net-Core 1.6.0 with rounds=10
- **Storage**: PasswordHash column, Password field hidden with [JsonIgnore]
- **Login**: Uses BCrypt.Verify() for comparison

### 3. Chat Workflow (Two-Step)

**Step 1: Local Database Search**
```
POST /api/chat/search-local
→ Searches documents + embeddings
→ Returns relevant chunks from local database
```

**Step 2: Optional AI Enhancement**
```
POST /api/chat/send
→ If Ollama running: streams response from Mistral
→ If Ollama fails: falls back to local database context
→ Stores both user and assistant messages
```

### 4. Document Management
- **Upload**: PDF files (max 10MB) extracted with iText7
- **Text Extraction**: Converts PDF to plain text
- **Chunking**: Configurable strategies (Word, Sentence, Paragraph, Character)
- **Embeddings**: Generated via Anthropic API (fallback: mock embeddings)
- **Storage**: Documents + chunks soft-deleted (IsDeleted flag)
- **RAG**: Cosine similarity search for relevant chunks

### 5. Ollama Integration
- **Model**: Mistral 7B (4.4GB)
- **Endpoint**: http://localhost:11434/api/generate
- **Stream Mode**: Required (stream=false hangs)
- **Response Parsing**: Line-by-line JSON, stops at "done": true
- **Timeout**: 180 seconds (model needs time to think)

---

## Development Guidelines

### Code Style & Conventions
- **C#**: Follow Microsoft naming conventions (PascalCase for public, camelCase for private)
- **JavaScript**: camelCase for variables/functions, PascalCase for components
- **Comments**: Only add when WHY is non-obvious, not what the code does
- **Naming**: Use clear, descriptive names (avoid abbreviations)

### Git Workflow
- **Branches**: `feature/*`, `bugfix/*`, `hotfix/*`
- **Commits**: Use present tense ("add feature" not "added feature")
- **Co-Authored**: Always include co-author line:
  ```
  Co-Authored-By: Claude Haiku 4.5 <noreply@anthropic.com>
  ```
- **Avoid**: Force pushes, rewriting history, hardcoded credentials

### Backend Changes
- Always rebuild before testing: `dotnet build`
- Check for compilation warnings (nullable types)
- Add logging for important operations (`_logger.LogInformation()`)
- Test with JWT token: `Authorization: Bearer <token>`

### Frontend Changes
- Test responsive design on mobile
- Check console for React warnings
- Verify API calls include Authorization header
- Use absolute URLs (http://localhost:5278)

### Database Changes
- Use soft deletes: Set `IsDeleted = true` instead of DELETE
- Add indexes for frequently queried columns
- Document any schema changes in comments
- Always include `WHERE !x.IsDeleted` in queries

### Testing
- **Test Location**: `UserManagementAPI.Tests/AuthorizationTests.cs`
- **Section E Tests**: Authorization & Access Control (7 test cases)
- **Run Tests**: `dotnet test`
- **Phase Approach**: User requested tests in phases (E first, then others)

---

## Important Rules for Claude

### Security First
1. **Never** hardcode credentials, API keys, or secrets
2. **Always** validate user input at system boundaries
3. **Use** HTTPS in production
4. **Hash** all passwords with BCrypt
5. **Filter** soft-deleted records in all queries

### Code Quality
1. **No premature abstractions** - keep code simple until justified
2. **No half-finished implementations** - complete features fully
3. **No unnecessary error handling** - trust framework guarantees
4. **Prefer editing** existing files over creating new ones
5. **Test before claiming** features work (especially UI features)

### Work Approach
1. **Ask clarifying questions** if requirements are ambiguous
2. **Prefer reverting mistakes** over complex fixes
3. **Read changes carefully** before making them
4. **Test locally** before claiming success
5. **One commit per logical change** with clear message

### When Stuck
1. Add logging to understand actual error (not assumed)
2. Test endpoints directly with curl/Postman
3. Check database directly with psql
4. Review recent git history for context
5. Ask user for clarification if uncertain

---

## Configuration Files

### appsettings.Development.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=127.0.0.1;Port=5432;Database=usermanagementdb;Username=postgres;Password=postgres;"
  },
  "Jwt": {
    "Secret": "dev-secret-key-...",
    "Issuer": "UserManagementApp",
    "Audience": "UserManagementAppUsers",
    "ExpirationMinutes": 60
  },
  "Anthropic": {
    "ApiKey": "sk-ant-..." (optional, for API fallback)
  },
  "LocalLLM": {
    "Url": "http://localhost:11434/api/generate",
    "Model": "mistral"
  },
  "UseLocalLLM": true
}
```

---

## Known Limitations & Solutions

| Issue | Solution |
|-------|----------|
| Ollama /api/generate hangs with stream=false | Use stream=true, read line-by-line until "done": true |
| Document deletion shows but doesn't persist | Verify soft-delete logic: `IsDeleted = true` saved to DB |
| Chat timeout on large prompts | Increase timeout to 180 seconds for Mistral model |
| Embedding generation fails | Falls back to mock embeddings automatically |
| PDF text extraction fails | Stores placeholder text, document still uploads |

---

## Performance Considerations

1. **Document Chunks**: Limited to 5 chunks in RAG by default (adjust in `DocumentService`)
2. **Chat History**: Retrieves last 10 messages for context (tunable)
3. **Embedding Generation**: Takes 2-5 seconds per chunk (use mock for testing)
4. **Ollama Response Time**: 15-30 seconds typical (depends on query complexity)
5. **Database**: Indexes on user_id, is_deleted, document_id for fast queries

---

## Next Steps / Future Enhancements

- [ ] Section A-D tests (before F, G)
- [ ] Database connection pooling optimization
- [ ] Redis caching for embeddings
- [ ] Batch PDF processing
- [ ] Web UI for Ollama model selection
- [ ] Vector database (pgvector when available)
- [ ] Document versioning

---

## Contact & Support

**Project Owner**: User (asifhameed.us@gmail.com)  
**Framework**: ASP.NET Core 8 + React  
**Database**: PostgreSQL 18.6  
**AI**: Ollama Mistral 7B (local)  

For issues or questions, refer to git history or run tests to diagnose.
