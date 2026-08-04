<script setup lang="ts">
import type { OpeningHoursDay } from '~/types/campi'

const open = defineModel<boolean>('open', { default: false })
const props = defineProps<{ campusId: number, days: OpeningHoursDay[] }>()
const emit = defineEmits<{ saved: [] }>()

const isMobile = useIsMobile()
const config = useRuntimeConfig()
const toast = useToast()
const saving = ref(false)

const hourOptions = buildOpeningHourOptions()

interface WindowRow {
  key: number
  start: string | undefined
  end: string | undefined
}
interface DayRow {
  day: string
  label: string
  open: boolean
  windows: WindowRow[]
}

let nextKey = 0

// Sem herança não há terceiro estado: o dia está aberto ou fechado. Fechado é a
// ausência de janelas, e é assim que ele vai pro backend.
function toRows(days: OpeningHoursDay[]): DayRow[] {
  return campusWeekDays.map((weekDay) => {
    const windows = days.find(d => d.day === weekDay.key)?.windows ?? []
    return {
      day: weekDay.key,
      label: weekDay.label,
      open: windows.length > 0,
      windows: windows.map(w => ({ key: nextKey++, start: w.start, end: w.end })),
    }
  })
}

const rows = ref<DayRow[]>([])

watch(open, (isOpen) => {
  if (isOpen) rows.value = toRows(props.days)
})

// Abrir um dia que nunca teve janela precisa de algo pra editar; o padrão do
// seed é o palpite menos surpreendente.
function toggleDay(row: DayRow, value: boolean) {
  row.open = value
  if (value && row.windows.length === 0) {
    row.windows = [{ key: nextKey++, start: 'H07_00', end: 'H22_00' }]
  }
}

function addWindow(row: DayRow) {
  row.windows = [...row.windows, { key: nextKey++, start: undefined, end: undefined }]
}

function removeWindow(row: DayRow, key: number) {
  row.windows = row.windows.filter(w => w.key !== key)
  if (row.windows.length === 0) row.open = false
}

// A maioria dos campi repete a mesma janela de segunda a sexta, e preencher
// cinco linhas na mão é atrito puro.
function applyMondayToWeekdays() {
  const monday = rows.value[0]
  if (!monday) return

  rows.value = rows.value.map((row, index) => {
    if (index === 0 || index > 4) return row
    return {
      ...row,
      open: monday.open,
      windows: monday.windows.map(w => ({ key: nextKey++, start: w.start, end: w.end })),
    }
  })
}

function windowIncomplete(w: WindowRow) {
  return !w.start || !w.end
}

function windowBadRange(w: WindowRow) {
  return !!w.start && !!w.end && openingHourValue(w.start) >= openingHourValue(w.end)
}

// Janelas sobrepostas somariam minutos que o campus não tem — o backend recusa,
// mas o erro é muito mais útil antes do submit.
function windowOverlaps(row: DayRow, w: WindowRow) {
  if (windowIncomplete(w) || windowBadRange(w)) return false
  return row.windows.some(other =>
    other.key !== w.key
    && !windowIncomplete(other)
    && !windowBadRange(other)
    && openingHourValue(w.start!) < openingHourValue(other.end!)
    && openingHourValue(other.start!) < openingHourValue(w.end!),
  )
}

function windowError(row: DayRow, w: WindowRow) {
  if (windowIncomplete(w)) return 'Preencha o início e o fim'
  if (windowBadRange(w)) return 'O fim precisa ser depois do início'
  if (windowOverlaps(row, w)) return 'Este intervalo se sobrepõe a outro do mesmo dia'
  return null
}

const hasErrors = computed(() =>
  rows.value.some(row => row.open && row.windows.some(w => windowError(row, w) !== null)),
)

const isClosedAllWeek = computed(() => rows.value.every(row => !row.open || !row.windows.length))

async function save() {
  if (hasErrors.value) return
  saving.value = true
  try {
    await $fetch(`${config.public.backendUrl}/campi/${props.campusId}/opening-hours`, {
      method: 'PUT',
      body: {
        days: rows.value.map(row => ({
          day: row.day,
          windows: row.open ? row.windows.map(w => ({ start: w.start, end: w.end })) : [],
        })),
      },
      credentials: 'include',
    })
    toast.add({ title: 'Horários de funcionamento atualizados', color: 'success' })
    open.value = false
    emit('saved')
  } catch (err: unknown) {
    const msg = (err as { data?: { message?: string } })?.data?.message ?? 'Erro ao atualizar os horários de funcionamento.'
    toast.add({ title: 'Erro', description: msg, color: 'error' })
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <UModal
    v-model:open="open"
    title="Horários de funcionamento"
    :fullscreen="isMobile"
    description="Defina em que dias e horários este campus abre. Dia fechado sai do mapa de ocupação."
  >
    <template #body>
      <div class="flex flex-col gap-4">
        <div class="flex justify-end">
          <UButton
            icon="i-lucide-copy"
            label="Aplicar segunda a toda a semana"
            color="neutral"
            variant="ghost"
            size="xs"
            @click="() => { applyMondayToWeekdays() }"
          />
        </div>

        <div
          v-for="row in rows"
          :key="row.day"
          class="flex flex-col gap-3 rounded-xl border border-default p-4"
        >
          <div class="flex items-center justify-between gap-3">
            <span class="font-medium text-highlighted">{{ row.label }}</span>
            <USwitch
              :model-value="row.open"
              :label="row.open ? 'Aberto' : 'Fechado'"
              @update:model-value="(value: boolean) => { toggleDay(row, value) }"
            />
          </div>

          <template v-if="row.open">
            <div
              v-for="window in row.windows"
              :key="window.key"
              class="flex flex-col gap-1"
            >
              <div class="flex items-center gap-2">
                <USelect
                  v-model="window.start"
                  :items="hourOptions"
                  value-key="value"
                  class="flex-1"
                  placeholder="Início"
                />
                <span class="text-muted">–</span>
                <USelect
                  v-model="window.end"
                  :items="hourOptions"
                  value-key="value"
                  class="flex-1"
                  placeholder="Fim"
                />
                <UButton
                  icon="i-lucide-trash-2"
                  color="neutral"
                  variant="ghost"
                  size="sm"
                  aria-label="Remover intervalo"
                  @click="() => { removeWindow(row, window.key) }"
                />
              </div>
              <p v-if="windowError(row, window)" class="text-xs text-error">
                {{ windowError(row, window) }}
              </p>
            </div>

            <!-- Dois intervalos no mesmo dia (fecha pro almoço) é minoria, então o
                 botão fica discreto em vez de disputar espaço com o caso comum. -->
            <div>
              <UButton
                icon="i-lucide-plus"
                label="Adicionar intervalo"
                color="neutral"
                variant="ghost"
                size="xs"
                @click="() => { addWindow(row) }"
              />
            </div>
          </template>
        </div>

        <p v-if="isClosedAllWeek" class="flex items-start gap-2 text-xs text-muted">
          <UIcon name="i-lucide-triangle-alert" class="size-4 shrink-0 text-warning" />
          <span>Com todos os dias fechados o campus fica sem horário nenhum, e o mapa de ocupação some.</span>
        </p>

        <div class="flex justify-end gap-2 pt-2">
          <UButton
            label="Cancelar"
            color="neutral"
            variant="subtle"
            :disabled="saving"
            @click="() => { open = false }"
          />
          <UButton
            label="Salvar"
            :loading="saving"
            :disabled="hasErrors"
            @click="() => { save() }"
          />
        </div>
      </div>
    </template>
  </UModal>
</template>
