<script setup lang="ts">
const OUTPUT_SIZE = 512
const MAX_SCALE = 4

const props = defineProps<{ file: File | null, loading?: boolean }>()
const emit = defineEmits<{ save: [blob: Blob] }>()
const open = defineModel<boolean>('open', { required: true })

const isMobile = useIsMobile()
const viewport = useTemplateRef<HTMLDivElement>('viewport')
const source = useTemplateRef<HTMLCanvasElement>('source')
const { width: diameter } = useElementSize(viewport)

const natural = ref({ w: 0, h: 0 })
const scale = ref(1)
const offset = reactive({ x: 0, y: 0 })

const ready = computed(() => natural.value.w > 0 && diameter.value > 0)

const base = computed(() => {
  const { w, h } = natural.value
  if (!w || !h || !diameter.value) return { w: 0, h: 0 }

  const k = diameter.value / Math.min(w, h)
  return { w: w * k, h: h * k }
})

const canvasStyle = computed(() => ({
  width: `${base.value.w * scale.value}px`,
  height: `${base.value.h * scale.value}px`,
  transform: `translate(-50%, -50%) translate(${offset.x}px, ${offset.y}px)`,
}))

watch([() => props.file, open], async ([file, isOpen]) => {
  if (!isOpen || !file) {
    natural.value = { w: 0, h: 0 }
    return
  }

  await nextTick()
  const canvas = source.value
  if (!canvas) return

  const bitmap = await createImageBitmap(file, { imageOrientation: 'from-image' })
  canvas.width = bitmap.width
  canvas.height = bitmap.height
  canvas.getContext('2d')?.drawImage(bitmap, 0, 0)

  natural.value = { w: bitmap.width, h: bitmap.height }
  scale.value = 1
  offset.x = 0
  offset.y = 0

  bitmap.close()
}, { immediate: true })

function clampOffset() {
  const maxX = Math.max(0, (base.value.w * scale.value - diameter.value) / 2)
  const maxY = Math.max(0, (base.value.h * scale.value - diameter.value) / 2)

  offset.x = Math.min(maxX, Math.max(-maxX, offset.x))
  offset.y = Math.min(maxY, Math.max(-maxY, offset.y))
}

watch([scale, base], clampOffset)

function setScale(value: number) {
  scale.value = Math.min(MAX_SCALE, Math.max(1, value))
}

const pointers = new Map<number, { x: number, y: number }>()
let pinchStart = { distance: 0, scale: 1 }

function pinchDistance() {
  const [a, b] = [...pointers.values()]
  if (!a || !b) return 0

  return Math.hypot(a.x - b.x, a.y - b.y)
}

function onPointerDown(event: PointerEvent) {
  if (!ready.value) return

  const target = event.currentTarget as HTMLElement
  target.setPointerCapture(event.pointerId)
  pointers.set(event.pointerId, { x: event.clientX, y: event.clientY })

  if (pointers.size === 2) pinchStart = { distance: pinchDistance(), scale: scale.value }
}

function onPointerMove(event: PointerEvent) {
  const previous = pointers.get(event.pointerId)
  if (!previous) return

  const dx = event.clientX - previous.x
  const dy = event.clientY - previous.y
  pointers.set(event.pointerId, { x: event.clientX, y: event.clientY })

  if (pointers.size === 1) {
    offset.x += dx
    offset.y += dy
    clampOffset()
    return
  }

  if (pointers.size === 2 && pinchStart.distance > 0) {
    setScale(pinchStart.scale * (pinchDistance() / pinchStart.distance))
  }
}

function onPointerUp(event: PointerEvent) {
  pointers.delete(event.pointerId)
  pinchStart = { distance: 0, scale: 1 }
}

function onWheel(event: WheelEvent) {
  if (!ready.value) return

  setScale(scale.value - event.deltaY * 0.002)
}

function toBlob(canvas: HTMLCanvasElement, type: string) {
  return new Promise<Blob | null>(resolve => canvas.toBlob(resolve, type, 0.9))
}

async function encode(canvas: HTMLCanvasElement, context: CanvasRenderingContext2D, size: number) {
  const webp = await toBlob(canvas, 'image/webp')
  if (webp?.type === 'image/webp') return webp

  // JPEG não tem canal alpha: sem pintar um fundo por baixo, um PNG transparente sai preto.
  context.globalCompositeOperation = 'destination-over'
  context.fillStyle = '#fff'
  context.fillRect(0, 0, size, size)

  return toBlob(canvas, 'image/jpeg')
}

async function save() {
  const canvas = source.value
  if (!canvas || !ready.value) return

  const ratio = natural.value.w / (base.value.w * scale.value)
  const crop = diameter.value * ratio
  const sx = (base.value.w * scale.value / 2 - diameter.value / 2 - offset.x) * ratio
  const sy = (base.value.h * scale.value / 2 - diameter.value / 2 - offset.y) * ratio

  const size = Math.max(1, Math.min(OUTPUT_SIZE, Math.round(crop)))
  const target = document.createElement('canvas')
  target.width = size
  target.height = size

  const context = target.getContext('2d')
  if (!context) return

  context.imageSmoothingQuality = 'high'
  context.drawImage(canvas, sx, sy, crop, crop, 0, 0, size, size)

  const blob = await encode(target, context, size)
  if (blob) emit('save', blob)
}
</script>

<template>
  <UModal
    v-model:open="open"
    title="Ajustar foto"
    description="Arraste para posicionar e use o zoom para enquadrar."
    :fullscreen="isMobile"
    :dismissible="!loading"
  >
    <template #body>
      <div class="space-y-6">
        <div
          ref="viewport"
          class="relative mx-auto aspect-square w-full max-w-xs overflow-hidden rounded-lg bg-elevated touch-none select-none"
          :class="ready ? 'cursor-grab active:cursor-grabbing' : ''"
          @pointerdown="onPointerDown"
          @pointermove="onPointerMove"
          @pointerup="onPointerUp"
          @pointercancel="onPointerUp"
          @wheel.prevent="onWheel"
        >
          <canvas
            ref="source"
            class="absolute left-1/2 top-1/2 max-w-none"
            :style="canvasStyle"
          />
          <div class="pointer-events-none absolute inset-0 rounded-full shadow-[0_0_0_9999px_rgba(0,0,0,0.55)] ring ring-inset ring-white/60" />
        </div>

        <div class="mx-auto flex max-w-xs items-center gap-3">
          <UIcon name="i-lucide-zoom-out" class="size-4 shrink-0 text-muted" />
          <USlider
            v-model="scale"
            :min="1"
            :max="MAX_SCALE"
            :step="0.01"
            :disabled="!ready || loading"
          />
          <UIcon name="i-lucide-zoom-in" class="size-4 shrink-0 text-muted" />
        </div>
      </div>
    </template>

    <template #footer>
      <div class="flex w-full justify-end gap-2">
        <UButton
          label="Cancelar"
          color="neutral"
          variant="subtle"
          :disabled="loading"
          @click="() => { open = false }"
        />
        <UButton
          label="Salvar"
          :loading="loading"
          :disabled="!ready"
          @click="save"
        />
      </div>
    </template>
  </UModal>
</template>
