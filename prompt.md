You are a senior full-stack engineer. Extend and implement the project
“LexOffice Smart Receipt Automation (Dockerized .NET App)”
so a developer can:

1) run the entire system with one command,
2) open it in a browser to interact with a demo UI,
3) have dummy data automatically seeded (users, receipts, bookings),
4) and verify end-to-end with an automated smoke test.

Keep everything Dockerized. Produce all missing files.

============================================================
A) RUNTIME GOAL
============================================================
- One command: `docker compose up --build`
- App reachable in browser at: http://localhost:8080
- Frontend shows:
  - Login with seeded demo user
  - Receipts list (pre-seeded)
  - Upload form (manual test)
  - Book button (to send to mock lexoffice)
- All services healthy via Docker healthchecks.

============================================================
B) SERVICES (compose)
============================================================
Keep the earlier architecture and add healthchecks + seed hooks.

- frontend  : React (Vite) or Blazor WASM (served via nginx)
- api       : .NET 8 Web API
- ocr       : FastAPI + Tesseract
- lexmock   : FastAPI mock of lexoffice
- db        : PostgreSQL 16
- mq        : (optional) RabbitMQ (can be off by default)
- proxy     : (optional) nginx reverse proxy (only if needed)

Expose ports via ENV:
  FRONTEND_PORT=8080
  API_PORT=8081
  OCR_PORT=8082
  LEXMOCK_PORT=8083
  POSTGRES_PORT=5432

Network: `appnet`
Volumes: `db_data`, `uploads_data`

Add **healthchecks**:
- api: GET http://api/healthz
- ocr: GET http://ocr/healthz
- lexmock: GET http://lexmock/healthz
- frontend: GET http://frontend/ (200)
- db: pg_isready

============================================================
C) SEEDING (dummy data)
============================================================
Implement **automatic seeding** when containers start (idempotent):
1) Database seed (EF Core) in `api`:
   - Users:
     - username: demo / password: demo123! (hashed via ASP.NET Identity or custom)
   - Receipts (3 examples):
     - R1: "Demo Supermarket GmbH", date: 2025-01-15, total: 23.85, vat: 19.00
     - R2: "Office Depot AG",      date: 2025-01-16, total: 89.90, vat: 19.00
     - R3: "Bäckerei Sonnig",      date: 2025-01-17, total:  5.40, vat:  7.00
   - Optional Bookings (1 pre-booked receipt linked to a fake voucherId).

2) OCR sample inputs:
   - Place 2 small PNG/JPG receipts in `/ocr/samples/` (generate tiny placeholder images with text using Pillow in a setup script).
   - Add an endpoint `POST /ocr/extract` that accepts these samples too (use OCR or deterministic fallback parsing if OCR fails in CI).

3) Frontend demo state:
   - On first load (after login), fetch `/api/receipts` and display the 3 seeded receipts.

4) File uploads:
   - Mount a volume `uploads_data:/app/uploads` in `api`.
   - Saving uploaded files there is sufficient for demo.

============================================================
D) API CONTRACTS (confirm/implement)
============================================================
Back-end (.NET):
- `GET  /healthz` → `{ status: "ok" }`
- `POST /api/auth/login` → `{ token }` (JWT)
- `POST /api/upload` (multipart/form-data: file)
   Flow: save → call ocr `/ocr/extract` → normalize → persist → return `ReceiptDto`
- `GET  /api/receipts?status?&page?` → `[ReceiptDto]`
- `POST /api/book` { receiptId, corrections? }
   Flow: map → POST `lexmock:/api/v1/vouchers` (x-api-key) → save Booking → update Status → return `{ voucherId }`

OCR (FastAPI):
- `GET  /healthz` → `{ status: "ok" }`
- `POST /ocr/extract` → returns:
  {
    "vendor": "Store GmbH",
    "invoiceDate": "2025-01-16",
    "total": 12.34,
    "vat": 19.00,
    "currency": "EUR",
    "rawText": "..."
  }

LexMock (FastAPI):
- `GET  /healthz` → `{ status: "ok" }`
- `POST /api/v1/vouchers` (requires header `x-api-key`)
  Body: { vendor, date, total, vat, lines? }
  Returns: { voucherId: uuid }

