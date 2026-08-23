<script setup lang="ts">
import type { CourseCurriculumDisciplineItem, CourseCurriculumDisciplineSelection } from '~/types/course-curriculums'

interface CourseDiscipline {
  id: number
  name: string
  code: string
}

const props = defineProps<{
  curriculumId: number
  curriculumName: string
  courseId: number
  disciplines: CourseCurriculumDisciplineItem[]
}>()
const emit = defineEmits<{ updated: [] }>()

const config = useRuntimeConfig()
const toast = useToast()

const editing = ref(false)
const loading = ref(false)
const saving = ref(false)
const search = ref('')
const courseDisciplines = ref<CourseDiscipline[]>([])
const selection = ref<CourseCurriculumDisciplineSelection[]>([])

const { dirty } = useCurriculumDisciplinesDiff(() => props.disciplines, selection)

// Buscar "algebra" tem que achar "Álgebra": os nomes vêm acentuados e ninguém
// digita acento numa busca.
function normalize(value: string | undefined) {
  return (value ?? '').normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase()
}

function matchesSearch(discipline: CourseCurriculumDisciplineItem) {
  const term = normalize(search.value.trim())
  if (!term) return true
  return normalize(discipline.name).includes(term) || normalize(discipline.code).includes(term)
}

const shownPeriods = computed(() => {
  const groups = new Map<number, CourseCurriculumDisciplineItem[]>()
  for (const discipline of props.disciplines.filter(matchesSearch)) {
    const group = groups.get(discipline.period)
    if (group) group.push(discipline)
    else groups.set(discipline.period, [discipline])
  }
  return [...groups.entries()]
    .sort(([a], [b]) => a - b)
    .map(([period, items]) => ({
      period,
      items: items.sort((a, b) => a.name.localeCompare(b.name, 'pt-BR')),
      credits: items.reduce((total, d) => total + d.credits, 0),
      workload: items.reduce((total, d) => total + d.workload, 0),
    }))
})

async function fetchCourseDisciplines() {
  loading.value = true
  try {
    const result = await $fetch<{ items: CourseDiscipline[] }>(
      `${config.public.backendUrl}/courses/${props.courseId}/disciplines`,
      { credentials: 'include' },
    )
    courseDisciplines.value = result.items
  } catch {
    toast.add({ title: 'Não foi possível carregar as disciplinas', color: 'error' })
    courseDisciplines.value = []
  } finally {
    loading.value = false
  }
}

async function startEditing() {
  editing.value = true
  search.value = ''
  selection.value = props.disciplines.map(d => ({
    id: d.id,
    period: d.period,
    credits: d.credits,
    workload: d.workload,
  }))
  await fetchCourseDisciplines()
}

function cancelEditing() {
  editing.value = false
  search.value = ''
  courseDisciplines.value = []
  selection.value = []
}

function errorMessage(err: unknown, fallback: string) {
  return (err as { data?: { message?: string } })?.data?.message ?? fallback
}

async function save() {
  saving.value = true
  try {
    await $fetch(`${config.public.backendUrl}/course-curriculums/${props.curriculumId}`, {
      method: 'PUT',
      body: { name: props.curriculumName, disciplines: selection.value },
      credentials: 'include',
    })

    toast.add({ title: 'Disciplinas da grade atualizadas', color: 'success' })
    cancelEditing()
  } catch (err: unknown) {
    toast.add({
      title: 'Não foi possível salvar as disciplinas',
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
        As disciplinas desta grade, por período.
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
          v-else-if="disciplines.length"
          icon="i-lucide-pencil"
          label="Editar"
          color="neutral"
          variant="subtle"
          @click="(e: MouseEvent) => { (e.currentTarget as HTMLElement).blur(); startEditing() }"
        />
      </div>
    </div>

    <template v-if="!editing">
      <div v-if="disciplines.length" class="flex flex-wrap items-center justify-between gap-3">
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

        <span class="text-sm text-muted">
          {{ disciplines.length }} {{ disciplines.length === 1 ? 'disciplina' : 'disciplinas' }}
        </span>
      </div>

      <div v-if="shownPeriods.length" class="flex flex-col gap-5">
        <div v-for="group in shownPeriods" :key="group.period" class="flex flex-col gap-2">
          <div class="flex flex-wrap items-center gap-x-3 gap-y-1">
            <span class="text-sm font-medium text-highlighted">{{ group.period }}º período</span>
            <span class="text-xs text-muted tabular-nums">
              {{ group.credits }} {{ group.credits === 1 ? 'crédito' : 'créditos' }} · {{ group.workload }}h
            </span>
          </div>

          <ul class="divide-y divide-default border-y border-default">
            <li v-for="discipline in group.items" :key="discipline.id">
              <NuxtLink
                :to="`/disciplines/${discipline.id}`"
                class="group flex items-center gap-4 px-1 py-3 transition-colors hover:bg-elevated/40 focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-primary"
              >
                <div class="flex min-w-0 flex-1 flex-col gap-1 sm:flex-row sm:items-center sm:justify-between sm:gap-4">
                  <span class="truncate text-sm text-muted">{{ discipline.name }}</span>
                  <div class="flex flex-col gap-1 text-xs text-muted tabular-nums sm:flex-row sm:shrink-0 sm:items-center sm:gap-4">
                    <span>{{ discipline.code }}</span>
                    <span class="flex items-center gap-4">
                      <span>{{ discipline.credits }} {{ discipline.credits === 1 ? 'crédito' : 'créditos' }}</span>
                      <span>{{ discipline.workload }}h</span>
                    </span>
                  </div>
                </div>
                <UIcon
                  name="i-lucide-arrow-right"
                  class="size-4 shrink-0 text-dimmed transition-colors group-hover:text-primary"
                />
              </NuxtLink>
            </li>
          </ul>
        </div>
      </div>
      <p v-else-if="disciplines.length" class="py-6 text-center text-sm text-muted">
        Nenhuma disciplina encontrada para "{{ search }}".
      </p>

      <TableEmptyState
        v-else
        :loading="false"
        icon="i-lucide-book-open"
        message="Nenhuma disciplina nesta grade"
        button-label="Adicionar disciplinas"
        @create="() => { startEditing() }"
      />
    </template>

    <CourseCurriculumsDisciplinesPicker
      v-else
      v-model="selection"
      :catalog="courseDisciplines"
      :saved="disciplines"
      :loading="loading"
    >
      <template #empty>
        <TableEmptyState
          :loading="false"
          icon="i-lucide-book-open"
          message="Nenhuma disciplina vinculada a este curso"
          button-label="Vincular disciplinas"
          @create="() => { navigateTo(`/courses/${courseId}`) }"
        />
      </template>
    </CourseCurriculumsDisciplinesPicker>
  </section>
</template>
