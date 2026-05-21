interface OE2Config {
  apiBaseUrl: string;
  version: string;
  pathPrefix?: string;
}

declare global {
  interface Window {
    __OE2_CONFIG__?: OE2Config;
  }
}

export {};
