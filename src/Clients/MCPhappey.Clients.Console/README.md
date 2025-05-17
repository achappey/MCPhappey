# MCPhappey.Clients.Console

CLI tool for discovering and interacting with MCP servers.

## Architecture

```mermaid
flowchart TD
    Program
    OAuthHandlerAsync
    ConsoleWriter
    AppSettings
    Program --> OAuthHandlerAsync
    Program --> ConsoleWriter
    Program --> AppSettings
```

## Key Features
- Lists and connects to MCP servers
- Invokes tools and reads resources
- Supports OAuth authentication

## Usage

```sh
dotnet run
```

## Dependencies
- MCPhappey.Core
- MCPhappey.Common
