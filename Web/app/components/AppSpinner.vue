<script setup lang="ts">
defineOptions({ inheritAttrs: false })
</script>

<template>
  <span
    role="status"
    aria-label="Carregando"
    :class="[
      'inline-flex shrink-0 items-center justify-center gap-[12%] text-primary',
      $attrs.class as string,
    ]"
  >
    <span
      v-for="i in 3"
      :key="i"
      class="app-spinner-dot w-[22%] aspect-square rounded-full bg-current"
      :style="{ animationDelay: `${(i - 1) * 0.15}s` }"
    />
  </span>
</template>

<style scoped>
.app-spinner-dot {
  animation: app-spinner-bounce 0.9s ease-in-out infinite;
}

@keyframes app-spinner-bounce {
  0%, 80%, 100% { transform: translateY(0); opacity: 0.4; }
  40% { transform: translateY(-80%); opacity: 1; }
}
</style>
