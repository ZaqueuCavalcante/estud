<script setup lang="ts">
import { dbSchema, type DbTable } from '~/utils/db-schema'

type GroupId =
  | 'identity'
  | 'auth'
  | 'institution'
  | 'catalog'
  | 'calendar'
  | 'classes'
  | 'people'
  | 'notifications'
  | 'webhooks'
  | 'platform'

interface TableRow {
  table: string
  entity: string
  group: GroupId
  note: string
  dbSet?: string
  join?: boolean
}

interface GroupInfo {
  label: string
  icon: string
  description: string
}

const groups: Record<GroupId, GroupInfo> = {
  identity: {
    label: 'Identidade & acesso',
    icon: 'i-lucide-key-round',
    description: 'Tabelas do ASP.NET Identity, com os tipos customizados do Estud. As permissões vivem como claims do perfil.',
  },
  auth: {
    label: 'Autenticação',
    icon: 'i-lucide-fingerprint',
    description: 'Artefatos dos fluxos de login próprios: magic link, reset de senha, SSO e login social.',
  },
  institution: {
    label: 'Instituição & estrutura física',
    icon: 'i-lucide-building-2',
    description: 'A raiz do multi-tenant e o que existe fisicamente dentro dela.',
  },
  catalog: {
    label: 'Catálogo acadêmico',
    icon: 'i-lucide-library',
    description: 'O que a instituição oferece: cursos, disciplinas, grades curriculares e ofertas.',
  },
  calendar: {
    label: 'Períodos & calendário',
    icon: 'i-lucide-calendar-days',
    description: 'As janelas de tempo que enquadram matrículas e turmas.',
  },
  classes: {
    label: 'Turmas, aulas & avaliação',
    icon: 'i-lucide-presentation',
    description: 'A execução do semestre: turmas, horários, aulas geradas, presença, atividades e notas.',
  },
  people: {
    label: 'Pessoas & papéis',
    icon: 'i-lucide-users',
    description: 'Aluno, professor, responsável e administrativo — cada papel com seus vínculos.',
  },
  notifications: {
    label: 'Notificações',
    icon: 'i-lucide-bell',
    description: 'Conteúdo da notificação separado da entrega por usuário.',
  },
  webhooks: {
    label: 'Webhooks',
    icon: 'i-lucide-webhook',
    description: 'Integrações de saída (com retry) e eventos recebidos de terceiros.',
  },
  platform: {
    label: 'Plataforma',
    icon: 'i-lucide-cpu',
    description: 'Infraestrutura interna: fila de comandos, eventos de domínio, auditoria e trilha de atividades.',
  },
}

