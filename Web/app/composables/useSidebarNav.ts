import type { PolicyName } from '~/policies'

export interface SidebarLink {
  label: string
  icon: string
  to: string
  policy: PolicyName
}

export interface SidebarGroup {
  id: string
  label?: string
  // Só os grupos com título são colapsáveis, e aí o ícone é obrigatório: com a
  // sidebar colapsada ele vira o gatilho do popover com os itens do grupo.
  icon?: string
  items: SidebarLink[]
}

export const TEACHER_CLASSES_GROUP_ID = 'turmas'

// O primeiro grupo não tem título: pro Manager ele fica vazio (e é descartado),
// e pros demais perfis (Professor, Aluno, Responsável) é a sidebar inteira —
// poucos itens, que não ganham nada em serem categorizados. A Home não está
// aqui: o logo do Estud no topo já leva pra ela.
export const sidebarGroups: SidebarGroup[] = [
  {
    id: 'geral',
    items: [
      { label: 'Filhos',        icon: 'i-lucide-users-round',    to: '/children',           policy: 'AccessChildrenPage' },
      { label: 'Agenda',        icon: 'i-lucide-calendar-days',  to: '/agenda',             policy: 'AccessAgendaPage' },
      { label: 'Frequência',    icon: 'i-lucide-calendar-check', to: '/frequencies',        policy: 'AccessFrequenciesPage' },
    ],
  },
  {
    id: 'academico',
    label: 'Acadêmico',
    icon: 'i-lucide-book-marked',
    items: [
      { label: 'Campi',         icon: 'i-lucide-map-pin',        to: '/campi',              policy: 'AccessCampiPage' },
      { label: 'Cursos',        icon: 'i-lucide-notebook',       to: '/courses',            policy: 'AccessCoursesPage' },
      { label: 'Grades',        icon: 'i-lucide-layout-list',    to: '/course-curriculums', policy: 'AccessCourseCurriculumsPage' },
      { label: 'Disciplinas',   icon: 'i-lucide-book-open',      to: '/disciplines',        policy: 'AccessDisciplinesPage' },
    ],
  },
  {
    id: 'secretaria',
    label: 'Secretaria',
    icon: 'i-lucide-archive',
    items: [
      { label: 'Turmas',        icon: 'i-lucide-door-open',      to: '/classes',            policy: 'AccessClassesPage' },
      { label: 'Ofertas',       icon: 'i-lucide-library',        to: '/course-offerings',   policy: 'AccessCourseOfferingsPage' },
      { label: 'Calendário',    icon: 'i-lucide-calendar-range', to: '/calendar',           policy: 'AccessCalendarPage' },
      { label: 'Notificações',  icon: 'i-lucide-bell',           to: '/notifications',      policy: 'AccessNotificationsPage' },
    ],
  },
  {
    id: 'pessoas',
    label: 'Pessoas',
    icon: 'i-lucide-contact',
    items: [
      { label: 'Alunos',        icon: 'i-lucide-graduation-cap', to: '/students',           policy: 'AccessStudentsPage' },
      { label: 'Professores',   icon: 'i-lucide-user-pen',       to: '/teachers',           policy: 'AccessTeachersPage' },
      { label: 'Responsáveis',  icon: 'i-lucide-users',          to: '/parents',            policy: 'AccessParentsPage' },
    ],
  },
  {
    id: 'sistema',
    label: 'Sistema',
    icon: 'i-lucide-cog',
    items: [
      { label: 'Segurança',     icon: 'i-lucide-shield',         to: '/security',           policy: 'AccessSecurityPage' },
      { label: 'Integrações',   icon: 'i-lucide-webhook',        to: '/integrations',       policy: 'AccessIntegrationsPage' },
      { label: 'Configurações', icon: 'i-lucide-settings',       to: '/configs',            policy: 'AccessConfigsPage' },
    ],
  },
  // Só pra adm do Estud (claim `adm`): pros demais o grupo fica sem nenhum
  // item visível e é descartado inteiro.
  {
    id: 'admin',
    label: 'Admin',
    icon: 'i-lucide-shield-user',
    items: [
      { label: 'Instituições',  icon: 'i-lucide-building-2',     to: '/admin/institutions', policy: 'AccessAdminInstitutionsPage' },
    ],
  },
]

const sidebarLinks = sidebarGroups.flatMap(group => group.items)

export function useSidebarNav() {
  const route = useRoute()

  // Um item cobre a própria rota e tudo abaixo dela: /campi/53 (detalhe de um
  // campus) mantém Campi como o item ativo.
  function isLinkActive(to: string) {
    return route.path === to || route.path.startsWith(`${to}/`)
  }

  // Item da sidebar correspondente à tela atual, quando existe — telas fora da
  // sidebar (conta, detalhe de turma do professor, ...) não têm.
  const currentLink = computed(() => sidebarLinks.find(({ to }) => isLinkActive(to)) ?? null)

  return { sidebarGroups, isLinkActive, currentLink }
}
