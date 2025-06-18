/**
 * Generates a PKCE code verifier and its corresponding code challenge.
 * @returns A Promise that resolves to an object containing the code_verifier and code_challenge.
 */
export declare const createPkceChallenge: () => Promise<{
    code_verifier: string;
    code_challenge: string;
}>;
//# sourceMappingURL=pkce.d.ts.map