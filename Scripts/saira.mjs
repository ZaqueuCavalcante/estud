// Lê os WOFF do Saira e devolve o texto já como contorno vetorial.
//
// O SVG do README é lido pelo GitHub como imagem, e a CSP que ele serve
// (`default-src 'none'`) bloqueia qualquer webfont — inclusive um @font-face
// embutido em data URI. Sem contorno, o texto cai na fonte do sistema e a
// imagem deixa de ser a tela que a landing mostra.

import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { inflateSync } from 'node:zlib'

const CACHE = join(dirname(fileURLToPath(import.meta.url)), '.fonts')
const WEIGHTS = [400, 500, 600, 700]
const SUBSET = 'latin'
const CDN = w => `https://cdn.jsdelivr.net/npm/@fontsource/saira/files/saira-${SUBSET}-${w}-normal.woff`

function readTables(buf) {
  if (buf.toString('ascii', 0, 4) !== 'wOFF') throw new Error('arquivo não é um WOFF')
  const tables = {}
  const count = buf.readUInt16BE(12)
  for (let i = 0; i < count; i++) {
    const e = 44 + i * 20
    const off = buf.readUInt32BE(e + 4)
    const comp = buf.readUInt32BE(e + 8)
    const orig = buf.readUInt32BE(e + 12)
    const raw = buf.subarray(off, off + comp)
    tables[buf.toString('ascii', e, e + 4)] = comp < orig ? inflateSync(raw) : raw.subarray(0, orig)
  }
  return tables
}

function readCmap(t) {
  let best = null
  for (let i = 0; i < t.readUInt16BE(2); i++) {
    const platform = t.readUInt16BE(4 + i * 8)
    const encoding = t.readUInt16BE(6 + i * 8)
    const score = platform === 3 && encoding === 10 ? 3 : platform === 3 && encoding === 1 ? 2 : platform === 0 ? 1 : 0
    if (score > (best?.score ?? 0)) best = { score, off: t.readUInt32BE(8 + i * 8) }
  }
  if (!best) throw new Error('cmap sem subtabela unicode')

  const map = new Map()
  const s = best.off
  const format = t.readUInt16BE(s)

  if (format === 4) {
    const segX2 = t.readUInt16BE(s + 6)
    const ends = s + 14
    const starts = ends + segX2 + 2
    const deltas = starts + segX2
    const ranges = deltas + segX2
    for (let i = 0; i < segX2 / 2; i++) {
      const end = t.readUInt16BE(ends + i * 2)
      const start = t.readUInt16BE(starts + i * 2)
      const delta = t.readInt16BE(deltas + i * 2)
      const rangeOff = t.readUInt16BE(ranges + i * 2)
      for (let c = start; c <= end && c !== 0xFFFF; c++) {
        let g
        if (rangeOff === 0) g = (c + delta) & 0xFFFF
        else {
          const at = ranges + i * 2 + rangeOff + (c - start) * 2
          if (at + 1 >= t.length) continue
          g = t.readUInt16BE(at)
          if (g !== 0) g = (g + delta) & 0xFFFF
        }
        if (g) map.set(c, g)
      }
    }
  }
  else if (format === 12) {
    for (let i = 0; i < t.readUInt32BE(s + 12); i++) {
      const g = s + 16 + i * 12
      const start = t.readUInt32BE(g)
      const end = t.readUInt32BE(g + 4)
      const gid = t.readUInt32BE(g + 8)
      for (let c = start; c <= end; c++) map.set(c, gid + (c - start))
    }
  }
  else throw new Error(`cmap formato ${format} não suportado`)

  return map
}

function parse(buf) {
  const t = readTables(buf)
  const numGlyphs = t.maxp.readUInt16BE(4)
  const numHMetrics = t.hhea.readUInt16BE(34)
  const longLoca = t.head.readInt16BE(50) === 1

  const loca = []
  for (let i = 0; i <= numGlyphs; i++)
    loca.push(longLoca ? t.loca.readUInt32BE(i * 4) : t.loca.readUInt16BE(i * 2) * 2)

  const advance = []
  for (let i = 0; i < numGlyphs; i++)
    advance.push(t.hmtx.readUInt16BE(Math.min(i, numHMetrics - 1) * 4))

  return {
    unitsPerEm: t.head.readUInt16BE(18),
    ascender: t.hhea.readInt16BE(4),
    descender: t.hhea.readInt16BE(6),
    cmap: readCmap(t.cmap),
    glyf: t.glyf,
    loca,
    advance,
  }
}

