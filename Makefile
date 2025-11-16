COMPOSE = docker compose
SERVICES = api ui db redis mailhog

.PHONY: up down purge build rebuild ps logs api-logs ui-logs restart api ui

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
