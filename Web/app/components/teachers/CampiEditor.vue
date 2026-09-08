<script setup lang="ts">
import type { TeacherCampusItem } from '~/types/teachers'

const props = defineProps<{ teacherId: number, campi: TeacherCampusItem[] }>()
const emit = defineEmits<{ updated: [] }>()

const config = useRuntimeConfig()
const toast = useToast()

const editing = ref(false)
const loading = ref(false)
const saving = ref(false)
const search = ref('')
const potential = ref<TeacherCampusItem[]>([])
const selectedIds = ref<number[]>([])

const linkedIds = computed(() => props.campi.map(c => c.id))

// Em edição a lista é a rede inteira da instituição: os vinculados vêm da tela,
// os demais do endpoint de potenciais.
const allCampi = computed(() => {
  const byId = new Map<number, TeacherCampusItem>()
  for (const c of props.campi) byId.set(c.id, { id: c.id, name: c.name, city: c.city, state: c.state })
  for (const c of potential.value) byId.set(c.id, c)
  return [...byId.values()].sort((a, b) => a.name.localeCompare(b.name, 'pt-BR'))
})

// Buscar "sertao" tem que achar "Sertão": os nomes vêm acentuados e ninguém
// digita acento numa busca.
function normalize(value: string | undefined) {
  return (value ?? '').normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase()
}

function matches(campus: TeacherCampusItem, term: string) {
  return normalize(campus.name).includes(term)
    || normalize(campus.city).includes(term)
    || normalize(campus.state).includes(term)
}

// Os filtros isolam grupos na lista: é a conferência do que vai ser salvo sem
// precisar caçar as marcações no meio de uma lista longa. Combinam entre si —
// nenhum ligado mostra tudo, vários ligados somam os grupos.
type CampusFilter = 'linked' | 'add' | 'remove'
const filters = ref<CampusFilter[]>([])

function toggleFilter(value: CampusFilter) {
  filters.value = filters.value.includes(value)
    ? filters.value.filter(f => f !== value)
    : [...filters.value, value]
}

function inFilter(id: number, value: CampusFilter) {
  if (value === 'add') return toAdd.value.includes(id)
  if (value === 'remove') return toRemove.value.includes(id)
  return linkedIds.value.includes(id) && !toRemove.value.includes(id)
}

const shownCampi = computed(() => {
  let items = allCampi.value

  if (filters.value.length)
    items = items.filter(c => filters.value.some(f => inFilter(c.id, f)))

  const term = normalize(search.value.trim())
  if (!term) return items
  return items.filter(c => matches(c, term))
})

const shownLinked = computed(() => {
  const term = normalize(search.value.trim())
  if (!term) return props.campi
  return props.campi.filter(c => matches(c, term))
})

function isSelected(id: number) {
  return selectedIds.value.includes(id)
}

// Fora da edição não existe rascunho: `selectedIds` vazio não significa que
// todos saíram do professor.
const toAdd = computed(() => editing.value ? selectedIds.value.filter(id => !linkedIds.value.includes(id)) : [])
const toRemove = computed(() => editing.value ? linkedIds.value.filter(id => !selectedIds.value.includes(id)) : [])
const dirty = computed(() => toAdd.value.length > 0 || toRemove.value.length > 0)

// O botão some quando o grupo zera; o filtro dele não pode ficar preso.
watch([toAdd, toRemove], () => {
  filters.value = filters.value.filter(f =>
    (f !== 'add' || toAdd.value.length > 0) && (f !== 'remove' || toRemove.value.length > 0),
  )
})

// Quem vai perder o vínculo fica indeterminado: é o estado em que o UCheckbox
// desenha o traço no lugar do check.
function checkboxValue(id: number): boolean | 'indeterminate' {
  return toRemove.value.includes(id) ? 'indeterminate' : isSelected(id)
}

function checkboxColor(id: number) {
  if (toAdd.value.includes(id)) return 'success' as const
  if (toRemove.value.includes(id)) return 'error' as const
  return 'primary' as const
}

