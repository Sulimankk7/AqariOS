import { afterEach, describe, expect, it } from "vitest";
import { cleanup, render, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { SeoManager } from "./SeoManager";

function renderAt(path: string) {
  document.head.innerHTML = `
    <meta name="description" content="initial" />
    <meta name="robots" content="index, follow" />
    <meta property="og:title" content="initial" />
    <meta name="twitter:title" content="initial" />
    <link rel="canonical" href="https://aqarios.online/" />
  `;

  return render(
    <MemoryRouter initialEntries={[path]}>
      <SeoManager />
    </MemoryRouter>,
  );
}

afterEach(() => {
  cleanup();
  document.head.innerHTML = "";
});

describe("SeoManager", () => {
  it("keeps the homepage indexable with the production canonical and valid JSON-LD", async () => {
    renderAt("/?source=test");

    await waitFor(() => {
      expect(document.querySelector('meta[name="robots"]')?.getAttribute("content"))
        .toBe("index, follow, max-image-preview:large");
    });
    expect(document.querySelector('link[rel="canonical"]')?.getAttribute("href"))
      .toBe("https://aqarios.online/");
    expect(document.querySelector('meta[property="og:url"]')?.getAttribute("content"))
      .toBe("https://aqarios.online/");

    const structuredData = document.querySelector('script[data-seo="structured-data"]')?.textContent;
    expect(() => JSON.parse(structuredData ?? "")).not.toThrow();
  });

  it("sets private routes to noindex and removes public canonical and sharing metadata", async () => {
    renderAt("/auth/login");

    await waitFor(() => {
      expect(document.querySelector('meta[name="robots"]')?.getAttribute("content"))
        .toBe("noindex, nofollow");
    });
    expect(document.querySelector('link[rel="canonical"]')).toBeNull();
    expect(document.querySelector('meta[property="og:title"]')).toBeNull();
    expect(document.querySelector('meta[name="twitter:title"]')).toBeNull();
    expect(document.querySelector('script[data-seo="structured-data"]')).toBeNull();
  });
});
