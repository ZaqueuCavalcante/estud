<script setup lang="ts">
const props = defineProps<{
  // Preenchimento do anel e posição do ponteiro das horas, de 0 a 100
  percent: number
}>()

// O tamanho e a cor vêm de fora (`class="size-14 text-primary"`): o mesmo
// mostrador serve o card da sala, grande, e a célula do mapa, pequena e sobre
// fundo tingido — onde a cor tem que ser a do texto da célula.

// O anel e os ponteiros entram do zero na primeira pintura. Sem isso o card
// nasce já com o valor final e a animação só apareceria ao trocar de célula.
// Dois requestAnimationFrame porque num só o browser ainda junta as duas mudanças numa pintura.
const entered = ref(false)
onMounted(() => {
  requestAnimationFrame(() => requestAnimationFrame(() => { entered.value = true }))
})

const clamped = computed(() =>
  entered.value ? Math.min(Math.max(props.percent, 0), 100) : 0,
)

// Mesma geometria do ClassesRingStat (r = 15.5 num viewBox de 36), pra os dois
// anéis lerem como a mesma família mesmo com miolos diferentes.
const CIRCUMFERENCE = 2 * Math.PI * 15.5
const dash = computed(() => `${(CIRCUMFERENCE * clamped.value) / 100} ${CIRCUMFERENCE}`)

// Ponta arredondada com arco de comprimento zero ainda desenha a tampa: em 0%
// aparecia um pontinho no topo do mostrador. A ponta reta some junto com o arco,
// e o arredondado volta assim que há o que preencher.
const cap = computed(() => (clamped.value > 0 ? 'round' : 'butt'))

// A volta inteira do mostrador são os 100% do turno: 25% aponta pra direita,
// 50% pra baixo, 75% pra esquerda. O ponteiro dos minutos fica parado no topo
// marcando o zero — é a distância até o das horas que vira a leitura.
const hourAngle = computed(() => clamped.value * 3.6)
</script>

<template>
  <svg
    viewBox="0 0 36 36"
    class="shrink-0 stroke-current"
    aria-hidden="true"
  >
    <!-- O -90° é atributo de SVG, e não classe: só o anel gira, os
         ponteiros têm rotação própria. -->
    <g transform="rotate(-90 18 18)">
      <circle
        cx="18"
        cy="18"
        r="15.5"
        fill="none"
        stroke-width="3.5"
        class="opacity-15"
      />
      <circle
        cx="18"
        cy="18"
        r="15.5"
        fill="none"
        stroke-width="3.5"
        :stroke-linecap="cap"
        :stroke-dasharray="dash"
        class="transition-all duration-500"
      />
    </g>

    <!-- Ponteiro dos minutos: o grande, sempre na vertical pra cima. -->
    <line
      x1="18"
      y1="18"
      x2="18"
      y2="7.5"
      stroke-width="1.5"
      stroke-linecap="round"
    />

    <!-- Ponteiro das horas: o menor, parado no valor da taxa. O
         transform-origin vai em unidades do viewBox (o centro é 18,18). -->
    <line
      x1="18"
      y1="18"
      x2="18"
      y2="11"
      stroke-width="2.4"
      stroke-linecap="round"
      class="transition-transform duration-500 motion-reduce:transition-none"
      :style="{ rotate: `${hourAngle}deg`, transformOrigin: '18px 18px' }"
    />

    <circle
      cx="18"
      cy="18"
      r="1.4"
      class="fill-current stroke-none"
    />
  </svg>
</template>
