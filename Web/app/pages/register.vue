<script setup lang="ts">
import * as z from 'zod'
import type { FormSubmitEvent } from '@nuxt/ui'

definePageMeta({ layout: 'landing' })

useSeoMeta({
  title: 'Criar conta - Estud',
  description: 'Crie sua conta gratuitamente e comece a usar o Estud.',
})

const config = useRuntimeConfig()
const toast = useToast()

interface SocialLoginAvailability {
  googleEnabled: boolean
  googleClientId: string | null
}

const { data: socialLogin } = await useFetch<SocialLoginAvailability>(
  `${config.public.backendUrl}/identity/social-login/check-availability`,
  { default: () => ({ googleEnabled: false, googleClientId: null }) }
)

const googleLoading = ref(false)

const schema = z.object({
  email: z.string().email('E-mail inválido')
})

type Schema = z.output<typeof schema>

const state = reactive<Partial<Schema>>({ email: '' })
const loading = ref(false)
const success = ref(false)

function registerWithGoogle() {
  googleLoading.value = true
  const email = state.email?.trim()
  const params = email ? `?email=${encodeURIComponent(email)}` : ''
  const url = `${config.public.backendUrl}/identity/social-login/challenge/Google${params}`
  requestAnimationFrame(() => { window.location.href = url })
}

async function onSubmit(event: FormSubmitEvent<Schema>) {
  loading.value = true
  try {
    await $fetch(`${config.public.backendUrl}/users/register`, {
      method: 'POST',
      body: { email: event.data.email }
    })
    success.value = true
  } catch (err: unknown) {
    const msg = (err as { data?: { message?: string } })?.data?.message ?? 'Erro ao criar conta. Tente novamente.'
    toast.add({ title: 'Erro', description: msg, color: 'error' })
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="flex items-start justify-center px-4 pt-4 md:pt-[8vh]">
    <div class="w-full max-w-sm">
      <div class="text-center space-y-1 mb-8">
        <h1 class="text-2xl font-bold">
          Criar conta
        </h1>
        <p class="text-muted text-sm">
          Preencha seus dados para criar sua conta.
        </p>
      </div>

      <div v-if="success" class="py-4 text-center space-y-3">
        <UIcon name="i-lucide-mail-check" class="size-12 text-primary mx-auto" />
        <h2 class="text-lg font-semibold">
          Verifique seu e-mail
        </h2>
        <p class="text-muted text-sm">
          Enviamos um link de acesso para <strong>{{ state.email }}</strong>.<br>
          Clique no link para concluir seu cadastro.
        </p>
      </div>

      <UForm
        v-else
        :schema="schema"
        :state="state"
        class="space-y-4"
        @submit="onSubmit"
      >
        <UFormField label="E-mail" name="email" required>
          <UInput
            v-model="state.email"
            type="email"
            placeholder="seu@email.com"
            class="w-full"
            autocomplete="email"
          />
        </UFormField>

        <UButton
          type="submit"
          label="Criar conta"
          class="w-full justify-center"
          :loading="loading"
        />
      </UForm>

      <div v-if="!success && socialLogin?.googleEnabled" class="flex items-center gap-3 my-6">
        <div class="flex-1 border-t border-gray-300 dark:border-gray-600" />
        <span class="text-sm text-gray-500">ou</span>
        <div class="flex-1 border-t border-gray-300 dark:border-gray-600" />
      </div>

      <UButton
        v-if="!success && socialLogin?.googleEnabled"
        color="neutral"
        variant="outline"
        size="lg"
        block
        :loading="googleLoading"
        @click="registerWithGoogle"
      >
        <template #leading>
          <svg class="w-5 h-5" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4" />
            <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853" />
            <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05" />
            <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335" />
          </svg>
        </template>
        Continuar com Google
      </UButton>

      <p v-if="!success" class="text-center text-sm text-muted mt-4">
        Já tem uma conta?
        <NuxtLink to="/login" class="text-primary hover:underline font-medium">
          Entrar
        </NuxtLink>
      </p>
    </div>
  </div>
</template>
