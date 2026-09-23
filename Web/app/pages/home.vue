<script setup lang="ts">
const { account } = useUserAccount()
</script>

<template>
  <UDashboardPanel id="home">
    <template #header>
      <UDashboardNavbar title="Home" :ui="{ right: 'gap-3' }">
        <template #leading>
          <PageIcon icon="i-lucide-house" />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div v-if="!account" class="flex flex-1 items-center justify-center">
        <AppSpinner class="size-8" />
      </div>
      <HomeManager v-else-if="account.userType === 'Manager'" />
      <HomeTeacher v-else-if="account.userType === 'Teacher'" />
      <HomeStudent v-else-if="account.userType === 'Student'" />
    </template>
  </UDashboardPanel>
</template>
