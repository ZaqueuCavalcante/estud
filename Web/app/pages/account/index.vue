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

const photoTypes = ['image/png', 'image/jpeg', 'image/webp']
const maxPhotoSize = 3 * 1024 * 1024
const photoInput = ref<HTMLInputElement | null>(null)
const uploadingPhoto = ref(false)
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

async function onPhotoSelected(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file) return

  if (!photoTypes.includes(file.type)) {
    toast.add({ title: 'Formato de foto inválido', description: 'Envie uma imagem PNG, JPEG ou WebP.', color: 'error' })
    return
  }

  if (file.size > maxPhotoSize) {
    toast.add({
      title: 'Foto muito grande',
      description: `${file.name} tem ${formatSize(file.size)}, e o limite é de ${formatSize(maxPhotoSize)}.`,
      color: 'error',
    })
    return
  }

  uploadingPhoto.value = true
  try {
    await updateProfilePhoto(file)
    toast.add({ title: 'Foto atualizada com sucesso', color: 'success' })
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

function formatSize(bytes: number) {
  return `${(bytes / 1024 / 1024).toFixed(1).replace('.', ',')} MB`
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
          <p class="text-sm font-medium text-highlighted">Foto</p>
          <p class="text-xs text-muted">PNG, JPEG ou WebP, até 3 MB.</p>
        </div>
        <div class="flex items-center gap-3">
          <UAvatar :src="account?.profilePhoto ?? undefined" :alt="account?.name" size="3xl" />
          <input
            ref="photoInput"
            type="file"
            class="hidden"
            :accept="photoTypes.join(',')"
            @change="onPhotoSelected"
          >
          <UButton
            label="Alterar"
            size="xs"
            color="neutral"
            variant="subtle"
            :loading="uploadingPhoto"
            :disabled="removingPhoto"
            @click="() => { photoInput?.click() }"
          />
          <UButton
            v-if="account?.profilePhoto"
            label="Remover"
            size="xs"
            color="error"
            variant="ghost"
            :disabled="uploadingPhoto"
            @click="() => { removePhotoOpen = true }"
          />
        </div>
      </div>

      <div class="flex max-sm:flex-col justify-between items-start gap-4 py-4 first:pt-0 last:pb-0">
        <div>
          <p class="text-sm font-medium text-highlighted">Nome</p>
        </div>
        <div class="flex items-center gap-2">
          <p class="text-sm text-muted">{{ account?.name }}</p>
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
          <p class="text-sm font-medium text-highlighted">Email</p>
        </div>
        <p class="text-sm text-muted">{{ account?.email }}</p>
      </div>

      <div class="flex max-sm:flex-col justify-between items-start gap-4 py-4 first:pt-0 last:pb-0">
        <div>
          <p class="text-sm font-medium text-highlighted">Instituição</p>
        </div>
        <p class="text-sm text-muted">{{ account?.institution }}</p>
      </div>

      <div class="flex max-sm:flex-col justify-between items-start gap-4 py-4 first:pt-0 last:pb-0">
        <div>
          <p class="text-sm font-medium text-highlighted">Perfil de acesso</p>
        </div>
        <p class="text-sm text-muted">{{ account?.role }}</p>
      </div>

      <div v-if="account?.course" class="flex max-sm:flex-col justify-between items-start gap-4 py-4 first:pt-0 last:pb-0">
        <div>
          <p class="text-sm font-medium text-highlighted">Curso</p>
        </div>
        <p class="text-sm text-muted">{{ account?.course }}</p>
      </div>

      <div class="flex justify-end py-4 first:pt-0 last:pb-0">
        <UButton
          label="Sair"
          icon="i-lucide-log-out"
          color="error"
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
