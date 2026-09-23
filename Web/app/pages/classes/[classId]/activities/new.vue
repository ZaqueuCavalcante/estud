<script setup lang="ts">
const route = useRoute()
const { account } = useUserAccount()

const classId = route.params.classId as string

const breadcrumb = [
  { label: 'Turmas', icon: 'i-lucide-presentation' },
  { label: 'Detalhes', to: `/classes/${classId}` },
  { label: 'Nova atividade' },
]
</script>

<template>
  <PanelLoading v-if="!account" id="activity-create" :breadcrumb="breadcrumb" />
  <LazyActivitiesFormTeacher
    v-else-if="account.userType === 'Teacher'"
    :class-id="classId"
  />
</template>
