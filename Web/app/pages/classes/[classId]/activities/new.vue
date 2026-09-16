<script setup lang="ts">
const route = useRoute()
const { account } = useUserAccount()

const classId = route.params.classId as string
</script>

<template>
  <LazyActivitiesCreateTeacher
    v-if="account?.userType === 'Teacher'"
    :class-id="classId"
  />
</template>
