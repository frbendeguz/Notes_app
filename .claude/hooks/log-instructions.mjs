// InstructionsLoaded hook: naplózza, melyik CLAUDE.md / .claude/rules fájl mikor és miért töltődött be.
// Nem blokkol és nem módosít semmit; csak egy sort ír a .claude/logs/instructions.log fájlba
// (ez a fájl .gitignore-ban van). Így ellenőrizhető, hogy az útvonalhoz kötött szabályok működnek-e.

import { readFileSync, appendFileSync, mkdirSync } from "node:fs";
import { join, relative } from "node:path";

let input;
try {
  input = JSON.parse(readFileSync(0, "utf8"));
} catch {
  process.exit(0);
}

const projectDir = process.env.CLAUDE_PROJECT_DIR || input.cwd || process.cwd();
const rel = (p) => (p ? relative(projectDir, p).replace(/\\/g, "/") || "." : "-");

const line = [
  new Date().toISOString(),
  input.load_reason ?? "?",
  rel(input.file_path),
  input.trigger_file_path ? `kiváltotta: ${rel(input.trigger_file_path)}` : "",
].filter(Boolean).join("  ");

try {
  const dir = join(projectDir, ".claude", "logs");
  mkdirSync(dir, { recursive: true });
  appendFileSync(join(dir, "instructions.log"), line + "\n", "utf8");
} catch {
  // a naplózás hibája soha ne zavarja a munkát
}
process.exit(0);