// Mesma receita do `subtle` dos botões: fundo lavado e ícone na cor, no lugar
// do preenchimento chapado com ícone branco. As classes ficam escritas por
// extenso porque o Tailwind não enxerga nome de classe montado em runtime.
const checkboxUiByColor = {
  primary: { base: 'ring-primary/25', indicator: 'bg-primary/10 text-primary' },
  success: { base: 'ring-success/25', indicator: 'bg-success/10 text-success' },
  error: { base: 'ring-error/25', indicator: 'bg-error/10 text-error' },
}

function checkboxUi(id: number) {
  return checkboxUiByColor[checkboxColor(id)]
}

// O `subtle` neutro para no `bg-elevated`, que quase não se separa do fundo do
// botão inativo — o filtro ligado precisa se ver de longe.
const activeFilterClass = 'bg-accented hover:bg-accented'

// O que ainda está vinculado de fato: marcar um campus para vincular não entra
// na conta enquanto não salvar, marcar para remover já sai.
const linkedCount = computed(() => props.campi.length - toRemove.value.length)

// A busca só aparece quando há o que filtrar: sobre uma lista vazia ela é ruído
// em cima do estado vazio.
const showSearch = computed(() => editing.value
  ? allCampi.value.length > 0
  : props.campi.length > 0,
)

async function fetchPotential() {
  loading.value = true
  try {
    const result = await $fetch<{ items: TeacherCampusItem[] }>(
      `${config.public.backendUrl}/teachers/${props.teacherId}/potential-campi`,
      { credentials: 'include' },
    )
    potential.value = result.items
  } catch {
    toast.add({ title: 'Não foi possível carregar os campus', color: 'error' })
    potential.value = []
  } finally {
    loading.value = false
  }
}

async function startEditing() {
  editing.value = true
  selectedIds.value = [...linkedIds.value]
  await fetchPotential()
}

function cancelEditing() {
  editing.value = false
  potential.value = []
  selectedIds.value = []
  filters.value = []
}

// O clique vale na linha inteira, mas o próprio checkbox já emite o seu — sem
// esse desvio a linha alternaria duas vezes ao clicar nele.
function onRowClick(event: MouseEvent, id: number) {
  if ((event.target as HTMLElement).closest('.campus-checkbox')) return
  toggle(id)
}

function toggle(id: number) {
  selectedIds.value = selectedIds.value.includes(id)
    ? selectedIds.value.filter(x => x !== id)
    : [...selectedIds.value, id]
}

function errorMessage(err: unknown, fallback: string) {
  return (err as { data?: { message?: string } })?.data?.message ?? fallback
}

async function save() {
  saving.value = true
  try {
    await $fetch(`${config.public.backendUrl}/teachers/${props.teacherId}/assign-campi`, {
      method: 'PUT',
      body: { campi: selectedIds.value },
      credentials: 'include',
    })

    toast.add({ title: 'Campus do professor atualizados', color: 'success' })
    cancelEditing()
  } catch (err: unknown) {
    toast.add({
      title: 'Não foi possível salvar os campus',
      description: errorMessage(err, 'Tente novamente.'),
      color: 'error',
    })
  } finally {
    saving.value = false
    emit('updated')
  }
}
</script>

