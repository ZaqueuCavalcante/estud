import type { PolicyName } from '~/policies'
import type { UserType } from '~/composables/useUserAccount'

const routePolicies: Record<string, PolicyName> = {
  '/home': 'AccessHomePage',
  '/classes': 'AccessClassesPage',
  '/campi': 'AccessCampiPage',
  '/courses': 'AccessCoursesPage',
  '/security/sso': 'AccessSsoPage',
  '/security/2fa': 'AccessTwoFactorEnforcementPage',
  '/security': 'AccessSecurityPage',
  '/teachers': 'AccessTeachersPage',
  '/students': 'AccessStudentsPage',
  '/parents': 'AccessParentsPage',
  '/disciplines': 'AccessDisciplinesPage',
  '/course-offerings': 'AccessCourseOfferingsPage',
  '/course-curriculums': 'AccessCourseCurriculumsPage',
  '/notifications': 'AccessNotificationsPage',
  '/agenda': 'AccessAgendaPage',
  '/frequencies': 'AccessFrequenciesPage',
  '/documents': 'AccessDocumentsPage',
  '/calendar': 'AccessCalendarPage',
  '/configs': 'AccessConfigsPage',
  '/children': 'AccessChildrenPage',
  '/admin/institutions': 'AccessAdminInstitutionsPage',
  '/admin/domain-events': 'AccessAdminDomainEventsPage',
}

// A página de detalhe da turma é a mesma rota para os 3 perfis, mas cada um
// consome um endpoint próprio — a policy exigida depende do UserType.
const classDetailPolicies: Record<UserType, PolicyName> = {
  Manager: 'AccessClassesPage',
  Teacher: 'GetTeacherClass',
  Student: 'GetStudentClass',
  Parent: 'GetStudentClass', // responsável ainda não tem detalhe de turma; a policy exige Student e o redireciona pra /home
}

export default defineNuxtRouteMiddleware(async (to) => {
  if (import.meta.server) return

  const routePolicy = routePolicies[to.path]
  const isCampusDetail = to.path.startsWith('/campi/')
  const isClassDetail = to.path.startsWith('/classes/')
  const isCourseDetail = to.path.startsWith('/courses/')
  const isTeacherDetail = to.path.startsWith('/teachers/')
  const isStudentDetail = to.path.startsWith('/students/')
  const isClassroomDetail = to.path.startsWith('/classrooms/')
  const isDisciplineDetail = to.path.startsWith('/disciplines/')
  const isCourseCurriculumRoute = to.path.startsWith('/course-curriculums/')
  const isCourseOfferingDetail = to.path.startsWith('/course-offerings/')
  if (!routePolicy && !isClassDetail && !isClassroomDetail && !isCampusDetail && !isTeacherDetail && !isStudentDetail && !isDisciplineDetail && !isCourseDetail && !isCourseCurriculumRoute && !isCourseOfferingDetail) return

  const { account, fetchAccount } = useUserAccount()

  if (!account.value) {
    try {
      await fetchAccount()
    } catch {
      return navigateTo('/')
    }
  }

  const policyName = routePolicy
    ?? (isClassroomDetail
      ? 'GetClassroom' as PolicyName
      : isCampusDetail
        ? 'AccessCampiPage' as PolicyName
        : isTeacherDetail
          ? 'AccessTeachersPage' as PolicyName
          : isStudentDetail
            ? 'AccessStudentsPage' as PolicyName
            : isDisciplineDetail
              ? 'AccessDisciplinesPage' as PolicyName
              : isCourseDetail
                ? 'AccessCoursesPage' as PolicyName
                : isCourseCurriculumRoute
                  ? 'AccessCourseCurriculumsPage' as PolicyName
                  : isCourseOfferingDetail
                    ? 'AccessCourseOfferingsPage' as PolicyName
                    : classDetailPolicies[account.value!.userType])
  if (!policyName) return

  const { can } = usePolicy()
  if (!can(policyName).value) {
    return navigateTo('/home')
  }
})
