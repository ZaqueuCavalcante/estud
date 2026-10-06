<script setup lang="ts">
import type { WeekEditorItem, WeekEditorSlot } from '~/types/classes'

const props = defineProps<{
  items: WeekEditorItem[]
}>()

const emit = defineEmits<{
  change: [key: number, slot: WeekEditorSlot]
  create: [slot: WeekEditorSlot]
  remove: [key: number]
  restore: [key: number]
}>()

defineSlots<{
  card(props: { item: WeekEditorItem, compact: boolean }): unknown
}>()

const HOUR_HEIGHT = 48
const GRID_START = 6 * 60
const GRID_END = 24 * 60
const MIN = OPENING_WINDOW_MIN_MINUTES
const DEFAULT_DURATION = 120
const BLOCK_GAP = 2

const days = campusWeekDays
const gridHeight = ((GRID_END - GRID_START) / 60) * HOUR_HEIGHT

const hourTicks = computed(() => {
  const ticks: { label: string, top: number }[] = []
  for (let m = GRID_START; m <= GRID_END; m += 60) {
    ticks.push({ label: formatOpeningMinutes(m), top: ((m - GRID_START) / 60) * HOUR_HEIGHT })
  }
  return ticks
})

interface Span { key: number, dayIdx: number, start: number, end: number, removed: boolean }
interface Gap { lo: number, hi: number }

const spans = computed<Span[]>(() => props.items.map(item => ({
  key: item.key,
  dayIdx: days.findIndex(d => d.key === item.day),
  start: openingHourToMinutes(item.start),
  end: openingHourToMinutes(item.end),
  removed: !!item.removed,
})))

const spanByKey = computed(() => new Map(spans.value.map(s => [s.key, s])))

const lanes = computed(() => {
  const map = new Map<number, { lane: number, lanes: number }>()
  for (let i = 0; i < days.length; i++) {
    const layout = layoutScheduleLanes(spans.value.filter(s => s.dayIdx === i))
    for (const [key, value] of layout) map.set(key, value)
  }
  return map
})

function freeGaps(dayIdx: number, exceptKey: number | null) {
  const busy = spans.value
    .filter(s => s.dayIdx === dayIdx && s.key !== exceptKey && !s.removed)
    .sort((a, b) => a.start - b.start)
  const gaps: Gap[] = []
  let cursor = GRID_START
  for (const s of busy) {
    if (s.start > cursor) gaps.push({ lo: cursor, hi: s.start })
    cursor = Math.max(cursor, s.end)
  }
  if (cursor < GRID_END) gaps.push({ lo: cursor, hi: GRID_END })
  return gaps
}

function gapAt(dayIdx: number, minute: number, exceptKey: number | null) {
  return freeGaps(dayIdx, exceptKey).find(g => g.lo <= minute && minute <= g.hi)
}

function fitStart(dayIdx: number, start: number, length: number, exceptKey: number) {
  let best: number | null = null
  for (const gap of freeGaps(dayIdx, exceptKey)) {
    if (gap.hi - gap.lo < length) continue
    const candidate = clamp(start, gap.lo, gap.hi - length)
    if (best == null || Math.abs(candidate - start) < Math.abs(best - start)) best = candidate
  }
  return best
}

function canRestore(item: WeekEditorItem) {
  const span = spanByKey.value.get(item.key)
  return !!span && fitStart(span.dayIdx, span.start, span.end - span.start, span.key) === span.start
}

function cardHeight(span: { start: number, end: number }) {
  return ((span.end - span.start) / 60) * HOUR_HEIGHT - BLOCK_GAP * 2
}

function isCompact(item: WeekEditorItem) {
  const span = spanByKey.value.get(item.key)
  return !!span && cardHeight(span) < 56
}

function blockStyle(span: { dayIdx: number, start: number, end: number }, lane = 0, laneCount = 1) {
  const col = 100 / days.length
  const width = col / laneCount
  return {
    top: `${((span.start - GRID_START) / 60) * HOUR_HEIGHT + BLOCK_GAP}px`,
    height: `${cardHeight(span)}px`,
    left: `calc(${span.dayIdx * col + lane * width}% + 2px)`,
    width: `calc(${width}% - 4px)`,
  }
}

