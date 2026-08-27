<script setup lang="ts">
// Renderiza a landing direto no SSR. O redirect de usuário logado para /home é feito no
// servidor pelo middleware redirect-if-logged (sem gate/spinner no caminho do não logado).
definePageMeta({ layout: false, middleware: 'redirect-if-logged' })

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
        :ui="{ container: 'pt-12 sm:pt-16 lg:pt-20' }"
        description="Sua instituição inteira no lugar certo, da matrícula à conclusão do curso."
        :links="[
          { label: 'Entrar', to: '/login', size: 'xl', trailingIcon: 'i-lucide-arrow-right', class: 'px-8 py-3 text-lg font-semibold' },
        ]"
      >
        <template #title>
          <TypewriterText :words="['Organize', 'Entenda', 'Controle']" /> sua instituição de ensino com excelência
        </template>

        <UColorModeImage
          light="/images/campus-light.png"
          dark="/images/campus-dark.png"
          alt="Tela de ocupação de campus do Estud, com mapa de uso das salas por turno e indicadores de tempo e espaço"
          width="1278"
          height="946"
          class="rounded-xl w-full h-auto ring ring-default shadow-2xl shadow-black/10 dark:shadow-black/40"
        />
      </UPageHero>

      <UPageSection
        id="features"
        headline="Funcionalidades"
        title="Tudo que sua instituição precisa"
        description="Uma plataforma completa para gestão acadêmica, do primeiro acesso ao diploma."
      >
        <UPageGrid>
          <UPageCard
            v-for="feature in features"
            :key="feature.title"
            :icon="feature.icon"
            :title="feature.title"
            :description="feature.description"
            spotlight
          />
        </UPageGrid>
      </UPageSection>

      <UPageSection
        headline="Como funciona"
        title="Simples de começar"
        description="Configure sua instituição em minutos e comece a usar imediatamente."
        orientation="horizontal"
        :features="[
          { icon: 'i-lucide-user-plus', title: 'Crie sua conta', description: 'Cadastre-se gratuitamente e configure sua instituição em poucos minutos.' },
          { icon: 'i-lucide-database', title: 'Importe seus dados', description: 'Importe alunos, professores e disciplinas via planilha ou cadastre manualmente.' },
          { icon: 'i-lucide-rocket', title: 'Comece a usar', description: 'Acesse todos os módulos imediatamente. Suporte disponível 24/7.' },
        ]"
      />

      <UPageCTA
        title="Pronto para transformar sua instituição?"
        description="Junte-se a centenas de instituições que já usam o Estud."
        variant="subtle"
        :links="[
          { label: 'Entrar', to: '/login', size: 'xl' },
        ]"
      />
    </div>
  </NuxtLayout>
</template>
