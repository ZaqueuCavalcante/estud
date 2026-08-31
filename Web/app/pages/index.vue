<script setup lang="ts">
// Renderiza a landing direto no SSR. O redirect de usuário logado para /home é feito no
// servidor pelo middleware redirect-if-logged (sem gate/spinner no caminho do não logado).
definePageMeta({ layout: false, middleware: 'redirect-if-logged' })

useSeoMeta({
  robots: 'index, follow'
})

const features = [
  {
    icon: 'i-lucide-graduation-cap',
    title: 'Gestão Acadêmica',
    description: 'Organize disciplinas, cursos e grades curriculares com facilidade. Tudo centralizado e sempre atualizado.',
  },
  {
    icon: 'i-lucide-users',
    title: 'Alunos e Professores',
    description: 'Cadastro completo de alunos e docentes, com histórico, notas e frequência em tempo real.',
  },
  {
    icon: 'i-lucide-calendar',
    title: 'Turmas e Horários',
    description: 'Monte grades de horários sem conflitos. Associe professores e salas automaticamente.',
  },
  {
    icon: 'i-lucide-bar-chart-2',
    title: 'Relatórios e Métricas',
    description: 'Acompanhe indicadores de desempenho da instituição com dashboards claros e exportáveis.',
  },
  {
    icon: 'i-lucide-shield-check',
    title: 'Segurança e Controle',
    description: 'Permissões por perfil (admin, professor, aluno) com autenticação segura e auditoria completa.',
  },
  {
    icon: 'i-lucide-zap',
    title: 'Rápido e Moderno',
    description: 'Interface responsiva, modo escuro e desempenho otimizado para qualquer dispositivo.',
  },
]
</script>

<template>
  <NuxtLayout name="landing">
    <div>
      <UPageHero
        :ui="{ container: 'pt-12 sm:pt-16 lg:pt-20 pb-14 sm:pb-14 lg:pb-14 gap-8 sm:gap-y-12' }"
        description="Sua instituição inteira no lugar certo, da matrícula à conclusão do curso."
        :links="[
          { label: 'Entrar', to: '/login', size: 'xl', trailingIcon: 'i-lucide-arrow-right', class: 'px-8 py-3 text-lg font-semibold' },
        ]"
      >
        <template #title>
          <TypewriterText :words="['Organize', 'Entenda', 'Controle']" /> sua instituição de ensino com excelência
        </template>

        <LandingCampusPreview class="hidden sm:block" />
      </UPageHero>

      <UPageSection
        id="features"
        :ui="{ container: 'py-1 sm:py-1 lg:py-1' }"
        title="Tudo que sua instituição precisa"
        description="Uma plataforma completa para gestão acadêmica, do primeiro acesso ao diploma."
      >
        <UPageGrid>
          <UPageCard
            v-for="feature in features"
            :key="feature.title"
            :description="feature.description"
            :ui="{ title: 'flex items-center gap-2' }"
            spotlight
          >
            <template #title>
              <UIcon :name="feature.icon" class="size-5 shrink-0 text-primary" />
              {{ feature.title }}
            </template>
          </UPageCard>
        </UPageGrid>
      </UPageSection>

      <UPageCTA
        title="Pronto para transformar sua instituição?"
        description="Junte-se a centenas de gestores que já usam o Estud."
        variant="naked"
        :ui="{ container: 'py-10 sm:py-14 lg:py-16' }"
        :links="[
          { label: 'Entrar', to: '/login', size: 'xl', trailingIcon: 'i-lucide-arrow-right', class: 'px-8 py-3 text-lg font-semibold' },
        ]"
      />
    </div>
  </NuxtLayout>
</template>
