<script setup lang="ts">
import type { NuxtError } from '#app'
import { pt_br as ptBR } from '@nuxt/ui/locale'

const props = defineProps<{
  error: NuxtError
}>()

const { account } = useUserAccount()

// O cookie de auth é httpOnly: só o SSR consegue lê-lo, e o valor chega ao client pelo payload.
const hasSession = useState('errorHasSession', () => !!useCookie(BEARER_COOKIE).value)
const logged = computed(() => !!account.value || hasSession.value)

const notFound = computed(() => props.error.status === 404)

const displayed = computed(() => ({
  status: props.error.status,
  statusText: notFound.value ? 'Página não encontrada' : 'Algo deu errado',
  message: notFound.value
    ? 'A página que você procura não existe.'
    : 'Ocorreu um erro inesperado. Tente novamente em instantes.',
}))

useSeoMeta({
  title: () => displayed.value.statusText,
  description: () => displayed.value.message,
})

useHead({
  htmlAttrs: {
    lang: 'pt-BR'
  }
})
</script>

<template>
  <UApp :locale="ptBR" :tooltip="{ delayDuration: 0 }">
    <NuxtLayout v-if="logged" name="default">
      <UDashboardPanel id="error">
        <template #body>
          <UError
            :error="displayed"
            :clear="{ label: 'Voltar ao início' }"
            redirect="/home"
            :ui="{ root: 'min-h-full flex-1' }"
          />
        </template>
      </UDashboardPanel>
    </NuxtLayout>

    <UError
      v-else
      :error="displayed"
      :clear="{ label: 'Voltar ao início' }"
      redirect="/"
    />
  </UApp>
</template>
