# LexiFlow

LexiFlow is a Docker-first demo that wires a React dashboard, a .NET 9 Web API, OCR microservice, and a LexOffice mock into an end-to-end receipt automation flow. Containers come pre-seeded so you can explore uploads, OCR extraction, and voucher bookings immediately.

## Quickstart
1. `cp .env.example .env`
2. `docker compose up --build`
3. Open http://localhost:8080 in your browser
4. Sign in with `demo / demo123!`
5. Upload a receipt, then click **Book** to send it to the LexOffice mock

## Services & Ports
- `frontend` (http://localhost:8080) – Vite/React UI served by Nginx, configurable via `VITE_API_BASE`
- `api` (http://localhost:8081) – .NET 9 Web API with EF Core, JWT auth, and seeding logic
- `ocr` (http://localhost:8082) – FastAPI wrapper around Tesseract with deterministic fallbacks and sample images
- `lexmock` (http://localhost:8083) – FastAPI mock that issues voucher IDs when booked
- `db` (postgres:5432) – Stores users, receipts, and bookings

## Developer Commands
- `make up` / `make down` – Spin the stack up or tear it down
- `make logs` – Follow all service logs
- `make reseed` – Rerun EF migrations and seed demo data inside the API container
- `make test` – Launch the smoke test container on demand

## Smoke Test Automation
Run smoke tests automatically by exporting `TEST_ON_START=true` before `docker compose up`, or execute them manually:
```bash
docker compose run --rm tester /infra/test/smoke.sh
```
The script waits for health checks, authenticates with the demo user, verifies the seeded receipts, uploads a PNG, and books it through the mock LexOffice API.

## Health Checks
- API: http://localhost:${API_PORT:-8081}/healthz
- OCR: http://localhost:${OCR_PORT:-8082}/healthz
- LexMock: http://localhost:${LEXMOCK_PORT:-8083}/healthz
- Frontend: http://localhost:${FRONTEND_PORT:-8080}/

## Troubleshooting
- **Containers restart immediately** – ensure ports 8080–8083 are free or adjust `.env` before composing.
- **Smoke test exits early** – confirm `TEST_ON_START=true` when running via `docker compose up`, or invoke the tester service directly for manual runs.
- **OCR returns defaults only** – make sure the OCR container built successfully; re-run `docker compose build ocr` if Pillow/Tesseract failed during the image stage.
- **CORS errors in the browser** – verify `FRONTEND_ALLOWED_ORIGINS` in `.env` matches the origin you use to access the frontend.
