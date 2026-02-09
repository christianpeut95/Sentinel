# Copilot Instructions

## General Guidelines
- When displaying lookup navigation properties in views, prefer eager-loading with EF Core Includes and null-safe access in the Razor views (use ?.Name) to avoid NullReferenceExceptions.