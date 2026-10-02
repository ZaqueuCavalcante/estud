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

    <UPageCard variant="subtle">
    <div class="flex flex-col divide-y divide-default">
      <div class="flex max-sm:flex-col justify-between items-start sm:items-center gap-4 py-4 first:pt-0 last:pb-0">
        <div>
          <p class="text-sm text-muted">Foto de perfil</p>
        </div>
        <div class="relative">
          <UAvatar
            :src="account?.profilePhoto ?? undefined"
            :alt="account?.name"
            size="3xl"
            class="cursor-pointer"
            @click="() => { photoOpen = true }"
          />
        </div>
      </div>

      <div class="flex max-sm:flex-col justify-between items-start gap-4 py-4 first:pt-0 last:pb-0">
        <div>
          <p class="text-sm text-muted">Nome</p>
        </div>
        <div class="flex items-center gap-2">
          <p class="text-sm font-medium text-highlighted">{{ account?.name }}</p>
          <UButton
            icon="i-lucide-pencil"
            size="xs"
            color="neutral"
            variant="ghost"
            aria-label="Editar nome"
            @click="() => { editNameOpen = true }"
          />
        </div>
      </div>

      <div class="flex max-sm:flex-col justify-between items-start gap-4 py-4 first:pt-0 last:pb-0">
        <div>
          <p class="text-sm text-muted">Email</p>
        </div>
        <p class="text-sm font-medium text-highlighted">{{ account?.email }}</p>
      </div>

      <div class="flex max-sm:flex-col justify-between items-start gap-4 py-4 first:pt-0 last:pb-0">
        <div>
          <p class="text-sm text-muted">Instituição</p>
        </div>
        <p class="text-sm font-medium text-highlighted">{{ account?.institution }}</p>
      </div>

      <div class="flex max-sm:flex-col justify-between items-start gap-4 py-4 first:pt-0 last:pb-0">
        <div>
          <p class="text-sm text-muted">Perfil de acesso</p>
        </div>
        <p class="text-sm font-medium text-highlighted">{{ account?.role }}</p>
      </div>

      <div v-if="account?.course" class="flex max-sm:flex-col justify-between items-start gap-4 py-4 first:pt-0 last:pb-0">
        <div>
          <p class="text-sm text-muted">Curso</p>
        </div>
        <p class="text-sm font-medium text-highlighted">{{ account?.course }}</p>
      </div>

      <div class="flex justify-end py-4 first:pt-0 last:pb-0">
        <UButton
          label="Sair"
          icon="i-lucide-log-out"
          color="neutral"
          variant="subtle"
          :loading="loggingOut"
          @click="logout"
        />
      </div>
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
