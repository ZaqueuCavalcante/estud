<script setup lang="ts">
const props = defineProps<{ rule: string | undefined, flat?: boolean }>()

const info = computed(() => (props.rule ? classGradeRules[props.rule] : undefined))
</script>

<template>
  <div v-if="info" :class="flat ? undefined : 'rounded-lg border border-default bg-elevated/50 overflow-hidden'">
    <p class="pb-3 text-xs text-muted" :class="flat ? undefined : 'px-4 pt-3'">
      {{ info.description }}
    </p>

    <div class="pb-4" :class="flat ? undefined : 'px-4'">
      <span class="text-[11px] font-medium uppercase tracking-wide text-dimmed">Exemplo</span>

      <div class="mt-3 flex flex-wrap items-stretch gap-2">
        <div
          v-for="note in info.example.notes"
          :key="note.label"
          class="flex-1 min-w-[72px] rounded-lg border px-3 py-2 text-center"
          :class="note.used
            ? 'border-success/30 bg-success/10'
            : 'border-error/30 bg-error/10'"
        >
          <div
            class="flex items-center justify-center gap-1 text-[11px] font-medium"
            :class="note.used ? 'text-success' : 'text-error'"
          >
            <UIcon :name="note.used ? 'i-lucide-check' : 'i-lucide-x'" class="size-3 shrink-0" />
            {{ note.label }}
          </div>
          <div
            class="mt-0.5 text-xl font-bold leading-none"
            :class="note.used ? 'text-success' : 'text-error'"
          >
            {{ note.value }}
          </div>
        </div>
      </div>

      <div class="mt-3 flex items-start gap-1.5 text-xs text-muted">
        <UIcon name="i-lucide-info" class="size-3.5 shrink-0 mt-px text-dimmed" />
        <span>{{ info.example.hint }}</span>
      </div>

      <div class="mt-4 flex flex-wrap items-center justify-center gap-x-3 gap-y-2 text-base text-toned tabular-nums">
        <template v-for="(term, i) in info.example.calculation" :key="i">
          <div v-if="term.type === 'fraction'" class="flex flex-col items-center">
            <span class="px-1.5">{{ term.numerator }}</span>
            <span class="h-px w-full bg-accented my-1" />
            <span class="px-1.5">{{ term.denominator }}</span>
          </div>
          <span v-else>{{ term.value }}</span>
        </template>

        <span class="text-dimmed">=</span>

        <div class="flex flex-col items-center">
          <span class="text-2xl font-bold leading-none text-highlighted">{{ info.example.result }}</span>
          <span class="mt-1.5 text-xs text-dimmed">Média final</span>
        </div>
      </div>
    </div>
  </div>
</template>
