#!/usr/bin/env python3
"""Integration check: authenticated menu create, public read, update and cleanup."""
import json
import os
import secrets
import urllib.error
import urllib.request
from pathlib import Path

BASE = os.environ.get("BASE_URL", "http://localhost:5000").rstrip("/")
config = dict(line.split("=", 1) for line in Path(".env").read_text().splitlines()
              if "=" in line and not line.lstrip().startswith("#"))
def request(method, path, data=None, token=None):
    headers = {}
    if data is not None:
        headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = "Bearer " + token
    req = urllib.request.Request(BASE + path,
                                 data=json.dumps(data).encode() if data is not None else None,
                                 headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=60) as response:
            body = response.read()
            return response.status, json.loads(body) if body else None
    except urllib.error.HTTPError as error:
        return error.code, error.read().decode()[:300]

status, login = request("POST", "/api/auth/login",
                        {"email": config["BOOTSTRAP_ADMIN_EMAIL"],
                         "password": config["BOOTSTRAP_ADMIN_PASSWORD"]})
assert status == 200 and isinstance(login, dict) and login.get("token"), "Admin login failed"
print("PASS Admin login")
token = login["token"]
item_id = "test-" + secrets.token_hex(6)
path = "/api/menu/TestItems/" + item_id
created = False
try:
    status, result = request("POST", "/api/menu",
                             {"category": "TestItems", "id": item_id,
                              "name": "Test Cappuccino", "description": "Temporary test",
                              "price": 35.0, "isAvailable": True}, token)
    assert status == 201, f"Create failed: HTTP {status}: {result}"
    created = True
    print("PASS Admin creates menu item (201)")
    status, items = request("GET", "/api/menu/category/TestItems")
    assert status == 200 and isinstance(items, list) and any(
        x.get("id", x.get("Id")) == item_id for x in items), "Public menu read failed"
    print("PASS Public menu retrieval (200)")
    status, result = request("PUT", path, {"price": 42.0, "isAvailable": True}, token)
    assert status == 200, f"Update failed: HTTP {status}: {result}"
    print("PASS Admin updates menu item (200)")
finally:
    if created:
        status, _ = request("DELETE", path, token=token)
        assert status == 204, f"Cleanup failed: HTTP {status}"
        print("PASS Menu cleanup (204)")
print("Menu CRUD integration checks passed.")
