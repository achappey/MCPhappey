

// --- ENV SAFE HELPERS -------------------------------------------------------

const webCrypto: Crypto | undefined =
  (globalThis as any)?.crypto ?? (window as any)?.crypto;

const assertSecure = () => {
  if (!isSecureContext) {
    console.warn(
      "[PKCE] Not a secure context (https or localhost). Some browsers disable crypto.subtle."
    );
  }
};

// --- TINY JS SHA-256 FALLBACK (public domain-ish compact impl) --------------

/* eslint-disable no-bitwise */
const sha256Fallback = (data: Uint8Array): ArrayBuffer => {
  // Minimal SHA-256 implementation (fast enough for PKCE)
  const K = new Uint32Array([
    0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
    0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
    0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
    0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
    0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
    0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
    0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
    0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
  ]);

  const H = new Uint32Array([
    0x6a09e667, 0xbb67ae85, 0x3c6ef372, 0xa54ff53a,
    0x510e527f, 0x9b05688c, 0x1f83d9ab, 0x5be0cd19
  ]);

  const l = data.length;
  const withOne = l + 1;
  const k = (64 - ((withOne + 8) % 64)) % 64;
  const total = withOne + k + 8;
  const m = new Uint8Array(total);
  m.set(data);
  m[l] = 0x80;
  const bitLen = l * 8;
  const dv = new DataView(m.buffer);
  dv.setUint32(total - 4, bitLen >>> 0);
  dv.setUint32(total - 8, Math.floor(bitLen / 2 ** 32));

  const W = new Uint32Array(64);
  for (let i = 0; i < total; i += 64) {
    for (let t = 0; t < 16; t++) W[t] = dv.getUint32(i + t * 4);
    for (let t = 16; t < 64; t++) {
      const s0 = (W[t - 15] >>> 7 | W[t - 15] << 25) ^ (W[t - 15] >>> 18 | W[t - 15] << 14) ^ (W[t - 15] >>> 3);
      const s1 = (W[t - 2] >>> 17 | W[t - 2] << 15) ^ (W[t - 2] >>> 19 | W[t - 2] << 13) ^ (W[t - 2] >>> 10);
      W[t] = (W[t - 16] + s0 + W[t - 7] + s1) >>> 0;
    }
    let [a, b, c, d, e, f, g, h] = H as unknown as number[];
    for (let t = 0; t < 64; t++) {
      const S1 = (e >>> 6 | e << 26) ^ (e >>> 11 | e << 21) ^ (e >>> 25 | e << 7);
      const ch = (e & f) ^ (~e & g);
      const T1 = (h + S1 + ch + K[t] + W[t]) >>> 0;
      const S0 = (a >>> 2 | a << 30) ^ (a >>> 13 | a << 19) ^ (a >>> 22 | a << 10);
      const maj = (a & b) ^ (a & c) ^ (b & c);
      const T2 = (S0 + maj) >>> 0;
      h = g; g = f; f = e; e = (d + T1) >>> 0; d = c; c = b; b = a; a = (T1 + T2) >>> 0;
    }
    H[0] = (H[0] + a) >>> 0;
    H[1] = (H[1] + b) >>> 0;
    H[2] = (H[2] + c) >>> 0;
    H[3] = (H[3] + d) >>> 0;
    H[4] = (H[4] + e) >>> 0;
    H[5] = (H[5] + f) >>> 0;
    H[6] = (H[6] + g) >>> 0;
    H[7] = (H[7] + h) >>> 0;
  }

  const out = new Uint8Array(32);
  for (let i = 0; i < 8; i++) {
    out[i * 4 + 0] = (H[i] >>> 24) & 0xff;
    out[i * 4 + 1] = (H[i] >>> 16) & 0xff;
    out[i * 4 + 2] = (H[i] >>> 8) & 0xff;
    out[i * 4 + 3] = H[i] & 0xff;
  }
  return out.buffer;
};


// --- PKCE API ----------------------------------------------------------------

/**
 * Cryptographically random verifier (falls back to Math.random if needed).
 */
const generateRandomString = (length: number): string => {
  assertSecure();
  const n = Math.ceil(length / 2);
  const buf = new Uint8Array(n);

  if (webCrypto?.getRandomValues) {
    webCrypto.getRandomValues(buf);
  } else {
    console.warn("[PKCE] crypto.getRandomValues unavailable; falling back to Math.random.");
    for (let i = 0; i < n; i++) buf[i] = (Math.random() * 256) | 0;
  }

  const hex = Array.from(buf, (b) => ("0" + b.toString(16)).slice(-2)).join("");
  return hex.slice(0, length);
};
// Force a plain ArrayBuffer from any TypedArray view (no SharedArrayBuffer union)
const toPlainArrayBuffer = (u8: Uint8Array): ArrayBuffer => {
  const ab = new ArrayBuffer(u8.byteLength);
  new Uint8Array(ab).set(new Uint8Array(u8.buffer, u8.byteOffset, u8.byteLength));
  return ab;
};

const generateCodeChallenge = async (codeVerifier: string): Promise<string> => {
  const encoder = new TextEncoder();
  const data = encoder.encode(codeVerifier); // Uint8Array<ArrayBufferLike>
  const ab = toPlainArrayBuffer(data);       // -> ArrayBuffer

  const digest = await (globalThis.crypto || window.crypto).subtle.digest("SHA-256", ab);

  const binary = String.fromCharCode(...new Uint8Array(digest));
  return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
};


export const createPkceChallenge = async (): Promise<{
  code_verifier: string;
  code_challenge: string;
}> => {
  const code_verifier = generateRandomString(128);
  const code_challenge = await generateCodeChallenge(code_verifier);
  return { code_verifier, code_challenge };
};






/**
 * Generates a cryptographically random string for the PKCE code verifier.
 * @param length The length of the random string.
 * @returns A random string.
 */
const generateRandomString2 = (length: number): string => {
  const array = new Uint32Array(Math.ceil(length / 2));
  crypto.getRandomValues(array);
  return Array.from(array, (dec) => ('0' + dec.toString(16)).slice(-2))
    .join('')
    .slice(0, length);
};

/**
 * Generates a PKCE code challenge from a code verifier.
 * Uses SHA-256 hashing and base64url encoding.
 * @param codeVerifier The code verifier string.
 * @returns A Promise that resolves to the code challenge string.
 */
const generateCodeChallenge2 = async (codeVerifier: string): Promise<string> => {
  const encoder = new TextEncoder();
  const data = encoder.encode(codeVerifier);
  const digest = await crypto.subtle.digest('SHA-256', data);

  // Convert ArrayBuffer to string then to base64 (btoa expects a string of binary characters)
  const binaryString = String.fromCharCode(...new Uint8Array(digest));

  // Base64url encode
  return btoa(binaryString)
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '');
};

/**
 * Generates a PKCE code verifier and its corresponding code challenge.
 * @returns A Promise that resolves to an object containing the code_verifier and code_challenge.
 */
export const createPkceChallenge2 = async (): Promise<{ code_verifier: string; code_challenge: string }> => {
  // OAuth 2.0 spec recommends a verifier length between 43 and 128 characters.
  const codeVerifier = generateRandomString(128);
  const codeChallenge = await generateCodeChallenge(codeVerifier);
  return { code_verifier: codeVerifier, code_challenge: codeChallenge };
};
