<script setup lang="ts">
const props = defineProps<{
  // Assentos ocupados no turno, de 0 a 100
  percent: number
}>()

// Cada sala vira 4 quadrados; cada quadrado é uma fatia de 25% dos assentos.
// 45% → 1 cheio. O resto da fatia (os 20% que sobram) aparece como um quadrado
// esmaecido: sem ele, 24% e 0% desenhariam a mesma coisa — quatro quadrados vazios.
const STEPS = [0, 1, 2, 3]
const SLICE = 100 / STEPS.length

// Os quadrados entram apagados na primeira pintura, do mesmo jeito que o anel
// do mostrador entra do zero — as duas leituras da sala se enchem juntas.
const entered = ref(false)
onMounted(() => {
  requestAnimationFrame(() => requestAnimationFrame(() => { entered.value = true }))
})

const target = computed(() =>
  entered.value ? Math.min(Math.max(props.percent, 0), 100) : 0,
)

// O valor que estava em tela antes desta mudança. O watch é 'pre', então grava
// o anterior antes do DOM ser atualizado — é o que deixa o render saber pra que
// lado a barra está indo.
const previous = ref(0)
watch(target, (_, old) => { previous.value = old })

// Encher é da esquerda pra direita; esvaziar é da direita pra esquerda, com o
// último quadrado apagando primeiro. Nos dois casos a animação acompanha a
// ponta que se mexe, em vez de varrer sempre pro mesmo lado.
const shrinking = computed(() => target.value < previous.value)

// Cada quadrado espera 90ms a mais que o vizinho pra virar de cor: o último
// fecha em 470ms, junto com os 500ms do anel. É o atraso que desenha o
// preenchimento — sem ele os quatro trocariam de estado no mesmo instante.
function delay(index: number): string {
  const position = shrinking.value ? STEPS.length - 1 - index : index
  return `${position * 90}ms`
}

function state(index: number): 'full' | 'partial' | 'empty' {
  const full = Math.floor(target.value / SLICE)
  if (index < full) return 'full'
  if (index === full && target.value % SLICE > 0) return 'partial'
  return 'empty'
}

const stateClass: Record<string, string> = {
  full: 'bg-primary',
  partial: 'bg-primary/35',
  empty: 'bg-elevated ring-1 ring-inset ring-default/60',
}
</script>

<template>
  <!-- 2×2 de quadrados: o bloco vira um ícone, em vez de uma barra que ocupa a
       largura do card. O tamanho vem de fora (`class="size-14"`), pra bater com
       o do mostrador ao lado de quem chama. A ordem de preenchimento é a de
       leitura — cima-esquerda → baixo-direita. -->
  <div class="grid shrink-0 grid-cols-2 grid-rows-2 gap-1">
    <div
      v-for="i in STEPS"
      :key="i"
      class="rounded-[22%] transition-[background-color,box-shadow] duration-200 ease-out motion-reduce:transition-none"
      :class="stateClass[state(i)]"
      :style="{ transitionDelay: delay(i) }"
    />
  </div>
</template>