<template>
  <section class="flex flex-col gap-4">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <p class="text-sm text-muted">
        Os campus em que este professor trabalha.
      </p>

      <div class="flex shrink-0 items-center gap-2">
        <template v-if="editing">
          <UButton
            label="Cancelar"
            color="neutral"
            variant="subtle"
            :disabled="saving"
            @click="() => { cancelEditing() }"
          />
          <UButton
            label="Salvar"
            :loading="saving"
            :disabled="!dirty"
            @click="() => { save() }"
          />
        </template>
        <UButton
          v-else-if="campi.length"
          icon="i-lucide-pencil"
          label="Editar"
          color="neutral"
          variant="subtle"
          @click="(e: MouseEvent) => { (e.currentTarget as HTMLElement).blur(); startEditing() }"
        />
      </div>
    </div>

    <div v-if="showSearch" class="flex flex-wrap items-center justify-between gap-3">
      <UInput
        v-model="search"
        class="w-full sm:max-w-sm"
        icon="i-lucide-search"
        placeholder="Buscar por nome ou cidade..."
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

      <span v-if="!editing" class="text-sm text-muted">
        {{ linkedCount }} {{ linkedCount === 1 ? 'vinculado' : 'vinculados' }}
      </span>

      <UFieldGroup v-else size="md">
        <UButton
          icon="i-lucide-square-check"
          color="neutral"
          :variant="filters.includes('linked') ? 'subtle' : 'outline'"
          :ui="{ leadingIcon: 'text-primary/70', base: filters.includes('linked') ? activeFilterClass : '' }"
          :label="String(linkedCount)"
          :aria-pressed="filters.includes('linked')"
          :aria-label="`Vinculados: ${linkedCount}`"
          @click="(e: MouseEvent) => { (e.currentTarget as HTMLElement).blur(); toggleFilter('linked') }"
        />
        <UButton
          v-if="toAdd.length"
          icon="i-lucide-square-check"
          color="neutral"
          :variant="filters.includes('add') ? 'subtle' : 'outline'"
          :ui="{ leadingIcon: 'text-success/70', base: filters.includes('add') ? activeFilterClass : '' }"
          :label="String(toAdd.length)"
          :aria-pressed="filters.includes('add')"
          :aria-label="`Para vincular: ${toAdd.length}`"
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

    <template v-if="!editing">
      <ul v-if="shownLinked.length" class="divide-y divide-default border-y border-default">
        <li v-for="campus in shownLinked" :key="campus.id">
          <NuxtLink
            :to="`/campi/${campus.id}`"
            class="group flex items-center gap-4 px-1 py-3 transition-colors hover:bg-elevated/40 focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-primary"
          >
            <div class="flex min-w-0 flex-1 flex-col sm:flex-row sm:items-center sm:justify-between sm:gap-4">
              <span class="truncate text-sm text-muted">{{ campus.name }}</span>
              <span class="shrink-0 text-xs text-muted">{{ campus.city }} · {{ campus.state }}</span>
            </div>
            <UIcon
              name="i-lucide-arrow-right"
              class="size-4 shrink-0 text-dimmed transition-colors group-hover:text-primary"
            />
          </NuxtLink>
        </li>
      </ul>
      <p v-else-if="campi.length" class="py-6 text-center text-sm text-muted">
        Nenhum campus encontrado para "{{ search }}".
      </p>

      <TableEmptyState
        v-else
        :loading="false"
        icon="i-lucide-map-pin"
        message="Nenhum campus vinculado a este professor"
        button-label="Vincular campus"
        @create="() => { startEditing() }"
      />
    </template>

    <template v-else>
      <div v-if="loading" class="flex justify-center py-8">
        <AppSpinner class="size-6 text-muted" />
      </div>

      <TableEmptyState
        v-else-if="!allCampi.length"
        :loading="false"
        icon="i-lucide-map-pin"
        message="Nenhum campus cadastrado nesta instituição"
        button-label="Campus"
        @create="() => { navigateTo('/campi') }"
      />

      <ul v-else-if="shownCampi.length" class="divide-y divide-default border-y border-default">
        <li v-for="campus in shownCampi" :key="campus.id">
          <div
            class="flex w-full cursor-pointer items-center gap-3 px-1 py-3 transition-colors hover:bg-elevated/40"
            @click="(e) => { onRowClick(e, campus.id) }"
          >
            <UCheckbox
              class="campus-checkbox"
              :model-value="checkboxValue(campus.id)"
              :color="checkboxColor(campus.id)"
              :ui="checkboxUi(campus.id)"
              :aria-label="campus.name"
              @update:model-value="() => { toggle(campus.id) }"
            />
            <div class="flex min-w-0 flex-1 flex-col sm:flex-row sm:items-center sm:justify-between sm:gap-4">
              <span class="truncate text-sm text-muted">{{ campus.name }}</span>
              <span class="shrink-0 text-xs text-muted">{{ campus.city }} · {{ campus.state }}</span>
            </div>
          </div>
        </li>
      </ul>

      <p v-else class="py-6 text-center text-sm text-muted">
        <template v-if="search">Nenhum campus encontrado para "{{ search }}".</template>
        <template v-else>Nenhum campus neste grupo.</template>
      </p>
    </template>
  </section>
</template>
