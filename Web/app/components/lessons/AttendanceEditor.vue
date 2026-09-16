<script setup lang="ts">
import type { GetTeacherClassLessonOut } from '~/types/classes'

const props = defineProps<{ lesson: GetTeacherClassLessonOut }>()
const emit = defineEmits<{ updated: [] }>()

const config = useRuntimeConfig()
const toast = useToast()

const editing = ref(false)
const saving = ref(false)
const presentIds = ref<number[]>([])

const students = computed(() => props.lesson.students)
const taken = computed(() => props.lesson.status === 'Finalized')
const isFuture = computed(() => isFutureClassLesson(props.lesson))

const savedPresentIds = computed(() => students.value.filter(s => s.present).map(s => s.id))

const presentCount = computed(() => editing.value ? presentIds.value.length : savedPresentIds.value.length)

// Uma chamada nunca feita pode ser salva sem mexer em nada: é o registro de que
// todos faltaram.
const dirty = computed(() => !taken.value
  || presentIds.value.length !== savedPresentIds.value.length
  || presentIds.value.some(id => !savedPresentIds.value.includes(id)),
)

function isPresent(id: number) {
  return presentIds.value.includes(id)
}

function toggle(id: number) {
  presentIds.value = isPresent(id)
    ? presentIds.value.filter(x => x !== id)
    : [...presentIds.value, id]
}

// O clique vale na linha inteira, mas o próprio checkbox já emite o seu — sem
// esse desvio a linha alternaria duas vezes ao clicar nele.
function onRowClick(event: MouseEvent, id: number) {
  if ((event.target as HTMLElement).closest('.student-checkbox')) return
  toggle(id)
}

function startEditing() {
  presentIds.value = [...savedPresentIds.value]
  editing.value = true
}

function cancelEditing() {
  editing.value = false
  presentIds.value = []
}

async function save() {
  saving.value = true
  try {
    await $fetch(`${config.public.backendUrl}/teachers/lessons/${props.lesson.id}/attendance`, {
      method: 'PUT',
      body: { presentStudents: presentIds.value },
      credentials: 'include',
    })

    toast.add({ title: 'Chamada salva', color: 'success' })
    cancelEditing()
    emit('updated')
  } catch (err: unknown) {
    toast.add({
      title: 'Não foi possível salvar a chamada',
      description: (err as { data?: { message?: string } })?.data?.message ?? 'Tente novamente.',
      color: 'error',
    })
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <section class="flex flex-col gap-4">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <p class="text-sm text-muted">
        A presença dos alunos matriculados na turma.
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
          v-else-if="taken && students.length"
          icon="i-lucide-pencil"
          label="Editar"
          color="neutral"
          variant="subtle"
          @click="(e: MouseEvent) => { (e.currentTarget as HTMLElement).blur(); startEditing() }"
        />
      </div>
    </div>

    <TableEmptyState
      v-if="!students.length"
      :loading="false"
      icon="i-lucide-users"
      message="Nenhum aluno matriculado na turma"
    />

    <TableEmptyState
      v-else-if="!taken && !editing && isFuture"
      :loading="false"
      icon="i-lucide-calendar-clock"
      message="A chamada fica disponível no dia da aula"
    />

    <TableEmptyState
      v-else-if="!taken && !editing"
      :loading="false"
      icon="i-lucide-clipboard-check"
      message="A chamada desta aula ainda não foi feita"
      button-label="Fazer chamada"
      @create="() => { startEditing() }"
    />

    <template v-else>
      <div class="flex flex-wrap items-center justify-between gap-3">
        <div v-if="editing" class="flex flex-wrap gap-2">
          <UButton
            label="Marcar todos"
            icon="i-lucide-check-check"
            color="neutral"
            variant="subtle"
            size="sm"
            @click="() => { presentIds = students.map(s => s.id) }"
          />
          <UButton
            label="Desmarcar todos"
            icon="i-lucide-x"
            color="neutral"
            variant="subtle"
            size="sm"
            @click="() => { presentIds = [] }"
          />
        </div>

        <span class="ml-auto text-sm text-muted">
          {{ presentCount }} / {{ students.length }} {{ presentCount === 1 ? 'presente' : 'presentes' }}
        </span>
      </div>

      <ul v-if="!editing" class="divide-y divide-default border-y border-default">
        <li
          v-for="student in students"
          :key="student.id"
          class="flex items-center gap-4 px-1 py-3"
        >
          <span class="min-w-0 flex-1 truncate text-sm text-muted">{{ student.name }}</span>
          <UBadge
            :label="student.present ? 'Presente' : 'Falta'"
            :color="student.present ? 'success' : 'error'"
            :icon="student.present ? 'i-lucide-check' : 'i-lucide-x'"
            variant="subtle"
          />
        </li>
      </ul>

      <ul v-else class="divide-y divide-default border-y border-default">
        <li v-for="student in students" :key="student.id">
          <div
            class="flex w-full cursor-pointer items-center gap-3 px-1 py-3 transition-colors hover:bg-elevated/40"
            @click="(e) => { onRowClick(e, student.id) }"
          >
            <UCheckbox
              class="student-checkbox"
              :model-value="isPresent(student.id)"
              :aria-label="student.name"
              @update:model-value="() => { toggle(student.id) }"
            />
            <span class="min-w-0 flex-1 truncate text-sm text-muted">{{ student.name }}</span>
          </div>
        </li>
      </ul>
    </template>
  </section>
</template>
