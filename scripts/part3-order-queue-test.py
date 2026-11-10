#!/usr/bin/env python3
"""End-to-end order queue integration check against the running gateway."""
import json
import os
import secrets
import time
import urllib.error
import urllib.request
from datetime import datetime, timezone

BASE = os.environ.get("BASE_URL", "http://localhost:5000").rstrip("/")
order_id = "test-" + secrets.token_hex(5)
timestamp = datetime.now(timezone.utc)
order_date = timestamp.strftime("%Y-%m-%d")

def call(method, path, data=None):
    request = urllib.request.Request(
        BASE + path,
        data=json.dumps(data).encode() if data is not None else None,
        headers={"Content-Type": "application/json"},
        method=method,
    )
    try:
        with urllib.request.urlopen(request, timeout=45) as response:
            body = response.read()
            return response.status, json.loads(body) if body else {}
    except urllib.error.HTTPError as error:
        return error.code, error.read().decode()[:300]

status, result = call("POST", "/api/orders/queue", {
    "orderId": order_id,
    "customerName": "Automated Test Customer",
    "selectedItemSKUs": ["test-cappuccino"],
    "totalPrice": 42.0,
    "orderTimestamp": timestamp.isoformat(),
})
if status != 202:
    raise SystemExit(f"FAIL Queue order: HTTP {status}, {result}")
print("PASS Order accepted into queue (202)")

path = f"/api/orders/{order_date}/{order_id}"
seen = set()
for attempt in range(30):
    status, result = call("GET", path)
    if status == 200 and isinstance(result, dict):
        current = result.get("status", result.get("Status"))
        if current and current not in seen:
            print("Order status:", current)
            seen.add(current)
        if current == "Collected":
            print("PASS Order processed through queue")
            print("PASS Final order status: Collected")
            break
    elif status not in (200, 404):
        raise SystemExit(f"FAIL Order lookup: HTTP {status}, {result}")
    time.sleep(3)
else:
    raise SystemExit("FAIL Order did not reach Collected within 90 seconds")
print("Order queue verification completed.")
