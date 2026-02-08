# Api.Tests

This project contains unit tests for the API. Follow the guidelines below when writing tests.

## Testing Framework

- **MSTest** - We use MSTest as our testing framework

## Unit Testing Guidelines

### Database Setup

- **Use In-Memory SQL Database** - All tests should use an in-memory SQL database to ensure fast, isolated tests
- **Seed the Database** - Ensure the in-memory database is properly seeded with necessary test data before running tests

### Test Organization

- **Reusable Stubs** - Create and use reusable stubs/mocks to minimize code duplication
- **Arrange-Act-Assert Pattern** - Structure tests using the AAA pattern for clarity:
  - **Arrange**: Set up test data and dependencies
  - **Act**: Execute the code under test
  - **Assert**: Verify the expected outcome

### Best Practices

- Keep tests independent and isolated
- Use descriptive test method names that clearly indicate what is being tested
- Clean up resources after tests complete
- Mock external dependencies
- Test both success and failure scenarios
- Keep test data minimal but sufficient

## Running Tests

```bash
dotnet test
```

## Project Structure

Organize test files to mirror the structure of the main API project for easy navigation.
