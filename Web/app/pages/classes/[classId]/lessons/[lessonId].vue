<script setup lang="ts">
const route = useRoute()
const { account } = useUserAccount()

const classId = route.params.classId as string
const lessonId = route.params.lessonId as string
</script>

<template>
  <LazyLessonsDetailTeacher
    v-if="account?.userType === 'Teacher'"
    :class-id="classId"
    :lesson-id="lessonId"
  />
</template>
