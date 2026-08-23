<script setup lang="ts">
import type { CourseCurriculumDisciplineItem, CourseCurriculumDisciplineSelection } from '~/types/course-curriculums'

interface CourseDiscipline {
  id: number
  name: string
  code: string
}

interface DisciplineDraft {
  period: number
  credits: number
  workload: number
}

const props = withDefaults(defineProps<{
  catalog: CourseDiscipline[]
  saved?: CourseCurriculumDisciplineItem[]
  loading?: boolean
}>(), {
  saved: () => [],
  loading: false,
})

const model = defineModel<CourseCurriculumDisciplineSelection[]>({ default: () => [] })

const { toAdd, toRemove, edited, fieldChanged } = useCurriculumDisciplinesDiff(() => props.saved, model)

const search = ref('')

// Os valores de quem foi desmarcado ficam guardados aqui: remarcar a linha traz
// de volta o que já tinha sido preenchido.
const drafts = ref<Record<number, DisciplineDraft>>({})

const selectionById = computed(() => new Map(model.value.map(s => [s.id, s])))

function isSelected(id: number) {
  return selectionById.value.has(id)
}

// A lista é o catálogo do curso; as que já estão na grade entram junto porque o
// catálogo ainda pode estar carregando.
const allDisciplines = computed(() => {
  const byId = new Map<number, CourseDiscipline>()
  for (const d of props.saved) byId.set(d.id, { id: d.id, name: d.name, code: d.code })
  for (const d of props.catalog) byId.set(d.id, d)
  return [...byId.values()].sort((a, b) => a.name.localeCompare(b.name, 'pt-BR'))
})

// Buscar "algebra" tem que achar "Álgebra": os nomes vêm acentuados e ninguém
// digita acento numa busca.
function normalize(value: string | undefined) {
  return (value ?? '').normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase()
}

function matchesSearch(discipline: { name: string, code: string }) {
  const term = normalize(search.value.trim())
  if (!term) return true
  return normalize(discipline.name).includes(term) || normalize(discipline.code).includes(term)
}

// Os filtros isolam grupos na lista: é a conferência do que vai ser salvo sem
// precisar caçar as marcações no meio de uma lista longa. Combinam entre si —
// nenhum ligado mostra tudo, vários ligados somam os grupos.
type DisciplineFilter = 'linked' | 'edited' | 'add' | 'remove'
const filters = ref<DisciplineFilter[]>([])

function toggleFilter(value: DisciplineFilter) {
  filters.value = filters.value.includes(value)
    ? filters.value.filter(f => f !== value)
    : [...filters.value, value]
}

function inFilter(id: number, value: DisciplineFilter) {
  if (value === 'add') return toAdd.value.includes(id)
  if (value === 'remove') return toRemove.value.includes(id)
  if (value === 'edited') return edited.value.includes(id)
  return isSelected(id) && !toAdd.value.includes(id)
}

const shownDisciplines = computed(() => {
  let items = allDisciplines.value

  if (filters.value.length)
    items = items.filter(d => filters.value.some(f => inFilter(d.id, f)))

  return items.filter(matchesSearch)
})

// O período que agrupa a linha é uma fotografia do valor do campo, atualizada
// quando ele perde o foco: reordenar a cada tecla move o input no DOM e o
// navegador tira o foco dele no meio da digitação.
const groupPeriods = ref<Record<number, number>>({})

function syncGroupPeriod(id: number) {
  const entry = selectionById.value.get(id)
  if (entry) groupPeriods.value[id] = entry.period
}

function onFieldsFocusOut(event: FocusEvent, id: number) {
  const next = event.relatedTarget as Node | null
  if (next && (event.currentTarget as HTMLElement).contains(next)) return
  syncGroupPeriod(id)
}

const unselectedLabel = computed(() => props.saved.length ? 'Fora da grade' : 'Não selecionadas')