function cardStyle(item: WeekEditorItem) {
  const span = spanByKey.value.get(item.key)!
  const layout = lanes.value.get(item.key)
  return blockStyle(span, layout?.lane, layout?.lanes)
}

// ── Arrasto ───────────────────────────────────────────────────────────────────
type DragMode = 'start' | 'end' | 'move' | 'create'

interface DragState {
  key: number | null
  mode: DragMode
  pointerY: number
  origin: { dayIdx: number, start: number, end: number }
  bounds: Gap
  moved: boolean
}

const board = useTemplateRef<HTMLElement>('board')
const drag = shallowRef<DragState | null>(null)
const ghost = ref<{ dayIdx: number, start: number, end: number } | null>(null)

function clamp(value: number, min: number, max: number) {
  return Math.min(Math.max(value, min), max)
}

function minutesAt(clientY: number) {
  const rect = board.value!.getBoundingClientRect()
  return clamp(snapToOpeningStep(GRID_START + ((clientY - rect.top) / HOUR_HEIGHT) * 60), GRID_START, GRID_END)
}

function dayIdxAt(clientX: number) {
  const rect = board.value!.getBoundingClientRect()
  return clamp(Math.floor(((clientX - rect.left) / rect.width) * days.length), 0, days.length - 1)
}

function toSlot(span: { dayIdx: number, start: number, end: number }): WeekEditorSlot {
  return {
    day: days[span.dayIdx]!.key,
    start: minutesToOpeningHour(span.start),
    end: minutesToOpeningHour(span.end),
  }
}

// O move/up fica na janela, e não no card: o card troca de coluna no meio do
// arrasto e o handle de redimensionar pode sair debaixo do ponteiro.
function beginDrag(e: PointerEvent, state: DragState) {
  if (e.button !== 0) return false
  e.preventDefault()
  e.stopPropagation()
  drag.value = state
  window.addEventListener('pointermove', onPointerMove)
  window.addEventListener('pointerup', onPointerUp)
  window.addEventListener('pointercancel', onPointerUp)
  return true
}

function endDrag() {
  drag.value = null
  ghost.value = null
  window.removeEventListener('pointermove', onPointerMove)
  window.removeEventListener('pointerup', onPointerUp)
  window.removeEventListener('pointercancel', onPointerUp)
}

onBeforeUnmount(endDrag)

function onCardPointerDown(e: PointerEvent, item: WeekEditorItem, mode: DragMode) {
  if (item.removed) {
    e.stopPropagation()
    return
  }
  const key = item.key
  const span = spanByKey.value.get(key)
  if (!span) return
  const bounds = {
    lo: gapAt(span.dayIdx, span.start, key)?.lo ?? span.start,
    hi: gapAt(span.dayIdx, span.end, key)?.hi ?? span.end,
  }
  beginDrag(e, { key, mode, pointerY: e.clientY, origin: { ...span }, bounds, moved: false })
}

// No toque, arrastar na área vazia tem que rolar a tela; o horário nasce no clique.
let lastPointerType = 'mouse'

function onBoardPointerDown(e: PointerEvent) {
  lastPointerType = e.pointerType
  if (e.pointerType === 'touch') return
  const anchor = minutesAt(e.clientY)
  const dayIdx = dayIdxAt(e.clientX)
  const bounds = gapAt(dayIdx, anchor, null)
  if (!bounds || bounds.hi - bounds.lo < MIN) return
  const started = beginDrag(e, { key: null, mode: 'create', pointerY: e.clientY, origin: { dayIdx, start: anchor, end: anchor }, bounds, moved: false })
  if (started) ghost.value = { dayIdx, ...resolveCreate(anchor, anchor + MIN, bounds) }
}

function onBoardClick(e: MouseEvent) {
  if (lastPointerType !== 'touch') return
  const start = minutesAt(e.clientY)
  const dayIdx = dayIdxAt(e.clientX)
  const bounds = gapAt(dayIdx, start, null)
  if (!bounds || bounds.hi - bounds.lo < MIN) return
  emit('create', toSlot({ dayIdx, ...resolveCreate(start, start + DEFAULT_DURATION, bounds) }))
}

