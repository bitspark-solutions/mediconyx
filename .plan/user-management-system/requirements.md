# User Management System - Requirements

## Feature Overview

The User Management System is a core module of the Mediconyx Hospital Management SaaS platform that provides comprehensive user administration capabilities with multi-tenant support, role-based access control (RBAC), and secure authentication mechanisms.

## Functional Requirements

### FR-001: User Authentication
- **Description**: Users must be able to authenticate using email and password
- **Acceptance Criteria**:
  - Users can register with email, password, and tenant information
  - Users can login and receive JWT access and refresh tokens
  - Users can refresh their access tokens using refresh tokens
  - Passwords are hashed using BCrypt before storage
  - Invalid credentials return appropriate error messages

### FR-002: User Profile Management
- **Description**: Users can view and update their own profile information
- **Acceptance Criteria**:
  - Users can retrieve their own profile via `/api/usermanagement/me`
  - Users can update their first name, last name, and phone number
  - Users cannot modify their role or active status
  - Profile updates are logged with timestamps

### FR-003: User CRUD Operations (Admin Only)
- **Description**: Administrators can create, read, update, and delete users
- **Acceptance Criteria**:
  - Admins can create new users with email, password, role, and tenant assignment
  - Admins can view all users with pagination and filtering
  - Admins can update any user's information including role and active status
  - Admins can soft-delete users (set IsActive = false)
  - Admins cannot delete their own account

### FR-004: User Search and Filtering
- **Description**: Administrators can search and filter users by various criteria
- **Acceptance Criteria**:
  - Search by email, first name, or last name (case-insensitive)
  - Filter by role (Admin, Doctor, Nurse, Receptionist, Accountant, Patient)
  - Filter by active status (active/inactive)
  - Filter by tenant ID
  - Results are paginated (default 20 per page, max 100)
  - Sorting by CreatedAt, Email, or LastName (ascending/descending)

### FR-005: User Activation/Deactivation
- **Description**: Administrators can activate or deactivate user accounts
- **Acceptance Criteria**:
  - Admins can toggle user active status via PATCH endpoint
  - Inactive users cannot login
  - Admins cannot deactivate their own account
  - Status changes are logged

### FR-006: Password Management
- **Description**: Users can change their own password; admins can reset user passwords
- **Acceptance Criteria**:
  - Users can change their password by providing current and new password
  - New passwords must be at least 6 characters long
  - Current password must be verified before allowing change
  - Admins can reset any user's password without knowing current password
  - Password changes are logged

### FR-007: Multi-Tenant Support
- **Description**: System must support multiple hospital tenants with data isolation
- **Acceptance Criteria**:
  - Each user belongs to a specific tenant (hospital)
  - Users can only view users from their own tenant (unless Admin)
  - Tenant filtering is applied automatically in queries
  - Tenant information is included in user responses

### FR-008: Role-Based Access Control (RBAC)
- **Description**: Different user roles have different permissions
- **Acceptance Criteria**:
  - Roles: Admin, Doctor, Nurse, Receptionist, Accountant, Patient
  - Admin: Full access to all user management operations
  - Other roles: Can only view/update their own profile
  - Role information is included in JWT tokens
  - Authorization is enforced at controller level

## Non-Functional Requirements

### NFR-001: Security
- **Requirement**: System must implement OWASP Top 10 security best practices
- **Details**:
  - Passwords hashed using BCrypt (cost factor 12)
  - JWT tokens with secure signing (HMAC-SHA256)
  - Input validation on all endpoints
  - SQL injection prevention via EF Core parameterized queries
  - XSS prevention via proper output encoding
  - CSRF protection via SameSite cookies (future)
  - Rate limiting on authentication endpoints (future)

### NFR-002: Performance
- **Requirement**: User list queries must respond within 500ms for up to 10,000 users
- **Details**:
  - Database indexes on Email, TenantId, Role, IsActive
  - Pagination to limit result sets
  - Efficient EF Core queries with proper includes
  - Caching of frequently accessed user data (future)

### NFR-003: Scalability
- **Requirement**: System must support 100+ tenants with 1,000+ users per tenant
- **Details**:
  - Multi-tenant architecture with tenant isolation
  - Horizontal scaling capability
  - Database partitioning by tenant (future)

