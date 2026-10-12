# Postman setup and testing

Import CoffeeNChill.postman_collection.json from the docs folder.
If prompted, upgrade the collection to v3.

For converted local collections, use Files view to locate requests.
Open a request and use the arrow icon to open its visual editor.

If {{baseUrl}} is unresolved, configure it as:
http://localhost:7071

For individual testing, the full localhost URL can be entered directly.
Replace unresolved body and route variables with matching test values.

Run the core requests in numbered order. Create a menu item first,
then use that item's ID for listing, updating and deletion checks.

For order testing:
1. POST /api/orders/queue with docs/sample-order.json.
2. Expect 202 Accepted.
3. GET /api/orders/2026-10-12/ORD-DEMO-001.
4. Expect 200 OK and eventually status Collected.
5. Verify receivedAt, preparingAt, readyAt and collectedAt are present.

Use a new OrderId for each new demonstration.
Document requests require a configured real Azure Files connection.

All 11 core requests passed when tested individually.
A full Collection Runner run has not yet been verified.