function resolveCreate(anchor: number, cursor: number, bounds: Gap) {
  let start = Math.min(anchor, cursor)
  let end = Math.max(anchor, cursor)
  if (end - start < MIN) {
    if (cursor < anchor) start = end - MIN
    else end = start + MIN
  }
  if (start < bounds.lo) {
    start = bounds.lo
    end = Math.max(end, bounds.lo + MIN)
  }
  if (end > bounds.hi) {
    end = bounds.hi
    start = Math.min(start, bounds.hi - MIN)
  }
  return { start, end }
}

function onPointerMove(e: PointerEvent) {
  const state = drag.value
  if (!state) return
  const delta = snapToOpeningStep(((e.clientY - state.pointerY) / HOUR_HEIGHT) * 60)
  const { origin, bounds } = state

  if (state.mode === 'create') {
    if (delta !== 0) state.moved = true
    ghost.value = { dayIdx: origin.dayIdx, ...resolveCreate(origin.start, clamp(origin.start + delta, bounds.lo, bounds.hi), bounds) }
    return
  }

  let next = { dayIdx: origin.dayIdx, start: origin.start, end: origin.end }
  if (state.mode === 'start') {
    next.start = clamp(origin.start + delta, bounds.lo, origin.end - MIN)
  }
  else if (state.mode === 'end') {
    next.end = clamp(origin.end + delta, origin.start + MIN, bounds.hi)
  }
  else {
    const length = origin.end - origin.start
    const dayIdx = dayIdxAt(e.clientX)
    const start = fitStart(dayIdx, clamp(origin.start + delta, GRID_START, GRID_END - length), length, state.key!)
    if (start == null) return
    next = { dayIdx, start, end: start + length }
  }

  const current = spanByKey.value.get(state.key!)
  if (current && current.dayIdx === next.dayIdx && current.start === next.start && current.end === next.end) return
  if (next.dayIdx !== origin.dayIdx || next.start !== origin.start || next.end !== origin.end) state.moved = true
  emit('change', state.key!, toSlot(next))
}

function onPointerUp() {
  const state = drag.value
  if (state?.mode === 'create') {
    const span = state.moved
      ? ghost.value!
      : { dayIdx: state.origin.dayIdx, ...resolveCreate(state.origin.start, state.origin.start + DEFAULT_DURATION, state.bounds) }
    emit('create', toSlot(span))
  }
  endDrag()
}

// ── Teclado ───────────────────────────────────────────────────────────────────
function onCardKeydown(e: KeyboardEvent, item: WeekEditorItem) {
  if (e.target !== e.currentTarget || item.removed) return
  const key = item.key
  const span = spanByKey.value.get(key)
  if (!span) return

  if (e.key === 'Delete' || e.key === 'Backspace') {
    e.preventDefault()
    emit('remove', key)
    return
  }

  const step = e.shiftKey ? 60 : OPENING_HOUR_STEP
  const length = span.end - span.start
  let next: { dayIdx: number, start: number, end: number } | null = null

  if (e.key === 'ArrowUp' || e.key === 'ArrowDown') {
    const lo = gapAt(span.dayIdx, span.start, key)?.lo ?? span.start
    const hi = gapAt(span.dayIdx, span.end, key)?.hi ?? span.end
    const start = clamp(span.start + (e.key === 'ArrowUp' ? -step : step), lo, hi - length)
    next = { dayIdx: span.dayIdx, start, end: start + length }
  }
  else if (e.key === 'ArrowLeft' || e.key === 'ArrowRight') {
    const dayIdx = clamp(span.dayIdx + (e.key === 'ArrowLeft' ? -1 : 1), 0, days.length - 1)
    const start = fitStart(dayIdx, span.start, length, key)
    if (start != null) next = { dayIdx, start, end: start + length }
  }

  if (!next) return
  e.preventDefault()
  emit('change', key, toSlot(next))
}

function cardLabel(item: WeekEditorItem) {
  const day = days.find(d => d.key === item.day)?.label ?? item.day
  const time = `${day}, das ${formatOpeningHour(item.start)} às ${formatOpeningHour(item.end)}.`
  return item.removed ? `${time} Removido.` : `${time} Setas movem, Delete remove.`
}
</script>