### NFR-004: Auditability
- **Requirement**: All user management operations must be logged
- **Details**:
  - Log user creation, updates, deletions
  - Log password changes and resets
  - Log login attempts (success and failure)
  - Include user ID, timestamp, and action type in logs

### NFR-005: Compliance
- **Requirement**: System must comply with healthcare data regulations
- **Details**:
  - HIPAA-equivalent compliance for Bangladesh healthcare regulations
  - Data encryption at rest and in transit
  - Access logging for audit trails
  - User consent management (future)

## User Stories

### US-001: As an Admin, I want to create new users
**Priority**: High  
**Acceptance Criteria**:
- I can create users with email, password, name, phone, role, and tenant
- System validates email format and password strength
- System prevents duplicate emails within the same tenant
- I receive confirmation when user is created successfully

### US-002: As an Admin, I want to search for users
**Priority**: High  
**Acceptance Criteria**:
- I can search by email, first name, or last name
- I can filter by role, active status, and tenant
- Results are paginated and sortable
- Search is case-insensitive and supports partial matches

### US-003: As a User, I want to view my profile
**Priority**: High  
**Acceptance Criteria**:
- I can access my profile information via `/api/usermanagement/me`
- Profile includes my email, name, phone, role, tenant, and login history
- Response is immediate (< 200ms)

### US-004: As a User, I want to update my profile
**Priority**: Medium  
**Acceptance Criteria**:
- I can update my first name, last name, and phone number
- Changes are saved immediately
- I cannot change my role or email

### US-005: As a User, I want to change my password
**Priority**: High  
**Acceptance Criteria**:
- I can change my password by providing current and new password
- System validates new password meets requirements
- System verifies current password before allowing change
- I receive confirmation when password is changed

### US-006: As an Admin, I want to deactivate user accounts
**Priority**: Medium  
**Acceptance Criteria**:
- I can toggle user active status
- Deactivated users cannot login
- I cannot deactivate my own account
- Status change is logged

## Tech Stack Decisions

### Backend
- **.NET 10 Preview** (targeting LTS when stable)
- **ASP.NET Core Web API** - RESTful API framework
- **Entity Framework Core 10** - ORM for database operations
- **PostgreSQL 15+** - Primary database
- **BCrypt.Net-Next 4.0.3** - Password hashing
- **System.IdentityModel.Tokens.Jwt 8.14.0** - JWT token generation
- **Microsoft.AspNetCore.Authentication.JwtBearer 10.0.0** - JWT authentication middleware
- **Swashbuckle.AspNetCore 10.0.0** - Swagger/OpenAPI documentation

### Frontend (Future)
- **Next.js 16.0.1** - React framework with App Router
- **React 19.2.0** - UI library
- **TypeScript 5.x** - Type safety
- **TanStack Query** - Server state management
- **Axios** - HTTP client
- **Zustand** - Client state management

## Integration Points

### Database
- **PostgreSQL** - User and Tenant tables
- **Entity Framework Core** - Data access layer
- **Migrations** - Database schema versioning

### Authentication
- **JWT Bearer Authentication** - Token-based auth
- **Refresh Token Mechanism** - Long-lived session management
- **Role Claims** - Embedded in JWT tokens

### External Services (Future)
- **Email Service** - Password reset emails
- **SMS Gateway** - Two-factor authentication
- **Audit Log Service** - Centralized logging

## Constraints & Assumptions

### Constraints
- Must work with existing authentication system (AuthController)
- Must maintain backward compatibility with existing User model
- Must support multi-tenant architecture from day one
- Database migrations must be reversible

### Assumptions
- Each tenant (hospital) has at least one Admin user
- Email addresses are unique within a tenant (not globally)
- Password reset via email will be implemented in Phase 2
- Two-factor authentication will be implemented in Phase 2
- User activity logging will be enhanced in Phase 2

## Success Criteria

1. ✅ All CRUD operations functional and tested
2. ✅ Role-based access control properly enforced
3. ✅ Multi-tenant data isolation verified
4. ✅ Search and filtering working correctly
5. ✅ Password management secure and functional
6. ✅ API documentation complete (Swagger)
7. ✅ Zero security vulnerabilities in code scan
8. ✅ All endpoints respond within performance targets

