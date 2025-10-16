# Content Scrape (Playwright + MarkItDown)

This helper captures content from dynamic pages (like CodeProject) using Playwright (Chromium) and converts it to Markdown with MarkItDown.

## Prerequisites

- Node.js 18+

## Install

```bash
cd tools/content-scrape
npm install
```

## Runpply the lint scope excludes and patch the few flagged files now, then re-run lint and proceed with link checks.

```bash
npm start
```

This will produce:

- `../../docs/external/codeproject-logviewer.markitdown.full.md`

## Notes

- The script normalizes images (removes Next.js wrappers, makes URLs absolute) and strips ads/iframes/scripts/styles.
- A light heuristic tags untyped code fences as `csharp`. You can refine the tagging logic if needed.
- If the site changes its DOM, adjust the selector near the top of `scrape.js`.
