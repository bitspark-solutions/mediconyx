# Configuration Setup

## Environment Configuration

This project uses `appsettings.json` and `appsettings.Development.json` for configuration.

### Setup Instructions

1. **Copy example files** (if they don't exist):
   ```bash
   cp appsettings.json.example appsettings.json
   cp appsettings.Development.json.example appsettings.Development.json
   ```

2. **Update with your values**:
   - Replace `YOUR_PASSWORD_HERE` with your PostgreSQL password
   - Replace `YOUR_SUPER_SECRET_KEY...` with a secure JWT secret (minimum 32 characters)
   - Update database connection string if needed
   - Update FrontendUrl if your Next.js app runs on a different port

### Security Note

- `appsettings.Development.json` is in `.gitignore` (contains secrets)
- `appsettings.json` is tracked (contains non-sensitive defaults)
- Always use environment variables or secure vaults for production secrets

### Environment Variables (Alternative)

You can also use environment variables instead of appsettings files:

```bash
# Database
export ConnectionStrings__DefaultConnection="Host=localhost;Database=mediconyx;Username=postgres;Password=your_password"

# JWT
export JwtSettings__Secret="your_super_secret_key_minimum_32_characters"
export JwtSettings__Issuer="Mediconyx"
export JwtSettings__Audience="MediconyxAPI"
export JwtSettings__ExpiryMinutes="60"

# Frontend
export FrontendUrl="http://localhost:3000"
```