const shownGroups = computed(() => {
  type Row = CourseDiscipline & { entry: CourseCurriculumDisciplineSelection | null }

  const groups = new Map<number, Row[]>()
  const unselected: Row[] = []

  for (const discipline of shownDisciplines.value) {
    const entry = selectionById.value.get(discipline.id)
    if (!entry) {
      unselected.push({ ...discipline, entry: null })
      continue
    }

    const period = groupPeriods.value[discipline.id] ?? entry.period
    const row = { ...discipline, entry }
    const group = groups.get(period)
    if (group) group.push(row)
    else groups.set(period, [row])
  }

  const byName = (a: Row, b: Row) => a.name.localeCompare(b.name, 'pt-BR')

  const periods = [...groups.entries()]
    .sort(([a], [b]) => a - b)
    .map(([period, items]) => ({
      key: String(period),
      label: `${period}º período`,
      credits: items.reduce((total, d) => total + (d.entry?.credits ?? 0), 0),
      workload: items.reduce((total, d) => total + (d.entry?.workload ?? 0), 0),
      items: items.sort(byName),
    }))

  if (!unselected.length) return periods

  return [...periods, {
    key: 'unselected',
    label: unselectedLabel.value,
    credits: null,
    workload: null,
    items: unselected.sort(byName),
  }]
})

// Quem vai perder o vínculo fica indeterminado: é o estado em que o UCheckbox
// desenha o traço no lugar do check.
function checkboxValue(id: number): boolean | 'indeterminate' {
  return toRemove.value.includes(id) ? 'indeterminate' : isSelected(id)
}

function checkboxColor(id: number) {
  if (toAdd.value.includes(id)) return 'success' as const
  if (toRemove.value.includes(id)) return 'error' as const
  if (edited.value.includes(id)) return 'warning' as const
  return 'primary' as const
}

// Mesma receita do `subtle` dos botões: fundo lavado e ícone na cor, no lugar
// do preenchimento chapado com ícone branco. As classes ficam escritas por
// extenso porque o Tailwind não enxerga nome de classe montado em runtime.
const checkboxUiByColor = {
  primary: { base: 'ring-primary/25', indicator: 'bg-primary/10 text-primary' },
  success: { base: 'ring-success/25', indicator: 'bg-success/10 text-success' },
  error: { base: 'ring-error/25', indicator: 'bg-error/10 text-error' },
  warning: { base: 'ring-warning/25', indicator: 'bg-warning/10 text-warning' },
}

function checkboxUi(id: number) {
  return checkboxUiByColor[checkboxColor(id)]
}

const changedFieldUi = { base: 'ring-warning/50 focus-visible:ring-warning' }

function fieldUi(id: number, field: 'period' | 'credits' | 'workload') {
  return fieldChanged(id, field) ? changedFieldUi : undefined
}

// O `subtle` neutro para no `bg-elevated`, que quase não se separa do fundo do
// botão inativo — o filtro ligado precisa se ver de longe.
const activeFilterClass = 'bg-accented hover:bg-accented'

const keptCount = computed(() => props.saved.length - toRemove.value.length)

// O botão some quando o grupo zera; o filtro dele não pode ficar preso.
watch([toAdd, toRemove, edited], () => {
  filters.value = filters.value.filter(f =>
    (f !== 'add' || toAdd.value.length > 0)
    && (f !== 'remove' || toRemove.value.length > 0)
    && (f !== 'edited' || edited.value.length > 0),
  )
})

// O clique vale na linha inteira, mas o checkbox e os campos do período já
// tratam o seu — sem esse desvio a linha alternaria junto.
function onRowClick(event: MouseEvent, id: number) {
  const target = event.target as HTMLElement
  if (target.closest('.discipline-checkbox') || target.closest('.discipline-fields')) return
  toggle(id)
}

