# SimpleGateway
A simple headless API Gateway

## Docker support

Build and run:

- `docker build -t simplegateway-api:latest -f api/SimpleGateway.Api/Dockerfile .`
- `docker run -p 8000:8000 -p 8001:8001 simplegateway-api:latest`

Or with Compose:

- `docker compose up --build`

API endpoints:
- Gateway: `http://localhost:8000` (all paths)
- Admin: `http://localhost:8001/admin/...`

API docs:
- Gateway OpenAPI JSON: `http://localhost:8000/openapi/v1.json`
- Gateway Swagger UI: `http://localhost:8000/swagger/index.html`
- Admin OpenAPI JSON: `http://localhost:8001/openapi/v1.json`
- Admin Swagger UI: `http://localhost:8001/swagger/index.html`

