from __future__ import annotations

import asyncio
import os
import uuid
from datetime import date
from decimal import Decimal
from fastapi import Depends, FastAPI, Header, HTTPException, Request
from pydantic import BaseModel, Field

API_KEY = os.getenv("LEXOFFICE_API_KEY", "demo-lexoffice-key")
DELAY_MS = int(os.getenv("LEXMOCK_DELAY_MS", "0"))
CURRENCY_DEFAULT = os.getenv("LEXOFFICE_CURRENCY", "EUR")

app = FastAPI(title="LexiFlow LexOffice Mock", version="1.0.0")

_VOUCHERS: list[dict] = []


class VoucherLine(BaseModel):
    description: str | None = None
    amount: Decimal | None = None


class VoucherRequest(BaseModel):
    vendor: str
    date: date
    total: Decimal
    vat: Decimal
    currency: str = Field(default=CURRENCY_DEFAULT)
    rawText: str | None = None
    lines: list[VoucherLine] = Field(default_factory=list)


class VoucherResponse(BaseModel):
    voucherId: str


async def verify_api_key(x_api_key: str = Header(default="")) -> None:
    if API_KEY and x_api_key != API_KEY:
        raise HTTPException(status_code=401, detail="Invalid API key")


@app.get("/healthz", response_model=dict)
async def healthcheck() -> dict[str, str]:
    return {"status": "ok"}


@app.post("/api/v1/vouchers", response_model=VoucherResponse, dependencies=[Depends(verify_api_key)])
async def create_voucher(payload: VoucherRequest, request: Request) -> VoucherResponse:
    if DELAY_MS > 0:
        await asyncio.sleep(DELAY_MS / 1000)

    voucher_id = str(uuid.uuid4())
    _VOUCHERS.append({
        "voucherId": voucher_id,
        "payload": payload.model_dump(),
        "remote": request.client.host if request.client else None,
    })
    return VoucherResponse(voucherId=voucher_id)


@app.get("/api/v1/vouchers", response_model=list[dict])
async def list_vouchers(api_key: str = Header(default="", alias="x-api-key")) -> list[dict]:
    if API_KEY and api_key != API_KEY:
        raise HTTPException(status_code=401, detail="Invalid API key")
    return _VOUCHERS


if __name__ == "__main__":  # pragma: no cover
    import uvicorn

    uvicorn.run(app, host="0.0.0.0", port=80)
