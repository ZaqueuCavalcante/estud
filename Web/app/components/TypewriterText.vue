<script setup lang="ts">
const props = withDefaults(defineProps<{
  words: string[]
  typeDelay?: number
  deleteDelay?: number
  holdDelay?: number
}>(), {
  typeDelay: 90,
  deleteDelay: 45,
  holdDelay: 2000,
})

const typed = ref(props.words[0] ?? '')
const reducedMotion = usePreferredReducedMotion()

let timer: ReturnType<typeof setTimeout> | undefined

function step(index: number, chars: number, deleting: boolean) {
  const word = props.words[index] ?? ''
  typed.value = word.slice(0, chars)

  if (!deleting && chars < word.length)
    timer = setTimeout(() => step(index, chars + 1, false), props.typeDelay)
  else if (!deleting)
    timer = setTimeout(() => step(index, chars, true), props.holdDelay)
  else if (chars > 0)
    timer = setTimeout(() => step(index, chars - 1, true), props.deleteDelay)
  else
    timer = setTimeout(() => step((index + 1) % props.words.length, 1, false), props.typeDelay)
}

onMounted(() => {
  if (reducedMotion.value === 'reduce') return
  timer = setTimeout(() => step(0, typed.value.length, true), props.holdDelay)
})

onBeforeUnmount(() => {
  clearTimeout(timer)
})
</script>

<template>
  <span class="typewriter">
    <span class="sr-only">{{ words[0] }}</span>

    <span
      v-for="word in words"
      :key="word"
      aria-hidden="true"
      class="typewriter-slot invisible"
    >{{ word }}</span>

    <span aria-hidden="true" class="typewriter-slot"><span class="typewriter-word">{{ typed }}</span><span class="typewriter-caret" /></span>
  </span>
</template>

<style scoped>
.typewriter {
  display: inline-grid;
  text-align: start;
}

.typewriter-slot {
  grid-area: 1 / 1;
}

.typewriter-word {
  text-decoration: underline;
  text-decoration-color: var(--ui-primary);
  text-decoration-thickness: 0.06em;
  text-underline-offset: 0.14em;
}

.typewriter-caret {
  display: inline-block;
  width: 0.07em;
  height: 0.8em;
  margin-inline-start: 0.08em;
  vertical-align: -0.06em;
  background: var(--ui-primary);
  animation: typewriter-blink 1s step-end infinite;
}

@keyframes typewriter-blink {
  50% { opacity: 0; }
}

@media (prefers-reduced-motion: reduce) {
  .typewriter-caret {
    display: none;
  }
}
</style>
