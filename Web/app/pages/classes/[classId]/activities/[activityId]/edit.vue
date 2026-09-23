<script setup lang="ts">
const route = useRoute()
const { account } = useUserAccount()

const classId = route.params.classId as string
const activityId = route.params.activityId as string

const breadcrumb = [
  { label: 'Turmas', icon: 'i-lucide-presentation' },
  { label: 'Detalhes', to: `/classes/${classId}` },
  { label: 'Atividade', to: `/classes/${classId}/activities/${activityId}` },
  { label: 'Editar' },
]
</script>

<template>
  <PanelLoading v-if="!account" id="activity-create" :breadcrumb="breadcrumb" />
  <LazyActivitiesFormTeacher
    v-else-if="account.userType === 'Teacher'"
    :class-id="classId"
    :activity-id="activityId"
  />
</template>
