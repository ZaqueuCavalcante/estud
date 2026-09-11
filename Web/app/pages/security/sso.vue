<script setup lang="ts">
import type { GetSsoConfigurationOut } from '~/types'

const config = useRuntimeConfig()
const toast = useToast()
const createModalOpen = ref(false)
const editModalOpen = ref(false)
const verifyingDomain = ref<string | null>(null)

const { data: ssoConfig, status, refresh } = await useFetch<GetSsoConfigurationOut | null>(
  `${config.public.backendUrl}/identity/sso/configuration`,
  { credentials: 'include', server: false },
)

const providerLabels: Record<string, string> = {
  AzureAd: 'Azure AD',
  GoogleWorkspace: 'Google Workspace',
  Okta: 'Okta',
  Auth0: 'Auth0',
  CustomOidc: 'OIDC Personalizado',
}

function copy(value: string) {
  navigator.clipboard.writeText(value)
  toast.add({ title: 'Copiado', color: 'success' })
}

async function verifyDomain(domain: string) {
  if (!ssoConfig.value) return

  verifyingDomain.value = domain
  try {
    await $fetch(`${config.public.backendUrl}/identity/sso/configurations/${ssoConfig.value.id}/domains/verify`, {
      method: 'POST',
      body: { domain },
      credentials: 'include',
    })
    toast.add({ title: 'Domínio verificado com sucesso', color: 'success' })
    await refresh()
  } catch (err: unknown) {
    const msg = (err as { data?: { message?: string } })?.data?.message ?? 'Erro ao verificar o domínio.'
    toast.add({ title: 'Erro', description: msg, color: 'error' })
  } finally {
    verifyingDomain.value = null
  }
}
</script>

<template>
  <div class="w-full lg:max-w-2xl mx-auto min-w-0">
    <template v-if="ssoConfig">
      <UPageCard
        title="Configuração Single Sign-On"
        description="Provedor configurado para sua instituição."
        variant="naked"
        orientation="horizontal"
        class="mb-4"
      >
        <UButton
          icon="i-lucide-pencil"
          label="Editar"
          color="neutral"
          variant="subtle"
          class="w-fit lg:ms-auto"
          @click="() => { editModalOpen = true }"
        />
      </UPageCard>

      <UPageCard variant="subtle">
        <div class="flex flex-col gap-1 sm:flex-row sm:justify-between sm:items-center sm:gap-3 py-1">
          <span class="text-sm font-medium shrink-0">Provedor</span>
          <span class="text-sm text-muted wrap-break-word sm:text-right">{{ providerLabels[ssoConfig.providerType] }}</span>
        </div>
        <USeparator />
        <div class="flex flex-col gap-1 sm:flex-row sm:justify-between sm:items-center sm:gap-3 py-1">
          <span class="text-sm font-medium shrink-0">Authority URL</span>
          <span class="text-sm text-muted break-all min-w-0 sm:text-right">{{ ssoConfig.authority }}</span>
        </div>
        <USeparator />
        <div class="flex flex-col gap-1 sm:flex-row sm:justify-between sm:items-center sm:gap-3 py-1">
          <span class="text-sm font-medium shrink-0">Client ID</span>
          <span class="text-sm text-muted font-mono break-all min-w-0 sm:text-right">{{ ssoConfig.clientId }}</span>
        </div>
        <USeparator />
        <div class="flex flex-col gap-1 sm:flex-row sm:justify-between sm:items-center sm:gap-3 py-1">
          <span class="text-sm font-medium shrink-0">SSO Obrigatório</span>
          <UBadge
            class="w-fit"
            :label="ssoConfig.requireSso ? 'Sim' : 'Não'"
            :color="ssoConfig.requireSso ? 'success' : 'neutral'"
            variant="subtle"
          />
        </div>
        <USeparator />
        <div class="flex flex-col gap-1 sm:flex-row sm:justify-between sm:items-center sm:gap-3 py-1">
          <span class="text-sm font-medium shrink-0">Configuração Ativa</span>
          <UBadge
            class="w-fit"
            :label="ssoConfig.isActive ? 'Sim' : 'Não'"
            :color="ssoConfig.isActive ? 'success' : 'neutral'"
            variant="subtle"
          />
        </div>
      </UPageCard>

      <UPageCard
        title="Domínios"
        description="O SSO só vale para um domínio depois que a instituição comprovar que o controla, publicando um registro TXT no DNS."
        variant="naked"
        class="mt-8 mb-4"
      />

      <UPageCard
        v-for="domain in ssoConfig.domains"
        :key="domain.domain"
        variant="subtle"
        class="mb-4"
      >
        <div class="flex flex-col gap-2 sm:flex-row sm:justify-between sm:items-center sm:gap-3">
          <span class="text-sm font-medium break-all min-w-0">{{ domain.domain }}</span>
          <UBadge
            class="w-fit"
            :label="domain.status === 'Verified' ? 'Verificado' : 'Pendente'"
            :color="domain.status === 'Verified' ? 'success' : 'warning'"
            variant="subtle"
          />
        </div>

        <template v-if="domain.status === 'Pending'">
          <USeparator />
          <p class="text-sm text-muted">
            Crie o registro abaixo no provedor de DNS do domínio e clique em verificar.
            A propagação pode levar alguns minutos.
          </p>

          <div class="flex flex-col gap-1">
            <span class="text-xs font-medium text-muted">Tipo</span>
            <span class="text-sm font-mono">TXT</span>
          </div>
          <div class="flex flex-col gap-1">
            <span class="text-xs font-medium text-muted">Nome</span>
            <div class="flex items-center gap-2 min-w-0">
              <code class="text-sm break-all min-w-0">{{ domain.txtRecordName }}</code>
              <UButton
                icon="i-lucide-copy"
                color="neutral"
                variant="ghost"
                size="xs"
                aria-label="Copiar nome"
                @click="() => { copy(domain.txtRecordName) }"
              />
            </div>
          </div>
          <div class="flex flex-col gap-1">
            <span class="text-xs font-medium text-muted">Valor</span>
            <div class="flex items-center gap-2 min-w-0">
              <code class="text-sm break-all min-w-0">{{ domain.txtRecordValue }}</code>
              <UButton
                icon="i-lucide-copy"
                color="neutral"
                variant="ghost"
                size="xs"
                aria-label="Copiar valor"
                @click="() => { copy(domain.txtRecordValue) }"
              />
            </div>
          </div>

          <UButton
            icon="i-lucide-shield-check"
            label="Verificar domínio"
            class="w-fit"
            :loading="verifyingDomain === domain.domain"
            :disabled="verifyingDomain !== null"
            @click="() => { verifyDomain(domain.domain) }"
          />
        </template>
      </UPageCard>
    </template>

    <TableEmptyState
      v-else
      :loading="status === 'pending'"
      icon="i-lucide-key-round"
      message="Nenhuma configuração SSO cadastrada"
      button-label="Configuração"
      @create="createModalOpen = true"
    />
  </div>

  <SecuritySsoAddSsoConfigurationModal v-model:open="createModalOpen" @created="refresh()" />
  <SecuritySsoEditSsoConfigurationModal
    v-if="ssoConfig"
    :sso-config="ssoConfig"
    v-model:open="editModalOpen"
    @updated="refresh()"
  />
</template>
