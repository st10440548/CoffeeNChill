#!/usr/bin/env python3
"""Integration check: bootstrap Admin can provision a Staff user and Staff can log in."""
import json
import os
import secrets
import urllib.error
import urllib.request
from pathlib import Path

BASE = os.environ.get("BASE_URL", "http://localhost:5000").rstrip("/")
config = dict(line.split("=", 1) for line in Path(".env").read_text().splitlines()
              if "=" in line and not line.lstrip().startswith("#"))
def post(path, data, token=None):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = "Bearer " + token
    req = urllib.request.Request(BASE + path, data=json.dumps(data).encode(),
                                 headers=headers, method="POST")
    try:
        with urllib.request.urlopen(req, timeout=45) as response:
            return response.status, json.load(response)
    except urllib.error.HTTPError as error:
        return error.code, error.read().decode()[:300]

status, admin = post("/api/auth/login", {"email": config["BOOTSTRAP_ADMIN_EMAIL"],
                                         "password": config["BOOTSTRAP_ADMIN_PASSWORD"]})
assert status == 200 and isinstance(admin, dict) and admin.get("token"), "Admin login failed"
print("PASS Administrator login (200)")
email = "staff-test-" + secrets.token_hex(6) + "@example.com"
password = secrets.token_urlsafe(24)
status, response = post("/api/users/staff", {"email": email, "fullName": "Integration Test Staff",
                                            "password": password}, admin["token"])
assert status == 201, f"Staff creation failed: HTTP {status}: {response}"
print("PASS Administrator creates Staff (201)")
status, staff = post("/api/auth/login", {"email": email, "password": password})
assert status == 200 and isinstance(staff, dict) and staff.get("token"), "Staff login failed"
print("PASS Staff login (200)")
print("Admin/Staff integration checks passed. Test Staff account remains in local storage.")
