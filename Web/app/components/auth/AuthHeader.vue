<template>
  <UHeader title="Estud" :toggle="false" :ui="{ title: 'items-center' }">
    <template #title>
      <EstudIcon class="size-6 shrink-0" />
      Estud
    </template>

    <template #right>
      <UTooltip text="Documentação">
        <UButton icon="i-lucide-book-open" color="neutral" variant="ghost" to="/docs" aria-label="Documentação" />
      </UTooltip>
      <UTooltip text="Alternar tema">
        <UColorModeButton />
      </UTooltip>
    </template>
  </UHeader>
</template>
