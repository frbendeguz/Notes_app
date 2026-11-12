// PreToolUse hook (Edit|Write|MultiEdit|NotebookEdit): védett fájlok szerkesztésének tiltása.
// Kilépési kód 2 = a művelet blokkolva, a stderr üzenetet Claude megkapja.
// Node-ban írva, hogy Windows-on, macOS-en és Linuxon is ugyanúgy fusson (nincs jq / bash függőség).

import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const norm = (p) => {
  let s = resolve(p).replace(/\\/g, "/").replace(/\/+$/, "");
  return process.platform === "win32" ? s.toLowerCase() : s;
};

let input;
try {
  input = JSON.parse(readFileSync(0, "utf8"));
} catch {
  process.exit(0); // értelmezhetetlen bemenet: nem blokkolunk, a jogosultsági szabályok még védenek
}

const filePath = input?.tool_input?.file_path ?? input?.tool_input?.notebook_path;
if (!filePath) process.exit(0);

const projectDir = norm(process.env.CLAUDE_PROJECT_DIR || input.cwd || process.cwd());
const target = norm(resolve(input.cwd || projectDir, filePath));

if (target !== projectDir && !target.startsWith(projectDir + "/")) process.exit(0);
const rel = target.slice(projectDir.length + 1);
const relLower = rel.toLowerCase();

const rules = [
  {
    test: () => relLower === "docs/spec.md",
    msg: "A docs/spec.md az eredeti specifikáció, nem módosítható. Ha szerinted változtatni kell rajta, írd le javaslatként a beszámolóban.",
  },
  {
    test: () => relLower === "docs/design.md",
    msg: "A docs/design.md a mérvadó terv, csak a fejlesztő módosíthatja. Írd le a javasolt változtatást a beszámolóban.",
  },
  {
    test: () => /(^|\/)migrations\//.test(relLower),
    msg: "A migrációs fájlokat (és a ModelSnapshot-ot) kézzel nem szerkesztjük. Sémaváltozáshoz módosítsd az entitást / AppDbContext-et, és generálj új migrációt: dotnet ef migrations add <Név> --project Notes.Api",
  },
  {
    test: () => /(^|\/)\.env(\.|$)/.test(relLower) || relLower.endsWith("/secrets.json") || relLower === "secrets.json",
    msg: "Titkokat tartalmazó fájlt nem szerkesztünk. A titkot a fejlesztő állítja be (dotnet user-secrets / környezeti változó); írd le neki a parancsot.",
  },
];

for (const rule of rules) {
  if (rule.test()) {
    process.stderr.write(`Blokkolva (${rel}): ${rule.msg}\n`);
    process.exit(2);
  }
}
process.exit(0);
