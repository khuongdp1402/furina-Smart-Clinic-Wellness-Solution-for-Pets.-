#!/usr/bin/env node
import { contrastRatio } from "./contrast.mjs";

// [foreground, background, label, minimum ratio required]
// Minimums: 4.5 for normal text, 3.0 for large text (>=18pt/24px) or UI
// components per WCAG AA. Each row here documents which one applies.
const PAIRS = [
  ["#292524", "#FAF9F6", "ink text on surface (body copy)", 4.5],
  ["#FAF9F6", "#0F1B1A", "near-white text on surface-dark (dark mode body)", 4.5],
  ["#FFFFFF", "#0F766E", "white text on primary (buttons)", 4.5],
  ["#292524", "#F97066", "ink text on accent (CTA buttons — NOT white text)", 3.0],
  ["#292524", "#FBBF24", "ink text on highlight (badges)", 4.5],
  ["#FFFFFF", "#DC2626", "white text on danger", 4.5],
  ["#FFFFFF", "#16A34A", "white text on success (large text/UI only)", 3.0],
];

let failed = false;
for (const [fg, bg, label, minimum] of PAIRS) {
  const ratio = contrastRatio(fg, bg);
  const pass = ratio >= minimum;
  if (!pass) failed = true;
  console.log(
    `${pass ? "PASS" : "FAIL"}  ${ratio.toFixed(2)}:1 (need >= ${minimum})  ${label}`
  );
}

if (failed) {
  console.error(
    "\nOne or more pairs fail WCAG AA. This is a sanity check, not a full audit " +
      "(see spec's Out of Scope section) — fix the failing pair's usage before shipping it."
  );
  process.exit(1);
}
console.log("\nAll documented pairs pass WCAG AA.");
