<script setup lang="ts">
const route = useRoute()
const { account } = useUserAccount()

const classId = route.params.classId as string
const lessonId = route.params.lessonId as string

const breadcrumb = [
  { label: 'Turmas', icon: 'i-lucide-presentation' },
  { label: 'Detalhes', to: `/classes/${classId}` },
  { label: 'Aula' },
]
</script>

<template>
  <PanelLoading v-if="!account" id="lesson-details" :breadcrumb="breadcrumb" />
  <LazyLessonsDetailTeacher
    v-else-if="account.userType === 'Teacher'"
    :class-id="classId"
    :lesson-id="lessonId"
  />
  <LazyLessonsDetailStudent
    v-else-if="account.userType === 'Student'"
    :class-id="classId"
    :lesson-id="lessonId"
  />
</template>
