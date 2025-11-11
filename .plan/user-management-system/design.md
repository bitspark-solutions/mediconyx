# User Management System - Design Document

## Architecture Overview

The User Management System follows a simple layered architecture (Phase 1) that will evolve to Clean Architecture in Phase 2. The current design emphasizes simplicity and rapid development while maintaining security and scalability.

### Component Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     Client (Next.js UI)                      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ User List    │  │ User Profile │  │ User Form    │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└───────────────────────────┬─────────────────────────────────┘
                            │ HTTPS/REST API
                            │ JWT Bearer Token
┌───────────────────────────▼─────────────────────────────────┐
│              ASP.NET Core Web API (.NET 10)                 │
│  ┌──────────────────────────────────────────────────────┐  │
│  │         UserManagementController                      │  │
│  │  - GET    /api/usermanagement                        │  │
│  │  - GET    /api/usermanagement/{id}                   │  │
│  │  - GET    /api/usermanagement/me                     │  │
│  │  - POST   /api/usermanagement                        │  │
│  │  - PUT    /api/usermanagement/{id}                   │  │
│  │  - DELETE /api/usermanagement/{id}                   │  │
│  │  - PATCH  /api/usermanagement/{id}/toggle-active     │  │
│  │  - POST   /api/usermanagement/change-password        │  │
│  │  - POST   /api/usermanagement/reset-password         │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │         AuthController (Existing)                    │  │
│  │  - POST /api/auth/register                           │  │
│  │  - POST /api/auth/login                               │  │
│  │  - POST /api/auth/refresh                            │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │         Services Layer                               │  │
│  │  - IPasswordService (BCrypt hashing)                 │  │
│  │  - IJwtService (Token generation)                   │  │
│  └──────────────────────────────────────────────────────┘  │
└───────────────────────────┬─────────────────────────────────┘
                            │ Entity Framework Core
┌───────────────────────────▼─────────────────────────────────┐
│                    PostgreSQL Database                       │
│  ┌──────────────┐  ┌──────────────┐                        │
│  │    Users      │  │   Tenants    │                        │
│  │  - Id         │  │  - Id        │                        │
│  │  - Email      │  │  - Name      │                        │
│  │  - Password   │  │  - IsActive  │                        │
│  │  - Role       │  └──────────────┘                        │
│  │  - TenantId   │                                            │
│  │  - IsActive   │                                            │
│  └──────────────┘                                            │
└──────────────────────────────────────────────────────────────┘
```

## Data Models

### User Entity

```csharp
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Multi-tenant support
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    
    // Refresh token for JWT
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public enum UserRole
{
    Admin = 1,
    Doctor = 2,
    Nurse = 3,
    Receptionist = 4,
    Accountant = 5,
    Patient = 6
}
```

### Tenant Entity

```csharp
public class Tenant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public ICollection<User> Users { get; set; } = new List<User>();
}
```

### Database Schema

```sql
-- Users table
CREATE TABLE "Users" (
    "Id" SERIAL PRIMARY KEY,
    "Email" VARCHAR(255) NOT NULL,
    "PasswordHash" VARCHAR(255) NOT NULL,
    "FirstName" VARCHAR(100) NOT NULL,
    "LastName" VARCHAR(100) NOT NULL,
    "PhoneNumber" VARCHAR(50),
    "Role" INTEGER NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "TenantId" INTEGER NOT NULL,
    "RefreshToken" TEXT,
    "RefreshTokenExpiryTime" TIMESTAMP,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP,
    "LastLoginAt" TIMESTAMP,
    CONSTRAINT "FK_Users_Tenants_TenantId" 
        FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT
);

