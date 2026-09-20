using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Font;
using System.IO;

namespace UserManagementAPI.Utils
{
    public class PdfGenerator
    {
        public static void GenerateSampleDocumentationPdf(string outputPath)
        {
            var documentationContent = GetDocumentationContent();

            using (var writer = new PdfWriter(outputPath))
            {
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);
                var timesFont = PdfFontFactory.CreateFont();

                // Title
                document.Add(new Paragraph("User Management System Documentation")
                    .SetFontSize(24)
                    .SetBold());

                document.Add(new Paragraph("Version 1.0 - September 2026")
                    .SetFontSize(12)
                    .SetItalic());

                document.Add(new Paragraph(" "));

                // Table of Contents
                document.Add(new Paragraph("Table of Contents")
                    .SetFontSize(14)
                    .SetBold());

                var toc = new List();
                toc.Add(new ListItem("Architecture Overview"));
                toc.Add(new ListItem("Authentication & Security"));
                toc.Add(new ListItem("User Management"));
                toc.Add(new ListItem("API Reference"));
                toc.Add(new ListItem("Database Schema"));
                toc.Add(new ListItem("Troubleshooting"));
                document.Add(toc);

                document.Add(new Paragraph("\n"));

                // Content sections
                AddSection(document, "1. Architecture Overview", GetArchitectureSection());
                AddSection(document, "2. Authentication & Security", GetSecuritySection());
                AddSection(document, "3. User Management", GetUserManagementSection());
                AddSection(document, "4. API Reference", GetApiReferenceSection());
                AddSection(document, "5. Database Schema", GetDatabaseSection());
                AddSection(document, "6. Troubleshooting", GetTroubleshootingSection());

                document.Close();
            }
        }

        private static void AddSection(Document document, string title, string content)
        {
            document.Add(new Paragraph(title)
                .SetFontSize(14)
                .SetBold());

            document.Add(new Paragraph(content)
                .SetFontSize(10));

            document.Add(new Paragraph("\n"));
        }

        private static string GetArchitectureSection()
        {
            return @"
The User Management System consists of three main layers:

Frontend Layer:
- React application running on localhost:3000
- TypeScript for type safety
- Real-time chat interface with LLM integration
- Document upload and management
- Test runner dashboard

Backend Layer:
- ASP.NET Core 10.0 Web API on port 5278
- Entity Framework Core for database access
- JWT-based authentication
- RESTful API design
- Vector database for document search

Data Layer:
- PostgreSQL 18.6 database
- pgvector extension for embedding storage
- Soft-delete pattern for data integrity
- Automatic indexing for performance

Technology Stack:
- Backend: C# .NET 10.0, ASP.NET Core
- Frontend: React 18+, JavaScript/TypeScript
- Database: PostgreSQL 18.6 with pgvector
- Authentication: JWT (JSON Web Tokens)
- Password Hashing: bcrypt with salt
- LLM Integration: Anthropic Claude API & Ollama
- Testing: XUnit framework
";
        }

        private static string GetSecuritySection()
        {
            return @"
JWT Token Flow:
1. User logs in with username/password
2. Backend validates credentials using bcrypt
3. Server generates JWT token (1-hour expiration)
4. Frontend stores token in localStorage
5. Token included in Authorization header for all requests
6. Server validates token on each request

Password Security:
- Passwords stored as bcrypt hashes (never plaintext)
- Bcrypt.Net-Core library for hashing
- Salt automatically included in hash
- Verification using BCrypt.Verify()

CORS Configuration:
- Allowed Origins: http://localhost:3000, https://localhost:3000
- All HTTP methods allowed
- All headers allowed
- Credentials supported

Environment Variables:
Store sensitive data in environment variables:
- DB_HOST, DB_USER, DB_PASSWORD
- ANTHROPIC_API_KEY
- JWT_SECRET (minimum 32 characters)
";
        }

        private static string GetUserManagementSection()
        {
            return @"
User Operations:

Create User:
- Endpoint: POST /api/users
- Requires: JWT authentication
- Password hashed before storage
- Returns: User object with ID

Get All Users:
- Endpoint: GET /api/users
- Requires: JWT authentication
- Excludes soft-deleted users
- Returns: Array of user objects

Update User:
- Endpoint: PUT /api/users/{id}
- Requires: JWT authentication
- New password hashed before update
- Returns: Updated user object

Delete User (Soft Delete):
- Endpoint: DELETE /api/users/{id}
- Sets IsDeleted=true instead of removing
- User can be restored if needed
- Maintains data integrity

Soft Delete Pattern:
Benefits:
- Recoverable if mistake made
- Maintains referential integrity
- Better audit trail
- Complies with data retention policies
";
        }

