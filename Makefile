COMPOSE ?= docker compose

.PHONY: up down build logs reseed test smoke

up:
	$(COMPOSE) up --build

down:
	$(COMPOSE) down --remove-orphans

build:
	$(COMPOSE) build

logs:
	$(COMPOSE) logs -f

reseed:
	$(COMPOSE) run --rm api dotnet run -- --seed

test smoke:
	$(COMPOSE) run --rm tester /infra/test/smoke.sh
