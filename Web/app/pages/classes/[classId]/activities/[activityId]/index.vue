<script setup lang="ts">
const route = useRoute()
const { account } = useUserAccount()

const classId = route.params.classId as string
const activityId = route.params.activityId as string

const breadcrumb = [
  { label: 'Turmas', icon: 'i-lucide-presentation' },
  { label: 'Detalhes', to: `/classes/${classId}` },
  { label: 'Atividade' },
]
</script>

<template>
  <PanelLoading v-if="!account" id="activity-details" :breadcrumb="breadcrumb" />
  <LazyActivitiesDetailTeacher
    v-else-if="account.userType === 'Teacher'"
    :class-id="classId"
    :activity-id="activityId"
  />
  <LazyActivitiesDetailStudent
    v-else-if="account.userType === 'Student'"
    :class-id="classId"
    :activity-id="activityId"
  />
</template>
