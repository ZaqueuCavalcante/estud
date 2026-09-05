<script setup lang="ts">
import * as z from 'zod'
import type { FormSubmitEvent } from '@nuxt/ui'
import type { UserTypeName } from '~/composables/usePermissions'

const open = defineModel<boolean>('open', { default: false })
const props = defineProps<{ roleId: number | null }>()
const emit = defineEmits<{ updated: [] }>()

const isMobile = useIsMobile()
const config = useRuntimeConfig()
const toast = useToast()
const loading = ref(false)
const fetching = ref(false)

interface GetRoleOut {
  id: number
  name: string
  description: string
  baseType: UserTypeName
  permissions: number[]
}

// Tipo base é imutável: apenas exibido, nunca enviado no update
const baseType = ref<UserTypeName | null>(null)

const baseTypeLabel = computed(() => baseType.value === null ? '' : userTypeLabels[baseType.value])

const schema = z.object({
  name: z.string().min(1, 'Nome obrigatório').max(50, 'Máximo 50 caracteres'),
  description: z.string().min(1, 'Descrição obrigatória').max(200, 'Máximo 200 caracteres'),
  permissions: z.array(z.number()),
})

type Schema = z.output<typeof schema>

const formState = reactive<Partial<Schema>>({
  name: '',
  description: '',
  permissions: [],
})

const { permissionsFor } = usePermissions()
const availablePermissions = permissionsFor(baseType)

watch(open, async (val) => {
  if (val && props.roleId !== null) {
    fetching.value = true
    try {
      const role = await $fetch<GetRoleOut>(
        `${config.public.backendUrl}/identity/roles/${props.roleId}`,
        { credentials: 'include' }
      )
      formState.name = role.name
      formState.description = role.description
      baseType.value = role.baseType
      formState.permissions = [...role.permissions]
      await nextTick()
    } catch {
      toast.add({ title: 'Erro ao carregar perfil', color: 'error' })
      open.value = false
    } finally {
      fetching.value = false
    }
  }
})

async function onSubmit(event: FormSubmitEvent<Schema>) {
  loading.value = true
  try {
    await $fetch(`${config.public.backendUrl}/identity/roles/${props.roleId}`, {
      method: 'PUT',
      body: event.data,
      credentials: 'include',
    })
    toast.add({ title: 'Perfil atualizado com sucesso', color: 'success' })
    open.value = false
    emit('updated')
  } catch (err: unknown) {
    const msg = (err as { data?: { message?: string } })?.data?.message ?? 'Erro ao atualizar perfil.'
    toast.add({ title: 'Erro', description: msg, color: 'error' })
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <UModal
    v-model:open="open"
    title="Editar perfil"
    :fullscreen="isMobile"
    description="Atualize os dados do perfil de acesso."
  >
    <template #body>
      <div v-if="fetching" class="flex justify-center py-8">
        <AppSpinner class="size-6" />
      </div>

      <UForm
        v-else
        :schema="schema"
        :state="formState"
        class="space-y-4"
        @submit="onSubmit"
      >
        <UFormField label="Nome" name="name">
          <UInput v-model="formState.name" class="w-full" placeholder="Ex: Coordenador" />
        </UFormField>

        <UFormField label="Descrição" name="description">
          <UTextarea v-model="formState.description" class="w-full" placeholder="Ex: Perfil com acesso a cursos e disciplinas" :rows="3" />
        </UFormField>

        <UFormField v-if="baseType !== null" label="Tipo base">
          <div class="flex items-center gap-2">
            <UBadge color="neutral" variant="subtle" :label="baseTypeLabel" />
            <span class="text-xs text-muted">Não pode ser alterado</span>
          </div>
        </UFormField>

        <div v-if="baseType !== null" class="flex flex-col gap-2">
          <p class="block text-sm font-medium text-default">Permissões</p>
          <SecurityRolesPermissionsField
            v-model="formState.permissions"
            :permissions="availablePermissions"
            :base-type="baseType"
          />
        </div>

        <div class="flex justify-end gap-2 pt-2">
          <UButton
            label="Cancelar"
            color="neutral"
            variant="subtle"
            :disabled="loading"
            @click="() => { open = false }"
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
</template>
