# Design System + Landing Page Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship shared design tokens (usable by this and future frontends) plus a working Next.js landing page and token-preview page for US-38 (ref 38).

**Architecture:** `web/design-system/` holds framework-agnostic CSS custom properties plus a Tailwind v4 `@theme` block that turns them into utility classes; `web/landing/` is a Next.js 15 App Router app that imports that CSS and renders the 8-section landing page (AC-2) and a `/design-preview` page proving the tokens resolve correctly in real components (AC-1).

**Tech Stack:** Next.js 15 (App Router), Tailwind CSS v4, shadcn/ui (Radix primitives), TypeScript, Node's built-in test runner (`node --test`) for the pure-function contrast checker, Playwright (via the `webapp-testing` skill) for page-structure checks.

**Spec:** `docs/superpowers/specs/2026-09-11-design-system-landing-design.md`

## Global Constraints

- Color tokens (exact hex, from the spec/wiki):
  `--color-primary: #0F766E`, `--color-accent: #F97066`, `--color-highlight: #FBBF24`,
  `--color-surface: #FAF9F6`, `--color-ink: #292524`, `--color-surface-dark: #0F1B1A`,
  `--color-danger: #DC2626`, `--color-success: #16A34A`.
- Typography: heading font = Fraunces (serif), UI font = Inter (sans-serif), both via `next/font/google`. Font-size scale is Tailwind's default (`text-sm`/`base`/`lg`/...) — never a custom scale.
- Default radius for cards/buttons: `rounded-xl`. Shadows: low elevation only (`shadow-sm`/`shadow-md`, never `shadow-2xl` or Material-style heavy shadows).
- Tech stack is fixed: Next.js 15 App Router, Tailwind v4, shadcn/ui. No other UI library.
- No monorepo build tooling (no Turborepo, no pnpm workspaces) — `web/landing` imports `web/design-system` files by relative path.
- Landing page has exactly 8 sections, in this order: Hero, ProblemSolution, Features, ClinicMap, FeedPreview, ForClinics, Testimonials, CtaFooter.
- `ClinicMap` and `FeedPreview` render static placeholder content only — no real map/feed integration (no backend exists for either yet).
- Vietnamese copy from the wiki is used verbatim where the wiki gives exact text; no translation, no rewriting.
- **Deviation from spec's file list, decided during implementation:** the spec named a `tailwind-preset.ts` file, but Tailwind v4's native mechanism for turning CSS variables into utility classes is an `@theme` block in CSS, not a JS preset — Tailwind v4 dropped the JS-config-first approach. Task 1 below implements this as `web/design-system/theme.css` (an `@theme inline` block) instead of a `.ts` file. This still satisfies the spec's actual requirement ("Tailwind config + CSS variables ... shared across frontends") — a future app adopts it the same way, via one `@import`, not a JS import.
- Contrast reality check computed during planning (WCAG relative-luminance formula, embedded as literal expected values in Task 1's test — do not recompute differently):
  - `#000000`/`#FFFFFF` = 21.00 (sanity pair)
  - `#292524` (ink) on `#FAF9F6` (surface) = 14.41
  - `#FFFFFF` on `#0F766E` (primary) = 5.47 — passes AA normal text (≥4.5)
  - `#FFFFFF` on `#F97066` (accent) = 2.79 — **fails** AA even for large text (needs ≥3.0 for large/UI, ≥4.5 normal) → accent backgrounds must use ink-colored text, never white
  - `#292524` (ink) on `#FBBF24` (highlight) = 9.09 — passes
  - `#FFFFFF` on `#DC2626` (danger) = 4.83 — passes AA normal text
  - `#FFFFFF` on `#16A34A` (success) = 3.30 — passes only for large text/UI components (≥3.0), fails normal text (needs ≥4.5)
  - `#F5F5F4` (near-white) on `#0F1B1A` (surface-dark) = 16.15

---

### Task 1: Design tokens + contrast checker

**Files:**
- Create: `web/design-system/tokens.css`
- Create: `web/design-system/theme.css`
- Create: `web/design-system/contrast.mjs`
- Create: `web/design-system/contrast.test.mjs`
- Create: `web/design-system/contrast-check.mjs`
- Create: `web/design-system/README.md`
- Create: `web/package.json` (root of the `web/` workspace, holds the Node version needed to run `node --test` and the CLI script — no framework deps here, those live in `web/landing/package.json` from Task 2)

**Interfaces:**
- Produces: `contrastRatio(hexA: string, hexB: string): number` exported from `web/design-system/contrast.mjs` — Task 1's own CLI (`contrast-check.mjs`) is the only consumer for now; no later task depends on this function directly, but the CSS variable names below are consumed by every later task.
- Produces: CSS custom properties `--color-primary`, `--color-accent`, `--color-highlight`, `--color-surface`, `--color-ink`, `--color-surface-dark`, `--color-danger`, `--color-success`, `--font-heading`, `--font-ui` (defined in `tokens.css`, values set by `next/font` CSS variables in Task 2 for the two font ones).
- Produces: Tailwind utility classes `bg-primary`, `text-primary`, `bg-accent`, `text-accent`, `bg-highlight`, `text-highlight`, `bg-surface`, `text-surface`, `bg-ink`, `text-ink`, `bg-surface-dark`, `bg-danger`, `text-danger`, `bg-success`, `text-success`, `font-heading`, `font-ui`, plus shadcn-convention aliases `bg-background`, `text-foreground`, `bg-primary`/`text-primary-foreground`, `--radius` (all defined in `theme.css`, consumed by every component in Tasks 3–8).

- [ ] **Step 1: Write the failing test for `contrastRatio`**

Create `web/design-system/contrast.test.mjs`:

```javascript
import { test } from "node:test";
import assert from "node:assert/strict";
import { contrastRatio } from "./contrast.mjs";

test("black on white is the maximum ratio, 21:1", () => {
  assert.equal(contrastRatio("#000000", "#FFFFFF").toFixed(2), "21.00");
});

test("ink text on the default surface passes AA comfortably", () => {
  assert.equal(contrastRatio("#292524", "#FAF9F6").toFixed(2), "14.41");
});

test("white text on primary passes AA for normal text (>= 4.5)", () => {
  const ratio = contrastRatio("#FFFFFF", "#0F766E");
  assert.ok(ratio >= 4.5, `expected >= 4.5, got ${ratio}`);
});

test("white text on accent fails AA even for large text (< 3.0)", () => {
  const ratio = contrastRatio("#FFFFFF", "#F97066");
  assert.ok(ratio < 3.0, `expected < 3.0, got ${ratio}`);
});

test("is symmetric regardless of argument order", () => {
  const a = contrastRatio("#292524", "#FAF9F6");
  const b = contrastRatio("#FAF9F6", "#292524");
  assert.equal(a, b);
});
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `node --test web/design-system/contrast.test.mjs`
Expected: FAIL — `contrast.mjs` does not exist yet (`Cannot find module`).

- [ ] **Step 3: Implement `contrast.mjs`**

Create `web/design-system/contrast.mjs`:

```javascript
/**
 * WCAG 2.x contrast ratio between two sRGB hex colors, per
 * https://www.w3.org/TR/WCAG21/#dfn-contrast-ratio
 */
export function hexToRgb(hex) {
  const clean = hex.replace("#", "");
  return [0, 2, 4].map((i) => parseInt(clean.slice(i, i + 2), 16) / 255);
}

function linearize(channel) {
  return channel <= 0.03928
    ? channel / 12.92
    : Math.pow((channel + 0.055) / 1.055, 2.4);
}

export function relativeLuminance([r, g, b]) {
  const [rl, gl, bl] = [r, g, b].map(linearize);
  return 0.2126 * rl + 0.7152 * gl + 0.0722 * bl;
}

export function contrastRatio(hexA, hexB) {
  const lA = relativeLuminance(hexToRgb(hexA));
  const lB = relativeLuminance(hexToRgb(hexB));
  const [lighter, darker] = lA > lB ? [lA, lB] : [lB, lA];
  return (lighter + 0.05) / (darker + 0.05);
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `node --test web/design-system/contrast.test.mjs`
Expected: PASS — 5 tests, 0 failures.

- [ ] **Step 5: Write the CLI contrast report**

Create `web/design-system/contrast-check.mjs`:

```javascript
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
```

- [ ] **Step 6: Run the CLI and confirm the known accent failure is reported**

Run: `node web/design-system/contrast-check.mjs`
Expected: exits with code 1 (because the report deliberately still includes what white-on-accent *would* be — actually the PAIRS list above already uses ink-on-accent, which passes at 9.09:1). Re-run and confirm output shows 7 `PASS` lines and exit code 0. If instead you see a `FAIL` line, re-check the hex values against Global Constraints before continuing — do not "fix" the checker's threshold to make a real failure disappear.

- [ ] **Step 7: Write the token CSS**

Create `web/design-system/tokens.css`:

```css
/*
 * Design tokens for Furina — source of truth is the Taiga wiki page
 * "thiet-ke-uiux". Every frontend (this landing page, and the Tenant/Super
 * Admin portals in Sprint 5/6) imports this file rather than redefining
 * colors. Change a value here, every consumer updates.
 */
:root {
  --color-primary: #0f766e;
  --color-accent: #f97066;
  --color-highlight: #fbbf24;
  --color-surface: #faf9f6;
  --color-ink: #292524;
  --color-surface-dark: #0f1b1a;
  --color-danger: #dc2626;
  --color-success: #16a34a;

  /* Set to real values by next/font in web/landing/app/layout.tsx (Task 2).
     Fall back to system fonts so this file alone is still usable. */
  --font-heading: ui-serif, Georgia, serif;
  --font-ui: ui-sans-serif, system-ui, sans-serif;

  --radius: 0.75rem; /* rounded-xl */
}
```

- [ ] **Step 8: Write the Tailwind v4 theme mapping**

Create `web/design-system/theme.css`:

```css
/*
 * Turns tokens.css's CSS variables into Tailwind v4 utility classes
 * (bg-primary, text-ink, font-heading, etc.) via Tailwind v4's native
 * `@theme` mechanism — the CSS-first replacement for a JS tailwind preset.
 * Import this AFTER tokens.css.
 */
@theme inline {
  --color-primary: var(--color-primary);
  --color-accent: var(--color-accent);
  --color-highlight: var(--color-highlight);
  --color-surface: var(--color-surface);
  --color-ink: var(--color-ink);
  --color-surface-dark: var(--color-surface-dark);
  --color-danger: var(--color-danger);
  --color-success: var(--color-success);

  --font-heading: var(--font-heading);
  --font-ui: var(--font-ui);

  --radius: var(--radius);

  /* shadcn/ui component convention aliases, so `npx shadcn add` components
     work unmodified — they read --background/--foreground/--primary/etc. */
  --color-background: var(--color-surface);
  --color-foreground: var(--color-ink);
  --color-primary-foreground: #ffffff; /* passes AA on primary, see contrast-check.mjs */
  --color-accent-foreground: var(--color-ink); /* white-on-accent fails AA, see Global Constraints */
  --color-destructive: var(--color-danger);
  --color-destructive-foreground: #ffffff;
}
```

- [ ] **Step 9: Write the package README**

Create `web/design-system/README.md`:

```markdown
# Furina design system tokens

Framework-agnostic CSS custom properties + a Tailwind v4 `@theme` mapping.
Source of truth for values: Taiga wiki page `thiet-ke-uiux`.

## Using this from a new frontend (e.g. the Sprint 5 Tenant Admin Portal)

In your app's global CSS, before your own styles:

\`\`\`css
@import "../../design-system/tokens.css";
@import "../../design-system/theme.css";
@import "tailwindcss";
\`\`\`

That's it — `bg-primary`, `text-ink`, `font-heading`, etc. are now real
Tailwind utility classes. To retint a portal (e.g. Super Admin's more
serious, less-coral tone per the wiki), override specific `--color-*`
variables in that app's own CSS after the imports above — never edit
`tokens.css` for a single app's tone, that file is shared.

## Verifying contrast

\`\`\`bash
node web/design-system/contrast-check.mjs
\`\`\`

Exits non-zero if any documented foreground/background pair drops below
WCAG AA. This is a sanity check on the *documented* pairs list in that
script, not an automatic scan of every color combination used anywhere in
the app — a real accessibility audit is still a separate, manual task (see
the design spec's Out of Scope section).
```

- [ ] **Step 10: Add the `web/` root package.json**

Create `web/package.json`:

```json
{
  "name": "furina-web",
  "private": true,
  "version": "0.0.0",
  "scripts": {
    "test:tokens": "node --test design-system/contrast.test.mjs",
    "check:contrast": "node design-system/contrast-check.mjs"
  }
}
```

- [ ] **Step 11: Commit**

```bash
git add web/design-system web/package.json
git commit -m "feat(design-system): color/typography tokens + WCAG contrast checker

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 2: Scaffold the Next.js landing app and wire the tokens

**Files:**
- Create: `web/landing/` (via `create-next-app`, then modified below)
- Modify: `web/landing/app/globals.css`
- Modify: `web/landing/app/layout.tsx`
- Create: `web/landing/next.config.ts` (only if the scaffold tool doesn't already produce a usable one — verify in Step 1)

**Interfaces:**
- Consumes: `web/design-system/tokens.css`, `web/design-system/theme.css` (Task 1).
- Produces: a running Next.js dev server at `http://localhost:3000` with the Fraunces/Inter font CSS variables set on `<html>`, and Tailwind v4 utility classes from Task 1 available in every component under `web/landing/app/`. Later tasks (3–9) all live under `web/landing/`.

- [ ] **Step 1: Scaffold the app**

Run (from `web/`):

```bash
npx create-next-app@latest landing --typescript --tailwind --app --no-src-dir --import-alias "@/*" --eslint --turbopack
```

When prompted, accept defaults. This produces `web/landing/` with Next.js 15, Tailwind v4 already wired to a default `app/globals.css` via `@import "tailwindcss";`, and an App Router `app/` directory.

- [ ] **Step 2: Verify the scaffold runs before changing anything**

Run: `cd web/landing && npm run dev`
Expected: server starts on port 3000, default Next.js starter page loads with no errors in the terminal. Stop the server (Ctrl+C) once confirmed.

- [ ] **Step 3: Import the shared tokens into `globals.css`**

Modify `web/landing/app/globals.css` — replace its entire contents with:

```css
@import "../../design-system/tokens.css";
@import "../../design-system/theme.css";
@import "tailwindcss";

body {
  background-color: var(--color-surface);
  color: var(--color-ink);
}
```

- [ ] **Step 4: Wire real fonts and override the token fallbacks**

Modify `web/landing/app/layout.tsx` to the following:

```tsx
import type { Metadata } from "next";
import { Fraunces, Inter } from "next/font/google";
import "./globals.css";

const fraunces = Fraunces({
  subsets: ["latin"],
  variable: "--font-heading",
  display: "swap",
});

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-ui",
  display: "swap",
});

export const metadata: Metadata = {
  title: "Furina — Chăm sóc thú cưng, trọn vẹn yêu thương",
  description:
    "Furina là hệ sinh thái số hỗ trợ chuyển đổi số cho các trung tâm y tế và trị liệu thú cưng.",
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="vi" className={`${fraunces.variable} ${inter.variable}`}>
      <body className="font-ui">{children}</body>
    </html>
  );
}
```

`next/font`'s `variable` option sets `--font-heading`/`--font-ui` on the `<html>` element, overriding `tokens.css`'s system-font fallback with the real Fraunces/Inter font stacks — no separate override file needed.

- [ ] **Step 5: Verify tokens resolve in the browser**

Run: `cd web/landing && npm run dev`, open `http://localhost:3000`.
Expected: page background is the warm cream `#FAF9F6` (not pure white), body text uses Inter. Open browser devtools, inspect `<html>`, confirm computed style shows `--font-heading` resolving to a Fraunces font stack. Stop the server.

- [ ] **Step 6: Commit**

```bash
git add web/landing
git commit -m "feat(landing): scaffold Next.js 15 app, wire shared design tokens

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 3: Add shadcn/ui components

**Files:**
- Create: `web/landing/components.json` (generated by shadcn CLI)
- Create: `web/landing/components/ui/button.tsx`
- Create: `web/landing/components/ui/card.tsx`
- Create: `web/landing/components/ui/badge.tsx`
- Create: `web/landing/components/ui/input.tsx`
- Create: `web/landing/components/ui/navigation-menu.tsx`
- Create: `web/landing/lib/utils.ts` (generated by shadcn CLI — the `cn()` classname helper)

**Interfaces:**
- Consumes: `theme.css` shadcn aliases from Task 1 (`--color-background`, `--color-foreground`, `--color-primary-foreground`, etc.).
- Produces: `Button`, `Card`/`CardHeader`/`CardTitle`/`CardContent`, `Badge`, `Input`, `NavigationMenu` (and its sub-components) importable from `@/components/ui/*`, consumed by Tasks 4–8.

- [ ] **Step 1: Run the shadcn init**

Run (from `web/landing/`):

```bash
npx shadcn@latest init -d
```

`-d` accepts the defaults (New York style, Zinc base color, CSS variables). This creates `components.json` and `lib/utils.ts`.

- [ ] **Step 2: Add only the primitives this feature uses**

Run:

```bash
npx shadcn@latest add button card badge input navigation-menu
```

- [ ] **Step 3: Verify the components build**

Run: `cd web/landing && npx tsc --noEmit`
Expected: no type errors.

- [ ] **Step 4: Commit**

```bash
git add web/landing/components.json web/landing/components/ui web/landing/lib/utils.ts
git commit -m "feat(landing): add shadcn/ui primitives (button, card, badge, input, nav)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 4: Design token preview page (AC-1)

**Files:**
- Create: `web/landing/app/design-preview/page.tsx`

**Interfaces:**
- Consumes: `Button`, `Card`, `Badge`, `Input` from Task 3; CSS utility classes from Task 1.
- Produces: page at route `/design-preview`, used by Task 9's Playwright check as evidence tokens resolve in real components (US-38 AC-1).

- [ ] **Step 1: Write the page**

Create `web/landing/app/design-preview/page.tsx`:

```tsx
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";

export default function DesignPreviewPage() {
  return (
    <main className="mx-auto max-w-3xl space-y-8 p-8">
      <h1 className="font-heading text-3xl text-ink">Design token preview</h1>
      <p className="text-ink">
        Trang nội bộ để xác minh design token (US-38 AC-1) — không phải
        trang login thật (Tenant Admin Portal chưa tồn tại tới Sprint 5).
      </p>

      <section className="space-y-3">
        <h2 className="font-heading text-xl text-ink">Buttons</h2>
        <div className="flex flex-wrap gap-3">
          <Button className="bg-primary text-primary-foreground">
            Primary
          </Button>
          <Button className="bg-accent text-accent-foreground">Accent CTA</Button>
          <Button variant="outline">Outline</Button>
          <Button className="bg-danger text-white">Danger</Button>
          <Button className="bg-success text-white">Success</Button>
        </div>
      </section>

      <section className="space-y-3">
        <h2 className="font-heading text-xl text-ink">Badges</h2>
        <div className="flex flex-wrap gap-3">
          <Badge className="bg-highlight text-ink">Highlight</Badge>
          <Badge className="bg-primary text-primary-foreground">Primary</Badge>
        </div>
      </section>

      <Card className="rounded-xl shadow-sm">
        <CardHeader>
          <CardTitle className="font-heading text-ink">Sample card</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <p className="text-ink">
            Card dùng bo góc rounded-xl và shadow nhẹ theo token.
          </p>
          <Input placeholder="Nhập email..." />
        </CardContent>
      </Card>

      <section className="rounded-xl bg-surface-dark p-6">
        <h2 className="font-heading text-xl text-white">Dark surface</h2>
        <p className="mt-2 text-[#F5F5F4]">
          Nền tối dùng cho 2 cổng admin (ca đêm) — kiểm tra contrast ở
          contrast-check.mjs.
        </p>
      </section>
    </main>
  );
}
```

- [ ] **Step 2: Verify in the browser**

Run: `cd web/landing && npm run dev`, open `http://localhost:3000/design-preview`.
Expected: no console errors; buttons show teal/coral/red/green backgrounds; accent button text is dark ink (not white — confirms the AA-failing pair from Task 1 was actually avoided in real UI, not just documented). Stop the server.

- [ ] **Step 3: Commit**

```bash
git add web/landing/app/design-preview
git commit -m "feat(landing): design token preview page (US-38 AC-1)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 5: Landing page shell, nav, and Hero section (section 1/8)

**Files:**
- Create: `web/landing/components/site-nav.tsx`
- Create: `web/landing/app/sections/hero.tsx`
- Modify: `web/landing/app/page.tsx`

**Interfaces:**
- Consumes: `Button`, `NavigationMenu*` from Task 3.
- Produces: `<SiteNav />` component (consumed by `page.tsx` only, not by other sections); `<Hero />` component, the first child rendered by `page.tsx`. Later section tasks each add one more `<X />` import + JSX line to `page.tsx` — this task establishes that pattern.

- [ ] **Step 1: Write the nav**

Create `web/landing/components/site-nav.tsx`:

```tsx
"use client";

import Link from "next/link";
import { Button } from "@/components/ui/button";

export function SiteNav() {
  return (
    <header className="sticky top-0 z-10 border-b border-black/5 bg-surface/90 backdrop-blur">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
        <Link href="/" className="font-heading text-xl text-ink">
          Furina
        </Link>
        <nav className="hidden gap-6 text-sm text-ink md:flex">
          <a href="#tinh-nang">Tính năng</a>
          <a href="#phong-kham">Dành cho phòng khám</a>
          <a href="#lien-he">Liên hệ</a>
        </nav>
        <Button className="bg-primary text-primary-foreground">
          Đặt lịch ngay
        </Button>
      </div>
    </header>
  );
}
```

- [ ] **Step 2: Write the Hero section**

Create `web/landing/app/sections/hero.tsx`:

```tsx
import { Button } from "@/components/ui/button";

export function Hero() {
  return (
    <section
      aria-label="Hero"
      className="mx-auto flex max-w-6xl flex-col items-center gap-8 px-6 py-20 text-center"
    >
      <h1 className="font-heading text-4xl text-ink md:text-6xl">
        Furina — Chăm sóc thú cưng, trọn vẹn yêu thương
      </h1>
      <p className="max-w-2xl text-lg text-ink/80">
        Hệ sinh thái số cho phòng khám thú y: tiếp nhận, y bạ điện tử, lịch
        hẹn và cộng đồng người yêu thú cưng — tất cả trong một nền tảng.
      </p>
      <div className="flex flex-wrap justify-center gap-4">
        <Button className="bg-accent text-accent-foreground" size="lg">
          Đặt lịch ngay
        </Button>
        <Button variant="outline" size="lg">
          Dành cho phòng khám: Bắt đầu miễn phí
        </Button>
      </div>
    </section>
  );
}
```

- [ ] **Step 3: Wire the page shell**

Modify `web/landing/app/page.tsx` — replace its entire contents with:

```tsx
import { SiteNav } from "@/components/site-nav";
import { Hero } from "./sections/hero";

export default function LandingPage() {
  return (
    <>
      <SiteNav />
      <Hero />
    </>
  );
}
```

- [ ] **Step 4: Verify in the browser**

Run: `cd web/landing && npm run dev`, open `http://localhost:3000`.
Expected: sticky nav with "Furina" wordmark, hero headline in Fraunces, two CTA buttons (coral accent + outline), no console errors. Stop the server.

- [ ] **Step 5: Commit**

```bash
git add web/landing/components/site-nav.tsx web/landing/app/sections/hero.tsx web/landing/app/page.tsx
git commit -m "feat(landing): nav + Hero section (1/8)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 6: ProblemSolution and Features sections (2/8, 3/8)

**Files:**
- Create: `web/landing/app/sections/problem-solution.tsx`
- Create: `web/landing/app/sections/features.tsx`
- Modify: `web/landing/app/page.tsx`

**Interfaces:**
- Consumes: `Card`/`CardHeader`/`CardTitle`/`CardContent` from Task 3.
- Produces: `<ProblemSolution />`, `<Features id="tinh-nang" />` — the nav's `#tinh-nang` anchor (Task 5) targets this section's `id`.

- [ ] **Step 1: Write ProblemSolution**

Create `web/landing/app/sections/problem-solution.tsx`:

```tsx
const PAIRS = [
  {
    problem: "Chủ nuôi khó tìm phòng khám uy tín gần nhà, phải hỏi khắp nơi.",
    solution: "Furina có bản đồ tìm phòng khám gần nhất, đánh giá minh bạch.",
  },
  {
    problem: "Hồ sơ khám bệnh của thú cưng rải rác, mất khi đổi phòng khám.",
    solution: "Y bạ điện tử (SOAP note) lưu trọn vẹn, theo thú cưng suốt đời.",
  },
  {
    problem: "Phòng khám truyền thống đặt lịch qua điện thoại, dễ trùng giờ.",
    solution: "Đặt lịch đa kênh, chống trùng lịch tự động, nhắc lịch chủ động.",
  },
  {
    problem: "Không có cộng đồng để chủ nuôi chia sẻ kinh nghiệm chăm sóc.",
    solution: "Furina Feed — cộng đồng người yêu thú cưng ngay trong app.",
  },
];

export function ProblemSolution() {
  return (
    <section
      aria-label="Vấn đề và giải pháp"
      className="mx-auto max-w-6xl px-6 py-16"
    >
      <h2 className="font-heading text-3xl text-ink">
        Vấn đề bạn gặp, Furina đã giải quyết
      </h2>
      <div className="mt-10 grid gap-6 md:grid-cols-2">
        {PAIRS.map((pair) => (
          <div
            key={pair.problem}
            className="rounded-xl border border-black/5 bg-white/50 p-6 shadow-sm"
          >
            <p className="text-sm font-medium text-danger">{pair.problem}</p>
            <p className="mt-2 text-ink">{pair.solution}</p>
          </div>
        ))}
      </div>
    </section>
  );
}
```

- [ ] **Step 2: Write Features**

Create `web/landing/app/sections/features.tsx`:

```tsx
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

const FEATURES = [
  {
    title: "EMR số hoá (SOAP note)",
    description: "Y bạ điện tử chuẩn SOAP, tra cứu lịch sử khám tức thì.",
  },
  {
    title: "Đặt lịch đa kênh",
    description: "Web, app, chatbot — chống trùng lịch, nhắc lịch tự động.",
  },
  {
    title: "Furina Feed cộng đồng",
    description: "Chia sẻ kinh nghiệm chăm sóc thú cưng cùng cộng đồng.",
  },
  {
    title: "Chatbot 24/7",
    description: "Trả lời câu hỏi chăm sóc cơ bản mọi lúc, không cần chờ.",
  },
];

export function Features() {
  return (
    <section id="tinh-nang" aria-label="Tính năng nổi bật" className="bg-white/40 py-16">
      <div className="mx-auto max-w-6xl px-6">
        <h2 className="font-heading text-3xl text-ink">Tính năng nổi bật</h2>
        <div className="mt-10 grid gap-6 md:grid-cols-2 lg:grid-cols-4">
          {FEATURES.map((feature) => (
            <Card key={feature.title} className="rounded-xl shadow-sm">
              <CardHeader>
                <CardTitle className="font-heading text-lg text-ink">
                  {feature.title}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-ink/80">{feature.description}</p>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    </section>
  );
}
```

- [ ] **Step 3: Wire into the page**

Modify `web/landing/app/page.tsx`:

```tsx
import { SiteNav } from "@/components/site-nav";
import { Hero } from "./sections/hero";
import { ProblemSolution } from "./sections/problem-solution";
import { Features } from "./sections/features";

export default function LandingPage() {
  return (
    <>
      <SiteNav />
      <Hero />
      <ProblemSolution />
      <Features />
    </>
  );
}
```

- [ ] **Step 4: Verify in the browser**

Run: `cd web/landing && npm run dev`, open `http://localhost:3000`, scroll down, click "Tính năng" in the nav.
Expected: 4 problem/solution cards render, nav anchor scrolls to the Features section's 4 feature cards, no console errors. Stop the server.

- [ ] **Step 5: Commit**

```bash
git add web/landing/app/sections/problem-solution.tsx web/landing/app/sections/features.tsx web/landing/app/page.tsx
git commit -m "feat(landing): ProblemSolution + Features sections (2/8, 3/8)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 7: ClinicMap and FeedPreview placeholder sections (4/8, 5/8)

**Files:**
- Create: `web/landing/app/sections/clinic-map.tsx`
- Create: `web/landing/app/sections/feed-preview.tsx`
- Modify: `web/landing/app/page.tsx`

**Interfaces:**
- Consumes: `Card`/`CardHeader`/`CardTitle`/`CardContent`, `Badge` from Task 3.
- Produces: `<ClinicMap />`, `<FeedPreview />` — both static placeholders per the spec (no PostGIS/Feed backend exists yet).

- [ ] **Step 1: Write ClinicMap**

Create `web/landing/app/sections/clinic-map.tsx`:

```tsx
export function ClinicMap() {
  return (
    <section aria-label="Bản đồ tìm phòng khám gần nhất" className="mx-auto max-w-6xl px-6 py-16">
      <h2 className="font-heading text-3xl text-ink">
        Tìm phòng khám gần nhất
      </h2>
      <p className="mt-2 text-ink/80">
        Bản đồ tương tác — sắp ra mắt khi dịch vụ định vị phòng khám hoàn tất.
      </p>
      <div
        role="img"
        aria-label="Xem trước bản đồ phòng khám (chưa có dữ liệu thật)"
        className="mt-8 flex h-72 items-center justify-center rounded-xl border border-dashed border-ink/20 bg-white/50 text-ink/50"
      >
        Xem trước bản đồ (PostGIS) — đang phát triển
      </div>
    </section>
  );
}
```

- [ ] **Step 2: Write FeedPreview**

Create `web/landing/app/sections/feed-preview.tsx`:

```tsx
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";

const SAMPLE_POSTS = [
  {
    author: "Chị Lan, chủ mèo Miu",
    excerpt: "Miu vừa tiêm phòng xong, bé rất ngoan! Cảm ơn phòng khám ạ.",
  },
  {
    author: "Anh Khoa, chủ chó Bún",
    excerpt: "Chia sẻ kinh nghiệm chăm sóc chó con mới nhận nuôi...",
  },
  {
    author: "Phòng khám Thú Cưng Vui Vẻ",
    excerpt: "Lịch tiêm phòng dại miễn phí cuối tuần này!",
  },
];

export function FeedPreview() {
  return (
    <section aria-label="Furina Feed" className="bg-white/40 py-16">
      <div className="mx-auto max-w-6xl px-6">
        <div className="flex items-center gap-3">
          <h2 className="font-heading text-3xl text-ink">Furina Feed</h2>
          <Badge className="bg-highlight text-ink">Xem trước</Badge>
        </div>
        <p className="mt-2 text-ink/80">
          Bài viết mẫu — cộng đồng thật sẽ hoạt động khi Furina Feed ra mắt.
        </p>
        <div className="mt-8 grid gap-6 md:grid-cols-3">
          {SAMPLE_POSTS.map((post) => (
            <Card key={post.author} className="rounded-xl shadow-sm">
              <CardHeader>
                <CardTitle className="text-sm text-ink/70">
                  {post.author}
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-ink">{post.excerpt}</p>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    </section>
  );
}
```

- [ ] **Step 3: Wire into the page**

Modify `web/landing/app/page.tsx`:

```tsx
import { SiteNav } from "@/components/site-nav";
import { Hero } from "./sections/hero";
import { ProblemSolution } from "./sections/problem-solution";
import { Features } from "./sections/features";
import { ClinicMap } from "./sections/clinic-map";
import { FeedPreview } from "./sections/feed-preview";

export default function LandingPage() {
  return (
    <>
      <SiteNav />
      <Hero />
      <ProblemSolution />
      <Features />
      <ClinicMap />
      <FeedPreview />
    </>
  );
}
```

- [ ] **Step 4: Verify in the browser**

Run: `cd web/landing && npm run dev`, open `http://localhost:3000`, scroll to confirm the map placeholder box and 3 sample feed cards render, no console errors. Stop the server.

- [ ] **Step 5: Commit**

```bash
git add web/landing/app/sections/clinic-map.tsx web/landing/app/sections/feed-preview.tsx web/landing/app/page.tsx
git commit -m "feat(landing): ClinicMap + FeedPreview placeholder sections (4/8, 5/8)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 8: ForClinics, Testimonials, CtaFooter sections (6/8, 7/8, 8/8)

**Files:**
- Create: `web/landing/app/sections/for-clinics.tsx`
- Create: `web/landing/app/sections/testimonials.tsx`
- Create: `web/landing/app/sections/cta-footer.tsx`
- Modify: `web/landing/app/page.tsx`

**Interfaces:**
- Consumes: `Button` from Task 3.
- Produces: `<ForClinics id="phong-kham" />` (nav's `#phong-kham` anchor from Task 5 targets it), `<Testimonials />`, `<CtaFooter id="lien-he" />` (nav's `#lien-he` anchor targets it) — the last three sections, completing all 8.

- [ ] **Step 1: Write ForClinics**

Create `web/landing/app/sections/for-clinics.tsx`:

```tsx
import { Button } from "@/components/ui/button";

const REASONS = [
  "Quản lý tiếp nhận, kho dược phẩm, phòng nội trú trong một hệ thống",
  "Báo cáo tài chính trực quan theo thời gian thực",
  "Triển khai nhanh — không cần đội IT riêng",
];

export function ForClinics() {
  return (
    <section
      id="phong-kham"
      aria-label="Dành cho phòng khám"
      className="mx-auto max-w-6xl px-6 py-16"
    >
      <div className="grid items-center gap-10 md:grid-cols-2">
        <div>
          <h2 className="font-heading text-3xl text-ink">
            Dành cho phòng khám
          </h2>
          <ul className="mt-6 space-y-3">
            {REASONS.map((reason) => (
              <li key={reason} className="flex gap-3 text-ink">
                <span className="text-primary">✓</span>
                {reason}
              </li>
            ))}
          </ul>
          <Button className="mt-8 bg-primary text-primary-foreground" size="lg">
            Bắt đầu miễn phí
          </Button>
        </div>
        <div
          role="img"
          aria-label="Xem trước demo Furina cho phòng khám (video ngắn sắp có)"
          className="flex h-64 items-center justify-center rounded-xl border border-dashed border-ink/20 bg-white/50 text-ink/50"
        >
          Demo nhanh / video ngắn — sắp có
        </div>
      </div>
    </section>
  );
}
```

- [ ] **Step 2: Write Testimonials**

Create `web/landing/app/sections/testimonials.tsx`:

```tsx
export function Testimonials() {
  return (
    <section
      aria-label="Testimonial và case study"
      className="bg-white/40 py-16"
    >
      <div className="mx-auto max-w-6xl px-6 text-center">
        <h2 className="font-heading text-3xl text-ink">
          Khách hàng nói gì về Furina
        </h2>
        <div className="mt-10 flex h-40 items-center justify-center rounded-xl border border-dashed border-ink/20 text-ink/50">
          Chưa có case study — sẽ cập nhật khi có khách hàng thật
        </div>
      </div>
    </section>
  );
}
```

- [ ] **Step 3: Write CtaFooter**

Create `web/landing/app/sections/cta-footer.tsx`:

```tsx
import { Button } from "@/components/ui/button";

export function CtaFooter() {
  return (
    <footer id="lien-he" aria-label="CTA cuối trang và footer" className="bg-surface-dark py-16 text-white">
      <div className="mx-auto max-w-6xl px-6 text-center">
        <h2 className="font-heading text-3xl">
          Sẵn sàng bắt đầu cùng Furina?
        </h2>
        <div className="mt-6 flex flex-wrap justify-center gap-4">
          <Button className="bg-accent text-accent-foreground" size="lg">
            Đặt lịch ngay
          </Button>
          <Button
            variant="outline"
            size="lg"
            className="border-white text-white hover:bg-white/10"
          >
            Dành cho phòng khám
          </Button>
        </div>
        <div className="mt-10 flex flex-wrap justify-center gap-6 text-sm text-[#F5F5F4]">
          <span>© 2026 Furina</span>
          <a href="mailto:hello@furina.app">hello@furina.app</a>
          <a href="#">Facebook</a>
          <a href="#">Instagram</a>
        </div>
      </div>
    </footer>
  );
}
```

- [ ] **Step 4: Wire all 8 sections into the final page**

Modify `web/landing/app/page.tsx` — final version:

```tsx
import { SiteNav } from "@/components/site-nav";
import { Hero } from "./sections/hero";
import { ProblemSolution } from "./sections/problem-solution";
import { Features } from "./sections/features";
import { ClinicMap } from "./sections/clinic-map";
import { FeedPreview } from "./sections/feed-preview";
import { ForClinics } from "./sections/for-clinics";
import { Testimonials } from "./sections/testimonials";
import { CtaFooter } from "./sections/cta-footer";

export default function LandingPage() {
  return (
    <>
      <SiteNav />
      <Hero />
      <ProblemSolution />
      <Features />
      <ClinicMap />
      <FeedPreview />
      <ForClinics />
      <Testimonials />
      <CtaFooter />
    </>
  );
}
```

- [ ] **Step 5: Verify in the browser**

Run: `cd web/landing && npm run dev`, open `http://localhost:3000`, scroll the full page top to bottom, click all 3 nav anchors ("Tính năng", "Dành cho phòng khám", "Liên hệ") and confirm each scrolls to its section.
Expected: 8 sections visible in order, footer has dark surface background, no console errors. Stop the server.

- [ ] **Step 6: Commit**

```bash
git add web/landing/app/sections/for-clinics.tsx web/landing/app/sections/testimonials.tsx web/landing/app/sections/cta-footer.tsx web/landing/app/page.tsx
git commit -m "feat(landing): ForClinics + Testimonials + CtaFooter sections (6/8, 7/8, 8/8)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 9: Structural verification (Playwright) for AC-1 and AC-2

**Files:**
- Create: `web/landing/tests/landing.spec.ts`
- Modify: `web/landing/package.json` (add `@playwright/test` devDependency + `test:e2e` script)

**Interfaces:**
- Consumes: the running dev server from Task 2 onward (`http://localhost:3000`), all section `aria-label`s set in Tasks 5–8 (used as the test's section selector).
- Produces: `npm run test:e2e` in `web/landing/` — no later task depends on this, it is the plan's final verification gate for AC-1/AC-2.

- [ ] **Step 1: Install Playwright**

Run (from `web/landing/`):

```bash
npm install -D @playwright/test
npx playwright install chromium
```

- [ ] **Step 2: Write the failing test**

Create `web/landing/tests/landing.spec.ts`:

```typescript
import { test, expect } from "@playwright/test";

test.describe("Landing page (US-38 AC-2)", () => {
  test("renders exactly 8 sections in the spec's order", async ({ page }) => {
    const consoleErrors: string[] = [];
    page.on("console", (msg) => {
      if (msg.type() === "error") consoleErrors.push(msg.text());
    });

    await page.goto("/");

    const expectedOrder = [
      "Hero",
      "Vấn đề và giải pháp",
      "Tính năng nổi bật",
      "Bản đồ tìm phòng khám gần nhất",
      "Furina Feed",
      "Dành cho phòng khám",
      "Testimonial và case study",
      "CTA cuối trang và footer",
    ];

    const sections = page.locator("section[aria-label], footer[aria-label]");
    await expect(sections).toHaveCount(expectedOrder.length);

    for (let i = 0; i < expectedOrder.length; i++) {
      await expect(sections.nth(i)).toHaveAttribute(
        "aria-label",
        expectedOrder[i]
      );
    }

    expect(consoleErrors).toEqual([]);
  });
});

test.describe("Design token preview (US-38 AC-1)", () => {
  test("renders every token-driven component without a console error", async ({
    page,
  }) => {
    const consoleErrors: string[] = [];
    page.on("console", (msg) => {
      if (msg.type() === "error") consoleErrors.push(msg.text());
    });

    await page.goto("/design-preview");

    await expect(page.getByRole("heading", { name: "Design token preview" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Primary" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Accent CTA" })).toBeVisible();
    await expect(page.getByText("Highlight")).toBeVisible();
    await expect(page.getByPlaceholder("Nhập email...")).toBeVisible();

    expect(consoleErrors).toEqual([]);
  });
});
```

- [ ] **Step 3: Add the Playwright config and script**

Create `web/landing/playwright.config.ts`:

```typescript
import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./tests",
  webServer: {
    command: "npm run dev",
    url: "http://localhost:3000",
    reuseExistingServer: true,
    timeout: 30_000,
  },
  use: {
    baseURL: "http://localhost:3000",
  },
});
```

Modify `web/landing/package.json`, add to `"scripts"`:

```json
"test:e2e": "playwright test"
```

- [ ] **Step 4: Run the test to verify it fails before section `aria-label`s existed would have failed — confirm it passes now that Tasks 5–8 are done**

Run: `cd web/landing && npm run test:e2e`
Expected: 2 passed (both the 8-section-order test and the design-preview test). If the section count or order test fails, compare the failing assertion's actual `aria-label` list against Tasks 5–8's `aria-label` values and fix the mismatched section file — do not edit the test's `expectedOrder` to match wrong output.

- [ ] **Step 5: Commit**

```bash
git add web/landing/tests web/landing/playwright.config.ts web/landing/package.json web/landing/package-lock.json
git commit -m "test(landing): Playwright checks for 8-section order (AC-2) and token preview (AC-1)

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 10: Wire into CI, final docs, and close out US-38

**Files:**
- Modify: `.github/workflows/pr.yml`
- Modify: `README.md`

**Interfaces:**
- Consumes: `web/package.json`'s `test:tokens`/`check:contrast` scripts (Task 1), `web/landing/package.json`'s `test:e2e` script (Task 9).
- Produces: nothing further consumed — this is the plan's last task.

- [ ] **Step 1: Add a `web` job to `pr.yml`**

Modify `.github/workflows/pr.yml` — add a new job alongside the existing `build-and-test` job (do not rename or remove the existing job; branch protection on `main` requires it by that exact name):

```yaml
  web-checks:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-node@v4
        with:
          node-version: "22"

      - name: Token contrast check
        run: node web/design-system/contrast-check.mjs

      - name: Token unit tests
        run: node --test web/design-system/contrast.test.mjs

      - name: Install landing deps
        working-directory: web/landing
        run: npm ci

      - name: Install Playwright browsers
        working-directory: web/landing
        run: npx playwright install --with-deps chromium

      - name: Build landing
        working-directory: web/landing
        run: npm run build

      - name: E2E tests
        working-directory: web/landing
        run: npm run test:e2e
```

- [ ] **Step 2: Verify the workflow file is valid YAML**

Run: `node -e "require('yaml').parse(require('fs').readFileSync('.github/workflows/pr.yml', 'utf8'))"` — if the `yaml` package isn't installed globally, instead run `python -c "import yaml,sys; yaml.safe_load(open('.github/workflows/pr.yml'))"` or simply open the file and confirm indentation is consistent 2-space YAML with the new `web-checks:` job at the same nesting level as `build-and-test:` under `jobs:`.
Expected: no parse error.

- [ ] **Step 3: Document the new app in the root README**

Modify `README.md`, add a new section (after the existing TASK-14 section, before "Việc còn lại"):

```markdown
## US-38: Thiết kế UI/UX & Bộ nhận diện thương hiệu (Design System)

Trạng thái AC:

- **AC-1** (design token áp vào trang mẫu, đúng màu/font/spacing): ✅ `web/landing/app/design-preview` — verify bằng Playwright (`web/landing/tests/landing.spec.ts`).
- **AC-2** (landing page đủ 8 section theo wiki, không thiếu/thừa): ✅ `web/landing/app/page.tsx` — verify bằng Playwright, đếm + kiểm tra thứ tự `aria-label` của từng section.

Spec đầy đủ: `docs/superpowers/specs/2026-09-11-design-system-landing-design.md` (nội dung thiết kế gốc: wiki Taiga trang `thiet-ke-uiux`).

### Chạy local

\`\`\`bash
cd web/landing
npm install
npm run dev              # http://localhost:3000

# Kiểm tra token + contrast (không cần chạy web server)
node ../design-system/contrast-check.mjs
node --test ../design-system/contrast.test.mjs

# E2E (cần server đang chạy hoặc để Playwright tự chạy dev server)
npm run test:e2e
\`\`\`

### Chưa làm (theo đúng phạm vi wiki "Việc CHƯA làm")

- Logo thật (mới có concept mô tả).
- A/B test copy landing page.
- Audit WCAG chính thức (có sanity-check tự động qua `contrast-check.mjs`, không thay thế audit thật).
- Tenant Admin / Super Admin Portal (Sprint 5/6) áp dụng cùng token với tông khác — chưa tồn tại, sẽ import `web/design-system` khi tới sprint đó.
```

- [ ] **Step 4: Commit**

```bash
git add .github/workflows/pr.yml README.md
git commit -m "ci,docs: wire web checks into pr.yml, document US-38 in README

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

- [ ] **Step 5: Push the branch and open a PR (do not merge — human reviews and merges, per this repo's `.agentpolicy.yml` autonomyLevel 2)**

Run:

```bash
git push -u origin design/us38-design-system
```

Then open a PR from `design/us38-design-system` into `main` (via the GitHub UI, or the GitHub API creating the PR object only — never call the merge endpoint). Title: "US-38: Design system tokens + landing page". Body: link the spec file path and summarize AC-1/AC-2 verification (mirrors the README section from Step 3).
