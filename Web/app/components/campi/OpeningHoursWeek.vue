<script setup lang="ts">
import type { OpeningHoursDay } from '~/types/campi'

const props = withDefaults(defineProps<{
  days: OpeningHoursDay[]
  editing?: boolean
}>(), { editing: false })

const emit = defineEmits<{ 'update:days': [OpeningHoursDay[]] }>()

const HOUR_HEIGHT = 48
const HEADER_HEIGHT = 36
const BLOCK_GAP = 3
const MIN = OPENING_WINDOW_MIN_MINUTES

const isMobile = useIsMobile()

interface Slot { start: number, end: number }

function slotKey(dayKey: string, shiftKey: string) {
  return `${dayKey}|${shiftKey}`
}

// O backend garante que cada janela cabe inteira num turno, mas recortar pela
// faixa em vez de descartar mantém legível qualquer linha gravada antes dessa
// regra existir.
const slots = computed(() => {
  const map = new Map<string, Slot>()
  for (const day of props.days) {
    for (const interval of day.windows) {
      const start = openingHourToMinutes(interval.start)
      const end = openingHourToMinutes(interval.end)
      for (const band of campusShiftBands) {
        const from = Math.max(start, band.start)
        const to = Math.min(end, band.end)
        if (to - from < OPENING_HOUR_STEP) continue
        const key = slotKey(day.day, band.key)
        const current = map.get(key)
        map.set(key, current
          ? { start: Math.min(current.start, from), end: Math.max(current.end, to) }
          : { start: from, end: to })
      }
    }
  }
  return map
})

function slotAt(dayKey: string, shiftKey: string) {
  return slots.value.get(slotKey(dayKey, shiftKey))
}

// A semana como estava quando a edição começou. É contra ela que o card decide
// se já divergiu do que está salvo — arrastar de volta pro horário original
// devolve a borda sólida.
const baseline = shallowRef(new Map<string, Slot>())

watch(() => props.editing, (editing) => {
  if (editing) baseline.value = new Map(slots.value)
}, { immediate: true })

function isPending(dayKey: string, shiftKey: string) {
  if (!props.editing) return false
  const slot = slots.value.get(slotKey(dayKey, shiftKey))
  if (!slot) return false
  const saved = baseline.value.get(slotKey(dayKey, shiftKey))
  return !saved || saved.start !== slot.start || saved.end !== slot.end
}

function commit(next: Map<string, Slot>) {
  emit('update:days', campusWeekDays.map(day => ({
    day: day.key,
    windows: campusShiftBands
      .map(band => next.get(slotKey(day.key, band.key)))
      .filter((slot): slot is Slot => slot !== undefined)
      .map(slot => ({
        start: minutesToOpeningHour(slot.start),
        end: minutesToOpeningHour(slot.end),
      })),
  })))
}

function setSlot(dayKey: string, shiftKey: string, slot: Slot) {
  const next = new Map(slots.value)
  next.set(slotKey(dayKey, shiftKey), slot)
  commit(next)
}

function clearSlot(dayKey: string, shiftKey: string) {
  const next = new Map(slots.value)
  next.delete(slotKey(dayKey, shiftKey))
  commit(next)
}

function copyDayToWeekdays(sourceKey: string) {
  const next = new Map(slots.value)
  for (const day of campusWeekDays) {
    if (day.key === sourceKey || day.key === 'Saturday') continue
    for (const band of campusShiftBands) {
      const source = slots.value.get(slotKey(sourceKey, band.key))
      if (source) next.set(slotKey(day.key, band.key), { ...source })
      else next.delete(slotKey(day.key, band.key))
    }
  }
  commit(next)
}

// ── Geometria ─────────────────────────────────────────────────────────────────
const selectedDayKey = ref(campusWeekDays[0]!.key)

const visibleDays = computed(() =>
  isMobile.value
    ? campusWeekDays.filter(day => day.key === selectedDayKey.value)
    : campusWeekDays,
)

const dayColumns = computed(() => `repeat(${visibleDays.value.length}, minmax(0, 1fr))`)

function bandHeight(band: typeof campusShiftBands[number]) {
  return ((band.end - band.start) / 60) * HOUR_HEIGHT
}

function bandOf(shiftKey: string) {
  return campusShiftBands.find(band => band.key === shiftKey)!
}

function hourTicks(band: typeof campusShiftBands[number]) {
  const ticks: { label: string, top: number }[] = []
  for (let m = band.start; m <= band.end; m += 60) {
    ticks.push({ label: formatOpeningMinutes(m), top: ((m - band.start) / 60) * HOUR_HEIGHT })
  }
  return ticks
}