<template>
  <div class="overflow-x-auto overflow-y-hidden pb-2">
    <div class="flex min-w-160" :class="drag ? 'select-none' : ''">
      <div class="w-12 shrink-0">
        <div class="h-9" />
        <div class="relative" :style="{ height: `${gridHeight}px` }">
          <div
            v-for="tick in hourTicks"
            :key="tick.label"
            class="absolute right-2 -translate-y-1/2 text-xs tabular-nums text-muted"
            :style="{ top: `${tick.top}px` }"
          >
            {{ tick.label }}
          </div>
        </div>
      </div>

      <div class="min-w-0 flex-1">
        <div class="grid h-9 items-center" :style="{ gridTemplateColumns: `repeat(${days.length}, minmax(0, 1fr))` }">
          <span
            v-for="day in days"
            :key="day.key"
            class="text-center text-sm font-semibold text-highlighted"
          >
            {{ day.label }}
          </span>
        </div>

        <div
          ref="board"
          class="relative cursor-copy overflow-hidden rounded-lg border border-default"
          :style="{ height: `${gridHeight}px` }"
          @pointerdown="(e: PointerEvent) => { onBoardPointerDown(e) }"
          @click="(e: MouseEvent) => { onBoardClick(e) }"
        >
          <div
            class="pointer-events-none absolute inset-0 grid"
            :style="{ gridTemplateColumns: `repeat(${days.length}, minmax(0, 1fr))` }"
          >
            <div
              v-for="(day, dayIdx) in days"
              :key="day.key"
              :class="dayIdx > 0 ? 'border-l border-default' : ''"
            />
          </div>

          <div
            v-for="tick in hourTicks.slice(1, -1)"
            :key="tick.label"
            class="pointer-events-none absolute inset-x-0 border-t border-default/60"
            :style="{ top: `${tick.top}px` }"
          />

          <div
            v-for="item in items"
            :key="item.key"
            class="group absolute touch-none overflow-hidden rounded-md border px-2 py-1 focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-primary"
            :class="[
              item.colorClass,
              item.removed ? 'cursor-default' : 'cursor-grab shadow-sm',
              drag?.key === item.key ? 'z-20 cursor-grabbing shadow-md' : '',
            ]"
            :style="cardStyle(item)"
            tabindex="0"
            role="group"
            :aria-label="cardLabel(item)"
            @pointerdown="(e: PointerEvent) => { onCardPointerDown(e, item, 'move') }"
            @click="(e: MouseEvent) => { e.stopPropagation() }"
            @keydown="(e: KeyboardEvent) => { onCardKeydown(e, item) }"
          >
            <slot name="card" :item="item" :compact="isCompact(item)" />

            <div
              v-for="edge in (item.removed ? [] : ['start', 'end'] as const)"
              :key="edge"
              class="absolute inset-x-0 h-2 cursor-ns-resize"
              :class="edge === 'start' ? 'top-0' : 'bottom-0'"
              @pointerdown="(e: PointerEvent) => { onCardPointerDown(e, item, edge) }"
            />

            <UButton
              :icon="item.removed ? 'i-lucide-undo-2' : 'i-lucide-x'"
              color="neutral"
              variant="ghost"
              size="xs"
              class="absolute right-0.5 top-0.5 opacity-0 transition-opacity group-hover:opacity-100 group-focus-visible:opacity-100"
              :disabled="item.removed && !canRestore(item)"
              :title="item.removed && !canRestore(item) ? 'Outro horário ocupa este lugar' : undefined"
              :aria-label="item.removed ? 'Restaurar horário' : 'Remover horário'"
              @pointerdown="(e: PointerEvent) => { e.stopPropagation() }"
              @click="(e: MouseEvent) => { e.stopPropagation(); item.removed ? emit('restore', item.key) : emit('remove', item.key) }"
            />
          </div>

          <div
            v-if="ghost"
            class="pointer-events-none absolute flex items-start justify-center rounded-md border-2 border-dashed border-primary bg-primary/15 pt-1 text-xs font-semibold tabular-nums text-highlighted"
            :style="blockStyle(ghost)"
          >
            {{ formatOpeningMinutes(ghost.start) }} – {{ formatOpeningMinutes(ghost.end) }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
