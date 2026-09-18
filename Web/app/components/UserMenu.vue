<script setup lang="ts">
defineProps<{
    collapsed?: boolean
}>()

const { account } = useUserAccount()
</script>

<template>
    <UButton
        v-if="collapsed"
        to="/account"
        color="neutral"
        variant="ghost"
        square
        class="p-0 rounded-full"
    >
        <UAvatar :src="account?.profilePhoto ?? undefined" :alt="account?.name" size="md" />
    </UButton>
    <UButton
        v-else
        to="/account"
        color="neutral"
        variant="ghost"
        block
        class="px-0 hover:bg-transparent active:bg-transparent justify-start"
    >
        <UAvatar :src="account?.profilePhoto ?? undefined" :alt="account?.name" size="md" />
        <div class="flex flex-col items-start min-w-0 w-full overflow-hidden text-left">
            <span class="truncate w-full text-sm font-medium">{{ account?.name }}</span>
            <span class="truncate w-full text-xs text-muted">{{ account?.role }}</span>
        </div>
    </UButton>
</template>
