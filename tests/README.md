# Unit Tests for DotNet10Template Project

This document describes the comprehensive unit test suite created for the DotNet10Template project.

## Test Projects

Three test projects have been created following best practices for .NET testing:

### 1. DotNet10Template.Application.Tests
Tests for the Application layer, including validators and DTOs.

**Location:** `tests/DotNet10Template.Application.Tests/`

**Test Classes:**
- `ProductValidatorsTests` (18 tests)
  - Tests for `CreateProductRequestValidator`
  - Tests for `UpdateProductRequestValidator`
  - Validates all validation rules: name, description, price, stock quantity, SKU format

### 2. DotNet10Template.Infrastructure.Tests
Tests for the Infrastructure layer, including services and data access.

**Location:** `tests/DotNet10Template.Infrastructure.Tests/`

**Test Classes:**
- `ProductServiceTests` (13 tests)
  - GetById with caching
  - Create with duplicate SKU validation
  - Update with business rules
  - Delete with soft delete
  - Search functionality

- `CategoryServiceTests` (16 tests)
  - GetById with caching
  - GetAll with caching
  - Create with duplicate name validation
  - Update with circular reference prevention
  - Delete with constraint checks (products, subcategories)

- `AuthServiceTests` (20 tests)
  - Login with various scenarios
  - Registration with validation
  - Password change
  - Password reset flow
  - Token management
  - Email confirmation

### 3. DotNet10Template.API.Tests
Tests for the API layer, including controllers.

**Location:** `tests/DotNet10Template.API.Tests/`

**Test Classes:**
- `ProductsControllerTests` (13 tests)
  - GetAll with pagination
  - GetById with success/failure scenarios
  - GetByCategory
  - Search functionality
  - Create with validation
  - Update with validation
  - Delete operations

## Test Frameworks and Libraries

All test projects use:
- **xUnit** - Test framework
- **Moq** - Mocking framework
- **FluentAssertions** - Assertion library for more readable tests
- **Microsoft.NET.Test.Sdk** - Test SDK
- **coverlet.collector** - Code coverage collection

Additional libraries per project:
- **Application.Tests**: FluentValidation test helpers
- **Infrastructure.Tests**: EF Core InMemory for database testing
- **API.Tests**: AspNetCore.Mvc.Testing for integration testing

## Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Tests from Specific Project
```bash
dotnet test tests/DotNet10Template.Application.Tests/DotNet10Template.Application.Tests.csproj
dotnet test tests/DotNet10Template.Infrastructure.Tests/DotNet10Template.Infrastructure.Tests.csproj
dotnet test tests/DotNet10Template.API.Tests/DotNet10Template.API.Tests.csproj
```

### Run Tests with Code Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Run Tests with Detailed Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

## Test Coverage Summary

**Total Tests: 67**
- Application Layer: 18 tests
- Infrastructure Layer: 49 tests (Product: 13, Category: 16, Auth: 20)
- API Layer: 13 tests

**Test Categories:**
- Unit Tests: All service and validator tests
- Controller Tests: Testing API endpoints with mocked dependencies
- Validation Tests: Testing FluentValidation rules

## Test Patterns Used

1. **AAA Pattern (Arrange-Act-Assert)**
   - All tests follow this standard pattern for clarity

2. **Descriptive Test Names**
   - Format: `MethodName_WhenCondition_ExpectedBehavior`
   - Example: `CreateAsync_WhenSkuAlreadyExists_ReturnsFailure`

3. **One Assertion Per Test**
   - Each test validates a single behavior
   - Makes failures easy to diagnose

4. **Mocking External Dependencies**
   - Services, repositories, and infrastructure are mocked
   - Tests are isolated and fast

5. **Edge Case Coverage**
   - Tests cover both happy path and error scenarios
   - Includes validation failures, not found cases, permission checks

## Adding New Tests

When adding new features:

1. **Add corresponding tests** in the appropriate test project
2. **Follow existing patterns** for consistency
3. **Run all tests** before committing to ensure nothing breaks
4. **Maintain high coverage** - aim for >80% code coverage

## CI/CD Integration

These tests are designed to run in CI/CD pipelines:
- All tests are deterministic (no randomness)
- No external dependencies (databases, APIs mocked)
- Fast execution (<30 seconds total)
- Clear failure messages

## Test Project Structure

```
tests/
├── DotNet10Template.Application.Tests/
│   ├── Validators/
│   │   └── ProductValidatorsTests.cs
│   └── DotNet10Template.Application.Tests.csproj
├── DotNet10Template.Infrastructure.Tests/
│   ├── Services/
│   │   ├── ProductServiceTests.cs
│   │   ├── CategoryServiceTests.cs
│   │   └── AuthServiceTests.cs
│   ├── TestHelpers/
│   │   └── DtoDefinitions.cs
│   └── DotNet10Template.Infrastructure.Tests.csproj
└── DotNet10Template.API.Tests/
    ├── Controllers/
    │   └── ProductsControllerTests.cs
    └── DotNet10Template.API.Tests.csproj
```

## Notes

- Tests use **.NET 10** targeting
- Central Package Management is configured in `Directory.Packages.props`
- All test projects reference the appropriate source projects
- Mock objects are created using Moq framework
- FluentAssertions provide readable assertion syntax

## Next Steps

Consider adding:
1. Integration tests using WebApplicationFactory
2. Performance tests for critical paths
3. End-to-end tests for complete workflows
4. Load tests for API endpoints
