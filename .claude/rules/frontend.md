---
globs: src/Wisbar.Frontend/**,src/Wisbar.Web/wwwroot/**
---

# Frontend rules

## Stack

- React 19 + Vite; production output goes to `src/Wisbar.Web/wwwroot/dist/`.
- `bundle.config.js` (in `src/Wisbar.Frontend/`) controls additional non-Vite bundle outputs — `main.min.js`, `main.min.css`.

## Commands (run from `src/Wisbar.Frontend/`)

- `npm ci` — install dependencies
- `npm run dev` — Vite dev server
- `npm run build` — production build → wwwroot/dist/
- `npm run lint` — ESLint

## ESLint

- Fix the root cause of every lint error.
- Never add `eslint-disable` or `// eslint-ignore` comments.

## Sandbox

- `src/Wisbar.Frontend/sandbox/` contains standalone HTML component previews used during development.
- Sandbox files are not shipped to production — don't reference them from application code.

## Output locations

- React components needing server-side integration → `src/Wisbar.Web/wwwroot/ReactComponents/`
- Plain JS → `src/Wisbar.Web/wwwroot/js/`
- CSS → `src/Wisbar.Web/wwwroot/css/`

## Styles

- Global SCSS lives in `src/Wisbar.Frontend/src/styles/`.
- Component-scoped SCSS sits alongside its component.
- Follow the existing `_component-name.scss` naming convention.
