COMPOSE = docker compose
SERVICES = api ui db mailhog
DOTNET = "C:/Program Files/dotnet/dotnet.exe"

.PHONY: up down purge build rebuild ps logs api-logs ui-logs restart api ui test test-watch test-coverage

up:
	$(COMPOSE) up --build -d

down:
	$(COMPOSE) down

purge:
	$(COMPOSE) down -v

build:
	$(COMPOSE) build

rebuild:
	$(COMPOSE) build --no-cache

ps:
	$(COMPOSE) ps

logs:
	$(COMPOSE) logs -f $(SERVICES)

api-logs:
	$(COMPOSE) logs -f api

ui-logs:
	$(COMPOSE) logs -f ui

restart:
	$(COMPOSE) restart $(SERVICES)

api:
	$(COMPOSE) up --build -d api

ui:
	$(COMPOSE) up --build -d ui

test:
	$(DOTNET) test mediconyx-api/Tests/mediconyx-api.Tests.csproj --logger "console;verbosity=normal"

test-watch:
	$(DOTNET) watch test --project mediconyx-api/Tests/mediconyx-api.Tests.csproj

test-coverage:
	$(DOTNET) test mediconyx-api/Tests/mediconyx-api.Tests.csproj --collect:"XPlat Code Coverage" --results-directory mediconyx-api/Tests/coverage
