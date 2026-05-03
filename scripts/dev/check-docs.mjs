#!/usr/bin/env node
import { existsSync, readdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import { dirname, join, normalize } from 'node:path';

const roots = ['AGENTS.md', 'README.md', 'docs'];
const fix = process.argv.includes('--fix');
const patterns = [
  { pattern: /\bTODO\b/g, allow: ['docs/automation/validation.md'] },
  { pattern: /\bTBD\b/g, allow: ['docs/automation/validation.md'] },
  { pattern: /\bFIXME\b/g, allow: [] },
  { pattern: /<placeholder>/g, allow: [] }
];

function listFiles(path) {
  const stat = statSync(path);
  if (stat.isFile()) {
    return [path];
  }

  return readdirSync(path).flatMap((entry) => listFiles(join(path, entry)));
}

const files = roots.flatMap((root) => listFiles(root));
const findings = [];
const markdownLinks = /\[[^\]]+\]\(([^)]+)\)/g;

function normalizeMarkdown(text) {
  const normalized = text
    .replace(/\r\n/g, '\n')
    .replace(/[ \t]+$/gm, '')
    .replace(/\n*$/, '\n');

  return normalized;
}

for (const file of files) {
  let text = readFileSync(file, 'utf8');
  const normalized = normalizeMarkdown(text);
  if (normalized !== text) {
    if (fix) {
      writeFileSync(file, normalized, 'utf8');
      text = normalized;
    } else {
      findings.push(`${file}: markdown formatting is not normalized; run npm run docs:fix`);
    }
  }

  const lines = text.split(/\r?\n/);

  for (const { pattern, allow } of patterns) {
    if (allow.includes(file)) {
      continue;
    }

    lines.forEach((line, index) => {
      pattern.lastIndex = 0;
      if (pattern.test(line)) {
        findings.push(`${file}:${index + 1}: ${line}`);
      }
    });
  }

  if (file.endsWith('.md')) {
    for (const match of text.matchAll(markdownLinks)) {
      let target = match[1].trim();
      if (
        !target ||
        target.startsWith('#') ||
        /^[a-z][a-z0-9+.-]*:/i.test(target)
      ) {
        continue;
      }

      target = target.split('#')[0];
      if (!target) {
        continue;
      }

      const resolved = normalize(join(dirname(file), target));
      if (!existsSync(resolved)) {
        findings.push(`${file}: broken markdown link ${match[1]}`);
        continue;
      }

      if (statSync(resolved).isDirectory() && !existsSync(join(resolved, 'README.md'))) {
        findings.push(`${file}: directory markdown link lacks README ${match[1]}`);
      }
    }
  }
}

if (findings.length > 0) {
  console.error('Documentation checks failed:');
  for (const finding of findings) {
    console.error(finding);
  }
  process.exit(1);
}

if (fix) {
  console.log('Documentation fixes applied.');
} else {
  console.log('Documentation checks passed.');
}
