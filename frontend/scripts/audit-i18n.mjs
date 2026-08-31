import { readFile, readdir } from "node:fs/promises";
import { extname, join, relative } from "node:path";
import { fileURLToPath } from "node:url";

const frontendRoot = fileURLToPath(new URL("..", import.meta.url));
const sourceRoot = join(frontendRoot, "src");
const pluralSuffix = /_(zero|one|two|few|many|other)$/;

async function loadDictionary(language) {
  const path = join(sourceRoot, "shared", "i18n", "translations", `${language}.ts`);
  const source = await readFile(path, "utf8");
  const marker = `export const ${language} =`;
  const start = source.indexOf(marker);
  if (start < 0) throw new Error(`Could not find ${marker} in ${path}`);
  const expression = source.slice(start + marker.length).replace(/\s+as const;\s*$/, "").trim();
  return Function(`"use strict"; return (${expression});`)();
}

async function loadLegacyDictionary(relativePath, exportName) {
  const path = join(sourceRoot, ...relativePath.split("/"));
  const source = await readFile(path, "utf8");
  const marker = `export const ${exportName} =`;
  const start = source.indexOf(marker);
  if (start < 0) throw new Error(`Could not find ${marker} in ${path}`);
  const tail = source.slice(start + marker.length);
  const end = tail.search(/\r?\n}\s*(?:as const)?;/);
  if (end < 0) throw new Error(`Could not find the end of ${exportName} in ${path}`);
  const expression = `${tail.slice(0, end)}\n}`.trim();
  return Function(`"use strict"; return (${expression});`)();
}

function flatten(value, prefix = "", output = new Map()) {
  for (const [key, child] of Object.entries(value)) {
    const path = prefix ? `${prefix}.${key}` : key;
    if (child && typeof child === "object" && !Array.isArray(child)) flatten(child, path, output);
    else output.set(path, child);
  }
  return output;
}

async function sourceFiles(directory) {
  const output = [];
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) output.push(...await sourceFiles(path));
    else if ([".ts", ".tsx"].includes(extname(entry.name))) output.push(path);
  }
  return output;
}

const [en, ar, files] = await Promise.all([
  loadDictionary("en").then(flatten),
  loadDictionary("ar").then(flatten),
  sourceFiles(sourceRoot),
]);

const legacySources = [
  ["features/auth/constants/translations.ts", "TRANSLATIONS"],
  ["features/apartments/constants/translations.ts", "dictionary"],
  ["features/buildings/constants/translations.ts", "dictionary"],
  ["features/floors/constants/translations.ts", "dictionary"],
  ["features/leasing/constants/translations.ts", "leasingTranslations"],
  ["features/tenants/constants/translations.ts", "tenantTranslations"],
];

const failures = [];
const normalized = (keys) => new Set([...keys].map((key) => key.replace(pluralSuffix, "")));
const enKeys = normalized(en.keys());
const arKeys = normalized(ar.keys());

for (const key of [...enKeys].filter((key) => !arKeys.has(key)).sort()) failures.push(`Arabic dictionary is missing: ${key}`);
for (const key of [...arKeys].filter((key) => !enKeys.has(key)).sort()) failures.push(`English dictionary is missing: ${key}`);

for (const [path, exportName] of legacySources) {
  const dictionary = await loadLegacyDictionary(path, exportName);
  const legacyEn = normalized(flatten(dictionary.en).keys());
  const legacyAr = normalized(flatten(dictionary.ar).keys());
  for (const key of [...legacyEn].filter((key) => !legacyAr.has(key)).sort()) failures.push(`${path}: Arabic dictionary is missing: ${key}`);
  for (const key of [...legacyAr].filter((key) => !legacyEn.has(key)).sort()) failures.push(`${path}: English dictionary is missing: ${key}`);
}

for (const file of files) {
  const source = await readFile(file, "utf8");
  const displayPath = relative(frontendRoot, file).replaceAll("\\", "/");
  const lineAt = (index) => source.slice(0, index).split("\n").length;

  for (const match of source.matchAll(/\bt\(\s*(["'`])([a-zA-Z][\w-]*(?:\.[\w-]+)+)\1/g)) {
    const key = match[2].replace(pluralSuffix, "");
    if (!enKeys.has(key) || !arKeys.has(key)) failures.push(`${displayPath}:${lineAt(match.index)} unresolved translation: ${match[2]}`);
  }

  for (const match of source.matchAll(/\btoast\.(?:success|error|warning|info)\(\s*(["'`])/g)) {
    failures.push(`${displayPath}:${lineAt(match.index)} raw toast literal`);
  }

  for (const match of source.matchAll(/\bt\(\s*(["'])[A-Za-z][\w.-]*\1\s*,\s*(["'])/g)) {
    failures.push(`${displayPath}:${lineAt(match.index)} embedded string translation fallback`);
  }

  for (const match of source.matchAll(/(?:error|err)\?*\.(?:detail|message)\s*\|\|\s*(?:error|err)\?*\.(?:detail|message)/g)) {
    failures.push(`${displayPath}:${lineAt(match.index)} direct backend error fallback`);
  }
}

if (failures.length) {
  console.error(`i18n audit failed with ${failures.length} issue(s):`);
  for (const failure of failures) console.error(`- ${failure}`);
  process.exitCode = 1;
} else {
  console.log(`i18n audit passed: ${en.size} English entries, ${ar.size} Arabic entries, ${legacySources.length} legacy dictionaries, ${files.length} source files checked.`);
}
