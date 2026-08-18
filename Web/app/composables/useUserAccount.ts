import { createSharedComposable } from '@vueuse/core'

export type UserType = 'Manager' | 'Teacher' | 'Student' | 'Parent'

interface UserAccount {
  id: string
  adm: boolean
  role: string
  name: string
  email: string
  userType: UserType
  institutionId: string
  institution: string
  permissions: number[]
  course: string | null
  profilePhoto: string | null
}

const _useUserAccount = () => {
  const config = useRuntimeConfig()
  const account = ref<UserAccount | null>(null)

  async function fetchAccount() {
    account.value = await $fetch<UserAccount>(`${config.public.backendUrl}/users/account`, {
      credentials: 'include'
    })

    // `/account` chama isso no setup, então o await acima também roda no SSR — e ali o
    // `useNuxtApp()` de dentro do `usePostHog()` estouraria por perda de contexto.
    if (import.meta.client) identifyOnPostHog(account.value)
  }

  function identifyOnPostHog(user: UserAccount) {
    const posthog = usePostHog()
    if (!posthog) return

    posthog.identify(user.id, {
      role: user.role,
      user_type: user.userType,
      is_adm: user.adm
    })
    posthog.group('institutionId', user.institutionId)
  }

  async function updateAccount(name: string) {
    await $fetch(`${config.public.backendUrl}/users/account`, {
      method: 'PUT',
      credentials: 'include',
      body: { name }
    })
    if (account.value) account.value.name = name
  }

  return { account, fetchAccount, updateAccount }
}

export const useUserAccount = createSharedComposable(_useUserAccount)