// Cada faixa rotula da sua hora inicial à anterior ao fim, senão a divisa entre
// duas faixas ganharia o mesmo horário duas vezes. Só a última fecha a conta.
function axisTicks(band: typeof campusShiftBands[number], bandIdx: number) {
  const ticks = hourTicks(band)
  return bandIdx === campusShiftBands.length - 1 ? ticks : ticks.slice(0, -1)
}

function slotStyle(band: typeof campusShiftBands[number], slot: Slot) {
  const top = ((slot.start - band.start) / 60) * HOUR_HEIGHT
  const height = ((slot.end - slot.start) / 60) * HOUR_HEIGHT
  return {
    top: `${top + BLOCK_GAP}px`,
    height: `${height - BLOCK_GAP * 2}px`,
  }
}

function slotHeight(slot: Slot) {
  return ((slot.end - slot.start) / 60) * HOUR_HEIGHT - BLOCK_GAP * 2
}

function isCompact(slot: Slot) {
  return slotHeight(slot) < 50
}

// No mínimo de meia hora o card tem 18px e os dois handles ocupam tudo. Eles
// continuam pegando o arrasto, mas sem a marca não escondem o horário.
function showsGrip(slot: Slot) {
  return slotHeight(slot) >= 40
}

function isClosedDay(dayKey: string) {
  return campusShiftBands.every(band => !slotAt(dayKey, band.key))
}

// A borda é o único sinal do card, e ela é sempre primary: o traço diz se o card
// diverge do salvo, e a opacidade diz o quanto ele pede atenção — o topo da rampa
// fica pro card sob a mão ou sob o foco. Nada de anel por fora, que desenharia um
// segundo contorno em volta da tracejada.
function cardClass(dayKey: string, shiftKey: string) {
  if (!props.editing) return ''

  const base = 'touch-none border-2 focus-visible:border-primary focus-visible:outline-none'
  const style = isPending(dayKey, shiftKey) ? 'border-dashed' : 'border-solid'

  if (isDragging(dayKey, shiftKey)) return `${base} ${style} border-primary cursor-grabbing`

  const color = isPending(dayKey, shiftKey) ? 'border-primary/70' : 'border-primary/40'
  return `${base} ${style} ${color} cursor-grab`
}

// ── Arrasto ───────────────────────────────────────────────────────────────────
type DragMode = 'start' | 'end' | 'move' | 'create'

interface DragState {
  dayKey: string
  shiftKey: string
  mode: DragMode
  pointerY: number
  origin: Slot
  anchor: number
  moved: boolean
}

const drag = shallowRef<DragState | null>(null)

function isDragging(dayKey: string, shiftKey: string) {
  return drag.value?.dayKey === dayKey && drag.value?.shiftKey === shiftKey
}

function clamp(value: number, min: number, max: number) {
  return Math.min(Math.max(value, min), max)
}

function beginDrag(e: PointerEvent, state: DragState) {
  if (!props.editing || e.button !== 0) return false
  e.preventDefault()
  e.stopPropagation();
  (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId)
  drag.value = state
  return true
}

function onResizePointerDown(e: PointerEvent, dayKey: string, shiftKey: string, mode: DragMode) {
  const slot = slotAt(dayKey, shiftKey)
  if (!slot) return
  beginDrag(e, { dayKey, shiftKey, mode, pointerY: e.clientY, origin: slot, anchor: 0, moved: false })
}

function onCellPointerDown(e: PointerEvent, dayKey: string, shiftKey: string) {
  if (isMobile.value || slotAt(dayKey, shiftKey)) return
  const band = bandOf(shiftKey)
  const rect = (e.currentTarget as HTMLElement).getBoundingClientRect()
  const anchor = clamp(
    snapToOpeningStep(band.start + ((e.clientY - rect.top) / HOUR_HEIGHT) * 60),
    band.start,
    band.end,
  )
  const started = beginDrag(e, {
    dayKey,
    shiftKey,
    mode: 'create',
    pointerY: e.clientY,
    origin: { start: anchor, end: anchor },
    anchor,
    moved: false,
  })
  if (started) setSlot(dayKey, shiftKey, resolveCreate(band, anchor, anchor))
}

function resolveCreate(band: typeof campusShiftBands[number], anchor: number, cursor: number): Slot {
  let start = Math.min(anchor, cursor)
  let end = Math.max(anchor, cursor)
  if (end - start < MIN) {
    if (cursor < anchor) start = end - MIN
    else end = start + MIN
  }
  if (start < band.start) {
    start = band.start
    end = Math.max(end, band.start + MIN)
  }
  if (end > band.end) {
    end = band.end
    start = Math.min(start, band.end - MIN)
  }
  return { start, end }
}

