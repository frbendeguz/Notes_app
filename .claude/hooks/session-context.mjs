// SessionStart hook: a munkamenet elején a haladás állapotát Claude kontextusába adja.
// A SessionStart hook stdout-ja Claude kontextusába kerül.

import { readFileSync } from "node:fs";
import { join } from "node:path";
import { execSync } from "node:child_process";

const projectDir = process.env.CLAUDE_PROJECT_DIR || process.cwd();
const lines = [];

try {
  const progress = readFileSync(join(projectDir, "docs", "progress.md"), "utf8");
  const items = progress.split(/\r?\n/).filter((l) => /^\s*- \[( |x|X)\]/.test(l));
  // Számozott agent-feladatok („- [ ] 03 – …”) a számláláshoz; a következő teendő bármelyik nyitott sor lehet.
  const tasks = items.filter((l) => /^\s*- \[( |x|X)\]\s*\d{2}[a-z]?\b/.test(l));
  const done = tasks.filter((l) => /\[(x|X)\]/.test(l)).length;
  const next = items.find((l) => /\[ \]/.test(l));
  lines.push(`Haladás (docs/progress.md): ${done}/${tasks.length} számozott feladat kész.`);
  lines.push(next ? `Következő nyitott teendő: ${next.replace(/^\s*- \[ \]\s*/, "")}` : "Minden teendő kész.");
} catch {
  lines.push("A docs/progress.md nem olvasható.");
}

try {
  const branch = execSync("git rev-parse --abbrev-ref HEAD", { cwd: projectDir, stdio: ["ignore", "pipe", "ignore"] })
    .toString().trim();
  const dirty = execSync("git status --porcelain", { cwd: projectDir, stdio: ["ignore", "pipe", "ignore"] })
    .toString().trim();
  lines.push(`Git ág: ${branch}${dirty ? " (van nem commitolt változás)" : ""}`);
} catch {
  // nincs git vagy nincs repó: nem baj
}

lines.push("Feladatot csak a /task NN (vagy a változtatási igénynél a /change-request) paranccsal kezdj el; addig ne módosíts fájlt.");
process.stdout.write(lines.join("\n") + "\n");