============================================================
E) FRONTEND (MVP)
============================================================
Provide a minimal, clean UI with these views:
- Login (username/password)
- Receipts List: table with vendor/date/total/status + "Book" button
- Upload Form: file input → POST to /api/upload → refresh list
- Toasts/alerts for success/fail
- Environment variable `VITE_API_BASE` for API URL
- Show demo credentials on the login screen.

============================================================
F) SECURITY
============================================================
- JWT for API routes (except `/healthz`, `/swagger`)
- x-api-key for lexmock (passed from api)
- CORS: allow frontend origin
- No secrets hardcoded; pull from ENV (provide defaults in `.env.example`)

============================================================
G) DOCKERFILE + COMPOSE ENHANCEMENTS
============================================================
- Add healthchecks to every service.
- Add `depends_on` with `condition: service_healthy` so startup waits correctly.
- Ensure `docker compose up --build` brings system to a healthy, usable state.
- Provide Makefile targets: `up`, `down`, `reseed`, `logs`, `test`.

============================================================
H) SEED & TEST SCRIPTS
============================================================
1) `api` should run a seeder at startup (only when needed / idempotent):
   - EF migration → apply
   - create demo user if not exists
   - insert demo receipts if not exist
   Provide `dotnet` command or an internal hosted service to run seeding.

2) Add a lightweight **smoke test** script in `/infra/test/smoke.sh` that:
   - waits for healthchecks
   - logs in with demo credentials → capture JWT
   - GET receipts → expect ≥ 3
   - Upload a tiny generated PNG → expect new receipt created
   - Book the newly created receipt → expect voucherId from lexmock
   - echoes "SMOKE TEST PASSED" on success; non-zero exit on failure.

   The script should use `curl` and **service names** on the Docker network
   (e.g., http://api:80 inside the test container) or `localhost:${API_PORT}` from host.

3) Add a dedicated lightweight test container in compose:
   - `tester`: Alpine or bash image
   - mounts `/infra/test/smoke.sh`
   - runs smoke.sh when `TEST_ON_START=true`
   - document usage in README.

============================================================
I) DUMMY RECEIPT IMAGES (AUTO-GENERATED)
============================================================
- During OCR container build or first start, generate 2 simple PNG receipts with PIL/Pillow:
  Example image text:
  "Office Depot AG\nRechnung 2025-01-16\nGesamt 89,90 €\nMwSt 19%"
- Store as `/ocr/samples/r1.png`, `/ocr/samples/r2.png`
- Add a tiny CLI to call OCR on these samples to verify OCR works.

============================================================
J) README — "Run & Demo"
============================================================
Update README with a **copy-paste-ready** section:

### Quickstart
1. `cp .env.example .env` (or use provided `.env`)
2. `docker compose up --build`
3. Open http://localhost:8080
4. Login: demo / demo123!
5. Try:
   - Upload a new receipt
   - Click "Book" on a receipt
   - Observe voucherId in UI

### Healthchecks
- http://localhost:${API_PORT}/healthz
- http://localhost:${OCR_PORT}/healthz
- http://localhost:${LEXMOCK_PORT}/healthz

### Smoke Test (optional)
- `TEST_ON_START=true docker compose up --build`
  or
- `docker compose run --rm tester /infra/test/smoke.sh`

============================================================
K) ACCEPTANCE CRITERIA (MUST PASS)
============================================================
- `docker compose up --build` starts all services healthy.
- Browser at http://localhost:8080 shows UI and seeded receipts.
- Can login (demo/demo123!).
- Uploading an image creates a new receipt with OCR-extracted fields.
- Clicking “Book” calls lexmock and returns a voucherId.
- `smoke.sh` prints “SMOKE TEST PASSED”.

============================================================
L) FINAL STEP — SELF-TEST BY LLM
============================================================
After generating all files:
1) Print the final `docker-compose.yml`, `.env.example`, Makefile, and any new scripts/files.
2) Simulate the smoke test by showing the exact `curl` commands and the **expected JSON responses** (mock the JWT and voucherId).
3) Double-check ports and service names are consistent.
4) Provide a short “Troubleshooting” section (common Docker/permissions/CORS pitfalls).
5) Confirm that the README’s Quickstart works **end-to-end**.