const tables: TableRow[] = [
  { table: 'users', entity: 'EstudUser', group: 'identity', dbSet: 'Users', note: 'Usuário do sistema. Um usuário pertence a uma instituição e pode acumular papéis (aluno, professor, responsável).' },
  { table: 'roles', entity: 'EstudRole', group: 'identity', dbSet: 'Roles', note: 'Perfil de acesso por instituição. Há papéis padrão em EstudDefaultRoles.' },
  { table: 'user_roles', entity: 'EstudUserRole', group: 'identity', dbSet: 'UserRoles', join: true, note: 'Vínculo usuário ↔ perfil.' },
  { table: 'user_claims', entity: 'EstudUserClaim', group: 'identity', dbSet: 'UserClaims', note: 'Claims atribuídas diretamente ao usuário.' },
  { table: 'role_claims', entity: 'EstudRoleClaim', group: 'identity', dbSet: 'RoleClaims', note: 'Claims do perfil — é aqui que as permissões (EstudPermission) são persistidas.' },
  { table: 'user_logins', entity: 'EstudUserLogin', group: 'identity', dbSet: 'UserLogins', note: 'Logins de provedores externos, no formato do Identity.' },
  { table: 'user_tokens', entity: 'EstudUserToken', group: 'identity', dbSet: 'UserTokens', note: 'Tokens do Identity, incluindo o segredo TOTP do 2FA.' },

  { table: 'magic_links', entity: 'MagicLink', group: 'auth', dbSet: 'WebMagicLinks', note: 'Link de uso único enviado por e-mail para login sem senha.' },
  { table: 'reset_password_tokens', entity: 'ResetPasswordToken', group: 'auth', dbSet: 'ResetPasswordTokens', note: 'Token de recuperação de senha.' },
  { table: 'sso_configurations', entity: 'SsoConfiguration', group: 'auth', dbSet: 'WebSsoConfigurations', note: 'Configuração OIDC de SSO da instituição, com o segredo cifrado e a flag de SSO obrigatório.' },
  { table: 'sso_allowed_domains', entity: 'SsoAllowedDomain', group: 'auth', dbSet: 'WebSsoAllowedDomains', note: 'Domínios de e-mail aceitos no SSO daquela configuração.' },
  { table: 'user_social_logins', entity: 'UserSocialLogin', group: 'auth', dbSet: 'UserSocialLogins', note: 'Vínculo do usuário com um provedor social (Google), usado pelo One Tap e pelo fluxo OAuth.' },
  { table: 'data_protection_keys', entity: 'DataProtectionKey', group: 'auth', dbSet: 'DataProtectionKeys', note: 'Chaves do ASP.NET Data Protection. Ficam no banco para as instâncias compartilharem o mesmo anel de chaves.' },

  { table: 'institutions', entity: 'Institution', group: 'institution', dbSet: 'Institutions', note: 'Raiz do multi-tenant. O InstitutionId do request sai daqui e escopa praticamente todas as consultas.' },
  { table: 'institution_configs', entity: 'InstitutionConfig', group: 'institution', dbSet: 'InstitutionConfigs', note: 'Configurações da instituição (1:1), como exigir 2FA.' },
  { table: 'campi', entity: 'Campus', group: 'institution', dbSet: 'Campi', note: 'Campus da instituição, com estado e cidade.' },
  { table: 'opening_hours', entity: 'OpeningHour', group: 'institution', dbSet: 'OpeningHours', note: 'Horário de funcionamento do campus por dia da semana.' },
  { table: 'classrooms', entity: 'Classroom', group: 'institution', dbSet: 'Classrooms', note: 'Sala de aula de um campus, com capacidade.' },

  { table: 'courses', entity: 'Course', group: 'catalog', dbSet: 'Courses', note: 'Curso oferecido pela instituição (tecnólogo, bacharelado, etc.).' },
  { table: 'disciplines', entity: 'Discipline', group: 'catalog', dbSet: 'Disciplines', note: 'Disciplina do catálogo, independente de curso.' },
  { table: 'courses_disciplines', entity: 'CourseDiscipline', group: 'catalog', dbSet: 'CoursesDisciplines', join: true, note: 'Vincula a disciplina ao curso. É pré-requisito para ela entrar numa grade.' },
  { table: 'course_curriculums', entity: 'CourseCurriculum', group: 'catalog', dbSet: 'CourseCurriculums', note: 'Matriz curricular de um curso, com vigência.' },
  { table: 'course_curriculums_disciplines', entity: 'CourseCurriculumDiscipline', group: 'catalog', dbSet: 'CourseCurriculumDisciplines', join: true, note: 'Disciplinas que compõem a grade, com período/semestre.' },
  { table: 'course_offerings', entity: 'CourseOffering', group: 'catalog', dbSet: 'CourseOfferings', note: 'Oferta de um curso num campus, com uma grade, num período acadêmico. É nela que o aluno se matricula.' },

  { table: 'academic_periods', entity: 'AcademicPeriod', group: 'calendar', dbSet: 'AcademicPeriods', note: 'Período letivo. Define as datas usadas para gerar as aulas das turmas.' },
  { table: 'enrollment_periods', entity: 'EnrollmentPeriod', group: 'calendar', dbSet: 'EnrollmentPeriods', note: 'Janela em que as matrículas ficam abertas.' },
  { table: 'calendar_days', entity: 'CalendarDay', group: 'calendar', dbSet: 'CalendarDays', note: 'Dia marcado no calendário acadêmico (feriado, recesso, evento).' },

  { table: 'classes', entity: 'Class', group: 'classes', dbSet: 'Classes', note: 'Turma de uma disciplina num período acadêmico, com status e vagas.' },
  { table: 'schedules', entity: 'Schedule', group: 'classes', dbSet: 'Schedules', note: 'Horário recorrente da turma (dia da semana + intervalo). Criado junto com a turma.' },
  { table: 'classrooms__classes', entity: 'ClassroomClass', group: 'classes', join: true, note: 'Alocação da turma numa sala. Chave composta (ClassroomId, ClassId) e sem DbSet — acessada por consulta direta.' },
  { table: 'class_lessons', entity: 'ClassLesson', group: 'classes', dbSet: 'ClassLessons', note: 'Aula concreta, gerada na criação da turma a partir dos horários e das datas do período.' },
  { table: 'class_lesson_attendances', entity: 'ClassLessonAttendance', group: 'classes', dbSet: 'ClassLessonAttendances', note: 'Presença do aluno numa aula. Só é criada para aulas já iniciadas.' },
  { table: 'classes__students', entity: 'ClassStudent', group: 'classes', dbSet: 'ClassStudents', join: true, note: 'Aluno matriculado na turma. Exige turma em matrícula e vaga disponível.' },
  { table: 'classes__teachers', entity: 'ClassTeacher', group: 'classes', dbSet: 'ClassTeachers', join: true, note: 'Professor responsável pela turma.' },
  { table: 'class_activities', entity: 'ClassActivity', group: 'classes', dbSet: 'ClassActivities', note: 'Atividade ou avaliação criada pelo professor da turma.' },
  { table: 'class_activity_works', entity: 'ClassActivityWork', group: 'classes', dbSet: 'ClassActivityWorks', note: 'Entrega do aluno para uma atividade.' },
  { table: 'student_class_notes', entity: 'StudentClassNote', group: 'classes', note: 'Nota do aluno na turma, com precisão (4,2). Única por (turma, aluno, tipo). Sem DbSet.' },

  { table: 'students', entity: 'EstudStudent', group: 'people', dbSet: 'Students', note: 'Aluno. O usuário correspondente é criado junto, no mesmo CreateStudent.' },
  { table: 'student_course_enrollments', entity: 'StudentCourseEnrollment', group: 'people', dbSet: 'StudentCourseEnrollments', note: 'Matrícula do aluno numa oferta de curso.' },
  { table: 'enrollment_proofs', entity: 'EnrollmentProof', group: 'people', dbSet: 'EnrollmentProofs', note: 'Documentos e comprovantes anexados à matrícula.' },
  { table: 'teachers', entity: 'EstudTeacher', group: 'people', dbSet: 'Teachers', note: 'Professor. O usuário também é criado junto, no CreateTeacher.' },
  { table: 'teachers_campi', entity: 'TeacherCampus', group: 'people', dbSet: 'TeachersCampi', join: true, note: 'Campi onde o professor pode lecionar.' },
  { table: 'teachers_disciplines', entity: 'TeacherDiscipline', group: 'people', dbSet: 'TeachersDisciplines', join: true, note: 'Disciplinas que o professor pode lecionar. Validado ao vincular a uma turma.' },
  { table: 'parents', entity: 'EstudParent', group: 'people', dbSet: 'Parents', note: 'Responsável por um ou mais alunos.' },
  { table: 'parent_students', entity: 'ParentStudent', group: 'people', dbSet: 'ParentStudents', join: true, note: 'Vínculo responsável ↔ aluno.' },
  { table: 'admin_users', entity: 'AdminUser', group: 'people', dbSet: 'AdminUsers', note: 'Usuário administrativo, fora do escopo de uma instituição.' },

  { table: 'notifications', entity: 'Notification', group: 'notifications', dbSet: 'Notifications', note: 'Conteúdo da notificação, criado no escopo da instituição.' },
  { table: 'user_notifications', entity: 'UserNotification', group: 'notifications', dbSet: 'UserNotifications', note: 'Entrega da notificação a um usuário, com estado de leitura.' },

  { table: 'webhook_subscriptions', entity: 'WebhookSubscription', group: 'webhooks', dbSet: 'WebhookSubscriptions', note: 'Inscrição da instituição num evento, com URL de destino e segredo de assinatura.' },
  { table: 'webhook_calls', entity: 'WebhookCall', group: 'webhooks', dbSet: 'WebhookCalls', note: 'Disparo de um evento para uma inscrição.' },
  { table: 'webhook_call_attempts', entity: 'WebhookCallAttempt', group: 'webhooks', dbSet: 'WebhookCallAttempts', note: 'Cada tentativa de entrega do disparo, com status e resposta. É o que sustenta o retry.' },
  { table: 'received_webhook_events', entity: 'ReceivedWebhookEvent', group: 'webhooks', dbSet: 'ReceivedWebhookEvents', note: 'Evento recebido de sistema externo, guardado para idempotência.' },

  { table: 'commands', entity: 'Command', group: 'platform', dbSet: 'Commands', note: 'Fila de comandos assíncronos, consumida pelo CommandsProcessorJob (Quartz). Suporta pai/filho, retry com backoff e execução adiada (NotBefore).' },
  { table: 'command_batches', entity: 'CommandBatch', group: 'platform', note: 'Agrupa comandos disparados juntos. Sem DbSet — acessada por navigation.' },
  { table: 'domain_events', entity: 'DomainEvent', group: 'platform', dbSet: 'DomainEvents', note: 'Eventos de domínio persistidos pelo SaveDomainEventsInterceptor no SaveChanges.' },
  { table: 'audit_trails', entity: 'AuditTrail', group: 'platform', note: 'Trilha de auditoria gravada pelo AuditSaveChangesInterceptor. O diff da entidade fica em Data (jsonb). Sem DbSet.' },
  { table: 'user_activities', entity: 'UserActivity', group: 'platform', dbSet: 'UserActivities', note: 'Trilha de atividades do usuário para exibição no produto.' },
]

