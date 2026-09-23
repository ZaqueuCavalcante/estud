<script setup lang="ts">
const route = useRoute()
const { account } = useUserAccount()

const classroomId = route.params.classroomId as string

const breadcrumb = [
  { label: 'Campi', to: '/campi', icon: 'i-lucide-map-pin' },
  { label: 'Sala' },
]
</script>

<template>
  <PanelLoading v-if="!account" id="classroom-details" :breadcrumb="breadcrumb" />
  <LazyClassroomsDetailManager v-else-if="account.userType === 'Manager'" :classroom-id="classroomId" />
</template>