function toggle(id: number) {
  const entry = selectionById.value.get(id)
  if (entry) {
    drafts.value[id] = { period: entry.period, credits: entry.credits, workload: entry.workload }
    model.value = model.value.filter(s => s.id !== id)
    return
  }

  const draft = drafts.value[id] ?? props.saved.find(d => d.id === id) ?? { period: 1, credits: 0, workload: 0 }
  model.value = [...model.value, { id, period: draft.period, credits: draft.credits, workload: draft.workload }]
  groupPeriods.value[id] = draft.period
}
</script>

<template>
  <div class="flex flex-col gap-4">
    <div v-if="allDisciplines.length" class="flex flex-wrap items-center justify-between gap-3">
      <UInput
        v-model="search"
        class="w-full sm:max-w-sm"
        icon="i-lucide-search"
        placeholder="Buscar por nome ou código..."
      >
        <template v-if="search" #trailing>
          <UButton
            icon="i-lucide-x"
            color="neutral"
            variant="link"
            size="sm"
            aria-label="Limpar busca"
            @click="() => { search = '' }"
          />
        </template>
      </UInput>

      <UFieldGroup size="md">
        <UButton
          v-if="saved.length"
          icon="i-lucide-square-check"
          color="neutral"
          :variant="filters.includes('linked') ? 'subtle' : 'outline'"
          :ui="{ leadingIcon: 'text-primary/70', base: filters.includes('linked') ? activeFilterClass : '' }"
          :label="String(keptCount)"
          :aria-pressed="filters.includes('linked')"
          :aria-label="`Na grade: ${keptCount}`"
          @click="(e: MouseEvent) => { (e.currentTarget as HTMLElement).blur(); toggleFilter('linked') }"
        />
        <UButton
          v-if="edited.length"
          icon="i-lucide-square-pen"
          color="neutral"
          :variant="filters.includes('edited') ? 'subtle' : 'outline'"
          :ui="{ leadingIcon: 'text-warning/70', base: filters.includes('edited') ? activeFilterClass : '' }"
          :label="String(edited.length)"
          :aria-pressed="filters.includes('edited')"
          :aria-label="`Alteradas: ${edited.length}`"
          @click="(e: MouseEvent) => { (e.currentTarget as HTMLElement).blur(); toggleFilter('edited') }"
        />
        <UButton
          v-if="toAdd.length"
          icon="i-lucide-square-check"
          color="neutral"
          :variant="filters.includes('add') ? 'subtle' : 'outline'"
          :ui="{ leadingIcon: 'text-success/70', base: filters.includes('add') ? activeFilterClass : '' }"
          :label="String(toAdd.length)"
          :aria-pressed="filters.includes('add')"
          :aria-label="`Para adicionar: ${toAdd.length}`"
          @click="(e: MouseEvent) => { (e.currentTarget as HTMLElement).blur(); toggleFilter('add') }"
        />
        <UButton
          v-if="toRemove.length"
          icon="i-lucide-square-minus"
          color="neutral"
          :variant="filters.includes('remove') ? 'subtle' : 'outline'"
          :ui="{ leadingIcon: 'text-error/70', base: filters.includes('remove') ? activeFilterClass : '' }"
          :label="String(toRemove.length)"
          :aria-pressed="filters.includes('remove')"
          :aria-label="`Para remover: ${toRemove.length}`"
          @click="(e: MouseEvent) => { (e.currentTarget as HTMLElement).blur(); toggleFilter('remove') }"
        />
      </UFieldGroup>
    </div>

    <div v-if="loading" class="flex justify-center py-8">
      <AppSpinner class="size-6 text-muted" />
    </div>

    <slot v-else-if="!allDisciplines.length" name="empty">
      <TableEmptyState
        :loading="false"
        icon="i-lucide-book-open"
        message="Nenhuma disciplina vinculada a este curso"
      />
    </slot>

    <div v-else-if="shownGroups.length" class="flex flex-col gap-5">
      <div v-for="group in shownGroups" :key="group.key" class="flex flex-col gap-2">
        <div class="flex items-end gap-3 px-1">
          <div class="flex min-w-0 flex-1 flex-wrap items-center gap-x-3 gap-y-1">
            <span class="text-sm font-medium text-highlighted">{{ group.label }}</span>
            <span v-if="group.credits !== null" class="text-xs text-muted tabular-nums">
              {{ group.credits }} {{ group.credits === 1 ? 'crédito' : 'créditos' }} · {{ group.workload }}h
            </span>
          </div>

          <div v-if="group.credits !== null" class="hidden shrink-0 items-center gap-2 text-xs text-muted sm:flex">
            <span class="w-16 text-center">Período</span>
            <span class="w-16 text-center">Créditos</span>
            <span class="w-16 text-center">C.H.</span>
          </div>
        </div>

        <ul class="divide-y divide-default border-y border-default">
          <li v-for="discipline in group.items" :key="discipline.id">
            <div
              class="flex w-full cursor-pointer items-start gap-3 px-1 py-3 transition-colors hover:bg-elevated/40 sm:items-center"
              @click="(e) => { onRowClick(e, discipline.id) }"
            >
              <UCheckbox
                class="discipline-checkbox mt-0.5 sm:mt-0"
                :model-value="checkboxValue(discipline.id)"
                :color="checkboxColor(discipline.id)"
                :ui="checkboxUi(discipline.id)"
                :aria-label="discipline.name"
                @update:model-value="() => { toggle(discipline.id) }"
              />
              <div class="flex min-w-0 flex-1 flex-col gap-2 sm:flex-row sm:items-center sm:gap-4">
                <div class="flex min-w-0 flex-1 flex-col gap-1 sm:flex-row sm:items-center sm:justify-between sm:gap-4">
                  <span class="truncate text-sm text-muted">{{ discipline.name }}</span>
                  <span class="shrink-0 text-xs text-muted">{{ discipline.code }}</span>
                </div>

                <div
                  v-if="discipline.entry"
                  class="discipline-fields flex w-full items-end gap-2 sm:w-auto sm:shrink-0"
                  @focusout="(e) => { onFieldsFocusOut(e, discipline.id) }"
                >
                  <div class="flex flex-1 flex-col gap-1 sm:flex-none">
                    <span class="text-xs text-muted sm:hidden">Período</span>
                    <UInputNumber
                      v-model="discipline.entry.period"
                      class="w-full sm:w-16"
                      :min="1"
                      :max="10"
                      :increment="false"
                      :decrement="false"
                      :ui="fieldUi(discipline.id, 'period')"
                      :aria-label="`Período de ${discipline.name}`"
                    />
                  </div>
                  <div class="flex flex-1 flex-col gap-1 sm:flex-none">
                    <span class="text-xs text-muted sm:hidden">Créditos</span>
                    <UInputNumber
                      v-model="discipline.entry.credits"
                      class="w-full sm:w-16"
                      :min="0"
                      :max="100"
                      :increment="false"
                      :decrement="false"
                      :ui="fieldUi(discipline.id, 'credits')"
                      :aria-label="`Créditos de ${discipline.name}`"
                    />
                  </div>
                  <div class="flex flex-1 flex-col gap-1 sm:flex-none">
                    <span class="text-xs text-muted sm:hidden">C.H.</span>
                    <UInputNumber
                      v-model="discipline.entry.workload"
                      class="w-full sm:w-16"
                      :min="0"
                      :max="500"
                      :increment="false"
                      :decrement="false"
                      :ui="fieldUi(discipline.id, 'workload')"
                      :aria-label="`Carga horária de ${discipline.name}`"
                    />
                  </div>
                </div>
              </div>
            </div>
          </li>
        </ul>
      </div>
    </div>

    <p v-else class="py-6 text-center text-sm text-muted">
      <template v-if="search">Nenhuma disciplina encontrada para "{{ search }}".</template>
      <template v-else>Nenhuma disciplina neste grupo.</template>
    </p>
  </div>
</template>
