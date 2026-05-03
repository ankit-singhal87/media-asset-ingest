#!/usr/bin/env node
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';

const roots = ['AGENTS.md', 'README.md', 'docs'];
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

for (const file of files) {
  const text = readFileSync(file, 'utf8');
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
}

if (findings.length > 0) {
  console.error('Unfinished documentation markers found:');
  for (const finding of findings) {
    console.error(finding);
  }
  process.exit(1);
}

console.log('Documentation placeholder check passed.');

