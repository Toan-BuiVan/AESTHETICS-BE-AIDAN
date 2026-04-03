# Contributing Guidelines

## Code Standards

### API Integration
- All external API integrations (OpenAI, payment providers, etc.) should use configuration from `appsettings.json`
- API keys must never be hardcoded in source code
- Create dedicated service interfaces for external API calls
- Implement proper error handling and logging for external API calls

### LLM Integration
- Use `HttpClient` for API calls instead of direct SDK when possible
- Validate API responses before processing
- Log all API requests and responses for debugging
- Implement retry logic for transient failures

### Service Layer Implementation
- Services must use dependency injection for repositories and loggers
- All database operations should be wrapped in try-catch blocks
- Implement comprehensive logging for debugging (use ILogger)
- Services should validate input data before processing
- Return boolean or result objects rather than throwing exceptions where appropriate
- Create invoice details automatically when creating an invoice
- Filter and sort data consistently (e.g., newest first for lists)
- Use Task-based async/await pattern throughout