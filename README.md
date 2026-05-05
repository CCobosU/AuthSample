# AuthSample - JWT Authentication API (Clean Architecture)

Small authentication API sample implemented with Clean Architecture principles.

Projects:
- Domain: core entities and interfaces
- Application: DTOs, CQRS commands and handlers, validators
- Infrastructure: EF Core, repositories, token & hashing services
- WebApi: API surface, DI container, middleware

Run locally (requires Docker):
1. docker-compose up --build
2. The API will be available at http://localhost:5000

Env/Config: see src/WebApi/appsettings.json for defaults. Important variables:
- ConnectionStrings:DefaultConnection
- Jwt:Secret, Issuer, Audience

Example curl:
- Register:
  curl -X POST http://localhost:5000/api/auth/register -H "Content-Type: application/json" -d '{"username":"alice","email":"alice@example.com","password":"P@ssw0rd"}'

- Login:
  curl -X POST http://localhost:5000/api/auth/login -H "Content-Type: application/json" -d '{"usernameOrEmail":"alice","password":"P@ssw0rd"}'

Notes and design decisions
- Controllers contain no business logic; use MediatR handlers.
- Password hashing with BCrypt via Infrastructure implementation.
- JWT tokens signed with symmetric key; refresh tokens persisted in DB.
- Validation with FluentValidation; centralized error middleware.
- Serilog configured for structured logging.
- Project ready to add EF migrations (dotnet ef) and CI/CD pipeline.