function onDragPointerMove(e: PointerEvent) {
  const state = drag.value
  if (!state) return
  const band = bandOf(state.shiftKey)
  const delta = snapToOpeningStep(((e.clientY - state.pointerY) / HOUR_HEIGHT) * 60)
  if (delta !== 0) state.moved = true

  let next: Slot
  if (state.mode === 'start') {
    next = { start: clamp(state.origin.start + delta, band.start, state.origin.end - MIN), end: state.origin.end }
  }
  else if (state.mode === 'end') {
    next = { start: state.origin.start, end: clamp(state.origin.end + delta, state.origin.start + MIN, band.end) }
  }
  else if (state.mode === 'move') {
    const span = state.origin.end - state.origin.start
    const start = clamp(state.origin.start + delta, band.start, band.end - span)
    next = { start, end: start + span }
  }
  else {
    next = resolveCreate(band, state.anchor, clamp(state.anchor + delta, band.start, band.end))
  }

  setSlot(state.dayKey, state.shiftKey, next)
}

// Clique sem arrasto abre o turno inteiro no horário padrão — é o que o gestor
// quer quase sempre, e a célula vazia anuncia esse horário antes do clique.
function onDragPointerUp() {
  const state = drag.value
  if (state?.mode === 'create' && !state.moved) {
    const band = bandOf(state.shiftKey)
    setSlot(state.dayKey, state.shiftKey, { start: band.defaultStart, end: band.defaultEnd })
  }
  drag.value = null
}

function openDefault(dayKey: string, shiftKey: string) {
  const band = bandOf(shiftKey)
  setSlot(dayKey, shiftKey, { start: band.defaultStart, end: band.defaultEnd })
}

// No desktop o toque na célula vazia já virou arrasto e o pointerup resolveu; no
// mobile não há arrasto pra criar, então o clique é o único caminho.
function onCellClick(dayKey: string, shiftKey: string) {
  if (!props.editing || !isMobile.value || slotAt(dayKey, shiftKey)) return
  openDefault(dayKey, shiftKey)
}

// ── Teclado ───────────────────────────────────────────────────────────────────
function onHandleKeydown(e: KeyboardEvent, dayKey: string, shiftKey: string, edge: 'start' | 'end') {
  const slot = slotAt(dayKey, shiftKey)
  if (!slot) return
  const band = bandOf(shiftKey)
  const step = e.shiftKey ? 60 : OPENING_HOUR_STEP

  let next: Slot | null = null
  if (e.key === 'ArrowUp' || e.key === 'ArrowDown') {
    const delta = e.key === 'ArrowUp' ? -step : step
    next = edge === 'start'
      ? { start: clamp(slot.start + delta, band.start, slot.end - MIN), end: slot.end }
      : { start: slot.start, end: clamp(slot.end + delta, slot.start + MIN, band.end) }
  }
  else if (e.key === 'Home') {
    next = edge === 'start' ? { ...slot, start: band.start } : { ...slot, end: slot.start + MIN }
  }
  else if (e.key === 'End') {
    next = edge === 'start' ? { ...slot, start: slot.end - MIN } : { ...slot, end: band.end }
  }

  if (!next) return
  e.preventDefault()
  setSlot(dayKey, shiftKey, next)
}

function onCardKeydown(e: KeyboardEvent, dayKey: string, shiftKey: string) {
  if (e.key !== 'Delete' && e.key !== 'Backspace') return
  e.preventDefault()
  clearSlot(dayKey, shiftKey)
}

function handleLabel(band: typeof campusShiftBands[number], edge: 'start' | 'end') {
  return `${edge === 'start' ? 'Abertura' : 'Fechamento'} do turno da ${band.label.toLowerCase()}`
}
</script>

