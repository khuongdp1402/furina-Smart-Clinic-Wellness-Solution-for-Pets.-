# Design System + Landing Page (US-38 / ref 38)

Source of truth for content decisions (palette, typography, landing copy,
per-audience UI rules): Taiga wiki page `thiet-ke-uiux` on project
`furinasmart-clinic-wellness-solution-for-pets`. This spec covers only the
*technical* design — how that content becomes code — and does not restate
wiki content that isn't needed to implement it.

## Why

Landing page + 2 future admin portals (Sprint 5/6) need one consistent
design language, locked early (parallel to Sprint 1) so later frontend work
never has to invent colors/fonts ad hoc. US-38's Definition of Done:

- Brand identity (palette, typography, tone) recorded in the wiki — done,
  already exists.
- Design tokens implementable as Tailwind config + CSS variables, shared
  across all 3 future frontends.
- Landing page wireframe + copy detailed enough to build without asking
  again — done, already exists (8 sections, described in wiki).

This spec's job is the token/landing implementation, not re-deciding
content the wiki already settled.

## Scope

In scope:
- A framework-agnostic design token layer (CSS variables + a Tailwind v4
  preset) that this repo's *next* two frontends (Tenant Admin, Super Admin
  — Sprint 5/6) can adopt without redoing token work.
- One Next.js 15 app, `web/landing`, implementing the 8-section landing
  page from the wiki.
- One preview page inside that app applying the tokens to basic UI
  components (button, card, input, badge, light/dark surface) — this is
  US-38 AC-1's "trang mẫu"; there is no real login page to apply tokens to
  yet since Tenant Admin Portal doesn't exist until Sprint 5.

Out of scope (explicitly deferred per the wiki's own "Việc CHƯA làm"
section, or per project scope):
- Real logo file (concept only exists as a text description).
- Copy A/B testing.
- A formal WCAG audit tool/process — this spec computes contrast ratios
  for the documented color pairs as a sanity check, which is not a
  substitute for a real audit.
- Turborepo/pnpm-workspace monorepo tooling — only one frontend app exists
  today; introduce shared build tooling when a second one (Sprint 5) is
  actually being built, not preemptively.
- Wiring the landing page to any real backend (map data, Furina Feed
  posts, clinic search) — those are later features; this page uses the
  placeholder content the wiki specifies.

## Approach

### Repository layout

```
web/
  design-system/
    tokens.css            CSS custom properties for every token in the wiki's color table
    tailwind-preset.ts     Tailwind v4 theme extension reading those CSS vars
    contrast-check.mjs      standalone script: WCAG contrast ratio for each documented fg/bg pair
    README.md               how a future frontend (Sprint 5/6) consumes this
  landing/
    (Next.js 15 App Router project)
    app/
      globals.css            imports ../../design-system/tokens.css
      page.tsx                the 8-section landing page
      design-preview/page.tsx token/component preview page (AC-1)
    tailwind.config.ts        extends the shared preset
```

`design-system/` is plain CSS + a small TS object — no build step of its
own. A Next.js app consumes it by importing the CSS file and spreading the
preset into its own `tailwind.config.ts`. This is the smallest thing that
lets Sprint 5/6 apps reuse the same tokens without a monorepo package
manager doing anything special (no publishing, no workspace protocol
needed — just a relative import, same as within `web/landing` itself).

### Design tokens

Every row of the wiki's color table becomes one CSS variable in
`tokens.css`, e.g.:

```css
:root {
  --color-primary: #0F766E;
  --color-accent: #F97066;
  --color-highlight: #FBBF24;
  --color-surface: #FAF9F6;
  --color-ink: #292524;
  --color-surface-dark: #0F1B1A;
  --color-danger: #DC2626;
  --color-success: #16A34A;
}
```

Typography: two font tokens (`--font-heading` for the landing page's serif
per the wiki — Fraunces — and `--font-ui` sans-serif — Inter — for the
future admin portals), loaded via `next/font` in the landing app and
exposed as CSS variables so the same variable names work once Tenant/Super
Admin portals exist.

`tailwind-preset.ts` maps `theme.colors.primary` etc. to `var(--color-primary)`
rather than hard-coding hex — this is the mechanism the wiki calls out
("đổi 1 biến, đổi cả hệ thống") for future white-labeling and for the
Tenant vs. Super Admin tonal difference (both portals import the same
preset; a portal-level CSS override file can shift which vars point to
which values without touching component code — that override work itself
is Sprint 5/6 scope, not this task's).

Spacing/radius/font-size scale: default Tailwind v4 scale, per the wiki
("theo thang chuẩn Tailwind... không tự chế thang riêng") — `rounded-xl`
as the default card/button radius, low-elevation shadow utilities only.

### Landing page

`web/landing/app/page.tsx` renders 8 section components (`app/sections/`),
one file per section from the wiki (Hero, ProblemSolution, Features,
ClinicMap, FeedPreview, ForClinics, Testimonials, CtaFooter), each a plain
server component (no client interactivity needed for a first static pass
except the two CTA buttons and a mobile nav toggle, which are the only
`"use client"` islands). Copy comes directly from the wiki's section
descriptions; where the wiki gives an example tagline/CTA verbatim, that
exact text is used rather than a paraphrase, so the delivered page matches
what's already been agreed rather than introducing new copy decisions.

`ClinicMap` and `FeedPreview` render static placeholder content (a styled
placeholder box + caption, not a real map/feed) — the wiki calls both
"preview", and the real data (PostGIS-backed map, real Feed posts) doesn't
exist yet; building fake interactivity against no backend would be waste
per YAGNI.

### Component library

shadcn/ui (Radix primitives) per the wiki's stated tech choice, added via
its CLI (`npx shadcn@latest add button card input badge ...`) directly
into `web/landing/components/ui/` — the same install path Sprint 5/6 will
use in their own apps. Only the primitives the landing page + preview page
actually use are added (button, card, badge, input, navigation-menu) —
not the full shadcn catalog.

### Testing

No backend exists for this page to integrate with, so testing here is
visual/structural, not behavioral:

- `contrast-check.mjs` computes the WCAG contrast ratio for every
  foreground/background pair the wiki documents (ink-on-surface,
  ink-on-surface-dark, white-on-primary, white-on-accent, etc.) and prints
  pass/fail against AA (4.5:1 normal text, 3:1 large text/UI components).
  This is deterministic and needs no browser — it's the "not a substitute
  for a real audit" sanity check the spec's Out of Scope section flags.
- `example-skills:webapp-testing` (Playwright) drives the dev server,
  screenshots `/` and `/design-preview`, and asserts the landing page has
  exactly 8 top-level `<section>` elements in the wiki's order (AC-2) and
  that the preview page renders every token-driven component without a
  console error (supports AC-1 by showing tokens actually resolve, not
  just that the CSS typechecks).

### Error handling

This is a static marketing page with no user input beyond the two CTA
buttons (which, with no backend yet, link to `#` anchors / a
"coming soon" state rather than a broken form) — there is no error
handling surface beyond normal Next.js build-time type/lint checking.

## Risks / open questions

- The wiki's copy examples are in Vietnamese; this spec keeps them as-is
  (no translation decision needed now — i18n isn't mentioned in the wiki
  and isn't part of US-38's DoD).
- Font licensing: Fraunces and Inter are both open-source (SIL OFL /
  OFL), loaded via `next/font/google`, no separate licensing step needed.
