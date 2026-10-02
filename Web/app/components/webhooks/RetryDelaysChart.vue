<script setup lang="ts">
const props = defineProps<{ delays: number[], unitSuffix: string }>()

const MAX_SLOTS = 5

const max = computed(() => Math.max(...props.delays, 1))
const total = computed(() => props.delays.reduce((sum, d) => sum + d, 0))

// Todas as colunas ficam sempre montadas; as inativas colapsam para largura 0,
// assim adicionar/remover retentativas anima em vez de pular.
const slots = computed(() =>
  Array.from({ length: Math.max(MAX_SLOTS, props.delays.length) }, (_, idx) => ({
    active: idx < props.delays.length,
    delay: props.delays[idx] ?? 0,
  })),
)
</script>

<template>
  <div class="flex flex-col gap-3 rounded-lg bg-elevated px-4 py-3">
    <div class="flex items-baseline justify-between text-sm">
      <span class="text-muted">Intervalo antes de cada retentativa</span>
      <span
        class="text-xs text-dimmed transition-opacity duration-500"
        :class="delays.length ? 'opacity-100' : 'opacity-0'"
      >
        Total {{ total }}{{ unitSuffix }}
      </span>
    </div>

    <div class="relative flex h-38">
      <div
        v-for="(slot, idx) in slots"
        :key="idx"
        class="flex min-w-0 basis-0 flex-col overflow-hidden transition-[flex-grow,opacity] duration-500 ease-out"
        :class="slot.active ? 'grow opacity-100' : 'grow-0 opacity-0'"
      >
        <div class="flex h-32 items-end justify-center border-b border-default px-1 pt-5">
          <div
            class="relative w-full max-w-8 rounded-t bg-primary transition-[height] duration-500 ease-out"
            :style="{ height: slot.active ? `${Math.max((slot.delay / max) * 100, slot.delay > 0 ? 3 : 0)}%` : '0%' }"
          >
            <span class="absolute bottom-full left-1/2 mb-1 -translate-x-1/2 whitespace-nowrap text-xs text-muted tabular-nums">
              {{ slot.delay }}{{ unitSuffix }}
            </span>
          </div>
        </div>
        <span class="mt-1.5 text-center text-xs text-dimmed tabular-nums">{{ idx + 1 }}ª</span>
      </div>

      <div
        class="pointer-events-none absolute inset-0 flex items-center justify-center text-sm text-muted transition-opacity duration-500"
        :class="delays.length ? 'opacity-0' : 'opacity-100'"
      >
        Chamadas com falha não serão retentadas.
      </div>
    </div>
  </div>
</template>
