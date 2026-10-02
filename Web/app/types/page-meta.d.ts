import type { UserType } from '~/composables/useUserAccount'

declare module '#app' {
  interface PageMeta {
    userTypes?: UserType[]
  }
}

export {}
