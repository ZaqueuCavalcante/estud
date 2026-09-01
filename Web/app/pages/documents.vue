<script setup lang="ts">
import type { TableColumn } from '@nuxt/ui'
import type { EnrollmentProofItem, GetEnrollmentProofsOut } from '~/types/documents'

const UButton = resolveComponent('UButton')
const UTooltip = resolveComponent('UTooltip')

const toast = useToast()
const config = useRuntimeConfig()

const { data, status, refresh } = await useFetch<GetEnrollmentProofsOut>(
  `${config.public.backendUrl}/students/enrollment-proofs`,
  { credentials: 'include', server: false }
)

const generating = ref(false)

function formatDateTime(value: string) {
  return new Date(value).toLocaleString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function download(pdf: Blob, fileName: string) {
  const url = URL.createObjectURL(pdf)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  link.click()
  URL.revokeObjectURL(url)
}

// Com `responseType: 'blob'` o corpo de um 400 também chega como Blob, e não
// como o ErrorOut já desserializado.
async function errorMessage(error: unknown) {
  const fallback = 'Não foi possível emitir o comprovante.'
  try {
    const body = (error as { data?: unknown })?.data
    const raw = body instanceof Blob ? await body.text() : body
    const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw
    return (parsed as { message?: string })?.message ?? fallback
  } catch {
    return fallback
  }
}

async function generate() {
  if (generating.value) return

  generating.value = true
  try {
    const pdf = await $fetch<Blob>(`${config.public.backendUrl}/students/enrollment-proofs`, {
      method: 'POST',
      credentials: 'include',
      responseType: 'blob'
    })

    // O PDF não é armazenado: o código só aparece depois que a lista recarrega.
    await refresh()
    const code = data.value?.items?.[0]?.code
    download(pdf, code ? `comprovante-matricula-${code}.pdf` : 'comprovante-matricula.pdf')

    toast.add({ title: 'Comprovante emitido com sucesso', color: 'success' })
  } catch (error: unknown) {
    toast.add({ title: 'Erro', description: await errorMessage(error), color: 'error' })
  } finally {
    generating.value = false
  }
}

const columns: TableColumn<EnrollmentProofItem>[] = [
  {
    accessorKey: 'code',
    header: 'Código',
    cell: ({ row }) => h('span', { class: 'font-mono' }, row.original.code),
  },
  {
    accessorKey: 'issuedAt',
    header: 'Emissão',
    cell: ({ row }) => formatDateTime(row.original.issuedAt),
  },
  {
    id: 'actions',
    header: '',
    cell: ({ row }) => h('div', { class: 'flex justify-end' }, h(UTooltip, { text: 'Validar comprovante' }, () => h(UButton, {
      icon: 'i-lucide-badge-check',
      color: 'neutral',
      variant: 'ghost',
      size: 'sm',
      to: `/validate-enrollment-proof?code=${row.original.code}`,
      target: '_blank',
      'aria-label': 'Validar comprovante',
    }))),
  },
]
</script>

<template>
  <UDashboardPanel id="documents">
    <template #header>
      <UDashboardNavbar title="Documentos">
        <template #leading>
          <PageIcon />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 pt-4">
        <p class="text-sm text-muted">
          Comprovantes de matrícula emitidos. Cada um tem um código de verificação que permite conferir sua autenticidade online.
        </p>
        <UButton
          v-if="data?.items?.length"
          class="self-start sm:self-auto shrink-0"
          icon="i-lucide-plus"
          label="Comprovante de matrícula"
          :loading="generating"
          @click="() => { generate() }"
        />
      </div>

      <DataTable :data="data?.items ?? []" :columns="columns" :loading="status === 'pending'">
        <template #empty>
          <TableEmptyState
            :loading="status === 'pending'"
            icon="i-lucide-file-text"
            message="Nenhum comprovante de matrícula emitido"
            button-label="Comprovante de matrícula"
            @create="() => { generate() }"
          />
        </template>
      </DataTable>

      <div v-if="(data?.total ?? 0) > 0" class="flex items-center gap-2 mt-4">
        <UBadge color="neutral" variant="subtle" class="h-8 px-3">
          {{ data?.total }} {{ data?.total === 1 ? 'comprovante emitido' : 'comprovantes emitidos' }}
        </UBadge>
      </div>
    </template>
  </UDashboardPanel>
</template>