const isMobile = useIsMobile()

const schemaByTable = new Map(dbSchema.map(t => [t.table, t]))

const inboundByTable = computed(() => {
  const map = new Map<string, { from: string, columns: string[] }[]>()
  for (const t of dbSchema) {
    for (const fk of t.fks) {
      if (!fk.target || fk.target === t.table) continue
      const list = map.get(fk.target) ?? []
      list.push({ from: t.table, columns: fk.columns })
      map.set(fk.target, list)
    }
  }
  return map
})

const search = ref('')
const activeGroup = ref<GroupId | null>(null)
const openTable = ref<string | null>(null)

const detail = computed<DbTable | null>(() => (openTable.value ? schemaByTable.get(openTable.value) ?? null : null))
const detailRow = computed(() => tables.find(t => t.table === openTable.value) ?? null)
const detailInbound = computed(() => (openTable.value ? inboundByTable.value.get(openTable.value) ?? [] : []))

const isOpen = computed({
  get: () => openTable.value !== null,
  set: (v: boolean) => { if (!v) openTable.value = null },
})

const pkSet = computed(() => new Set(detail.value?.pk ?? []))

const fkByColumn = computed(() => {
  const map = new Map<string, string[]>()
  for (const fk of detail.value?.fks ?? []) {
    for (const c of fk.columns) {
      const list = map.get(c) ?? []
      if (fk.target) list.push(fk.target)
      map.set(c, list)
    }
  }
  return map
})

