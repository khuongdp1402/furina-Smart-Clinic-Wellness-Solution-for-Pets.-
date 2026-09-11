# Furina design system tokens

Framework-agnostic CSS custom properties + a Tailwind v4 `@theme` mapping.
Source of truth for values: Taiga wiki page `thiet-ke-uiux`.

## Using this from a new frontend (e.g. the Sprint 5 Tenant Admin Portal)

In your app's global CSS, before your own styles:

```css
@import "../../design-system/tokens.css";
@import "../../design-system/theme.css";
@import "tailwindcss";
```

That's it — `bg-primary`, `text-ink`, `font-heading`, etc. are now real
Tailwind utility classes. To retint a portal (e.g. Super Admin's more
serious, less-coral tone per the wiki), override specific `--color-*`
variables in that app's own CSS after the imports above — never edit
`tokens.css` for a single app's tone, that file is shared.

## Verifying contrast

```bash
node web/design-system/contrast-check.mjs
```

Exits non-zero if any documented foreground/background pair drops below
WCAG AA. This is a sanity check on the *documented* pairs list in that
script, not an automatic scan of every color combination used anywhere in
the app — a real accessibility audit is still a separate, manual task (see
the design spec's Out of Scope section).
