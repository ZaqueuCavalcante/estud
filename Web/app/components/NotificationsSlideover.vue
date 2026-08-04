<script setup lang="ts">
import { formatTimeAgo } from '@vueuse/core'
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

async function markOne(id: number) {
  await markAsViewed(id)
}

async function markAll() {
  await markAsViewed()
}

async function onLinkClick(notification: NotificationItem, link: NotificationLink) {
  if (!link.newTab) isNotificationsSlideoverOpen.value = false
  if (!notification.viewedAt) await markAsViewed(notification.id)
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
            variant="ghost"
            color="neutral"
            size="xs"
            icon="i-lucide-check-check"
            @click="markAll"
          >
            Marcar todas
          </UButton>
        </div>
      </div>
    </template>

    <template #body>
      <div v-if="loading" class="flex items-center justify-center py-12">
        <AppSpinner class="size-6 text-muted" />
      </div>

      <div
        v-else-if="notifications.length === 0"
        class="flex flex-col items-center justify-center gap-2 py-12 text-center"
      >
        <UIcon name="i-lucide-bell-off" class="size-8 text-muted" />
        <p class="text-sm text-muted">
          {{ onlyUnread ? 'Nenhuma notificação não lida' : 'Nenhuma notificação' }}
        </p>
      </div>

      <div v-else class="flex flex-col gap-1 -mx-3">
        <div
          v-for="notification in notifications"
          :key="notification.id"
          class="px-3 py-3 rounded-md hover:bg-elevated/50 flex items-start gap-3 transition-colors"
          :class="!notification.viewedAt ? 'bg-elevated/30' : ''"
        >
          <div class="flex-1 min-w-0">
            <div class="flex items-start justify-between gap-2">
              <div class="flex items-center gap-1.5 min-w-0">
                <span
                  v-if="!notification.viewedAt"
                  class="shrink-0 size-1.5 rounded-full bg-primary mt-1"
                />
                <p
                  class="text-sm font-medium text-highlighted truncate"
                  :class="notification.viewedAt ? 'pl-3' : ''"
                >
                  {{ notification.title }}
                </p>
                <UIcon
                  v-if="isWelcome(notification)"
                  name="i-lucide-party-popper"
                  class="shrink-0 size-3.5 text-primary"
                />
              </div>
              <time
                :datetime="notification.createdAt"
                class="shrink-0 text-xs text-muted whitespace-nowrap"
              >
                {{ formatTimeAgo(new Date(notification.createdAt)) }}
              </time>
            </div>
            <p
              class="text-sm text-dimmed mt-0.5 pl-3"
              :class="isWelcome(notification) ? '' : 'line-clamp-2'"
            >
              {{ notification.description }}
            </p>

            <div
              v-if="linksOf(notification).length > 0"
              class="flex flex-wrap gap-1.5 mt-2 pl-3"
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
                @click="() => { onLinkClick(notification, link) }"
              />
            </div>
          </div>

          <UTooltip text="Marcar como lida">
            <UButton
              v-if="!notification.viewedAt"
              icon="i-lucide-check"
              variant="ghost"
              color="neutral"
              size="xs"
              class="shrink-0 mt-0.5"
              @click="markOne(notification.id)"
            />
          </UTooltip>
        </div>
      </div>
    </template>
  </USlideover>
</template>