const indexedColumns = computed(() => {
  const set = new Set<string>()
  for (const i of detail.value?.indexes ?? []) for (const c of i.columns) set.add(c)
  return set
})

const groupIds = Object.keys(groups) as GroupId[]

const countByGroup = computed(() => {
  const counts = {} as Record<GroupId, number>
  for (const id of groupIds) counts[id] = 0
  for (const row of tables) counts[row.group] += 1
  return counts
})

const filtered = computed(() => {
  const term = search.value.trim().toLowerCase()
  return tables.filter((row) => {
    if (activeGroup.value && row.group !== activeGroup.value) return false
    if (!term) return true
    return row.table.includes(term)
      || row.entity.toLowerCase().includes(term)
      || row.note.toLowerCase().includes(term)
      || (row.dbSet?.toLowerCase().includes(term) ?? false)
  })
})

const sections = computed(() =>
  groupIds
    .map(id => ({ id, info: groups[id], rows: filtered.value.filter(r => r.group === id) }))
    .filter(s => s.rows.length > 0),
)

const withoutDbSet = computed(() => tables.filter(t => !t.dbSet))

function openDetail(table: string) {
  if (!schemaByTable.has(table)) return
  openTable.value = table
}

function toggleGroup(id: GroupId) {
  activeGroup.value = activeGroup.value === id ? null : id
}

function clearFilters() {
  activeGroup.value = null
  search.value = ''
}
</script>

