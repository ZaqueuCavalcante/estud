<script setup lang="ts">
const route = useRoute()
const { account } = useUserAccount()

const classId = route.params.classId as string

const breadcrumb = [
  { label: 'Turmas', icon: 'i-lucide-presentation' },
  { label: 'Detalhes' },
]
</script>

<template>
  <PanelLoading v-if="!account" id="class-details" :breadcrumb="breadcrumb" />
  <LazyClassesDetailManager v-else-if="account.userType === 'Manager'" :class-id="classId" />
  <LazyClassesDetailTeacher v-else-if="account.userType === 'Teacher'" :class-id="classId" />
  <LazyClassesDetailStudent v-else-if="account.userType === 'Student'" :class-id="classId" />
</template>
