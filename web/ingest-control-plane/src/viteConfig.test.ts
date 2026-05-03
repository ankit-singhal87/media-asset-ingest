// @vitest-environment node

import { describe, expect, it } from "vitest";

import config from "../vite.config";

describe("Vite development server", () => {
  it("proxies API requests to the local .NET API host", () => {
    expect(config).toMatchObject({
      server: {
        proxy: {
          "/api": {
            target: "http://127.0.0.1:5000",
            changeOrigin: true,
            secure: false
          }
        }
      }
    });
  });
});
