# MCPhappey.Clients.Web

React web client for discovering and interacting with MCP servers via a user-friendly interface.

## Architecture

```mermaid
flowchart TD
    main[main.tsx]
    App[App.tsx]
    config[config.ts]
    auth[auth/]
    context[context/]
    features[features/]
    hooks[hooks/]
    types[types/]

    main --> App
    App --> auth
    App --> context
    App --> features
    App --> hooks
    App --> types
    App --> config
```

## Key Features
- Lists available MCP servers
- User-friendly web interface
- Built with React, Bootstrap, and @modelcontextprotocol/sdk

## Usage

```sh
npm install
npm run dev
```

## Dependencies
- React 19
- @modelcontextprotocol/sdk
- Bootstrap
