<script setup lang="ts">
const route = useRoute()
const { account } = useUserAccount()

const teacherId = route.params.teacherId as string

const breadcrumb = [
  { label: 'Professores', to: '/teachers', icon: 'i-lucide-user-pen' },
  { label: 'Detalhes' },
]
</script>

<template>
  <PanelLoading v-if="!account" id="teacher-details" :breadcrumb="breadcrumb" />
  <LazyTeachersDetailManager v-else-if="account.userType === 'Manager'" :teacher-id="teacherId" />
</template>
