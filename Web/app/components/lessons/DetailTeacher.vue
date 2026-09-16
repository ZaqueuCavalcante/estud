<script setup lang="ts">
import type { NavigationMenuItem } from '@nuxt/ui'
import type { GetTeacherClassLessonOut } from '~/types/classes'

const props = defineProps<{ classId: string, lessonId: string }>()

const config = useRuntimeConfig()

// "Turmas" não é link: pro professor as turmas vivem no grupo da sidebar, não
// numa página de listagem.
const breadcrumb = computed(() => [
  { label: 'Turmas', icon: 'i-lucide-presentation' },
  { label: 'Detalhes', to: `/classes/${props.classId}` },
  { label: 'Aula' },
])

const { data, status, error, refresh } = await useFetch<GetTeacherClassLessonOut>(
  `${config.public.backendUrl}/teachers/classes/${props.classId}/lessons/${props.lessonId}`,
  { credentials: 'include', server: false },
)

const activeTab = ref('plan')

const tabs = computed(() => [[
  { label: 'Planejamento', icon: 'i-lucide-notebook-pen', active: activeTab.value === 'plan', onSelect: () => { activeTab.value = 'plan' } },
  { label: 'Chamada', icon: 'i-lucide-clipboard-check', active: activeTab.value === 'attendance', onSelect: () => { activeTab.value = 'attendance' } },
]] satisfies NavigationMenuItem[][])
</script>

<template>
  <UDashboardPanel id="lesson-details">
    <template #header>
      <UDashboardNavbar>
        <template #title>
          <UBreadcrumb :items="breadcrumb" />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div v-if="status === 'pending' && !data" class="flex justify-center py-12">
        <UIcon name="i-lucide-loader-circle" class="size-8 animate-spin text-muted" />
      </div>

      <div v-else-if="error || !data" class="flex flex-col items-center gap-4 py-12">
        <UIcon name="i-lucide-triangle-alert" class="size-16 text-muted" />
        <p class="text-muted text-sm">
          Aula não encontrada
        </p>
        <UButton icon="i-lucide-arrow-left" label="Voltar" :to="`/classes/${props.classId}`" />
      </div>

      <div v-else class="flex flex-col gap-6 py-2">
        <div class="flex flex-col gap-1">
          <h1 class="text-2xl font-semibold tracking-tight text-highlighted">
            Aula {{ data.number }}
          </h1>
          <div class="flex flex-wrap items-center gap-x-6 gap-y-1 text-sm text-muted">
            <span class="flex items-center gap-1.5">
              <UIcon name="i-lucide-book-open" class="size-4" />
              {{ data.discipline }}
            </span>
            <span class="flex items-center gap-1.5">
              <UIcon name="i-lucide-calendar" class="size-4" />
              {{ formatClassLessonDate(data.date) }}
            </span>
            <span class="flex items-center gap-1.5">
              <UIcon name="i-lucide-clock" class="size-4" />
              {{ formatClassHour(data.startAt) }} – {{ formatClassHour(data.endAt) }}
            </span>
            <UBadge
              :label="classLessonStatusLabels[data.status] ?? data.status"
              :color="classLessonStatusColors[data.status] ?? 'neutral'"
              variant="subtle"
            />
          </div>
        </div>

        <UNavigationMenu :items="tabs" highlight class="-mx-1" />

        <LessonsPlanEditor
          v-show="activeTab === 'plan'"
          :lesson-id="data.id"
          :planned-content="data.plannedContent"
          @updated="refresh()"
        />

        <LessonsAttendanceEditor
          v-show="activeTab === 'attendance'"
          :lesson="data"
          @updated="refresh()"
        />
      </div>
    </template>
  </UDashboardPanel>
</template>
