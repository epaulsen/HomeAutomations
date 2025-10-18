# GitHub Copilot URL Whitelist

This file documents the URL whitelist configuration for GitHub Copilot agents in this repository.

## Configuration File

The whitelist is configured in `.github/copilot-whitelist.json`.

## Whitelisted URLs

The following URLs are whitelisted for GitHub Copilot agents to access:

1. **NetDaemon Getting Started Documentation**
   - URL: https://netdaemon.xyz/docs/user/started/get_started/
   - Purpose: Official NetDaemon documentation for getting started with the framework
   - This is essential for the Copilot agent to provide accurate guidance about NetDaemon development

## Why Whitelist URLs?

GitHub Copilot agents have limited internet access by default. Whitelisting specific URLs allows the agent to:
- Access official documentation
- Provide more accurate code suggestions
- Reference up-to-date API information
- Follow best practices from official sources

## Adding New URLs

To add a new URL to the whitelist:

1. Edit `.github/copilot-whitelist.json`
2. Add the URL to the `allowed_urls` array
3. Ensure the URL is from a trusted source
4. Update this documentation file with the URL and its purpose

## Format

The whitelist file uses JSON format:

```json
{
  "allowed_urls": [
    "https://example.com/docs/"
  ],
  "description": "Whitelist of URLs that GitHub Copilot agents can access",
  "version": "1.0"
}
```

## Security Considerations

Only whitelist URLs from trusted sources:
- Official documentation sites
- Well-known package repositories
- Verified API documentation

Avoid whitelisting:
- Unknown or untrusted domains
- Sites that might contain sensitive information
- Dynamic content that could change unexpectedly