        private static string GetApiReferenceSection()
        {
            return @"
Authentication Endpoints:

Login:
POST /api/auth/login
Content-Type: application/json
Request: {username, password}
Response: {success, message, user, token}

Users Endpoints:

Create User:
POST /api/users
Authorization: Bearer {token}
Request: {username, password}
Response: {id, username}

Get All Users:
GET /api/users
Authorization: Bearer {token}
Response: Array of user objects

Chat Endpoints:

Send Message:
POST /api/chat/send
Authorization: Bearer {token}
Request: {message}
Response: {success, response}

Get Chat History:
GET /api/chat/history
Authorization: Bearer {token}
Response: Array of chat messages

Document Endpoints:

Upload Document:
POST /api/document/upload
Authorization: Bearer {token}
Content-Type: multipart/form-data
Response: {success, message, documentId}

Get My Documents:
GET /api/document/my-documents
Authorization: Bearer {token}
Response: {success, documents}

Delete Document:
DELETE /api/document/{id}
Authorization: Bearer {token}
Response: {success, message}
";
        }

        private static string GetDatabaseSection()
        {
            return @"
Users Table:
- id: SERIAL PRIMARY KEY
- username: VARCHAR(255) UNIQUE NOT NULL
- password_hash: VARCHAR(255) NOT NULL
- is_deleted: BOOLEAN DEFAULT FALSE

Chat Messages Table:
- id: SERIAL PRIMARY KEY
- user_id: INTEGER NOT NULL (FOREIGN KEY)
- role: VARCHAR(20) NOT NULL
- content: TEXT NOT NULL
- created_at: TIMESTAMP WITH TIME ZONE NOT NULL

Documents Table:
- id: SERIAL PRIMARY KEY
- file_name: VARCHAR(500) NOT NULL
- file_type: VARCHAR(50) NOT NULL
- original_text: TEXT NOT NULL
- uploaded_at: TIMESTAMP WITH TIME ZONE NOT NULL
- uploaded_by_user_id: INTEGER
- is_deleted: BOOLEAN DEFAULT FALSE
- deleted_at: TIMESTAMP WITH TIME ZONE

Document Chunks Table (with Vector Embeddings):
- id: SERIAL PRIMARY KEY
- document_id: INTEGER NOT NULL (FOREIGN KEY)
- content: TEXT NOT NULL
- chunk_index: INTEGER NOT NULL
- embedding: vector(1536)
- created_at: TIMESTAMP WITH TIME ZONE NOT NULL
- is_deleted: BOOLEAN DEFAULT FALSE

Data Relationships:
users (1) ---> (many) chat_messages
users (1) ---> (many) documents
documents (1) ---> (many) document_chunks
";
        }

        private static string GetTroubleshootingSection()
        {
            return @"
Common Issues & Solutions:

Issue: Unauthorized (401) Error
Cause: Missing or invalid JWT token
Solution:
- Verify token in Authorization header
- Check token hasn't expired (1-hour expiration)
- Login again to get new token

Issue: Database connection failed
Cause: PostgreSQL not running or wrong credentials
Solution:
- Check PostgreSQL is running
- Verify connection string in appsettings.json

Issue: Password verification failed after update
Cause: Old plaintext passwords in database
Solution:
- Clear existing users
- Re-create test users through UI

Issue: Document upload fails
Cause: pgvector extension not enabled
Solution:
- Run: CREATE EXTENSION IF NOT EXISTS vector;

Issue: Chat gives generic responses
Cause: No documents uploaded yet
Solution:
- Upload PDF documents through Documents tab
- System automatically extracts and indexes text

Issue: Ollama not responding
Cause: Ollama service not running
Solution:
- Start Ollama: ollama serve
- Pull Mistral: ollama pull mistral
- Or use Anthropic API by setting UseLocalLLM=false

Best Practices:
- Always use HTTPS in production
- Rotate JWT secret regularly
- Never commit credentials to git
- Use environment variables for sensitive data
- Keep dependencies updated
- Regular database backups
- Monitor disk space for documents
- Archive old documents periodically

Default Credentials (Development Only):
- Username: user1
- Password: admin
";
        }

        private static string GetDocumentationContent()
        {
            return @"User Management System Documentation - Complete Reference Guide";
        }
    }
}
