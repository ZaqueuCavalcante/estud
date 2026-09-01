<script setup lang="ts">
import type { ValidateEnrollmentProofOut } from '~/types/documents'

definePageMeta({ layout: 'landing' })

useSeoMeta({
  title: 'Validar comprovante de matrícula — Estud',
  description: 'Confira a autenticidade de um comprovante de matrícula pelo seu código de verificação.'
})

const route = useRoute()
const router = useRouter()
const config = useRuntimeConfig()

const code = ref(((route.query.code as string) ?? '').trim())
const loading = ref(false)
const proof = ref<ValidateEnrollmentProofOut | null>(null)
const notFound = ref(false)

async function validate() {
  const value = code.value.trim()
  if (!value || loading.value) return

  loading.value = true
  proof.value = null
  notFound.value = false

  router.replace({ query: { code: value } })

  try {
    proof.value = await $fetch<ValidateEnrollmentProofOut>(
      `${config.public.backendUrl}/students/enrollment-proofs/${encodeURIComponent(value)}/validate`,
      { method: 'POST' }
    )
  } catch {
    notFound.value = true
  } finally {
    loading.value = false
  }
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

const fields = computed(() => {
  const data = proof.value
  if (!data) return []
  return [
    { label: 'Aluno', value: data.studentName },
    { label: 'Matrícula', value: data.enrollmentCode },
    { label: 'Instituição', value: data.institution },
    { label: 'Curso', value: data.course },
    { label: 'Campus', value: data.campus },
    { label: 'Período', value: data.period },
    { label: 'Turno', value: courseSessionLabels[data.session] ?? data.session },
    { label: 'Emissão', value: formatDateTime(data.issuedAt) },
  ]
})

onMounted(() => { validate() })
</script>

<template>
  <UContainer class="py-16 max-w-2xl">
    <h1 class="text-3xl font-bold text-highlighted mb-2">
      Validar comprovante de matrícula
    </h1>
    <p class="text-muted mb-8">
      Informe o código de verificação impresso no comprovante para conferir sua autenticidade.
    </p>

    <form class="flex flex-col sm:flex-row gap-2" @submit.prevent="() => { validate() }">
      <UInput
        v-model="code"
        class="flex-1"
        icon="i-lucide-shield-check"
        placeholder="ESTUD-2026-3F9A1B7C2D"
        autocomplete="off"
        :ui="{ base: 'font-mono' }"
      />
      <UButton
        type="submit"
        label="Validar"
        icon="i-lucide-search"
        :loading="loading"
        :disabled="!code.trim()"
      />
    </form>

    <div v-if="loading" class="flex justify-center py-16">
      <AppSpinner class="size-8 text-primary" />
    </div>

    <UPageCard v-else-if="proof" variant="subtle" class="mt-8">
      <div class="flex items-center gap-3">
        <UIcon name="i-lucide-circle-check" class="size-8 shrink-0 text-success" />
        <div>
          <p class="font-semibold text-highlighted">
            Comprovante autêntico
          </p>
          <p class="text-sm text-muted">
            Emitido pelo Estud e conferido nesta consulta.
          </p>
        </div>
      </div>

      <USeparator class="my-6" />

      <dl class="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div v-for="field in fields" :key="field.label">
          <dt class="text-xs uppercase tracking-wide text-muted">
            {{ field.label }}
          </dt>
          <dd class="text-sm text-highlighted">
            {{ field.value }}
          </dd>
        </div>
        <div class="sm:col-span-2">
          <dt class="text-xs uppercase tracking-wide text-muted">
            Código de verificação
          </dt>
          <dd class="text-sm font-mono text-highlighted">
            {{ proof.code }}
          </dd>
        </div>
      </dl>
    </UPageCard>

    <UPageCard v-else-if="notFound" variant="subtle" class="mt-8">
      <div class="flex items-center gap-3">
        <UIcon name="i-lucide-circle-x" class="size-8 shrink-0 text-error" />
        <div>
          <p class="font-semibold text-highlighted">
            Comprovante não encontrado
          </p>
          <p class="text-sm text-muted">
            Nenhum comprovante foi emitido com esse código. Confira se ele foi digitado corretamente.
          </p>
        </div>
      </div>
    </UPageCard>
  </UContainer>
</template>
