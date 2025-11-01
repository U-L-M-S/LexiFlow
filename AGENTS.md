# Repository Guidelines

## Project Structure & Module Organization
- `docker-compose.yml` orchestrates `frontend`, `api`, `ocr`, `lexmock`, `db`, and optional `tester` services on the `appnet` network.
- Application code lives in language-specific folders: `frontend/` (Vite React UI), `api/` (.NET 8 Web API + EF Core), `ocr/` (FastAPI + OCR helpers), and `lexmock/` (FastAPI mock).
- Shared automation sits under `infra/` (`infra/test/smoke.sh`, helper scripts) and `Makefile` shortcuts wrap common workflows.
- Persistent assets use volumes: `uploads_data` for API uploads and `db_data` for PostgreSQL. Generated sample receipts reside in `ocr/samples/`.

## Build, Test, and Development Commands
- `docker compose up --build` : builds images, seeds dummy data, and runs the whole stack at `http://localhost:8080`.
- `make up | make down | make logs` : convenience targets for lifecycle management; `make reseed` replays database seeding.
- `make test` (alias for `docker compose run --rm tester /infra/test/smoke.sh`) executes the end-to-end smoke flow.
- Local service checks: `dotnet test` inside `api/`, `npm run test` and `npm run lint` inside `frontend/`, `pytest` inside `ocr/`.
- Troubleshooting tweaks already applied: container healthchecks require `curl` (bundled in API/OCR/LexMock images) and JWT secret must be at least 32 chars; update `.env` accordingly.

## Coding Style & Naming Conventions
- C# follows .NET defaults: 4-space indents, `PascalCase` for classes, `camelCase` for locals, `IInterface` prefix for interfaces. Run `dotnet format` before committing.
- Frontend TypeScript/React uses 2-space indents, functional components, and `camelCase` for variables; format with `npm run lint -- --fix` or `npm run format` (Prettier).
- Python services (`ocr`, `lexmock`) use `black` + `ruff`, with `snake_case` modules/functions and type hints for new code. Keep startup scripts idempotent.

## Testing Guidelines
- Unit level: place C# tests under `api/tests/`, frontend tests under `frontend/src/__tests__/`, and Python tests under `ocr/tests/` or `lexmock/tests/` named `test_*.py`.
- End-to-end: ensure `infra/test/smoke.sh` passes; it expects ≥3 seeded receipts, successful upload, and booking with a voucherId echo.
- Aim for meaningful coverage on critical flows (auth, receipt pipeline). Add regression tests whenever touching seeding, booking, or OCR parsing.

## Commit & Pull Request Guidelines
- Git history is currently sparse; adopt Conventional Commits (`feat:`, `fix:`, `chore:`, etc.) with a short imperative subject and optional body detailing intent.
- Keep PRs focused: link related issues, describe impacted services, list new env vars, and include screenshots/GIFs for UI changes.
- Ensure CI (lint, unit tests, smoke test) is green before requesting review. Note any follow-up tasks or known gaps in the PR description.

## Security & Configuration Tips
- Copy `.env.example` to `.env`, override sensitive values locally, and keep secrets out of version control. Document defaults in the example file.
- JWT signing keys, database passwords, and the `LEXOFFICE_API_KEY` must be provided via environment variables or Docker secrets.
- Limit CORS origins to the bundled frontend (`http://localhost:${FRONTEND_PORT}`) and rotate sample credentials if leaking outside demos.
- When mirroring `.env.example`, ensure `JWT_SECRET` is ≥32 characters; smoke tests expect that length and will 500 otherwise.