function contours(font, gid, depth = 0) {
  const start = font.loca[gid]
  const end = font.loca[gid + 1]
  if (start >= end) return []

  const g = font.glyf.subarray(start, end)
  const count = g.readInt16BE(0)

  if (count < 0) {
    if (depth > 4) return []
    const out = []
    let p = 10
    for (;;) {
      const flags = g.readUInt16BE(p)
      const index = g.readUInt16BE(p + 2)
      p += 4

      let dx, dy
      if (flags & 0x1) { dx = g.readInt16BE(p); dy = g.readInt16BE(p + 2); p += 4 }
      else { dx = g.readInt8(p); dy = g.readInt8(p + 1); p += 2 }
      if (!(flags & 0x2)) { dx = 0; dy = 0 }

      const f2dot14 = i => g.readInt16BE(i) / 16384
      let [a, b, c, d] = [1, 0, 0, 1]
      if (flags & 0x8) { a = d = f2dot14(p); p += 2 }
      else if (flags & 0x40) { a = f2dot14(p); d = f2dot14(p + 2); p += 4 }
      else if (flags & 0x80) { a = f2dot14(p); b = f2dot14(p + 2); c = f2dot14(p + 4); d = f2dot14(p + 6); p += 8 }

      for (const contour of contours(font, index, depth + 1)) {
        out.push(contour.map(pt => ({
          on: pt.on,
          x: a * pt.x + c * pt.y + dx,
          y: b * pt.x + d * pt.y + dy,
        })))
      }
      if (!(flags & 0x20)) break
    }
    return out
  }

  const ends = []
  for (let i = 0; i < count; i++) ends.push(g.readUInt16BE(10 + i * 2))
  const total = ends[count - 1] + 1

  let p = 10 + count * 2
  p += 2 + g.readUInt16BE(p)

  const flags = []
  while (flags.length < total) {
    const f = g.readUInt8(p++)
    flags.push(f)
    if (f & 0x8) { let repeat = g.readUInt8(p++); while (repeat-- > 0) flags.push(f) }
  }

  const deltas = (shortBit, sameBit) => {
    const out = []
    let v = 0
    for (const f of flags) {
      if (f & shortBit) { const d = g.readUInt8(p++); v += (f & sameBit) ? d : -d }
      else if (!(f & sameBit)) { v += g.readInt16BE(p); p += 2 }
      out.push(v)
    }
    return out
  }
  const xs = deltas(0x2, 0x10)
  const ys = deltas(0x4, 0x20)

  const out = []
  let from = 0
  for (const end of ends) {
    const points = []
    for (let i = from; i <= end; i++) points.push({ on: !!(flags[i] & 0x1), x: xs[i], y: ys[i] })
    if (points.length) out.push(points)
    from = end + 1
  }
  return out
}

async function load() {
  mkdirSync(CACHE, { recursive: true })
  const fonts = {}
  for (const weight of WEIGHTS) {
    const file = join(CACHE, `saira-${SUBSET}-${weight}.woff`)
    if (!existsSync(file)) {
      const res = await fetch(CDN(weight))
      if (!res.ok) throw new Error(`falha ao baixar Saira ${weight}: HTTP ${res.status}`)
      writeFileSync(file, Buffer.from(await res.arrayBuffer()))
      console.log(`baixado ${file}`)
    }
    fonts[weight] = parse(readFileSync(file))
  }
  return fonts
}

const round = n => Number(n.toFixed(1))

// O peso pedido cai no arquivo estático mais próximo, do mesmo jeito que o
// browser resolveria — a landing carrega 300/400/500/600/700 do Google.
const pick = (fonts, weight) =>
  fonts[WEIGHTS.reduce((a, b) => (Math.abs(b - weight) < Math.abs(a - weight) ? b : a))] ?? fonts[weight]

// O contorno sai em unidades da fonte, com o y pra cima: quem desenha aplica
// `scale(size/upem, -size/upem)` e reaproveita o mesmo <path> em cada tamanho.
function outline(font, gid) {
  const parts = []
  for (const points of contours(font, gid)) {
    const at = i => points[(i + points.length) % points.length]

    let first = points.findIndex(p => p.on)
    let start
    if (first < 0) {
      const [a, b] = [at(0), at(1)]
      start = { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 }
      first = 0
    }
    else start = at(first)

    const d = [`M${round(start.x)} ${round(start.y)}`]
    for (let k = 1; k <= points.length; k++) {
      const p = at(first + k)
      if (p.on) { d.push(`L${round(p.x)} ${round(p.y)}`); continue }
      const next = at(first + k + 1)
      const to = next.on ? next : { x: (p.x + next.x) / 2, y: (p.y + next.y) / 2 }
      d.push(`Q${round(p.x)} ${round(p.y)} ${round(to.x)} ${round(to.y)}`)
      if (next.on) k++
    }
    parts.push(`${d.join('')}Z`)
  }
  return parts.join('')
}

export async function loadSaira() {
  const fonts = await load()
  const unitsPerEm = fonts[400].unitsPerEm

  const width = (str, size, weight = 400) => {
    const font = pick(fonts, weight)
    let units = 0
    for (const ch of str) units += font.advance[font.cmap.get(ch.codePointAt(0)) ?? 0] ?? 0
    return units * size / font.unitsPerEm
  }

  const glyphs = (str, weight = 400) => {
    const font = pick(fonts, weight)
    const out = []
    let pen = 0
    for (const ch of str) {
      const gid = font.cmap.get(ch.codePointAt(0)) ?? 0
      if (font.loca[gid] < font.loca[gid + 1]) out.push({ gid, x: pen })
      pen += font.advance[gid] ?? 0
    }
    return out
  }

  const outlineOf = (gid, weight = 400) => outline(pick(fonts, weight), gid)

  return { width, glyphs, outlineOf, unitsPerEm }
}
