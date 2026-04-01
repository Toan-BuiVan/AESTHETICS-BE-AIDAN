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