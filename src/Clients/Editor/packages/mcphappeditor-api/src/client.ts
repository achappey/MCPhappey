// High-level API client for MCPHappeditor
import { createRest } from './http';
import { routes } from './routes';
import type {
    Server,
    Resource,
    Prompt,
    PromptArgument
} from 'mcphappeditor-types';

export interface ApiClientConfig {
    baseUrl?: string;
    getAuthToken?: () => Promise<string>;
    headers?: Record<string, string>;
    fetch?: typeof fetch;
}

export const createApiClient = (cfg: ApiClientConfig = {}) => {
    const http = createRest({
        ...cfg,
        headers: { 'Content-Type': 'application/json', ...(cfg.headers || {}) }
    });

    // Servers
    const listServers = () => http.get<Server[]>(routes.servers());
    const getServer = (id: string) => http.get<Server>(routes.server(id));
    const createServer = (s: Server) => http.post<Server>(routes.servers(), s);
    const updateServer = (s: Server) => http.put<void>(routes.server(s.name), s);
    const deleteServer = (id: string) => http.del<void>(routes.server(id));

    // Resources
    const listResources = (mcpId: string) =>
        http.get<Resource[]>(routes.resources(mcpId));
    const getResource = (mcpId: string, id: string) =>
        http.get<Resource>(routes.resource(mcpId, id));
    const createResource = (mcpId: string, r: Resource) =>
        http.post<Resource>(routes.resources(mcpId), r);
    const updateResource = (mcpId: string, r: Resource) =>
        http.put<void>(routes.resource(mcpId, String(encodeURIComponent(r.uri))), r);
    const deleteResource = (mcpId: string, id: string) =>
        http.del<void>(routes.resource(mcpId, id));

    // Prompts
    const listPrompts = (mcpId: string) =>
        http.get<Prompt[]>(routes.prompts(mcpId));
    const getPrompt = (mcpId: string, id: string) =>
        http.get<Prompt>(routes.prompt(mcpId, id));
    const createPrompt = (mcpId: string, p: Prompt) =>
        http.post<Prompt>(routes.prompts(mcpId), p);
    const updatePrompt = (mcpId: string, p: Prompt) =>
        http.put<void>(routes.prompt(mcpId, String(p.name)), p);
    const deletePrompt = (mcpId: string, id: string) =>
        http.del<void>(routes.prompt(mcpId, id));

    return {
        servers: { listServers, getServer, createServer, updateServer, deleteServer },
        resources: {
            listResources,
            getResource,
            createResource,
            updateResource,
            deleteResource
        },
        prompts: {
            listPrompts,
            getPrompt,
            createPrompt,
            updatePrompt,
            deletePrompt
        }
    };
};