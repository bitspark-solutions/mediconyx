## Goals
- Containerize backend (`mediconyx-api/`) with .NET 10 and PostgreSQL 18 using unique host ports
- Add a Next.js UI (`mediconyx-ui`) with staff login wired to the API
- Keep docker-compose simplified (no YAML anchors), professional, with a single `mediconyx` network
- After approval, implement changes and publish to DocView

## Prerequisites
- Create `.env` at repo root with:
  - `POSTGRES_USER=postgres`
  - `POSTGRES_PASSWORD=<strong-password>`
  - `JWT_SECRET=<64+ char secret>`
- Confirm unique ports are free on host: API `9765`, UI `9673`, DB `9543`

## Directory Layout
- `mediconyx-api/` (existing API)
- `mediconyx-ui/` (new Next.js UI)
- `docker-compose.yml` (root)
- `.env` (root, untracked)

## Dockerfiles
- API (`mediconyx-api/Dockerfile`): multi-stage build using .NET 10 SDK → ASP.NET runtime
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY mediconyx-api/ ./
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet","mediconyx-api.dll"]
```
- UI (`mediconyx-ui/Dockerfile`): Node 24.11.0-alpine3.21 build → run
```dockerfile
FROM node:24.11.0-alpine3.21 AS deps
WORKDIR /app
RUN apk add --no-cache libc6-compat
COPY package.json package-lock.json ./
RUN npm ci

FROM node:24.11.0-alpine3.21 AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
ENV NEXT_PUBLIC_API_BASE_URL=http://localhost:9765
RUN npm run build

FROM node:24.11.0-alpine3.21 AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=3000
ENV NEXT_PUBLIC_API_BASE_URL=http://localhost:9765
COPY --from=builder /app/.next ./.next
COPY --from=builder /app/public ./public
COPY --from=builder /app/package.json ./package.json
COPY --from=builder /app/package-lock.json ./package-lock.json
RUN npm ci --omit=dev
EXPOSE 3000
CMD ["npm","run","start"]
```

## Compose (Simplified, Professional)
- Single network named `mediconyx`, unique host ports, minimal healthcheck only for DB
```yaml
version: "3.9"

services:
  db:
    image: postgres:18-alpine3.22
    container_name: mediconyx-db
    networks:
      - mediconyx
    restart: unless-stopped
    env_file:
      - .env
    environment:
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_DB: mediconyx
    ports:
      - "9543:5432"
    volumes:
      - db_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD", "pg_isready", "-U", "${POSTGRES_USER}"]
      interval: 10s
      timeout: 5s
      retries: 10

  api:
    build:
      context: .
      dockerfile: mediconyx-api/Dockerfile
    container_name: mediconyx-api
    networks:
      - mediconyx
    restart: unless-stopped
    env_file:
      - .env
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__DefaultConnection: Host=db;Port=5432;Database=mediconyx;Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      JwtSettings__Secret: ${JWT_SECRET}
      JwtSettings__Issuer: Mediconyx
      JwtSettings__Audience: MediconyxAPI
      JwtSettings__ExpiryMinutes: "60"
      FrontendUrl: http://localhost:9673
    ports:
      - "9765:8080"
    depends_on:
      db:
        condition: service_healthy

  ui:
    build:
      context: ./mediconyx-ui
      dockerfile: Dockerfile
    container_name: mediconyx-ui
    networks:
      - mediconyx
    restart: unless-stopped
    environment:
      NODE_ENV: production
      NEXT_PUBLIC_API_BASE_URL: http://localhost:9765
    ports:
      - "9673:3000"
    depends_on:
      api:
        condition: service_started

networks:
  mediconyx:
    driver: bridge

volumes:
  db_data:
```

## Backend Changes (Staff Login)
- Add `POST api/auth/staff/login` in `Controllers/AuthController.cs` next to existing login (`Controllers/AuthController.cs:90-143`):
  - Find user by email; verify password
  - Reject if `user.Role == UserRole.Patient`
  - Generate token pair; update refresh token and audit; return `AuthResponse`
- CORS already supports `FrontendUrl` env (`Program.cs:47-57`);
- Auto-migrations on dev startup are in place (`Program.cs:79-94`).

## UI Implementation
- Scaffold Next.js (TypeScript) app router in `mediconyx-ui`
- Page `/login` with fields `email` and `password`
- Submit to `POST ${NEXT_PUBLIC_API_BASE_URL}/api/auth/staff/login`
- Store `accessToken`/`refreshToken` client-side initially; call `GET ${NEXT_PUBLIC_API_BASE_URL}/api/usermanagement/me` to show profile
- Later harden to HttpOnly cookies if needed

## Run & Verify
- Build & start: `docker compose --env-file .env up --build`
- Access:
  - API: `http://localhost:9765` (Swagger UI enabled in dev)
  - UI: `http://localhost:9673`
  - DB: `localhost:9543`
- Verify flows:
  - Staff login success; patient rejected on staff login
  - `me` endpoint returns current user; role guard works in `UserManagementController`
  - Refresh token endpoint functions

## Deliverables (after approval)
- `docker-compose.yml` (root)
- `mediconyx-api/Dockerfile`
- `mediconyx-ui/Dockerfile` + minimal UI login page
- Staff login endpoint in `AuthController`
- Documentation placed in DocView for easy reference