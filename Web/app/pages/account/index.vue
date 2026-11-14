<script setup lang="ts">
import * as z from 'zod'
import type { FormSubmitEvent } from '@nuxt/ui'

const { account, updateAccount, updateProfilePhoto, removeProfilePhoto } = useUserAccount()

const config = useRuntimeConfig()
const loggingOut = useState('loggingOut', () => false)

const isMobile = useIsMobile()
const toast = useToast()
const editNameOpen = ref(false)
const loading = ref(false)

const uploadingPhoto = ref(false)
const photoOpen = ref(false)
const removePhotoOpen = ref(false)
const removingPhoto = ref(false)

const schema = z.object({
  name: z.string({ error: 'Campo obrigatório' }).min(1, 'Nome obrigatório').max(100, 'Máximo 100 caracteres'),
})

type Schema = z.output<typeof schema>

const formState = reactive<Partial<Schema>>({ name: '' })

watch(editNameOpen, (val) => {
  formState.name = val ? (account.value?.name ?? '') : ''
})

async function onSubmit(event: FormSubmitEvent<Schema>) {
  loading.value = true
  try {
    await updateAccount(event.data.name)
    toast.add({ title: 'Nome atualizado com sucesso', color: 'success' })
    editNameOpen.value = false
  } catch (err: unknown) {
    const msg = (err as { data?: { message?: string } })?.data?.message ?? 'Erro ao atualizar nome.'
    toast.add({ title: 'Erro', description: msg, color: 'error' })
  } finally {
    loading.value = false
  }
}

async function onPhotoCropped(photo: Blob) {
  uploadingPhoto.value = true
  try {
    await updateProfilePhoto(photo)
    toast.add({ title: 'Foto atualizada com sucesso', color: 'success' })
    photoOpen.value = false
  } catch (err: unknown) {
    const msg = (err as { data?: { message?: string } })?.data?.message ?? 'Erro ao atualizar foto.'
    toast.add({ title: 'Erro', description: msg, color: 'error' })
  } finally {
    uploadingPhoto.value = false
  }
}

async function onRemovePhoto() {
  removingPhoto.value = true
  try {
    await removeProfilePhoto()
    toast.add({ title: 'Foto removida com sucesso', color: 'success' })
    removePhotoOpen.value = false
  } catch (err: unknown) {
    const msg = (err as { data?: { message?: string } })?.data?.message ?? 'Erro ao remover foto.'
    toast.add({ title: 'Erro', description: msg, color: 'error' })
  } finally {
    removingPhoto.value = false
  }
}

async function logout() {
  loggingOut.value = true
  try {
    await $fetch(`${config.public.backendUrl}/identity/logout`, {
      method: 'POST',
      credentials: 'include',
    })
    account.value = null
    usePostHog()?.reset()
    await navigateTo('/')
  } finally {
    loggingOut.value = false
  }
}
</script>

<template>
  <div>
    <UPageCard
      title="Geral"
      description="Informações da sua conta."
      variant="naked"
      class="mb-4"
    />

    <UPageCard variant="subtle" :ui="{ container: 'p-0 sm:p-0 gap-y-0' }">
      <div class="flex flex-col sm:flex-row items-center sm:items-start gap-6 p-6 sm:p-8">
        <button
          type="button"
          class="group relative shrink-0 rounded-full focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-primary"
          aria-label="Alterar foto de perfil"
          @click="() => { photoOpen = true }"
        >
          <UAvatar
            :src="account?.profilePhoto ?? undefined"
            :alt="account?.name"
            class="size-24 ring-4 ring-(--ui-bg) shadow-sm"
            :ui="{ fallback: 'text-3xl' }"
          />
          <span class="absolute inset-0 flex items-center justify-center rounded-full bg-black/40 opacity-0 transition-opacity group-hover:opacity-100">
            <UIcon name="i-lucide-camera" class="size-6 text-white" />
          </span>
          <span class="absolute bottom-0 right-0 flex size-8 items-center justify-center rounded-full bg-default ring-1 ring-default shadow-sm">
            <UIcon name="i-lucide-camera" class="size-4 text-muted" />
          </span>
        </button>

        <div class="flex min-w-0 flex-1 flex-col items-center sm:items-start text-center sm:text-left">
          <div class="flex items-center gap-1">
            <h2 class="text-2xl font-semibold tracking-tight text-highlighted break-words">
              {{ account?.name }}
            </h2>
            <UButton
              icon="i-lucide-pencil"
              size="sm"
              color="neutral"
              variant="ghost"
              aria-label="Editar nome"
              @click="() => { editNameOpen = true }"
            />
          </div>

          <p class="text-muted break-all">{{ account?.email }}</p>

          <div class="mt-4">
            <UBadge :label="account?.role" color="primary" variant="soft" />
          </div>

          <div class="mt-2 flex flex-wrap items-center justify-center sm:justify-start gap-x-4 gap-y-2 text-sm text-toned">
            <span class="flex items-center gap-1.5">
              <UIcon name="i-lucide-building-2" class="size-4 text-dimmed" />
              {{ account?.institution }}
            </span>
            <span v-if="account?.course" class="flex items-center gap-1.5">
              <UIcon name="i-lucide-graduation-cap" class="size-4 text-dimmed" />
              {{ account.course }}
            </span>
          </div>
        </div>
      </div>

      <div class="flex justify-end border-t border-default px-6 py-3 sm:px-8">
        <UButton
          label="Sair da conta"
          icon="i-lucide-log-out"
          color="neutral"
          variant="ghost"
          :loading="loggingOut"
          @click="logout"
        />
      </div>
    </UPageCard>
  </div>

  <UModal
    v-model:open="editNameOpen"
    title="Editar nome"
    :fullscreen="isMobile"
    description="Atualize o seu nome de exibição."
  >
    <template #body>
      <UForm
        :schema="schema"
        :state="formState"
        class="space-y-4"
        @submit="onSubmit"
      >
        <UFormField label="Nome" name="name">
          <UInput v-model="formState.name" class="w-full" placeholder="Seu nome" />
        </UFormField>

        <div class="flex justify-end gap-2 pt-2">
          <UButton
            label="Cancelar"
            color="neutral"
            variant="subtle"
            :disabled="loading"
            @click="() => { editNameOpen = false }"
          />
          <UButton
            label="Salvar"
            type="submit"
            :loading="loading"
          />
        </div>
      </UForm>
    </template>
  </UModal>

  <AccountProfilePhotoModal
    v-model:open="photoOpen"
    :photo="account?.profilePhoto ?? null"
    :name="account?.name"
    :loading="uploadingPhoto"
    @save="onPhotoCropped"
    @remove="() => { photoOpen = false; removePhotoOpen = true }"
  />

  <UModal
    v-model:open="removePhotoOpen"
    title="Remover foto"
    description="Sua foto de perfil será apagada e substituída pelas iniciais do seu nome."
  >
    <template #footer>
      <div class="flex justify-end gap-2 w-full">
        <UButton
          label="Cancelar"
          color="neutral"
          variant="subtle"
          :disabled="removingPhoto"
          @click="() => { removePhotoOpen = false }"
        />
        <UButton
          label="Remover"
          color="error"
          :loading="removingPhoto"
          @click="onRemovePhoto"
        />
      </div>
    </template>
  </UModal>
</template>
