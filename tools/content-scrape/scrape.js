// Scrape CodeProject article with Playwright and convert to Markdown using MarkItDown
// Usage: node scrape.js

import { chromium } from 'playwright';
import fs from 'node:fs/promises';
import path from 'node:path';
import TurndownService from 'turndown';

const URL = 'https://www.codeproject.com/articles/LogViewer-Control-for-WinForms-WPF-and-Avalonia-in';
const OUT_FILE = path.resolve(process.cwd(), '../../docs/external/codeproject-logviewer.markitdown.full.md');

/**
 * Normalize images and remove clutter from CodeProject article DOM
 */
async function extractCleanHtml(page) {
  return await page.evaluate(() => {
    const selectors = [
      'article',
      'main article',
      'div.text-gray-900.leading-relaxed.font-nunito',
      'main',
      '#content'
    ];
    let container = null;
    for (const s of selectors) {
      const el = document.querySelector(s);
      if (el) { container = el; break; }
    }
    if (!container) container = document.body;
    const clone = container.cloneNode(true);

    // Remove ads/scripts/styles/iframes
    clone.querySelectorAll('script, style, iframe, noscript, link[rel="preload"], nav, header, footer, [data-e2e="ad"], [id^="google_ads_"]').forEach(n => n.remove());

    // Rewrite next/image to original and absolutize relative img urls
    const toAbs = (url) => {
      try { return new URL(url, location.origin).toString(); } catch { return url; }
    };
    clone.querySelectorAll('img').forEach(img => {
      if (!img.src) return;
      const u = new URL(img.src, location.origin);
      if (u.pathname.startsWith('/_next/image')) {
        const orig = u.searchParams.get('url');
        if (orig) img.src = toAbs(orig);
      } else {
        img.src = toAbs(img.getAttribute('src'));
      }
      img.removeAttribute('srcset');
      img.removeAttribute('data-nimg');
    });

    // Absolutize anchors
    clone.querySelectorAll('a[href]').forEach(a => {
      const href = a.getAttribute('href');
      if (!href) return;
      try { a.setAttribute('href', new URL(href, location.origin).toString()); } catch {}
    });

    // Include article title if present
  const title = document.querySelector('h1')?.textContent?.trim() || document.title || 'CODEPROJECT';

    const html = `<!doctype html><html><head><meta charset="utf-8"/><base href="${location.origin}"/><title>${title}</title></head><body><h1>${title}</h1>${clone.outerHTML}</body></html>`;
    return html;
  });
}

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({
    userAgent: 'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120 Safari/537.36',
    locale: 'en-US'
  });
  const page = await ctx.newPage();
  await page.goto(URL, { waitUntil: 'domcontentloaded', timeout: 120000 });
  // Try to accept cookie/consent if present (best-effort)
  try {
    const candidates = [
      'button:has-text("I Agree")',
      'button:has-text("I accept")',
      'button:has-text("Accept")',
      'button:has-text("Consent")',
      'button:has-text("Agree & Continue")',
      'button:has-text("Continue")'
    ];
    for (const sel of candidates) {
      const loc = page.locator(sel);
      if (await loc.first().isVisible({ timeout: 1000 }).catch(() => false)) {
        await loc.first().click({ timeout: 1000 }).catch(() => {});
        break;
      }
    }
    for (const frame of page.frames()) {
      const loc = frame.locator('button:has-text("Accept"), button:has-text("I Agree"), button:has-text("Consent")');
      if (await loc.first().isVisible({ timeout: 1000 }).catch(() => false)) {
        await loc.first().click({ timeout: 1000 }).catch(() => {});
        break;
      }
    }
  } catch {}
  // Wait for article content area
  await page.waitForSelector('article, main article, div.text-gray-900.leading-relaxed.font-nunito, main', { timeout: 120000 });
  // Encourage lazy-loaded assets
  await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
  await page.waitForTimeout(1500);

  const cleanHtml = await extractCleanHtml(page);
  await browser.close();

  const turndown = new TurndownService({
    codeBlockStyle: 'fenced',
    headingStyle: 'atx',
    bulletListMarker: '*'
  });
  // Preserve code blocks better
  turndown.addRule('preCode', {
    filter: (node) => node.nodeName === 'PRE' && node.querySelector('code'),
    replacement: (content, node) => {
      const code = node.querySelector('code');
      const cls = code?.getAttribute('class') || '';
      let lang = '';
      const m = cls.match(/language-([\w-]+)/i);
      if (m) lang = m[1];
      if (lang.toLowerCase() === 'cs') lang = 'csharp';
      if (lang.toLowerCase() === 'vb' || lang.toLowerCase() === 'vbnet') lang = 'vbnet';
      if (lang.toLowerCase() === 'xaml') lang = 'xml';
      const codeText = code.textContent || '';
      return `\n\n\`\`\`${lang}\n${codeText}\n\`\`\`\n\n`;
    }
  });
  const markdown = turndown.turndown(cleanHtml);

  // Simple heuristic: upgrade code fences where possible
  const tagged = markdown
    .replace(/```\s*\n/g, '```csharp\n')
    .replace(/```xaml\n/g, '```xml\n');

  await fs.mkdir(path.dirname(OUT_FILE), { recursive: true });
  await fs.writeFile(OUT_FILE, tagged, 'utf8');
  console.log(`Saved Markdown to ${OUT_FILE}`);
})();
