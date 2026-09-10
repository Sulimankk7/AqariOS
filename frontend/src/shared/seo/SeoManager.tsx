import { useEffect } from "react";
import { useLocation } from "react-router";

const HOME_TITLE = "AqariOS | نظام إدارة العقارات في الأردن";
const HOME_DESCRIPTION =
  "AqariOS منصة متكاملة لإدارة العقارات والإيجارات والدفعات والعمليات اليومية، مصممة للسوق الأردني.";
const PRIVATE_TITLE = "AqariOS | منطقة آمنة";
const PRODUCTION_SITE_URL = "https://aqarios.online/";

function siteUrl(): URL {
  return new URL(PRODUCTION_SITE_URL);
}

function setMeta(selector: string, attributes: Record<string, string>) {
  let element = document.head.querySelector<HTMLMetaElement>(selector);
  if (!element) {
    element = document.createElement("meta");
    document.head.append(element);
  }
  Object.entries(attributes).forEach(([name, value]) => element?.setAttribute(name, value));
}

function removeMeta(selector: string) {
  document.head.querySelector(selector)?.remove();
}

function setCanonical(href: string) {
  let canonical = document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]');
  if (!canonical) {
    canonical = document.createElement("link");
    canonical.rel = "canonical";
    document.head.append(canonical);
  }
  canonical.href = href;
}

function setStructuredData(baseUrl: URL) {
  const url = new URL("/", baseUrl).href;
  const logo = new URL("/branding/aqarios-logo.png", baseUrl).href;
  const graph = {
    "@context": "https://schema.org",
    "@graph": [
      { "@type": "Organization", "@id": `${url}#organization`, name: "AqariOS", url, logo },
      {
        "@type": "WebSite",
        "@id": `${url}#website`,
        url,
        name: "AqariOS",
        inLanguage: "ar-JO",
        publisher: { "@id": `${url}#organization` },
      },
      {
        "@type": "WebPage",
        "@id": `${url}#webpage`,
        url,
        name: HOME_TITLE,
        description: HOME_DESCRIPTION,
        inLanguage: "ar-JO",
        isPartOf: { "@id": `${url}#website` },
        about: { "@id": `${url}#organization` },
      },
    ],
  };

  let script = document.head.querySelector<HTMLScriptElement>('script[data-seo="structured-data"]');
  if (!script) {
    script = document.createElement("script");
    script.type = "application/ld+json";
    script.dataset.seo = "structured-data";
    document.head.append(script);
  }
  script.textContent = JSON.stringify(graph);
}

export function SeoManager() {
  const { pathname } = useLocation();

  useEffect(() => {
    const isHome = pathname === "/";
    document.documentElement.lang = "ar";
    document.documentElement.dir = "rtl";
    document.title = isHome ? HOME_TITLE : PRIVATE_TITLE;

    setMeta('meta[name="robots"]', {
      name: "robots",
      content: isHome ? "index, follow, max-image-preview:large" : "noindex, nofollow",
    });

    if (!isHome) {
      document.head.querySelector('link[rel="canonical"]')?.remove();
      ["og:url", "og:image", "og:title", "og:description"].forEach((property) =>
        removeMeta(`meta[property="${property}"]`),
      );
      ["twitter:title", "twitter:description", "twitter:image"].forEach((name) =>
        removeMeta(`meta[name="${name}"]`),
      );
      document.head.querySelector('script[data-seo="structured-data"]')?.remove();
      return;
    }

    const baseUrl = siteUrl();
    const canonical = new URL("/", baseUrl).href;
    const image = new URL("/branding/landing/hero-dashboard-desktop-light.webp", baseUrl).href;
    setCanonical(canonical);
    setMeta('meta[name="description"]', { name: "description", content: HOME_DESCRIPTION });
    setMeta('meta[property="og:title"]', { property: "og:title", content: HOME_TITLE });
    setMeta('meta[property="og:description"]', {
      property: "og:description",
      content: HOME_DESCRIPTION,
    });
    setMeta('meta[property="og:url"]', { property: "og:url", content: canonical });
    setMeta('meta[property="og:image"]', { property: "og:image", content: image });
    setMeta('meta[property="og:image:alt"]', {
      property: "og:image:alt",
      content: "واجهة لوحة تحكم AqariOS لإدارة العقارات",
    });
    setMeta('meta[name="twitter:title"]', { name: "twitter:title", content: HOME_TITLE });
    setMeta('meta[name="twitter:description"]', {
      name: "twitter:description",
      content: HOME_DESCRIPTION,
    });
    setMeta('meta[name="twitter:image"]', { name: "twitter:image", content: image });
    setStructuredData(baseUrl);
  }, [pathname]);

  return null;
}
