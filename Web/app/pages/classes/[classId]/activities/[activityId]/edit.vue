<script setup lang="ts">
const route = useRoute()
const { account } = useUserAccount()

const classId = route.params.classId as string
const activityId = route.params.activityId as string
</script>

<template>
  <LazyActivitiesFormTeacher
    v-if="account?.userType === 'Teacher'"
    :class-id="classId"
    :activity-id="activityId"
  />
</template>
