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