<template>
  <div class="flex flex-col gap-3">
    <!-- No mobile as seis colunas ficam estreitas demais pra acertar um handle,
         então o editor mostra um dia por vez. -->
    <div v-if="isMobile" class="flex flex-wrap gap-1">
      <UButton
        v-for="day in campusWeekDays"
        :key="day.key"
        :label="day.short"
        :color="day.key === selectedDayKey ? 'primary' : 'neutral'"
        :variant="day.key === selectedDayKey ? 'solid' : 'ghost'"
        size="xs"
        class="shrink-0"
        @click="() => { selectedDayKey = day.key }"
      >
        <template #trailing>
          <span
            class="size-1.5 rounded-full"
            :class="isClosedDay(day.key) ? 'bg-transparent' : 'bg-current opacity-60'"
          />
        </template>
      </UButton>
    </div>

    <div class="flex" :class="drag ? 'select-none' : ''">
      <!-- Eixo de horas. Acompanha a altura das faixas ao lado, inclusive as
           divisas de 2px, senão as horas saem do lugar faixa a faixa. -->
      <div class="w-14 shrink-0">
        <div :style="{ height: `${HEADER_HEIGHT}px` }" />
        <div
          v-for="(band, bandIdx) in campusShiftBands"
          :key="band.key"
          class="relative"
          :class="bandIdx > 0 ? 'border-t-2 border-transparent' : ''"
          :style="{ height: `${bandHeight(band)}px` }"
        >
          <div
            v-for="tick in axisTicks(band, bandIdx)"
            :key="tick.label"
            class="absolute right-2 -translate-y-1/2 text-xs tabular-nums text-muted"
            :style="{ top: `${tick.top}px` }"
          >
            {{ tick.label }}
          </div>
        </div>
      </div>

      <div class="min-w-0 flex-1">
        <!-- Cabeçalho: dias da semana -->
        <div
          class="grid items-center"
          :style="{ gridTemplateColumns: dayColumns, height: `${HEADER_HEIGHT}px` }"
        >
          <div
            v-for="day in visibleDays"
            :key="day.key"
            class="flex items-center justify-center gap-1 px-1"
          >
            <span class="flex flex-col items-center leading-tight">
              <span class="text-sm font-semibold text-highlighted">
                <span class="md:hidden">{{ day.short }}</span>
                <span class="hidden md:inline">{{ day.label }}</span>
              </span>
              <span v-if="!editing && isClosedDay(day.key)" class="text-[10px] text-dimmed">
                fechado
              </span>
            </span>
            <UTooltip v-if="editing && day.key !== 'Saturday'" :text="`Replicar para semana inteira`" :content="{ side: 'top' }">
              <UButton
                icon="i-lucide-copy"
                color="neutral"
                variant="ghost"
                size="xs"
                :aria-label="`Replicar para semana inteira`"
                @click="(e: MouseEvent) => { (e.currentTarget as HTMLElement).blur(); copyDayToWeekdays(day.key) }"
              />
            </UTooltip>
          </div>
        </div>

        <!-- Faixas de turno. A divisa entre elas é a parede que o card não
             atravessa: o backend recusa janela que cruze dois turnos. -->
        <div class="overflow-hidden rounded-lg border border-default">
          <div
            v-for="(band, bandIdx) in campusShiftBands"
            :key="band.key"
            class="grid"
            :class="bandIdx > 0 ? 'border-t-2 border-default' : ''"
            :style="{ gridTemplateColumns: dayColumns }"
          >
            <div
              v-for="(day, dayIdx) in visibleDays"
              :key="day.key"
              class="relative"
              :class="[
                dayIdx > 0 ? 'border-l border-default' : '',
                !slotAt(day.key, band.key)
                  ? 'bg-elevated/40 [background-image:repeating-linear-gradient(45deg,transparent,transparent_5px,var(--ui-border)_5px,var(--ui-border)_6px)]'
                  : '',
                editing && !slotAt(day.key, band.key) ? 'group cursor-copy' : '',
                editing && !slotAt(day.key, band.key) && !isMobile ? 'touch-none' : '',
              ]"
              :style="{ height: `${bandHeight(band)}px` }"
              :role="editing && !slotAt(day.key, band.key) ? 'button' : undefined"
              :tabindex="editing && !slotAt(day.key, band.key) ? 0 : undefined"
              :aria-label="editing && !slotAt(day.key, band.key)
                ? `Abrir ${day.label} no turno da ${band.label.toLowerCase()}`
                : undefined"
              @pointerdown="(e: PointerEvent) => { onCellPointerDown(e, day.key, band.key) }"
              @pointermove="(e: PointerEvent) => { onDragPointerMove(e) }"
              @pointerup="() => { onDragPointerUp() }"
              @pointercancel="() => { onDragPointerUp() }"
              @click="() => { onCellClick(day.key, band.key) }"
              @keydown.enter.prevent="() => { openDefault(day.key, band.key) }"
              @keydown.space.prevent="() => { openDefault(day.key, band.key) }"
            >
              <div
                v-for="tick in hourTicks(band).slice(1, -1)"
                :key="tick.label"
                class="pointer-events-none absolute inset-x-0 border-t border-default/60"
                :style="{ top: `${tick.top}px` }"
              />

              <!-- Célula vazia em edição anuncia o horário que o clique cria,
                   pra ninguém ser surpreendido pelo padrão. -->
              <span
                v-if="editing && !slotAt(day.key, band.key)"
                class="pointer-events-none absolute inset-0 flex flex-col items-center justify-center gap-1 text-dimmed opacity-0 transition-opacity group-hover:opacity-100 group-focus:opacity-100"
              >
                <UIcon name="i-lucide-plus" class="size-4" />
                <span class="text-[11px] tabular-nums">
                  {{ formatOpeningMinutes(band.defaultStart) }}–{{ formatOpeningMinutes(band.defaultEnd) }}
                </span>
              </span>

              <div
                v-if="slotAt(day.key, band.key)"
                class="absolute inset-x-1 overflow-hidden rounded-md bg-primary/15 text-center shadow-sm"
                :class="cardClass(day.key, band.key)"
                :style="slotStyle(band, slotAt(day.key, band.key)!)"
                :tabindex="editing ? 0 : undefined"
                :role="editing ? 'group' : undefined"
                :aria-label="editing
                  ? `${day.label}, turno da ${band.label.toLowerCase()}, das ${formatOpeningMinutes(slotAt(day.key, band.key)!.start)} às ${formatOpeningMinutes(slotAt(day.key, band.key)!.end)}.${isPending(day.key, band.key) ? ' Alterado, ainda não salvo.' : ''} Delete remove.`
                  : undefined"
                @pointerdown="(e: PointerEvent) => { onResizePointerDown(e, day.key, band.key, 'move') }"
                @pointermove="(e: PointerEvent) => { onDragPointerMove(e) }"
                @pointerup="() => { onDragPointerUp() }"
                @pointercancel="() => { onDragPointerUp() }"
                @keydown="(e: KeyboardEvent) => { onCardKeydown(e, day.key, band.key) }"
              >
                <div class="flex h-full flex-col items-center justify-center px-1">
                  <div class="text-[11px] font-semibold leading-tight tabular-nums text-highlighted">
                    {{ formatOpeningMinutes(slotAt(day.key, band.key)!.start) }} – {{ formatOpeningMinutes(slotAt(day.key, band.key)!.end) }}
                  </div>
                  <div
                    v-if="!isCompact(slotAt(day.key, band.key)!)"
                    class="text-[11px] leading-tight text-muted"
                  >
                    {{ formatOpeningDuration(slotAt(day.key, band.key)!.end - slotAt(day.key, band.key)!.start) }}
                  </div>
                </div>

                <template v-if="editing">
                  <div
                    v-for="edge in (['start', 'end'] as const)"
                    :key="edge"
                    class="absolute inset-x-0 flex h-2.5 touch-none cursor-ns-resize justify-center focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-primary"
                    :class="edge === 'start' ? 'top-0 items-start pt-0.5' : 'bottom-0 items-end pb-0.5'"
                    role="slider"
                    tabindex="0"
                    :aria-label="handleLabel(band, edge)"
                    :aria-valuemin="band.start"
                    :aria-valuemax="band.end"
                    :aria-valuenow="edge === 'start' ? slotAt(day.key, band.key)!.start : slotAt(day.key, band.key)!.end"
                    :aria-valuetext="formatOpeningMinutes(edge === 'start' ? slotAt(day.key, band.key)!.start : slotAt(day.key, band.key)!.end)"
                    @pointerdown="(e: PointerEvent) => { onResizePointerDown(e, day.key, band.key, edge) }"
                    @pointermove="(e: PointerEvent) => { onDragPointerMove(e) }"
                    @pointerup="() => { onDragPointerUp() }"
                    @pointercancel="() => { onDragPointerUp() }"
                    @keydown="(e: KeyboardEvent) => { onHandleKeydown(e, day.key, band.key, edge) }"
                  >
                    <span
                      v-if="showsGrip(slotAt(day.key, band.key)!)"
                      class="h-0.5 w-6 rounded-full bg-primary/70"
                    />
                  </div>
                </template>

                <UButton
                  v-if="editing"
                  icon="i-lucide-x"
                  color="neutral"
                  variant="ghost"
                  size="xs"
                  class="absolute right-0.5 top-1/2 -translate-y-1/2"
                  :aria-label="`Fechar ${day.label} no turno da ${band.label.toLowerCase()}`"
                  @pointerdown="(e: PointerEvent) => { e.stopPropagation() }"
                  @click="() => { clearSlot(day.key, band.key) }"
                />
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
