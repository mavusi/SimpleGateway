# Docker Setup for SimpleGateway

This solution is containerized with Docker and can be run using Docker Compose.

## Architecture

The solution consists of 4 containers:

1. **postgres** - PostgreSQL 17 database
2. **api** - Gateway API (port 8000)
3. **adminapi** - Admin API with database migrations (port 8001)
4. **web** - Web UI (port 8002)

## Port Mapping

- **8000** - Gateway API - Handles proxy requests to backend services
- **8001** - Admin API - Manages gateway configuration and runs DB migrations
- **8002** - Web UI - User interface for managing the gateway
- **5432** - PostgreSQL database

## Running the Application

### Using Docker Compose

```bash
# Build and start all services
docker-compose up --build

# Run in detached mode
docker-compose up -d --build

# Stop all services
docker-compose down

# Stop and remove volumes
docker-compose down -v
```

### Individual Services

```bash
# Build individual service
docker-compose build api
docker-compose build adminapi
docker-compose build web

# Start specific service
docker-compose up api
```

## Database Migrations

The **AdminAPI** container automatically applies database migrations on startup using Entity Framework Core migrations. This ensures the database schema is always up to date.

The migration process:
1. AdminAPI waits for PostgreSQL to be healthy
2. Connects to the database using the `POSTGRES_CONNECTION` environment variable
3. Runs `dbContext.Database.MigrateAsync()` to apply pending migrations
4. Logs success or failure

## Environment Variables

### Default Configuration (docker-compose.yml)

```yaml
POSTGRES_CONNECTION: postgresql://postgres:postgres@postgres:5432/simplegateway
ASPNETCORE_ENVIRONMENT: Docker
```

### Custom Configuration

You can override the default database connection by creating a `.env` file:

```bash
POSTGRES_CONNECTION=postgresql://user:password@host:port/database
```

Or export environment variables:

```bash
export POSTGRES_CONNECTION="postgresql://user:password@host:port/database"
docker-compose up
```

## Accessing the Services

Once running:

- Gateway API: http://localhost:8000
- Admin API: http://localhost:8001
- Web UI: http://localhost:8002
- PostgreSQL: localhost:5432

## Healthchecks

The PostgreSQL container includes a healthcheck that ensures it's ready before dependent services start:

```yaml
healthcheck:
  test: ["CMD-SHELL", "pg_isready -U postgres"]
  interval: 5s
  timeout: 5s
  retries: 5
```

## Volumes

PostgreSQL data is persisted in a Docker volume named `postgres_data`. This ensures data survives container restarts.

To reset the database:
```bash
docker-compose down -v  # Removes volumes
docker-compose up --build
```

## Troubleshooting

### Database Connection Issues

Check the database is ready:
```bash
docker-compose logs postgres
```

Check AdminAPI migration logs:
```bash
docker-compose logs adminapi
```

### Port Already in Use

If you get port conflicts, either:
1. Stop the conflicting service
2. Modify the port mappings in `docker-compose.yml`

### View Container Logs

```bash
# All services
docker-compose logs

# Specific service
docker-compose logs api
docker-compose logs adminapi
docker-compose logs web

# Follow logs in real-time
docker-compose logs -f
```

## Development

Each project has its own Dockerfile:
- `SimpleGateway.Api/Dockerfile` - Gateway API
- `SimpleGateway.AdminApi/Dockerfile` - Admin API with migrations
- `SimpleGateway.Web/Dockerfile` - Web UI

All Dockerfiles use multi-stage builds for optimal image size.
