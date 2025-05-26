import { z } from "zod";
/**
 * Zod schema for a single MCP server entry.
 * Allows extra properties for extensibility.
 */
export declare const mcpServerSchema: z.ZodObject<{
    type: z.ZodString;
    url: z.ZodString;
    headers: z.ZodOptional<z.ZodRecord<z.ZodString, z.ZodString>>;
}, "passthrough", z.ZodTypeAny, z.objectOutputType<{
    type: z.ZodString;
    url: z.ZodString;
    headers: z.ZodOptional<z.ZodRecord<z.ZodString, z.ZodString>>;
}, z.ZodTypeAny, "passthrough">, z.objectInputType<{
    type: z.ZodString;
    url: z.ZodString;
    headers: z.ZodOptional<z.ZodRecord<z.ZodString, z.ZodString>>;
}, z.ZodTypeAny, "passthrough">>;
/**
 * Zod schema for the MCP server list JSON file.
 * The servers object maps name → server definition.
 */
export declare const mcpServerListResponseSchema: z.ZodObject<{
    servers: z.ZodRecord<z.ZodString, z.ZodObject<{
        type: z.ZodString;
        url: z.ZodString;
        headers: z.ZodOptional<z.ZodRecord<z.ZodString, z.ZodString>>;
    }, "passthrough", z.ZodTypeAny, z.objectOutputType<{
        type: z.ZodString;
        url: z.ZodString;
        headers: z.ZodOptional<z.ZodRecord<z.ZodString, z.ZodString>>;
    }, z.ZodTypeAny, "passthrough">, z.objectInputType<{
        type: z.ZodString;
        url: z.ZodString;
        headers: z.ZodOptional<z.ZodRecord<z.ZodString, z.ZodString>>;
    }, z.ZodTypeAny, "passthrough">>>;
}, "strip", z.ZodTypeAny, {
    servers: Record<string, z.objectOutputType<{
        type: z.ZodString;
        url: z.ZodString;
        headers: z.ZodOptional<z.ZodRecord<z.ZodString, z.ZodString>>;
    }, z.ZodTypeAny, "passthrough">>;
}, {
    servers: Record<string, z.objectInputType<{
        type: z.ZodString;
        url: z.ZodString;
        headers: z.ZodOptional<z.ZodRecord<z.ZodString, z.ZodString>>;
    }, z.ZodTypeAny, "passthrough">>;
}>;
//# sourceMappingURL=schemas.d.ts.map