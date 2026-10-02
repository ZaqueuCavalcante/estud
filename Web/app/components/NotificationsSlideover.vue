<script setup lang="ts">
import type { NotificationItem } from '~/composables/useNotifications'

interface NotificationLink {
  label: string
  to: string
  icon?: string
  newTab?: boolean
}

const { isNotificationsSlideoverOpen } = useDashboard()
const { notifications, onlyUnread, loading, markAsViewed } = useNotifications()

const unreadNotifications = computed(() => notifications.value.filter(n => !n.viewedAt))

function isWelcome(notification: NotificationItem) {
  return notification.notificationType === 'Welcome'
}

function linksOf(notification: NotificationItem): NotificationLink[] {
  const links = (notification.metadata as { links?: NotificationLink[] } | null)?.links
  return Array.isArray(links) ? links : []
}

const relativeFormatter = new Intl.RelativeTimeFormat('pt-BR', { numeric: 'auto' })

function formatCreatedAt(value: string) {
  const date = new Date(value)
  const now = new Date()

  if (date.toDateString() !== now.toDateString()) {
    return formatDateTime(date)
  }

  const minutes = Math.max(0, Math.floor((now.getTime() - date.getTime()) / 60000))
  if (minutes < 1) return 'agora mesmo'
  if (minutes < 60) return relativeFormatter.format(-minutes, 'minute')
  return relativeFormatter.format(-Math.floor(minutes / 60), 'hour')
}

async function onCardClick(notification: NotificationItem) {
  if (!notification.viewedAt) await markAsViewed(notification.id)
}

async function markAll() {
  await markAsViewed()
}

function onLinkClick(link: NotificationLink) {
  if (!link.newTab) isNotificationsSlideoverOpen.value = false
}

</script>

<template>
  <USlideover
    v-model:open="isNotificationsSlideoverOpen"
    title="Notificações"
  >
    <template #header>
      <div class="flex flex-col gap-2 w-full">
        <div class="flex items-center justify-between w-full gap-3">
          <p class="text-base font-semibold text-highlighted">Notificações</p>
          <UButton
            variant="ghost"
            color="neutral"
            size="md"
            icon="i-lucide-x"
            @click="() => { isNotificationsSlideoverOpen = false }"
          />
        </div>
        <div class="flex items-center justify-between w-full gap-3">
          <USwitch
            v-model="onlyUnread"
            size="xs"
            label="Não lidas"
          />
          <UButton
            v-if="unreadNotifications.length > 0"
            variant="outline"
            color="neutral"
            size="xs"
            icon="i-lucide-check-check"
            @click="markAll"
          >
            Marcar todas como lidas
          </UButton>
        </div>
      </div>
    </template>

    <template #body>
      <div v-if="loading" class="flex items-center justify-center py-12">
        <AppSpinner class="size-8" />
      </div>

      <TableEmptyState
        v-else-if="notifications.length === 0"
        :loading="false"
        icon="i-lucide-bell-off"
        message="Nenhuma notificação"
        :filtered="onlyUnread"
        not-found-icon="i-lucide-bell-off"
        not-found-message="Nenhuma notificação não lida"
        clear-filters-label="Ver todas"
        @clear-filters="() => { onlyUnread = false }"
      />

      <div v-else class="flex flex-col gap-3">
        <div
          v-for="notification in notifications"
          :key="notification.id"
          class="relative p-3 rounded-lg border shadow-sm"
          :class="!notification.viewedAt ? 'bg-elevated border-primary/40' : 'bg-default border-default'"
          @click="() => { onCardClick(notification) }"
        >
          <span
            v-if="!notification.viewedAt"
            class="absolute top-3 right-3 size-2 rounded-full bg-primary"
          />
          <UIcon
            v-else
            name="i-lucide-check-check"
            class="absolute top-2.5 right-2.5 size-4 text-dimmed"
          />
          <div class="min-w-0">
            <div class="flex items-start gap-2 pr-4">
              <div class="flex items-center gap-1.5 min-w-0">
                <p class="text-sm font-medium text-highlighted truncate">
                  {{ notification.title }}
                </p>
                <UIcon
                  v-if="isWelcome(notification)"
                  name="i-lucide-party-popper"
                  class="shrink-0 size-3.5 text-primary"
                />
              </div>
            </div>
            <p class="text-sm text-dimmed mt-0.5">
              {{ notification.description }}
            </p>

            <div
              v-if="linksOf(notification).length > 0"
              class="flex flex-wrap gap-1.5 mt-2"
            >
              <UButton
                v-for="link in linksOf(notification)"
                :key="link.to"
                :to="link.to"
                :icon="link.icon"
                :label="link.label"
                :target="link.newTab ? '_blank' : undefined"
                size="xs"
                color="neutral"
                variant="subtle"
                @click="() => { onLinkClick(link) }"
              />
            </div>

            <time
              :datetime="notification.createdAt"
              class="block mt-2 text-xs text-muted text-right"
            >
              {{ formatCreatedAt(notification.createdAt) }}
            </time>
          </div>
        </div>
      </div>
    </template>
  </USlideover>
</template>
