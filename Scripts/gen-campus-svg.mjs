// Gera as duas versões da imagem da tela de ocupação usada no README.
//
// É o mesmo desenho do `<LandingCampusPreview>`, redesenhado em SVG puro: o
// markdown do GitHub não roda Vue nem CSS, e um PNG perderia nitidez em tela
// retina. Os ícones vêm do Lucide (os mesmos do app), copiados aqui pra o
// script não depender do node_modules do Web.
//
// Rodar depois de mexer no componente:  node Scripts/gen-campus-svg.mjs

import { mkdirSync, readFileSync, writeFileSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const ROOT = join(dirname(fileURLToPath(import.meta.url)), '..')
const OUT_DIR = join(ROOT, '.github', 'assets')
const data = JSON.parse(readFileSync(join(ROOT, 'Web', 'app', 'mocks', 'campus-occupancy.json'), 'utf8'))

// A célula aberta na imagem é a mesma que o componente abre por padrão.
const SELECTED = { day: 'Wednesday', shift: 'Afternoon' }

const ICONS = {
  "book-marked": "<g fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\"><path d=\"M10 2v8l3-3l3 3V2\"/><path d=\"M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H19a1 1 0 0 1 1 1v18a1 1 0 0 1-1 1H6.5a1 1 0 0 1 0-5H20\"/></g>",
  "archive": "<g fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\"><rect width=\"20\" height=\"5\" x=\"2\" y=\"3\" rx=\"1\"/><path d=\"M4 8v11a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8m-10 4h4\"/></g>",
  "contact": "<g fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\"><path d=\"M16 2v2M7 22v-2a2 2 0 0 1 2-2h6a2 2 0 0 1 2 2v2M8 2v2\"/><circle cx=\"12\" cy=\"11\" r=\"3\"/><rect width=\"18\" height=\"18\" x=\"3\" y=\"4\" rx=\"2\"/></g>",
  "cog": "<g fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\"><path d=\"M11 10.27L7 3.34m4 10.39l-4 6.93M12 22v-2m0-18v2m2 8h8m-5 8.66l-1-1.73m1-15.59l-1 1.73M2 12h2m16.66 5l-1.73-1m1.73-9l-1.73 1M3.34 17l1.73-1M3.34 7l1.73 1\"/><circle cx=\"12\" cy=\"12\" r=\"2\"/><circle cx=\"12\" cy=\"12\" r=\"8\"/></g>",
  "map-pin": "<g fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\"><path d=\"M20 10c0 4.993-5.539 10.193-7.399 11.799a1 1 0 0 1-1.202 0C9.539 20.193 4 14.993 4 10a8 8 0 0 1 16 0\"/><circle cx=\"12\" cy=\"10\" r=\"3\"/></g>",
  "notebook": "<g fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\"><path d=\"M2 6h4m-4 4h4m-4 4h4m-4 4h4\"/><rect width=\"16\" height=\"20\" x=\"4\" y=\"2\" rx=\"2\"/><path d=\"M16 2v20\"/></g>",
  "layout-list": "<g fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\"><rect width=\"7\" height=\"7\" x=\"3\" y=\"3\" rx=\"1\"/><rect width=\"7\" height=\"7\" x=\"3\" y=\"14\" rx=\"1\"/><path d=\"M14 4h7m-7 5h7m-7 6h7m-7 5h7\"/></g>",
  "book-open": "<path fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\" d=\"M12 7v14m-9-3a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1h5a4 4 0 0 1 4 4a4 4 0 0 1 4-4h5a1 1 0 0 1 1 1v13a1 1 0 0 1-1 1h-6a3 3 0 0 0-3 3a3 3 0 0 0-3-3z\"/>",
  "library": "<path fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\" d=\"m16 6l4 14M12 6v14M8 8v12M4 4v16\"/>",
  "presentation": "<path fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\" d=\"M2 3h20m-1 0v11a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V3m4 18l5-5l5 5\"/>",
  "calendar-range": "<g fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\"><rect width=\"18\" height=\"18\" x=\"3\" y=\"4\" rx=\"2\"/><path d=\"M16 2v4M3 10h18M8 2v4m9 8h-6m2 4H7m0-4h.01M17 18h.01\"/></g>",
  "bell": "<path fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\" d=\"M10.268 21a2 2 0 0 0 3.464 0m-10.47-5.674A1 1 0 0 0 4 17h16a1 1 0 0 0 .74-1.673C19.41 13.956 18 12.499 18 8A6 6 0 0 0 6 8c0 4.499-1.411 5.956-2.738 7.326\"/>",
  "graduation-cap": "<g fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\"><path d=\"M21.42 10.922a1 1 0 0 0-.019-1.838L12.83 5.18a2 2 0 0 0-1.66 0L2.6 9.08a1 1 0 0 0 0 1.832l8.57 3.908a2 2 0 0 0 1.66 0zM22 10v6\"/><path d=\"M6 12.5V16a6 3 0 0 0 12 0v-3.5\"/></g>",
  "user-pen": "<g fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\"><path d=\"M11.5 15H7a4 4 0 0 0-4 4v2m18.378-4.374a1 1 0 0 0-3.004-3.004l-4.01 4.012a2 2 0 0 0-.506.854l-.837 2.87a.5.5 0 0 0 .62.62l2.87-.837a2 2 0 0 0 .854-.506z\"/><circle cx=\"10\" cy=\"7\" r=\"4\"/></g>",
  "users": "<g fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\"><path d=\"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M16 3.128a4 4 0 0 1 0 7.744M22 21v-2a4 4 0 0 0-3-3.87\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/></g>",
  "shield": "<path fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\" d=\"M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z\"/>",
  "webhook": "<g fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\"><path d=\"M18 16.98h-5.99c-1.1 0-1.95.94-2.48 1.9A4 4 0 0 1 2 17c.01-.7.2-1.4.57-2\"/><path d=\"m6 17l3.13-5.78c.53-.97.1-2.18-.5-3.1a4 4 0 1 1 6.89-4.06\"/><path d=\"m12 6l3.13 5.73C15.66 12.7 16.9 13 18 13a4 4 0 0 1 0 8\"/></g>",
  "settings": "<g fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\"><path d=\"M9.671 4.136a2.34 2.34 0 0 1 4.659 0a2.34 2.34 0 0 0 3.319 1.915a2.34 2.34 0 0 1 2.33 4.033a2.34 2.34 0 0 0 0 3.831a2.34 2.34 0 0 1-2.33 4.033a2.34 2.34 0 0 0-3.319 1.915a2.34 2.34 0 0 1-4.659 0a2.34 2.34 0 0 0-3.32-1.915a2.34 2.34 0 0 1-2.33-4.033a2.34 2.34 0 0 0 0-3.831A2.34 2.34 0 0 1 6.35 6.051a2.34 2.34 0 0 0 3.319-1.915\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/></g>",
  "pencil": "<path fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\" d=\"M21.174 6.812a1 1 0 0 0-3.986-3.987L3.842 16.174a2 2 0 0 0-.5.83l-1.321 4.352a.5.5 0 0 0 .623.622l4.353-1.32a2 2 0 0 0 .83-.497zM15 5l4 4\"/>",
  "layout-grid": "<g fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\"><rect width=\"7\" height=\"7\" x=\"3\" y=\"3\" rx=\"1\"/><rect width=\"7\" height=\"7\" x=\"14\" y=\"3\" rx=\"1\"/><rect width=\"7\" height=\"7\" x=\"14\" y=\"14\" rx=\"1\"/><rect width=\"7\" height=\"7\" x=\"3\" y=\"14\" rx=\"1\"/></g>",
  "clock": "<g fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\"><circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M12 6v6l4 2\"/></g>",
  "door-open": "<path fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\" d=\"M11 20H2m9-15.438v16.157a1 1 0 0 0 1.242.97L19 20V5.562a2 2 0 0 0-1.515-1.94l-4-1A2 2 0 0 0 11 4.561zM11 4H8a2 2 0 0 0-2 2v14m8-8h.01M22 20h-3\"/>",
  "table-2": "<path fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\" d=\"M9 3H5a2 2 0 0 0-2 2v4m6-6h10a2 2 0 0 1 2 2v4M9 3v18m0 0h10a2 2 0 0 0 2-2V9M9 21H5a2 2 0 0 1-2-2V9m0 0h18\"/>",
  "panel-left-close": "<g fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\"><rect width=\"18\" height=\"18\" x=\"3\" y=\"3\" rx=\"2\"/><path d=\"M9 3v18m7-6l-3-3l3-3\"/></g>",
  "chevron-up": "<path fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\" d=\"m18 15l-6-6l-6 6\"/>",
  "chevron-right": "<path fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\" d=\"m9 18l6-6l-6-6\"/>",
  "sun": "<g fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\"><circle cx=\"12\" cy=\"12\" r=\"4\"/><path d=\"M12 2v2m0 16v2M4.93 4.93l1.41 1.41m11.32 11.32l1.41 1.41M2 12h2m16 0h2M6.34 17.66l-1.41 1.41M19.07 4.93l-1.41 1.41\"/></g>",
  "moon": "<path fill=\"none\" stroke=\"currentColor\" stroke-linecap=\"round\" stroke-linejoin=\"round\" stroke-width=\"2\" d=\"M20.985 12.486a9 9 0 1 1-9.473-9.472c.405-.022.617.46.402.803a6 6 0 0 0 8.268 8.268c.344-.215.825-.004.803.401\"/>",
  "github": "<path fill=\"currentColor\" d=\"M12 .297c-6.63 0-12 5.373-12 12c0 5.303 3.438 9.8 8.205 11.385c.6.113.82-.258.82-.577c0-.285-.01-1.04-.015-2.04c-3.338.724-4.042-1.61-4.042-1.61C4.422 18.07 3.633 17.7 3.633 17.7c-1.087-.744.084-.729.084-.729c1.205.084 1.838 1.236 1.838 1.236c1.07 1.835 2.809 1.305 3.495.998c.108-.776.417-1.305.76-1.605c-2.665-.3-5.466-1.332-5.466-5.93c0-1.31.465-2.38 1.235-3.22c-.135-.303-.54-1.523.105-3.176c0 0 1.005-.322 3.3 1.23c.96-.267 1.98-.399 3-.405c1.02.006 2.04.138 3 .405c2.28-1.552 3.285-1.23 3.285-1.23c.645 1.653.24 2.873.12 3.176c.765.84 1.23 1.91 1.23 3.22c0 4.61-2.805 5.625-5.475 5.92c.42.36.81 1.096.81 2.22c0 1.606-.015 2.896-.015 3.286c0 .315.21.69.825.57C20.565 22.092 24 17.592 24 12.297c0-6.627-5.373-12-12-12\"/>",
}

// Tokens do Nuxt UI com neutral=zinc e primary=violet, achatados: um SVG lido
// como imagem não herda variável de CSS nenhuma, então cada cor composta
// (`bg-elevated/40`, `primary/25`) já entra aqui resolvida sobre o fundo.
const THEMES = {
  light: {
    bg: '#ffffff',
    sidebar: '#fcfcfc',
    border: '#e4e4e7',
    borderAccented: '#d4d4d8',
    elevated: '#f4f4f5',
    elevatedSoft: '#fbfbfb',
    primary: '#7d52f4',
    primarySoft: '#faf8ff',
    primaryBorder: '#dccffc',
    primaryFaded: '#d6c7fc',
    text: '#3f3f46',
    highlighted: '#18181b',
    muted: '#71717a',
    dimmed: '#a1a1aa',
  },
  dark: {
    bg: '#18181b',
    sidebar: '#1c1c1f',
    border: '#27272a',
    borderAccented: '#3f3f46',
    elevated: '#27272a',
    elevatedSoft: '#1e1e21',
    primary: '#a07ff6',
    primarySoft: '#1d1c23',
    primaryBorder: '#3d3558',
    primaryFaded: '#4c4170',
    text: '#e4e4e7',
    highlighted: '#ffffff',
    muted: '#a1a1aa',
    dimmed: '#71717a',
  },
}

const FONT = "Saira, ui-sans-serif, system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif"

const esc = s => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')

// Sem webfont (SVG lido como imagem não carrega nenhuma), a largura do texto
// tem que ser estimada: é o que alinha as abas à direita e posiciona as linhas
// tracejadas ao redor do título do turno. Os fatores são generosos de
// propósito — errar pra mais só abre espaço, errar pra menos sobrepõe texto.
const textWidth = (s, size, weight = 400) =>
  s.length * size * (weight >= 600 ? 0.58 : weight >= 500 ? 0.55 : 0.52)

function text(content, x, y, opts = {}) {
  const { size = 14, fill, weight = 400, anchor = 'start', tabular = false } = opts
  const attrs = [
    `x="${round(x)}"`,
    `y="${round(y)}"`,
    `font-size="${size}"`,
    `fill="${fill}"`,
    weight !== 400 ? `font-weight="${weight}"` : '',
    anchor !== 'start' ? `text-anchor="${anchor}"` : '',
    tabular ? 'font-variant-numeric="tabular-nums"' : '',
  ].filter(Boolean).join(' ')
  // O `tail` continua a mesma linha com outro tamanho e outra cor. Vai de
  // tspan, e não de um segundo <text> posicionado: assim o espaçamento sai da
  // métrica real da fonte em vez da estimativa.
  const tail = opts.tail
    ? `<tspan dx="6" font-size="${opts.tail.size}" font-weight="${opts.tail.weight ?? 400}" fill="${opts.tail.fill}">${esc(opts.tail.content)}</tspan>`
    : ''
  return `<text ${attrs}>${esc(content)}${tail}</text>`
}

function rect(x, y, w, h, { r = 0, fill = 'none', stroke = null, strokeWidth = 1 } = {}) {
  const s = stroke ? ` stroke="${stroke}" stroke-width="${strokeWidth}"` : ''
  return `<rect x="${round(x)}" y="${round(y)}" width="${round(w)}" height="${round(h)}" rx="${r}" fill="${fill}"${s}/>`
}

function icon(name, x, y, size, color) {
  const body = ICONS[name]
  if (!body) throw new Error(`ícone desconhecido: ${name}`)
  return `<g transform="translate(${round(x)} ${round(y)}) scale(${round(size / 24, 4)})" color="${color}">${body}</g>`
}

const round = (n, digits = 2) => Number(n.toFixed(digits))

// ── Os dois mostradores, redesenhados com a geometria dos componentes ────────

// `ClassroomsUsedMinutesRing`: viewBox 36, raio 15.5, ponteiro dos minutos
// parado no topo e o das horas girando 3.6° por ponto percentual.
function ring(percent, x, y, size, color) {
  const p = Math.min(Math.max(percent, 0), 100)
  const c = 2 * Math.PI * 15.5
  const cap = p > 0 ? 'round' : 'butt'
  const s = size / 36
  return [
    `<g transform="translate(${round(x)} ${round(y)}) scale(${round(s, 4)})" stroke="${color}" fill="none">`,
    `<g transform="rotate(-90 18 18)">`,
    `<circle cx="18" cy="18" r="15.5" stroke-width="3.5" opacity="0.15"/>`,
    `<circle cx="18" cy="18" r="15.5" stroke-width="3.5" stroke-linecap="${cap}" stroke-dasharray="${round(c * p / 100)} ${round(c)}"/>`,
    `</g>`,
    `<line x1="18" y1="18" x2="18" y2="7.5" stroke-width="1.5" stroke-linecap="round"/>`,
    `<line x1="18" y1="18" x2="18" y2="11" stroke-width="2.4" stroke-linecap="round" transform="rotate(${round(p * 3.6)} 18 18)"/>`,
    `<circle cx="18" cy="18" r="1.4" fill="${color}" stroke="none"/>`,
    `</g>`,
  ].join('')
}

// `ClassroomsUsedCapacityBlocks`: 2×2, cada quadrado vale 25% dos assentos, e a
// fatia quebrada vira um quadrado esmaecido em vez de sumir.
function blocks(percent, x, y, size, t) {
  const p = Math.min(Math.max(percent, 0), 100)
  const gap = size / 14
  const cell = (size - gap) / 2
  const full = Math.floor(p / 25)
  const parts = []
  for (let i = 0; i < 4; i++) {
    const cx = x + (i % 2) * (cell + gap)
    const cy = y + Math.floor(i / 2) * (cell + gap)
    const state = i < full ? 'full' : (i === full && p % 25 > 0 ? 'partial' : 'empty')
    const fill = state === 'full' ? t.primary : state === 'partial' ? t.primaryFaded : t.elevated
    parts.push(rect(cx, cy, cell, cell, { r: cell * 0.22, fill }))
  }
  return parts.join('')
}

const ESTUD_ICON = '<rect width="24" height="24" rx="6" fill="#7c3aed"/><path fill="white" d="M 10.98 20.5Q 9.46 20.5 8.27 19.61Q 7.08 18.72 6.4 17.13Q 5.73 15.53 5.73 13.49Q 5.73 11.37 6.34 9.54Q 6.96 7.72 8.08 6.37Q 9.2 5.02 10.71 4.26Q 12.22 3.5 13.97 3.5Q 16.09 3.5 17.18 4.56Q 18.27 5.62 18.27 7.66Q 18.27 9.19 17.44 10.41Q 16.61 11.63 15.16 12.32Q 13.71 13.01 11.84 13.01Q 10.92 13.01 10.12 12.9Q 9.32 12.8 8.57 12.63L 8.65 11.63H 9.72Q 11.38 11.63 12.62 11.07Q 13.85 10.51 14.54 9.47Q 15.23 8.44 15.23 7.09Q 15.23 6.08 14.76 5.54Q 14.28 4.99 13.42 4.99Q 12.33 4.99 11.47 5.81Q 10.61 6.63 10.0 7.94Q 9.4 9.24 9.09 10.74Q 8.77 12.23 8.77 13.58Q 8.77 16.11 9.53 17.38Q 10.29 18.66 11.78 18.66Q 14.6 18.66 16.41 15.85Q 17.47 16.02 17.47 16.62Q 16.24 18.63 14.67 19.57Q 13.11 20.5 10.98 20.5Z"/>'

// Espelha `sidebarGroups` do `useSidebarNav`, no recorte que um diretor vê.
const SIDEBAR = [
  { label: 'Acadêmico', icon: 'book-marked', items: [['Campi', 'map-pin'], ['Cursos', 'notebook'], ['Grades', 'layout-list'], ['Disciplinas', 'book-open']] },
  { label: 'Secretaria', icon: 'archive', items: [['Ofertas', 'library'], ['Turmas', 'presentation'], ['Calendário', 'calendar-range'], ['Notificações', 'bell']] },
  { label: 'Pessoas', icon: 'contact', items: [['Alunos', 'graduation-cap'], ['Professores', 'user-pen'], ['Responsáveis', 'users']] },
  { label: 'Sistema', icon: 'cog', items: [['Segurança', 'shield'], ['Integrações', 'webhook'], ['Configurações', 'settings']] },
]

const DAYS = [
  { key: 'Monday', label: 'Segunda', short: 'Seg' },
  { key: 'Tuesday', label: 'Terça', short: 'Ter' },
  { key: 'Wednesday', label: 'Quarta', short: 'Qua' },
  { key: 'Thursday', label: 'Quinta', short: 'Qui' },
  { key: 'Friday', label: 'Sexta', short: 'Sex' },
  { key: 'Saturday', label: 'Sábado', short: 'Sáb' },
]
const SHIFTS = [
  { key: 'Morning', label: 'Manhã', window: '07h–12h' },
  { key: 'Afternoon', label: 'Tarde', window: '12h–18h' },
  { key: 'Evening', label: 'Noite', window: '18h–22h' },
]
const SHIFT_LABELS = Object.fromEntries(SHIFTS.map(s => [s.key, s.label]))
const DAY_LABELS = Object.fromEntries(DAYS.map(d => [d.key, d.label]))
const DAY_SHORT = Object.fromEntries(DAYS.map(d => [d.key, d.short]))

const formatRate = rate => `${rate > 0 ? Math.max(Math.round(rate), 1) : 0}%`

function formatMinutes(minutes) {
  if (minutes <= 0) return '0min'
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  if (h === 0) return `${m}min`
  if (m === 0) return `${h}h`
  return `${h}h ${m}min`
}

function formatStudents(students) {
  const n = students > 0 ? Math.max(Math.round(students), 1) : 0
  return `${n} ${n === 1 ? 'aluno' : 'alunos'} em média`
}

const cellFor = (day, shift) => data.cells.find(c => c.day === day && c.shift === shift)
const visibleDays = DAYS.filter(d => data.cells.some(c => c.day === d.key && c.open))
const visibleShifts = SHIFTS.filter(s => data.cells.some(c => c.shift === s.key && c.open))

const peakCell = data.cells.filter(c => c.open)
  .reduce((a, b) => (a === null || b.usedCapacity > a.usedCapacity ? b : a), null)
const peakStudents = peakCell && peakCell.openMinutes > 0
  ? Math.round(peakCell.usedCapacity / peakCell.openMinutes)
  : 0

// ── Geometria (os mesmos espaçamentos do componente, em px) ──────────────────
const W = 1280
const SIDEBAR_W = 208
const NAVBAR_H = 64
const PAD_X = 24
const PAD_Y = 20
const X0 = SIDEBAR_W + PAD_X
const X1 = W - PAD_X
const CONTENT_W = X1 - X0

const HEADER_Y = NAVBAR_H + PAD_Y
const STATS_Y = HEADER_Y + 76
const STATS_H = 96
const MAP_Y = STATS_Y + STATS_H + 24
const GRID_Y = MAP_Y + 40
const ROW_H = 64
const ROW_GAP = 8
const CELLS_X = X0 + 78
const COL_GAP = 8
const COL_W = (X1 - CELLS_X - COL_GAP * (visibleDays.length - 1)) / visibleDays.length
const MAP_BOTTOM = GRID_Y + 24 + visibleShifts.length * ROW_H + (visibleShifts.length - 1) * ROW_GAP
const DRILL_Y = MAP_BOTTOM + 24
const CARDS_Y = DRILL_Y + 104
const CARD_W = (CONTENT_W - 24) / 3
const CARD_H = 196
const CARD_GAP = 12
const CARD_ROWS = Math.ceil(data.classrooms.length / 3)
const H = CARDS_Y + CARD_ROWS * CARD_H + (CARD_ROWS - 1) * CARD_GAP + PAD_Y

function renderSidebar(t) {
  const out = []
  // Cantos arredondados só do lado de fora: o retângulo nasce redondo dos dois
  // lados e a faixa da direita quadra a borda que encosta no conteúdo.
  out.push(rect(0, 0, SIDEBAR_W, H, { r: 12, fill: t.sidebar }))
  out.push(rect(SIDEBAR_W - 12, 0, 12, H, { fill: t.sidebar }))
  out.push(`<line x1="${SIDEBAR_W - 0.5}" y1="0" x2="${SIDEBAR_W - 0.5}" y2="${H}" stroke="${t.border}"/>`)

  out.push(`<g transform="translate(16 20) scale(1)">${ESTUD_ICON}</g>`)
  out.push(text('Estud', 48, 42, { size: 20, weight: 600, fill: t.highlighted }))
  out.push(icon('panel-left-close', 172, 22, 20, t.muted))

  const rows = SIDEBAR.flatMap(group => [
    { type: 'group', label: group.label, icon: group.icon },
    ...group.items.map(([label, name]) => ({ type: 'item', label, icon: name })),
  ])

  rows.forEach((row, i) => {
    const top = 72 + i * 32
    const active = row.label === 'Campi'
    const color = active ? t.primary : t.text

    if (active) out.push(rect(32, top, 168, 32, { r: 6, fill: t.elevated }))

    if (row.type === 'group') {
      out.push(icon(row.icon, 20, top + 8, 16, t.text))
      out.push(text(row.label, 44, top + 21, { size: 14, weight: 500, fill: t.text }))
      out.push(icon('chevron-up', 176, top + 8, 16, t.dimmed))
    }
    else {
      out.push(icon(row.icon, 48, top + 8, 16, color))
      out.push(text(row.label, 72, top + 21, { size: 14, fill: color, weight: active ? 500 : 400 }))
    }
  })

  const footerTop = H - 56
  out.push(icon('github', 20, footerTop - 72, 16, t.text))
  out.push(text('Code', 44, footerTop - 59, { size: 14, fill: t.text }))
  out.push(icon('book-open', 20, footerTop - 40, 16, t.text))
  out.push(text('Documentação', 44, footerTop - 27, { size: 14, fill: t.text }))

  out.push(`<line x1="0" y1="${footerTop}" x2="${SIDEBAR_W}" y2="${footerTop}" stroke="${t.border}"/>`)
  out.push(text('Zaqueu Cavalcante', 16, footerTop + 26, { size: 14, weight: 500, fill: t.text }))
  out.push(text('Diretor', 16, footerTop + 44, { size: 12, fill: t.muted }))

  return out.join('')
}

function renderNavbar(t, mode) {
  const out = []
  out.push(`<line x1="${SIDEBAR_W}" y1="${NAVBAR_H - 0.5}" x2="${W}" y2="${NAVBAR_H - 0.5}" stroke="${t.border}"/>`)

  out.push(icon('map-pin', X0, 24, 16, t.muted))
  out.push(text('Campi', X0 + 22, 37, { size: 14, fill: t.muted }))
  const chevronX = X0 + 22 + textWidth('Campi', 14) + 6
  out.push(icon('chevron-right', chevronX, 24, 16, t.dimmed))
  out.push(text('Detalhes', chevronX + 22, 37, { size: 14, weight: 500, fill: t.primary }))

  out.push(icon(mode === 'dark' ? 'moon' : 'sun', W - 84, 22, 20, t.text))
  out.push(icon('bell', W - 44, 22, 20, t.text))
  return out.join('')
}

function renderHeader(t) {
  const out = []
  const name = 'Campus Agreste'
  out.push(text(name, X0, HEADER_Y + 22, { size: 24, weight: 600, fill: t.highlighted }))
  out.push(icon('pencil', X0 + textWidth(name, 24, 600) + 12, HEADER_Y + 6, 16, t.muted))
  out.push(icon('map-pin', X0, HEADER_Y + 38, 16, t.muted))
  out.push(text('Caruaru · PE', X0 + 22, HEADER_Y + 51, { size: 14, fill: t.muted }))

  const tabs = [
    { label: 'Ocupação', icon: 'layout-grid', active: true },
    { label: 'Horários', icon: 'clock' },
    { label: 'Salas', icon: 'door-open' },
  ].map(tab => ({ ...tab, w: 16 + 6 + textWidth(tab.label, 14, 500) }))

  const total = tabs.reduce((sum, tab) => sum + tab.w, 0) + 24 * (tabs.length - 1)
  let x = X1 - total
  for (const tab of tabs) {
    const color = tab.active ? t.primary : t.text
    out.push(icon(tab.icon, x, HEADER_Y + 14, 16, color))
    out.push(text(tab.label, x + 22, HEADER_Y + 27, { size: 14, weight: 500, fill: color }))
    if (tab.active) out.push(rect(x, HEADER_Y + 42, tab.w, 2, { fill: t.primary }))
    x += tab.w + 24
  }
  return out.join('')
}

function renderStats(t) {
  const out = []
  const w = (CONTENT_W - 36) / 4
  const at = i => X0 + i * (w + 12)

  for (const i of [0, 1]) {
    out.push(rect(at(i), STATS_Y, w, STATS_H, { r: 12, fill: t.primarySoft, stroke: t.primaryBorder }))
  }
  out.push(ring(data.overallUsedMinutesRate, at(0) + 16, STATS_Y + 24, 48, t.primary))
  out.push(text(formatRate(data.overallUsedMinutesRate), at(0) + 80, STATS_Y + 50, { size: 30, weight: 700, fill: t.primary, tabular: true }))
  out.push(text('Tempo usado', at(0) + 80, STATS_Y + 70, { size: 12, weight: 500, fill: t.muted }))

  out.push(blocks(data.overallUsedCapacityRate, at(1) + 16, STATS_Y + 24, 48, t))
  out.push(text(formatRate(data.overallUsedCapacityRate), at(1) + 80, STATS_Y + 50, { size: 30, weight: 700, fill: t.primary, tabular: true }))
  out.push(text('Espaço alocado', at(1) + 80, STATS_Y + 70, { size: 12, weight: 500, fill: t.muted }))

  for (const i of [2, 3]) {
    out.push(rect(at(i), STATS_Y, w, STATS_H, { r: 12, fill: t.elevatedSoft, stroke: t.border }))
  }
  out.push(text(`${DAY_SHORT[peakCell.day]} · ${SHIFT_LABELS[peakCell.shift]}`, at(2) + 16, STATS_Y + 32, { size: 14, weight: 600, fill: t.highlighted }))
  out.push(text(`~${peakStudents}`, at(2) + 16, STATS_Y + 58, {
    size: 20,
    weight: 700,
    fill: t.primary,
    tabular: true,
    tail: { content: peakStudents === 1 ? 'aluno' : 'alunos', size: 16, weight: 500, fill: t.muted },
  }))
  out.push(text('Horário de pico', at(2) + 16, STATS_Y + 78, { size: 12, fill: t.muted }))

  out.push(text(String(data.totalClassrooms), at(3) + 16, STATS_Y + 44, { size: 24, weight: 700, fill: t.highlighted, tabular: true }))
  out.push(text('Salas no campus', at(3) + 16, STATS_Y + 70, { size: 12, fill: t.muted }))
  return out.join('')
}

function renderMap(t) {
  const out = []
  out.push(icon('table-2', X0, MAP_Y + 2, 20, t.primary))
  out.push(text('Mapa de ocupação', X0 + 28, MAP_Y + 18, { size: 16, weight: 600, fill: t.highlighted }))

  const espacoW = textWidth('espaço', 12)
  const tempoW = textWidth('tempo', 12)
  out.push(text('espaço', X1 - espacoW, MAP_Y + 16, { size: 12, fill: t.muted }))
  out.push(blocks(75, X1 - espacoW - 28, MAP_Y + 2, 20, t))
  out.push(text('tempo', X1 - espacoW - 44 - tempoW, MAP_Y + 16, { size: 12, fill: t.muted }))
  out.push(ring(75, X1 - espacoW - 72 - tempoW, MAP_Y + 2, 20, t.primary))

  visibleDays.forEach((day, j) => {
    out.push(text(day.label, CELLS_X + j * (COL_W + COL_GAP) + COL_W / 2, GRID_Y + 14, { size: 14, weight: 600, fill: t.highlighted, anchor: 'middle' }))
  })

  visibleShifts.forEach((shift, i) => {
    const top = GRID_Y + 24 + i * (ROW_H + ROW_GAP)
    out.push(text(shift.label, CELLS_X - 12, top + 30, { size: 14, weight: 500, fill: t.highlighted, anchor: 'end' }))
    out.push(text(shift.window, CELLS_X - 12, top + 46, { size: 11, fill: t.muted, anchor: 'end', tabular: true }))

    visibleDays.forEach((day, j) => {
      const x = CELLS_X + j * (COL_W + COL_GAP)
      const cell = cellFor(day.key, shift.key)
      const selected = day.key === SELECTED.day && shift.key === SELECTED.shift
      out.push(rect(x, top, COL_W, ROW_H, {
        r: 8,
        fill: t.elevatedSoft,
        stroke: selected ? t.primary : t.border,
        strokeWidth: selected ? 2 : 1,
      }))
      const ringX = x + (COL_W - 88) / 2
      out.push(ring(cell.usedMinutesRate, ringX, top + 16, 32, cell.usedMinutesRate > 0 ? t.primary : t.dimmed))
      out.push(blocks(cell.usedCapacityRate, ringX + 56, top + 16, 32, t))
    })
  })
  return out.join('')
}

function renderDrilldown(t) {
  const out = []
  const cell = cellFor(SELECTED.day, SELECTED.shift)
  const cx = (X0 + X1) / 2
  const title = `${DAY_LABELS[cell.day]} · ${SHIFT_LABELS[cell.shift]}`
  const half = textWidth(title, 16, 600) / 2

  const dashed = `stroke="${t.borderAccented}" stroke-dasharray="4 4"`
  out.push(`<line x1="${X0}" y1="${DRILL_Y + 10}" x2="${round(cx - half - 16)}" y2="${DRILL_Y + 10}" ${dashed}/>`)
  out.push(`<line x1="${round(cx + half + 16)}" y1="${DRILL_Y + 10}" x2="${X1}" y2="${DRILL_Y + 10}" ${dashed}/>`)
  out.push(text(title, cx, DRILL_Y + 16, { size: 16, weight: 600, fill: t.highlighted, anchor: 'middle' }))

  const y = DRILL_Y + 40
  const groupW = 170
  let x = cx - (groupW * 2 + 40) / 2
  out.push(ring(cell.usedMinutesRate, x, y, 48, cell.usedMinutesRate > 0 ? t.primary : t.dimmed))
  out.push(text(formatRate(cell.usedMinutesRate), x + 60, y + 24, { size: 24, weight: 700, fill: t.primary, tabular: true }))
  out.push(text('Tempo usado', x + 60, y + 42, { size: 12, fill: t.muted }))

  x += groupW + 40
  out.push(blocks(cell.usedCapacityRate, x, y, 48, t))
  out.push(text(formatRate(cell.usedCapacityRate), x + 60, y + 24, { size: 24, weight: 700, fill: t.primary, tabular: true }))
  out.push(text('Espaço alocado', x + 60, y + 42, { size: 12, fill: t.muted }))

  cell.classrooms.forEach((room, i) => {
    const rx = X0 + (i % 3) * (CARD_W + CARD_GAP)
    const ry = CARDS_Y + Math.floor(i / 3) * (CARD_H + CARD_GAP)
    const color = room.usedMinutesRate > 0 ? t.primary : t.dimmed

    out.push(rect(rx, ry, CARD_W, CARD_H, { r: 8, fill: t.bg, stroke: t.border }))
    out.push(text(room.name, rx + 16, ry + 32, { size: 14, weight: 500, fill: t.highlighted }))

    out.push(ring(room.usedMinutesRate, rx + 16, ry + 52, 56, color))
    out.push(text(`${formatRate(room.usedMinutesRate)} de tempo usado`, rx + 84, ry + 78, { size: 14, weight: 500, fill: t.highlighted }))
    out.push(text(`${formatMinutes(room.usedMinutes)} de ${formatMinutes(room.availableMinutes)}`, rx + 84, ry + 96, { size: 12, fill: t.muted }))

    out.push(blocks(room.usedCapacityRate, rx + 16, ry + 124, 56, t))
    out.push(text(`${formatRate(room.usedCapacityRate)} de espaço alocado`, rx + 84, ry + 150, { size: 14, weight: 500, fill: t.highlighted }))
    out.push(text(formatStudents(room.averageStudents), rx + 84, ry + 168, { size: 12, fill: t.muted }))
  })
  return out.join('')
}

function buildSvg(mode) {
  const t = THEMES[mode]
  const body = [
    rect(0, 0, W, H, { r: 12, fill: t.bg }),
    renderSidebar(t),
    renderNavbar(t, mode),
    renderHeader(t),
    renderStats(t),
    renderMap(t),
    renderDrilldown(t),
    rect(0.5, 0.5, W - 1, H - 1, { r: 12, stroke: t.border }),
  ].join('\n')

  return [
    `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${W} ${H}" width="${W}" height="${H}" font-family="${FONT}" role="img" aria-label="Tela de ocupação de campus do Estud: mapa de uso das salas por dia e turno, com indicadores de tempo usado e espaço alocado">`,
    body,
    '</svg>',
  ].join('\n')
}

mkdirSync(OUT_DIR, { recursive: true })
for (const mode of ['light', 'dark']) {
  const file = join(OUT_DIR, `campus-${mode}.svg`)
  writeFileSync(file, `${buildSvg(mode)}\n`)
  console.log(`${file} (${W}×${H})`)
}
