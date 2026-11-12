// PreToolUse hook (Bash): megakadályozza, hogy shell-paranccsal kerülje meg a védett fájlok tiltását.
// A Read/Edit jogosultsági szabályok a shell-parancsokra nem vonatkoznak, ezért kell ez a hook.
// Heurisztika: ha a parancs védett útvonalat említ ÉS írásra/törlésre utaló műveletet tartalmaz, blokkol.

import { readFileSync } from "node:fs";

let input;
try {
  input = JSON.parse(readFileSync(0, "utf8"));
} catch {
  process.exit(0);
}

const cmd = String(input?.tool_input?.command ?? "");
if (!cmd) process.exit(0);

// Írásra, áthelyezésre vagy törlésre utaló műveletek (bash és PowerShell).
const writeOps =
  /(>>?|\btee\b|\bsed\s+(-[a-z]*i|--in-place)|\bperl\s+-[a-z]*i|\brm\b|\bmv\b|\bcp\b|\btruncate\b|\bdd\b|Set-Content|Add-Content|Out-File|Remove-Item|Move-Item|Copy-Item|\bgit\s+(rm|mv|checkout|restore)\b)/i;

const guards = [
  {
    path: /docs[\/\\](spec|design)\.md/i,
    msg: "A docs/spec.md és a docs/design.md védett; shell-paranccsal sem módosítható. Javaslatként írd le a változtatást.",
  },
  {
    path: /Migrations[\/\\]/i,
    msg: "A migrációs fájlokat nem módosítjuk kézzel (shell-paranccsal sem). Új migrációt generálj: dotnet ef migrations add <Név> --project Notes.Api",
  },
];

// A dotnet ef saját parancsai kezelik a migrációkat: ezeket a jogosultsági szabályok szabályozzák.
const isEfCommand = /^\s*dotnet\s+ef\b/i.test(cmd);

for (const g of guards) {
  if (g.path.test(cmd) && writeOps.test(cmd) && !isEfCommand) {
    process.stderr.write(`Blokkolva: ${g.msg}\n`);
    process.exit(2);
  }
}
process.exit(0);