<template>
  <UDashboardPanel id="dev-entities">
    <template #header>
      <UDashboardNavbar title="Tabelas do Banco">
        <template #leading>
          <PageIcon icon="i-lucide-database" />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="entities space-y-6">
        <div class="space-y-2">
          <p class="text-sm text-muted">
            As <span class="font-medium text-highlighted">{{ tables.length }}</span> tabelas do sistema, agrupadas por
            contexto de negócio. Todas vivem no schema <code class="text-xs">estud</code>, com nomes em
            <code class="text-xs">snake_case</code>. O mapeamento vem dos
            <code class="text-xs">IEntityTypeConfiguration</code> em <code class="text-xs">Back/Database/</code> — não há
            pasta de <span class="italic">migrations</span> no repositório. Para atualizar depois de mexer no backend, rode
            <code class="text-xs">python3 scripts/gen-db-schema.py</code>.
          </p>
          <p class="text-sm text-muted">
            Clique no nome de uma tabela para ver colunas, tipos, chave primária, chaves estrangeiras e índices.
            Para ver a ordem de criação e as dependências entre as entidades, veja
            <NuxtLink to="/dev/dependencies" class="text-primary hover:underline">Dependências de Entidades</NuxtLink>.
          </p>
        </div>

        <div class="flex items-center gap-2 flex-wrap">
          <UInput
            v-model="search"
            icon="i-lucide-search"
            placeholder="Filtrar por tabela, entidade ou descrição"
            class="w-full sm:w-80"
          />
          <UButton
            v-if="search || activeGroup"
            variant="ghost"
            color="neutral"
            icon="i-lucide-x"
            label="Limpar"
            @click="() => { clearFilters() }"
          />
        </div>

        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-2">
          <button
            v-for="id in groupIds"
            :key="id"
            type="button"
            class="group-chip rounded-lg border p-3 text-left transition"
            :class="activeGroup === id ? 'border-primary bg-primary/5' : 'border-default bg-default hover:bg-elevated/50'"
            :style="{ '--group-color': `var(--g-${id})` }"
            @click="() => { toggleGroup(id) }"
          >
            <div class="flex items-center gap-2">
              <UIcon :name="groups[id].icon" class="size-4 shrink-0 text-(--group-color)" />
              <span class="text-2xl font-semibold text-highlighted leading-none">{{ countByGroup[id] }}</span>
            </div>
            <p class="mt-1.5 text-xs text-toned leading-snug">{{ groups[id].label }}</p>
          </button>
        </div>

        <p v-if="sections.length === 0" class="text-sm text-muted">
          Nenhuma tabela corresponde ao filtro.
        </p>

        <section v-for="s in sections" :key="s.id" class="space-y-3" :style="{ '--group-color': `var(--g-${s.id})` }">
          <div class="space-y-1">
            <div class="flex items-center gap-2">
              <UIcon :name="s.info.icon" class="size-4 shrink-0 text-(--group-color)" />
              <h2 class="text-base font-semibold text-highlighted">{{ s.info.label }}</h2>
              <UBadge variant="subtle" color="neutral" size="sm">{{ s.rows.length }}</UBadge>
            </div>
            <p class="text-sm text-muted">{{ s.info.description }}</p>
          </div>

          <div class="overflow-x-auto rounded-lg border border-default">
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-default bg-elevated/50 text-left text-xs text-muted">
                  <th class="px-3 py-2 font-medium">Tabela</th>
                  <th class="px-3 py-2 font-medium">Entidade</th>
                  <th class="px-3 py-2 font-medium">DbSet</th>
                  <th class="px-3 py-2 font-medium">Descrição</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="row in s.rows" :key="row.table" class="border-b border-default last:border-b-0">
                  <td class="px-3 py-2 whitespace-nowrap">
                    <button type="button" class="table-name" @click="() => { openDetail(row.table) }">
                      {{ row.table }}
                    </button>
                    <UBadge v-if="row.join" variant="subtle" color="neutral" size="sm" class="ml-2">vínculo</UBadge>
                  </td>
                  <td class="px-3 py-2 text-toned whitespace-nowrap font-mono text-xs">{{ row.entity }}</td>
                  <td class="px-3 py-2 whitespace-nowrap font-mono text-xs">
                    <span v-if="row.dbSet" class="text-muted">ctx.{{ row.dbSet }}</span>
                    <span v-else class="text-dimmed">—</span>
                  </td>
                  <td class="px-3 py-2 text-muted">{{ row.note }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        <UCard :ui="{ body: 'p-3 sm:p-4' }">
          <div class="space-y-1.5">
            <div class="flex items-center gap-2">
              <UIcon name="i-lucide-info" class="size-4 shrink-0 text-muted" />
              <span class="text-sm font-semibold text-highlighted">Tabelas sem DbSet</span>
            </div>
            <p class="text-sm text-muted">
              {{ withoutDbSet.length }} tabelas existem só como configuração no <code class="text-xs">EstudDbContext</code>,
              sem propriedade <code class="text-xs">DbSet</code>: são alcançadas por navigation property, consulta direta
              ou escritas por interceptor.
            </p>
            <div class="flex items-center gap-2 flex-wrap pt-0.5">
              <code v-for="row in withoutDbSet" :key="row.table" class="text-xs text-toned">{{ row.table }}</code>
            </div>
          </div>
        </UCard>

        <UModal
          v-model:open="isOpen"
          :fullscreen="isMobile"
          :title="detail?.table ?? ''"
          :description="detailRow?.note ?? ''"
          :ui="{ content: 'sm:max-w-6xl' }"
        >
          <template #body>
            <div v-if="detail" class="space-y-5">
              <div class="flex items-center gap-2 flex-wrap text-xs">
                <UBadge variant="subtle" color="neutral">{{ detail.entity }}</UBadge>
                <UBadge variant="subtle" color="neutral">schema estud</UBadge>
                <UBadge variant="subtle" color="neutral">{{ detail.columns.length }} colunas</UBadge>
                <code class="text-muted">{{ detail.file }}</code>
              </div>

              <div>
                <h3 class="mb-2 text-xs font-semibold uppercase tracking-wide text-dimmed">Colunas</h3>
                <div class="overflow-x-auto rounded-lg border border-default">
                  <table class="w-full text-sm">
                    <thead>
                      <tr class="border-b border-default bg-elevated/50 text-left text-xs text-muted">
                        <th class="px-3 py-2 font-medium">Coluna</th>
                        <th class="px-3 py-2 font-medium">Tipo</th>
                        <th class="px-3 py-2 font-medium">Nulo</th>
                        <th class="px-3 py-2 font-medium">Chaves</th>
                        <th class="px-3 py-2 font-medium">Propriedade</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="col in detail.columns" :key="col.name" class="border-b border-default last:border-b-0">
                        <td class="px-3 py-2 font-mono text-xs text-highlighted whitespace-nowrap">{{ col.name }}</td>
                        <td class="px-3 py-2 font-mono text-xs text-toned whitespace-nowrap">
                          {{ col.type }}
                          <span v-if="col.enum" class="text-dimmed">· {{ col.enum }}</span>
                          <span v-if="col.default !== undefined" class="text-dimmed">· default {{ col.default }}</span>
                        </td>
                        <td class="px-3 py-2 text-xs whitespace-nowrap">
                          <span :class="col.nullable ? 'text-muted' : 'text-dimmed'">{{ col.nullable ? 'sim' : 'não' }}</span>
                        </td>
                        <td class="px-3 py-2 whitespace-nowrap">
                          <span v-if="pkSet.has(col.name)" class="key-tag key-pk">PK</span>
                          <span v-for="t in fkByColumn.get(col.name) ?? []" :key="t" class="key-tag key-fk">FK → {{ t }}</span>
                          <span v-if="indexedColumns.has(col.name) && !pkSet.has(col.name)" class="key-tag key-idx">IDX</span>
                        </td>
                        <td class="px-3 py-2 font-mono text-xs text-muted whitespace-nowrap">
                          {{ col.prop }}<span class="text-dimmed"> : {{ col.clr }}</span>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

              <div class="grid gap-5 lg:grid-cols-2">
                <div>
                  <h3 class="mb-2 text-xs font-semibold uppercase tracking-wide text-dimmed">Chave primária</h3>
                  <p v-if="detail.pk.length === 0" class="text-sm text-muted">Sem chave primária declarada.</p>
                  <div v-else class="flex items-center gap-1.5 flex-wrap">
                    <code v-for="c in detail.pk" :key="c" class="text-xs text-toned">{{ c }}</code>
                    <span v-if="detail.pk.length > 1" class="text-xs text-dimmed">(composta)</span>
                  </div>
                </div>

                <div>
                  <h3 class="mb-2 text-xs font-semibold uppercase tracking-wide text-dimmed">Índices</h3>
                  <p v-if="detail.indexes.length === 0" class="text-sm text-muted">Nenhum índice explícito além da chave primária.</p>
                  <ul v-else class="space-y-1">
                    <li v-for="(idx, i) in detail.indexes" :key="i" class="text-sm">
                      <code class="text-xs text-toned">{{ idx.columns.join(', ') }}</code>
                      <UBadge v-if="idx.unique" variant="subtle" color="neutral" size="sm" class="ml-1.5">único</UBadge>
                    </li>
                  </ul>
                </div>
              </div>

              <div>
                <h3 class="mb-2 text-xs font-semibold uppercase tracking-wide text-dimmed">Chaves estrangeiras</h3>
                <p v-if="detail.fks.length === 0" class="text-sm text-muted">Esta tabela não aponta para nenhuma outra.</p>
                <ul v-else class="space-y-1.5">
                  <li v-for="(fk, i) in detail.fks" :key="i" class="text-sm flex items-center gap-1.5 flex-wrap">
                    <code class="text-xs text-toned">{{ fk.columns.join(', ') }}</code>
                    <UIcon name="i-lucide-arrow-right" class="size-3.5 shrink-0 text-dimmed" />
                    <button
                      v-if="fk.target"
                      type="button"
                      class="text-xs font-mono text-primary hover:underline"
                      @click="() => { openDetail(fk.target!) }"
                    >
                      {{ fk.target }}<span v-if="fk.principal.length">.{{ fk.principal.join(', ') }}</span>
                    </button>
                    <span v-else class="text-xs text-muted">{{ fk.targetEntity ?? '—' }}</span>
                    <UBadge v-if="fk.convention" variant="subtle" color="neutral" size="sm">por convenção</UBadge>
                  </li>
                </ul>
              </div>

              <div>
                <h3 class="mb-2 text-xs font-semibold uppercase tracking-wide text-dimmed">Referenciada por</h3>
                <p v-if="detailInbound.length === 0" class="text-sm text-muted">Nenhuma outra tabela aponta para esta.</p>
                <ul v-else class="space-y-1.5">
                  <li v-for="(inb, i) in detailInbound" :key="i" class="text-sm flex items-center gap-1.5 flex-wrap">
                    <button type="button" class="text-xs font-mono text-primary hover:underline" @click="() => { openDetail(inb.from) }">
                      {{ inb.from }}
                    </button>
                    <code class="text-xs text-dimmed">({{ inb.columns.join(', ') }})</code>
                  </li>
                </ul>
              </div>
            </div>
          </template>
        </UModal>
      </div>
    </template>
  </UDashboardPanel>
</template>

<style scoped>
.entities {
  --g-identity: #2a78d6;
  --g-auth: #eda100;
  --g-institution: #1baf7a;
  --g-catalog: #e87ba4;
  --g-calendar: #8b5cf6;
  --g-classes: #0891b2;
  --g-people: #d9480f;
  --g-notifications: #7048e8;
  --g-webhooks: #0ca678;
  --g-platform: #94a3b8;
}

.dark .entities {
  --g-identity: #3987e5;
  --g-auth: #c98500;
  --g-institution: #199e70;
  --g-catalog: #d55181;
  --g-calendar: #a78bfa;
  --g-classes: #22b8cf;
  --g-people: #f76707;
  --g-notifications: #9775fa;
  --g-webhooks: #20c997;
  --g-platform: #64748b;
}

.group-chip {
  cursor: pointer;
}

.table-name {
  font-family: var(--font-mono, monospace);
  font-size: 0.8125rem;
  color: var(--ui-text-highlighted);
  font-weight: 500;
  cursor: pointer;
}

.table-name:hover {
  color: var(--ui-primary);
  text-decoration: underline;
}

.key-tag {
  display: inline-block;
  margin-right: 0.25rem;
  border-radius: 0.25rem;
  padding: 0.0625rem 0.3125rem;
  font-family: var(--font-mono, monospace);
  font-size: 0.6875rem;
  line-height: 1.2;
  white-space: nowrap;
}

.key-pk {
  background: color-mix(in oklab, var(--ui-primary) 15%, transparent);
  color: var(--ui-primary);
}

.key-fk {
  background: var(--ui-bg-elevated);
  color: var(--ui-text-toned);
}

.key-idx {
  background: var(--ui-bg-elevated);
  color: var(--ui-text-dimmed);
}
</style>
