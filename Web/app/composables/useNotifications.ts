import { createSharedComposable, useIntervalFn } from '@vueuse/core'

export interface NotificationItem {
  id: number
  notificationType: string
  title: string
  description: string
  createdAt: string
  viewedAt: string | null
  metadata: Record<string, unknown> | null
}

interface NotificationsResponse {
  total: number
  page: number
  pageSize: number
  items: NotificationItem[]
}

const _useNotifications = () => {
  const config = useRuntimeConfig()
  const { isNotificationsSlideoverOpen } = useDashboard()
  const { account } = useUserAccount()

  const unreadCount = ref(0)
  const notifications = ref<NotificationItem[]>([])
  const onlyUnread = ref(false)
  const loading = ref(false)

  async function fetchUnreadCount() {
    try {
      const data = await $fetch<{ count: number }>(
        `${config.public.backendUrl}/notifications/unread-count`,
        { credentials: 'include' }
      )
      unreadCount.value = data.count
    } catch { /* ignore */ }
  }

  // A busca do boot e a do watch da conta disparam praticamente juntas quando a
  // aba já abre logada. Reaproveitando a chamada em voo o app sobe com uma
  // requisição só, em vez de duas idênticas.
  let inFlight: Promise<void> | null = null

  function fetchUnreadCountOnce() {
    inFlight ??= fetchUnreadCount().finally(() => { inFlight = null })
    return inFlight
  }

  async function fetchNotifications() {
    loading.value = true
    try {
      const data = await $fetch<NotificationsResponse>(
        `${config.public.backendUrl}/notifications`,
        {
          credentials: 'include',
          query: { page: 1, pageSize: 50, unreadOnly: onlyUnread.value },
        }
      )
      notifications.value = data.items
    } catch { /* ignore */ }
    finally { loading.value = false }
  }

  async function markAsViewed(id?: number) {
    await $fetch(`${config.public.backendUrl}/notifications/mark-as-viewed`, {
      method: 'PUT',
      credentials: 'include',
      body: id ? { notificationId: id } : { markAll: true },
    })
    if (id) {
      const item = notifications.value.find(n => n.id === id)
      if (item) item.viewedAt = new Date().toISOString()
    } else {
      notifications.value.forEach(n => { n.viewedAt = n.viewedAt ?? new Date().toISOString() })
    }
    await fetchUnreadCount()
  }

  watch(isNotificationsSlideoverOpen, (open) => {
    if (open) fetchNotifications()
  })

  watch(onlyUnread, () => {
    if (isNotificationsSlideoverOpen.value) fetchNotifications()
  })

  // O composable é criado uma vez só, quando o app monta — no login isso acontece
  // com o usuário ainda deslogado e a contagem volta 401. Buscar de novo assim que
  // a conta é carregada (no boot da aba ou logo depois do login) faz o sininho já
  // nascer com o badge certo, em vez de esperar o próximo polling.
  // Quando a conta some (logout), limpar o estado: o contador aparece no título da
  // aba e não faz sentido continuar mostrando com o usuário deslogado.
  watch(account, (value) => {
    if (value) {
      fetchUnreadCountOnce()
      return
    }
    unreadCount.value = 0
    notifications.value = []
  }, { immediate: true })

  useIntervalFn(fetchUnreadCount, 60_000)
  if (import.meta.client) fetchUnreadCountOnce()

  return {
    unreadCount,
    notifications,
    onlyUnread,
    loading,
    fetchNotifications,
    markAsViewed,
  }
}

export const useNotifications = createSharedComposable(_useNotifications)