-- Indexes
CREATE UNIQUE INDEX "IX_Users_Email" ON "Users"("Email");
CREATE INDEX "IX_Users_TenantId_Email" ON "Users"("TenantId", "Email");
CREATE INDEX "IX_Users_Role" ON "Users"("Role");
CREATE INDEX "IX_Users_IsActive" ON "Users"("IsActive");
CREATE INDEX "IX_Users_TenantId" ON "Users"("TenantId");
```

## API Contracts

### GET /api/usermanagement/me
**Description**: Get current user's profile  
**Authentication**: Required (JWT)  
**Authorization**: Any authenticated user  
**Response**: `200 OK`
```json
{
  "id": 1,
  "email": "doctor@hospital.com",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+8801712345678",
  "role": "Doctor",
  "isActive": true,
  "tenantId": 1,
  "tenantName": "City Hospital",
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": null,
  "lastLoginAt": "2024-01-20T08:15:00Z"
}
```

### GET /api/usermanagement?pageNumber=1&pageSize=20&role=Doctor&isActive=true&searchTerm=john
**Description**: Get paginated list of users with filtering  
**Authentication**: Required (JWT)  
**Authorization**: Admin only  
**Query Parameters**:
- `tenantId` (optional): Filter by tenant
- `role` (optional): Filter by role (Admin, Doctor, Nurse, etc.)
- `isActive` (optional): Filter by active status
- `searchTerm` (optional): Search in email, first name, last name
- `pageNumber` (default: 1): Page number
- `pageSize` (default: 20, max: 100): Items per page
- `sortBy` (default: "CreatedAt"): Sort field (CreatedAt, Email, LastName)
- `sortOrder` (default: "desc"): Sort direction (asc, desc)

**Response**: `200 OK`
```json
{
  "users": [
    {
      "id": 1,
      "email": "doctor@hospital.com",
      "firstName": "John",
      "lastName": "Doe",
      "role": "Doctor",
      "isActive": true,
      "tenantId": 1,
      "tenantName": "City Hospital",
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ],
  "totalCount": 45,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

### POST /api/usermanagement
**Description**: Create a new user  
**Authentication**: Required (JWT)  
**Authorization**: Admin only  
**Request Body**:
```json
{
  "email": "nurse@hospital.com",
  "password": "SecurePass123",
  "firstName": "Jane",
  "lastName": "Smith",
  "phoneNumber": "+8801712345679",
  "role": "Nurse",
  "tenantId": 1
}
```
**Response**: `201 Created` (with user details) or `400 Bad Request` (validation errors)

### PUT /api/usermanagement/{id}
**Description**: Update user information  
**Authentication**: Required (JWT)  
**Authorization**: Admin (any user) or User (own profile only)  
**Request Body**:
```json
{
  "firstName": "Jane",
  "lastName": "Smith",
  "phoneNumber": "+8801712345679",
  "role": "Nurse",
  "isActive": true
}
```
**Response**: `200 OK` (with updated user details) or `403 Forbidden` (unauthorized)

### DELETE /api/usermanagement/{id}
**Description**: Soft delete user (set IsActive = false)  
**Authentication**: Required (JWT)  
**Authorization**: Admin only  
**Response**: `204 No Content` or `400 Bad Request` (cannot delete self)

### PATCH /api/usermanagement/{id}/toggle-active
**Description**: Toggle user active status  
**Authentication**: Required (JWT)  
**Authorization**: Admin only  
**Response**: `200 OK` (with updated user details)

### POST /api/usermanagement/change-password
**Description**: Change own password  
**Authentication**: Required (JWT)  
**Authorization**: Any authenticated user  
**Request Body**:
```json
{
  "currentPassword": "OldPass123",
  "newPassword": "NewPass456"
}
```
**Response**: `200 OK` or `400 Bad Request` (invalid current password)

### POST /api/usermanagement/reset-password
**Description**: Reset user password (Admin only)  
**Authentication**: Required (JWT)  
**Authorization**: Admin only  
**Request Body**:
```json
{
  "userId": 5,
  "newPassword": "NewPass456"
}
```
**Response**: `200 OK` or `404 Not Found` (user not found)

## Workflow Diagrams

### User Registration Flow
1. Client sends POST /api/auth/register with user details
2. System validates email format and checks for duplicates
3. System verifies tenant exists and is active
4. System hashes password using BCrypt
5. System creates user record in database
6. System generates JWT tokens
7. System returns tokens and user information

### User Login Flow
1. Client sends POST /api/auth/login with email and password
2. System finds user by email
3. System verifies password hash
4. System checks user and tenant are active
5. System updates LastLoginAt timestamp
6. System generates new JWT tokens
7. System returns tokens and user information

### User List Flow (Admin)
1. Admin sends GET /api/usermanagement with query parameters
2. System extracts JWT token and validates
3. System checks user role is Admin
4. System builds query with filters (tenant, role, active, search)
5. System applies pagination and sorting
6. System returns paginated user list

### Password Change Flow
1. User sends POST /api/usermanagement/change-password
2. System validates JWT token
3. System finds user by ID from token
4. System verifies current password
5. System validates new password strength
6. System hashes new password
7. System updates password hash in database
8. System logs password change
9. System returns success response

## Error Handling Strategy

### HTTP Status Codes
- `200 OK` - Successful GET, PUT, PATCH operations
- `201 Created` - Successful POST (create) operations
- `204 No Content` - Successful DELETE operations
- `400 Bad Request` - Validation errors, invalid input
- `401 Unauthorized` - Missing or invalid JWT token
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Unexpected server errors

### Error Response Format
```json
{
  "message": "User with this email already exists in this tenant.",
  "errors": {
    "email": ["Email is already registered"]
  }
}
```

### Validation Rules
- Email: Must be valid email format, unique within tenant
- Password: Minimum 6 characters
- FirstName/LastName: Required, max 100 characters
- PhoneNumber: Optional, max 50 characters
- Role: Must be valid enum value
- TenantId: Must reference existing active tenant

## Security Measures

### Authentication
- JWT Bearer token authentication
- Access token expiry: 60 minutes
- Refresh token expiry: 7 days
- Token stored in HTTP-only cookies (future) or localStorage (current)

### Authorization
- Role-based access control (RBAC)
- `[Authorize]` attribute on controller
- `[Authorize(Roles = "Admin")]` for admin-only endpoints
- Custom authorization logic for profile updates

### Password Security
- BCrypt hashing with cost factor 12
- Password never stored in plain text
- Password change requires current password verification
- Password reset requires admin privileges

### Data Protection
- SQL injection prevention via EF Core parameterized queries
- XSS prevention via proper output encoding
- Input validation on all endpoints
- Email validation using MailAddress class

### Multi-Tenant Isolation
- Tenant ID included in JWT claims
- Automatic tenant filtering in queries
- Users can only access data from their tenant
- Admins can access all tenants (configurable)

## Deployment Plan

### Development Environment
- Local PostgreSQL database
- .NET 10 SDK
- Swagger UI for API testing
- Hot reload enabled

### Docker Compose (Local Dev)
```yaml
services:
  postgres:
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: mediconyx
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres123
    ports:
      - "5432:5432"
  
  backend:
    build: ./mediconyx-api
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=mediconyx;..."
    depends_on:
      - postgres
```

### Production Deployment
- PostgreSQL on managed database service
- .NET 10 runtime in Docker container
- Nginx reverse proxy
- SSL/TLS certificates (Let's Encrypt)
- Environment variables for secrets
- Database migrations via CI/CD pipeline

## Testing Strategy

### Unit Tests
- Test password hashing and verification
- Test JWT token generation and validation
- Test user query filtering logic
- Test authorization helper methods
- Target: 80%+ code coverage

### Integration Tests
- Test user CRUD operations with database
- Test authentication flow end-to-end
- Test role-based authorization
- Test multi-tenant data isolation
- Use test database container

### E2E Tests (Future)
- Test user registration and login flow
- Test admin user management workflow
- Test password change flow
- Use Playwright for browser automation

## Performance Considerations

### Database Optimization
- Indexes on Email, TenantId, Role, IsActive
- Composite index on (TenantId, Email)
- Pagination to limit result sets
- Efficient EF Core queries (avoid N+1)

### Caching Strategy (Future)
- Cache user profile data in Redis
- Cache tenant information
- Cache user list queries (with TTL)
- Invalidate cache on user updates

### Response Time Targets
- User profile retrieval: < 200ms
- User list (paginated): < 500ms
- User creation: < 300ms
- User update: < 250ms

