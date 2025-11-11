# User Management System - Sequence Diagram

## User Registration Sequence

```mermaid
sequenceDiagram
    participant Client
    participant AuthController
    participant PasswordService
    participant JwtService
    participant DbContext
    participant Database

    Client->>AuthController: POST /api/auth/register
    AuthController->>DbContext: Check if email exists
    DbContext->>Database: SELECT * FROM Users WHERE Email = ?
    Database-->>DbContext: User result
    DbContext-->>AuthController: User exists check
    
    alt User already exists
        AuthController-->>Client: 400 Bad Request
    else User does not exist
        AuthController->>DbContext: Verify tenant exists
        DbContext->>Database: SELECT * FROM Tenants WHERE Id = ?
        Database-->>DbContext: Tenant result
        DbContext-->>AuthController: Tenant validation
        
        alt Invalid tenant
            AuthController-->>Client: 400 Bad Request
        else Valid tenant
            AuthController->>PasswordService: HashPassword(password)
            PasswordService-->>AuthController: Hashed password
            
            AuthController->>JwtService: GenerateTokenPair(user)
            JwtService->>JwtService: GenerateAccessToken(user)
            JwtService->>JwtService: GenerateRefreshToken()
            JwtService-->>AuthController: (accessToken, refreshToken)
            
            AuthController->>DbContext: Create new User
            DbContext->>Database: INSERT INTO Users (...)
            Database-->>DbContext: User created
            DbContext-->>AuthController: User saved
            
            AuthController-->>Client: 201 Created (tokens + user info)
        end
    end
```

## User Login Sequence

```mermaid
sequenceDiagram
    participant Client
    participant AuthController
    participant PasswordService
    participant JwtService
    participant DbContext
    participant Database

    Client->>AuthController: POST /api/auth/login
    AuthController->>DbContext: Find user by email
    DbContext->>Database: SELECT * FROM Users WHERE Email = ?
    Database-->>DbContext: User result
    DbContext-->>AuthController: User entity
    
    alt User not found
        AuthController-->>Client: 401 Unauthorized
    else User found
        AuthController->>PasswordService: VerifyPassword(password, hash)
        PasswordService-->>AuthController: Verification result
        
        alt Invalid password
            AuthController-->>Client: 401 Unauthorized
        else Valid password
            alt User inactive
                AuthController-->>Client: 401 Unauthorized
            else User active
                alt Tenant inactive
                    AuthController-->>Client: 401 Unauthorized
                else Tenant active
                    AuthController->>JwtService: GenerateTokenPair(user)
                    JwtService-->>AuthController: (accessToken, refreshToken)
                    
                    AuthController->>DbContext: Update LastLoginAt
                    DbContext->>Database: UPDATE Users SET LastLoginAt = ?
                    Database-->>DbContext: Updated
                    DbContext-->>AuthController: User updated
                    
                    AuthController-->>Client: 200 OK (tokens + user info)
                end
            end
        end
    end
```

## Get User List Sequence (Admin)

```mermaid
sequenceDiagram
    participant Client
    participant UserManagementController
    participant JwtMiddleware
    participant Authorization
    participant DbContext
    participant Database

    Client->>UserManagementController: GET /api/usermanagement?page=1&role=Doctor
    UserManagementController->>JwtMiddleware: Validate JWT token
    JwtMiddleware-->>UserManagementController: Token validated
    
    UserManagementController->>Authorization: Check role claim
    Authorization-->>UserManagementController: Role = Admin
    
    alt Not Admin
        UserManagementController-->>Client: 403 Forbidden
    else Is Admin
        UserManagementController->>UserManagementController: Extract tenant ID from claims
        UserManagementController->>DbContext: Build query with filters
        DbContext->>DbContext: Apply tenant filter
        DbContext->>DbContext: Apply role filter
        DbContext->>DbContext: Apply search term
        DbContext->>DbContext: Apply pagination
        DbContext->>DbContext: Apply sorting
        
        DbContext->>Database: SELECT * FROM Users WHERE ... LIMIT ? OFFSET ?
        Database-->>DbContext: User results
        DbContext->>Database: SELECT COUNT(*) FROM Users WHERE ...
        Database-->>DbContext: Total count
        
        DbContext-->>UserManagementController: Users + total count
        UserManagementController->>UserManagementController: Map to DTOs
        UserManagementController-->>Client: 200 OK (paginated user list)
    end
```

## Update User Profile Sequence

```mermaid
sequenceDiagram
    participant Client
    participant UserManagementController
    participant JwtMiddleware
    participant Authorization
    participant DbContext
    participant Database

    Client->>UserManagementController: PUT /api/usermanagement/5
    UserManagementController->>JwtMiddleware: Validate JWT token
    JwtMiddleware-->>UserManagementController: Token validated
    
    UserManagementController->>Authorization: Get current user ID
    Authorization-->>UserManagementController: CurrentUserId = 3
    
    UserManagementController->>Authorization: Get current user role
    Authorization-->>UserManagementController: Role = Doctor
    
    UserManagementController->>DbContext: Find user by ID
    DbContext->>Database: SELECT * FROM Users WHERE Id = 5
    Database-->>DbContext: User entity
    DbContext-->>UserManagementController: User found
    
    alt User not found
        UserManagementController-->>Client: 404 Not Found
    else User found
        alt Not own profile AND not Admin
            UserManagementController-->>Client: 403 Forbidden
        else Own profile OR Admin
            alt Not Admin AND trying to change role/active
                UserManagementController-->>Client: 403 Forbidden
            else Valid update
                UserManagementController->>UserManagementController: Update user fields
                UserManagementController->>DbContext: Save changes
                DbContext->>Database: UPDATE Users SET ... WHERE Id = 5
                Database-->>DbContext: Updated
                DbContext-->>UserManagementController: User saved
                
                UserManagementController->>UserManagementController: Log update
                UserManagementController-->>Client: 200 OK (updated user)
            end
        end
    end
```

## Change Password Sequence

```mermaid
sequenceDiagram
    participant Client
    participant UserManagementController
    participant PasswordService
    participant DbContext
    participant Database

    Client->>UserManagementController: POST /api/usermanagement/change-password
    UserManagementController->>UserManagementController: Get current user ID from JWT
    UserManagementController->>DbContext: Find user by ID
    DbContext->>Database: SELECT * FROM Users WHERE Id = ?
    Database-->>DbContext: User entity
    DbContext-->>UserManagementController: User found
    
    UserManagementController->>PasswordService: VerifyPassword(currentPassword, hash)
    PasswordService-->>UserManagementController: Verification result
    
    alt Invalid current password
        UserManagementController-->>Client: 400 Bad Request
    else Valid current password
        alt New password too weak
            UserManagementController-->>Client: 400 Bad Request
        else Valid new password
            UserManagementController->>PasswordService: HashPassword(newPassword)
            PasswordService-->>UserManagementController: New hashed password
            
            UserManagementController->>DbContext: Update password hash
            DbContext->>Database: UPDATE Users SET PasswordHash = ? WHERE Id = ?
            Database-->>DbContext: Updated
            DbContext-->>UserManagementController: Password updated
            
            UserManagementController->>UserManagementController: Log password change
            UserManagementController-->>Client: 200 OK (success message)
        end
    end
```

