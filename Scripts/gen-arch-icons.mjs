// Gera o mapa de ícones usado pelos pôsteres (/dev/arch-diagram e /dev/tests-diagram).
//
// A página exporta o SVG como arquivo solto (download de SVG/PNG), então os
// ícones precisam estar embutidos como path — o <UIcon>, que resolve o ícone em
// runtime pelo endpoint do Nuxt, não sobreviveria ao export.
//
// Rodar depois de incluir um ícone novo na lista abaixo:  node Scripts/gen-arch-icons.mjs

import { readFileSync, writeFileSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..')
const OUT = join(ROOT, 'Web', 'app', 'utils', 'arch-icons.ts')

const WANTED = {
  'lucide': [
    'activity',
    'arrow-up-right',
    'bell-ring',
    'book-open-text',
    'box',
    'building-2',
    'chart-column',
    'chart-line',
    'check-check',
    'circle-alert',
    'circle-check-big',
    'container',
    'database',
    'dna',
    'drama',
    'file-text',
    'fingerprint',
    'flask-conical',
    'globe',
    'hourglass',
    'image',
    'layers',
    'library-big',
    'list-end',
    'mail',
    'plug',
    'quote',
    'route',
    'scroll-text',
    'send',
    'server',
    'shield-check',
    'split',
    'test-tube-diagonal',
    'trending-up',
    'triangle-alert',
    'unlink',
    'waypoints',
    'webhook',
    'zap'
  ],
  'simple-icons': [
    'caddy',
    'cloudflare',
    'docker',
    'dotnet',
    'githubactions',
    'google',
    'grafana',
    'keycloak',
    'linkedin',
    'minio',
    'nuxt',
    'opentelemetry',
    'postgresql',
    'posthog',
    'railway',
    'scalar',
    'vitest'
  ]
}

// O R2 não tem ícone em nenhuma das duas coleções, então este é desenhado à mão
// no traço do Lucide (24x24, stroke 2, currentColor) a partir da marca da Cloudflare.
const CUSTOM = {
  'i-estud-r2': '<g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2">'
    + '<ellipse cx="12" cy="4" rx="9" ry="3"/>'
    + '<path d="M3 4v16a9 3 0 0 0 18 0V4"/>'
    + '<path d="M3 9.3a9 3 0 0 0 18 0"/>'
    + '<path d="M3 14.6a9 3 0 0 0 18 0"/>'
    + '<path d="M7 9.2h.01M7 14.5h.01M7 19.8h.01"/>'
    + '</g>'
}

function body(collection, name) {
  let icon = collection.icons[name]

  let alias = collection.aliases?.[name]
  while (!icon && alias) {
    icon = collection.icons[alias.parent]
    alias = collection.aliases?.[alias.parent]
  }

  if (!icon) throw new Error(`ícone não encontrado: ${name}`)
  return icon.body
}

const entries = []

for (const [prefix, names] of Object.entries(WANTED)) {
  const path = join(ROOT, 'Web', 'node_modules', `@iconify-json/${prefix}`, 'icons.json')
  const collection = JSON.parse(readFileSync(path, 'utf8'))

  for (const name of names) {
    const svg = body(collection, name).replace(/\\/g, '\\\\').replace(/'/g, '\\\'')
    entries.push(`  'i-${prefix}-${name}': '${svg}'`)
  }
}

for (const [name, svg] of Object.entries(CUSTOM)) {
  entries.push(`  '${name}': '${svg}'`)
}

const ts = `// Gerado por Scripts/gen-arch-icons.mjs a partir do @iconify-json/lucide e do
// @iconify-json/simple-icons. Não editar à mão: inclua o ícone na lista do
// script e rode \`node Scripts/gen-arch-icons.mjs\`.
//
// Os corpos vêm com \`currentColor\`, então a cor sai do \`color\` do <g> que os envolve.

export const archIcons: Record<string, string> = {
${entries.join(',\n')}
}
`

writeFileSync(OUT, ts)
console.log(`${entries.length} ícones → ${OUT}`)
