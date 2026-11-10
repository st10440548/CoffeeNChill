#!/usr/bin/env python3
"""Verify Staff menu permissions and Customer order-status denial locally."""
import json
import os
import secrets
import urllib.error
import urllib.request
from datetime import datetime, timezone
from pathlib import Path
import time

BASE = os.environ.get("BASE_URL", "http://localhost:5000").rstrip("/")
env = dict(line.split("=", 1) for line in Path(".env").read_text().splitlines()
           if "=" in line and not line.lstrip().startswith("#"))
def call(method, path, data=None, token=None):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = "Bearer " + token
    req = urllib.request.Request(BASE + path, data=json.dumps(data).encode() if data is not None else None,
                                 headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=45) as r:
            raw = r.read()
            return r.status, json.loads(raw) if raw else None
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode()[:250]

def login(email, password):
    status, body = call("POST", "/api/auth/login", {"email": email, "password": password})
    assert status == 200 and isinstance(body, dict) and body.get("token"), f"Login failed: HTTP {status}"
    return body["token"]

admin = login(env["BOOTSTRAP_ADMIN_EMAIL"], env["BOOTSTRAP_ADMIN_PASSWORD"])
suffix = secrets.token_hex(6)
staff_email = f"staff-permissions-{suffix}@example.com"
staff_password = secrets.token_urlsafe(24)
status, body = call("POST", "/api/users/staff",
                    {"email": staff_email, "fullName": "Permission Test Staff", "password": staff_password}, admin)
assert status == 201, f"Staff provisioning: HTTP {status}: {body}"
staff = login(staff_email, staff_password)
print("PASS Staff provisioned and authenticated")

customer_email = f"customer-permissions-{suffix}@example.com"
customer_password = secrets.token_urlsafe(24)
status, body = call("POST", "/api/auth/register",
                    {"email": customer_email, "fullName": "Permission Test Customer", "password": customer_password})
assert status == 201, f"Customer registration: HTTP {status}: {body}"
customer = login(customer_email, customer_password)
print("PASS Customer registered and authenticated")

item_path = f"/api/menu/PermissionTests/{suffix}"
created = False
try:
    item = {"category": "PermissionTests", "id": suffix, "name": "Staff Test Item",
            "description": "Temporary RBAC test", "price": 25.0, "isAvailable": True}
    status, _ = call("POST", "/api/menu", item, customer)
    assert status == 403, f"Customer menu create should be 403, got {status}"
    print("PASS Customer cannot create menu items (403)")
    status, body = call("POST", "/api/menu", item, staff)
    assert status == 201, f"Staff menu create: HTTP {status}: {body}"
    created = True
    print("PASS Staff creates menu item (201)")
    status, _ = call("PUT", item_path, {"price": 31.0}, customer)
    assert status == 403, f"Customer menu update should be 403, got {status}"
    print("PASS Customer cannot update menu items (403)")
    status, body = call("PUT", item_path, {"price": 31.0}, staff)
    assert status == 200, f"Staff menu update: HTTP {status}: {body}"
    print("PASS Staff updates menu item (200)")
    status, _ = call("DELETE", item_path, token=customer)
    assert status == 403, f"Customer menu delete should be 403, got {status}"
    print("PASS Customer cannot delete menu items (403)")
finally:
    if created:
        status, _ = call("DELETE", item_path, token=staff)
        assert status == 204, f"Staff cleanup: HTTP {status}"
        print("PASS Staff deletes menu item (204)")

timestamp = datetime.now(timezone.utc)
order_id = "permission-" + suffix
date = timestamp.strftime("%Y-%m-%d")
status, body = call("POST", "/api/orders/queue",
                    {"orderId": order_id, "customerName": "Permission Test",
                     "selectedItemSKUs": ["test-item"], "totalPrice": 31.0,
                     "orderTimestamp": timestamp.isoformat()})
assert status == 202, f"Order queue: HTTP {status}: {body}"
print("PASS Order queued (202)")
payload = {"orderDate": date, "orderId": order_id, "status": "Collected"}
status, _ = call("PUT", "/api/orders/status", payload, customer)
assert status == 403, f"Customer status update should be 403, got {status}"
print("PASS Customer cannot update order status (403)")
for _ in range(30):
    status, body = call("GET", f"/api/orders/{date}/{order_id}")
    if status == 200:
        break
    if status != 404:
        raise SystemExit(f"Order retrieval: HTTP {status}: {body}")
    time.sleep(1)
else:
    raise SystemExit("Order did not appear within 30 seconds")
status, body = call("PUT", "/api/orders/status", payload, staff)
assert status == 200, f"Staff status update: HTTP {status}: {body}"
print("PASS Staff updates order status (200)")
print("Staff role and order-status integration checks passed.")
