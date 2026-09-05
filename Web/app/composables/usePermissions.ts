export type UserTypeName = 'Manager' | 'Teacher' | 'Student' | 'Parent'

export interface PermissionItem {
  id: number
  name: string
  description: string
  group: string
  allowedTypes: UserTypeName[]
}

export interface GetPermissionsOut {
  items: PermissionItem[]
}

export interface PermissionCategory {
  label: string
  icon: string
  groups: string[]
}

export interface BaseTypeAccess {
  title: string
  description: string
}

export const userTypeLabels: Record<UserTypeName, string> = {
  Manager: 'Gestor',
  Teacher: 'Professor',
  Student: 'Aluno',
  Parent: 'Responsável',
}

// A ordem é a do enum `UserType` no backend: o índice de cada nome é o valor
// que a API espera em `baseType`.
export const userTypeNames: UserTypeName[] = ['Manager', 'Teacher', 'Student', 'Parent']

export const baseTypeOptions = userTypeNames.map((name, value) => ({
  label: userTypeLabels[name],
  value,
}))

// O tipo base não é só um filtro da lista de permissões: as telas de professor,
// aluno e responsável são liberadas por ele sozinho, sem nenhuma permissão.
export const baseTypeAccess: Record<UserTypeName, BaseTypeAccess> = {
  Manager: {
    title: 'O tipo base Gestor não libera nada sozinho',
    description: 'Este perfil vai acessar exatamente o que estiver marcado abaixo.',
  },
  Teacher: {
    title: 'O tipo base Professor já libera as telas de professor',
    description: 'Quem tem este perfil acessa a própria agenda, as turmas em que leciona, o registro de aulas e frequência e as atividades e notas dos alunos.',
  },
  Student: {
    title: 'O tipo base Aluno já libera as telas de aluno',
    description: 'Quem tem este perfil acessa a própria agenda, as turmas e o curso em que está matriculado, as atividades e entregas, a frequência e a declaração de matrícula.',
  },
  Parent: {
    title: 'O tipo base Responsável já libera as telas de responsável',
    description: 'Quem tem este perfil acessa a agenda e o acompanhamento dos alunos vinculados a ele.',
  },
}

// Espelha os grupos da sidebar: o usuário configura o perfil na mesma
// organização em que vai navegar depois. A ordem de `groups` é a ordem em que
// as permissões aparecem dentro da categoria.
export const permissionCategories: PermissionCategory[] = [
  {
    label: 'Acadêmico',
    icon: 'i-lucide-book-marked',
    groups: ['Campi', 'Classrooms', 'Courses', 'CourseCurriculums', 'Disciplines'],
  },
  {
    label: 'Secretaria',
    icon: 'i-lucide-archive',
    groups: ['CourseOfferings', 'Classes', 'Calendar', 'Notifications', 'Periods'],
  },
  {
    label: 'Pessoas',
    icon: 'i-lucide-contact',
    groups: ['Students', 'Teachers', 'Parents'],
  },
  {
    label: 'Sistema',
    icon: 'i-lucide-cog',
    groups: ['Identity', 'Institutions', 'Webhooks'],
  },
]

export function usePermissions() {
  const config = useRuntimeConfig()

  const { data } = useFetch<GetPermissionsOut>(
    `${config.public.backendUrl}/identity/permissions`,
    { key: 'identity-permissions', credentials: 'include', server: false }
  )

  function permissionsFor(userType: MaybeRefOrGetter<UserTypeName | null | undefined>) {
    return computed(() => {
      const type = toValue(userType)
      if (!type) return []
      return (data.value?.items ?? []).filter(p => p.allowedTypes.includes(type))
    })
  }

  return { permissionsFor }
}
