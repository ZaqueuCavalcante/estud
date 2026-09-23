<script setup lang="ts">
const route = useRoute()
const { account } = useUserAccount()

const studentId = route.params.studentId as string

const breadcrumb = [
  { label: 'Alunos', to: '/students', icon: 'i-lucide-graduation-cap' },
  { label: 'Detalhes' },
]
</script>

<template>
  <PanelLoading v-if="!account" id="student-details" :breadcrumb="breadcrumb" />
  <LazyStudentsDetailManager v-else-if="account.userType === 'Manager'" :student-id="studentId" />
</template>
