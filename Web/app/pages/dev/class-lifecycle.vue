<script setup lang="ts">
type GroupId = 'action' | 'status' | 'virtual' | 'effect' | 'missing'

type EdgeStyle = 'solid' | 'dashed' | 'dotted'

interface DiagramNode {
  id: string
  label: string
  sub?: string
  group: GroupId
  x: number
  y: number
  w?: number
  planned?: boolean
  note?: string
}

interface DiagramEdge {
  from: string
  to: string
  label?: string
  style?: EdgeStyle
}

interface DiagramFrame {
  label: string
  x: number
  y: number
  w: number
  h: number
}

interface Diagram {
  id: string
  title: string
  description: string
  width: number
  height: number
  frames?: DiagramFrame[]
  nodes: DiagramNode[]
  edges: DiagramEdge[]
}

const groups: Record<GroupId, string> = {
  action: 'Endpoint / ação',
  status: 'Status persistido',
  virtual: 'Status virtual (só na leitura)',
  effect: 'Efeito colateral',
  missing: 'Não existe no código',
}

const NODE_H = 54
const NODE_W = 190

const diagrams: Diagram[] = [
  {
    id: 'machine',
    title: 'Máquina de estados',
    description: 'Os cinco status do enum ClassStatus e as duas únicas transições que existem hoje. Só quatro valores chegam a ser gravados na coluna status: OnReview é calculado em memória e Finalized nunca é escrito por ninguém.',
    width: 1180,
    height: 440,
    frames: [
      { label: 'Editável (professores, horários)', x: 232, y: 168, w: 460, h: 138 },
    ],
    nodes: [
      { id: 'create', label: 'POST /classes', sub: 'CreateClass', group: 'action', x: 16, y: 210, w: 180, note: 'CreateClassService valida vagas (0 a 100), disciplina, período letivo e, se informado, campus — tudo recortado pela instituição do ctx.RequestUser. O construtor de Class já grava Status = OnPreEnrollment e inicia as listas de professores, horários, aulas, alunos e atividades vazias.' },
      { id: 'pre', label: 'Pré-matrícula', sub: 'OnPreEnrollment · 0', group: 'status', x: 246, y: 210, note: 'Status inicial. É aqui que a turma é montada: UpdateClassTeachers (no máximo 2, e o professor precisa lecionar a disciplina) e UpdateClassSchedules (checa conflito de horário de professor e de sala contra outras turmas não finalizadas). Alunos já podem ser matriculados neste status — AssignStudentToClass não olha o status, só barra Finalized.' },
      { id: 'enroll', label: 'Matrícula', sub: 'OnEnrollment · 1', group: 'status', x: 486, y: 210, note: 'Única saída da pré-matrícula. A turma continua totalmente editável: professores, horários e matrículas seguem liberados. O docstring do enum diz que só se chega aqui dentro do período de matrícula vigente, mas o ReleaseClassForEnrollmentService não faz essa checagem.' },
      { id: 'review', label: 'Revisão', sub: 'OnReview · 2 (virtual)', group: 'virtual', x: 486, y: 70, note: 'Nunca é gravado no banco. Na leitura, se a turma está OnEnrollment e não existe EnrollmentPeriod com StartAt ≤ hoje ≤ EndAt, o status devolvido é trocado por OnReview. A conta é repetida em GetClassService, GetClassesService, GetDisciplineDetailsService, GetStudentDetailsService e GetTeacherDetailsService — cinco cópias da mesma regra. GetClassesService ainda precisa traduzir o filtro: pedir Status=OnEnrollment sem período aberto devolve lista vazia, e pedir OnReview busca OnReview ou OnEnrollment.' },
      { id: 'started', label: 'Iniciada', sub: 'Started · 3', group: 'status', x: 726, y: 210, note: 'Ponto sem volta: nenhum código volta o status daqui. StartClassService exige status OnEnrollment (o que inclui o OnReview virtual, já que no banco ainda é OnEnrollment), pelo menos um professor e pelo menos um horário. Não checa se o período de matrícula terminou, apesar do remarks do controller afirmar que sim.' },
      { id: 'finalized', label: 'Finalizada', sub: 'Finalized · 4', group: 'missing', x: 966, y: 210, planned: true, note: 'O valor existe no enum e é lido em vários lugares (AssignStudentToClass barra, UpdateClassSchedules barra, as consultas de ocupação de campus excluem), mas nenhum serviço grava ClassStatus.Finalized. Não há feature FinalizeClass, nem policy, nem endpoint — a turma fica em Started para sempre.' },
      { id: 'lessons', label: 'Aulas geradas', sub: 'Class.CreateLessons', group: 'effect', x: 726, y: 350, note: 'No start, o CalendarResolver da instituição (e do campus, quando presencial) percorre dia a dia o período letivo. Para cada dia letivo e cada horário do dia da semana correspondente cria uma ClassLesson numerada e soma a duração no Workload da turma. Feriado e recesso não viram aula.' },
      { id: 'frozen', label: 'Horários travados', sub: 'ClassAlreadyStarted', group: 'effect', x: 966, y: 350, note: 'A partir de Started, UpdateClassSchedules devolve ClassAlreadyStarted — as aulas já foram derivadas dos horários e ficariam inconsistentes. UpdateClassTeachers, porém, continua liberado: dá para trocar o professor de uma turma iniciada, e a remoção zera o TeacherId dos horários dele.' },
    ],
    edges: [
      { from: 'create', to: 'pre', label: 'status inicial' },
      { from: 'pre', to: 'enroll', label: 'PUT /classes/{id}/release-for-enrollment' },
      { from: 'enroll', to: 'review', label: 'sem período de matrícula aberto', style: 'dashed' },
      { from: 'enroll', to: 'started', label: 'PUT /classes/{id}/start' },
      { from: 'review', to: 'started', label: 'PUT .../start', style: 'dashed' },
      { from: 'started', to: 'finalized', label: 'ninguém grava', style: 'dotted' },
      { from: 'started', to: 'lessons' },
      { from: 'started', to: 'frozen' },
    ],
  },
  {
    id: 'student',
    title: 'Ciclo do aluno dentro da turma',
    description: 'O StudentClassStatus tem seis valores, mas o código só grava um. Os quatro status de fechamento dependem de uma finalização de turma que ainda não existe.',
    width: 1010,
    height: 340,
    nodes: [
      { id: 'assign', label: 'POST /students/{id}/classes', sub: 'AssignStudentToClass', group: 'action', x: 16, y: 140, w: 210, note: 'Barra apenas turma finalizada (ClassAlreadyFinalized), matrícula duplicada e ausência de vagas — a contagem usa todas as linhas de class_students, sem filtrar por status. Como Finalized nunca é gravado, na prática dá para matricular aluno em qualquer turma, inclusive numa já iniciada e com as aulas geradas.' },
      { id: 'pendente', label: 'Pendente', sub: 'Pendente · 0', group: 'missing', x: 276, y: 30, planned: true, note: 'Valor zero do enum, nunca usado: o construtor de ClassStudent já entra como Matriculado. Não há fluxo de solicitação de matrícula pendente de aprovação.' },
      { id: 'matriculado', label: 'Matriculado', sub: 'Matriculado · 1', group: 'status', x: 276, y: 140, note: 'Único valor que o código escreve, direto no construtor de ClassStudent. É também o filtro usado em toda leitura pesada: agenda do aluno e do responsável, criação de atividade, chamada e as consultas de ocupação de campus só enxergam matriculados.' },
      { id: 'fechamento', label: 'Fechamento da turma', sub: 'não implementado', group: 'missing', x: 536, y: 140, w: 200, planned: true, note: 'A peça que falta. Seria ela a aplicar o ClassGradeRule da instituição (média de duas, média de três, melhores duas de três, ou média/terceira) sobre as notas das atividades, comparar com o NoteLimit e o FrequencyLimit do InstitutionConfig e decidir o status final de cada aluno — e só então marcar a turma como Finalized.' },
      { id: 'aprovado', label: 'Aprovado', sub: 'Aprovado · 2', group: 'missing', x: 796, y: 20, planned: true, note: 'Mapeado em GetStudentCourseDetails para StudentDisciplineStatus.Aprovada no histórico do aluno. Como nunca é gravado, toda disciplina cursada aparece como Cursando.' },
      { id: 'reprovadoNota', label: 'Reprovado por nota', sub: 'ReprovadoPorNota · 4', group: 'missing', x: 796, y: 100, planned: true, note: 'Dependeria da média final calculada pelo ClassGradeRule ficar abaixo do NoteLimit da instituição (padrão 7).' },
      { id: 'reprovadoFalta', label: 'Reprovado por falta', sub: 'ReprovadoPorFalta · 5', group: 'missing', x: 796, y: 180, planned: true, note: 'Dependeria da frequência ficar abaixo do FrequencyLimit da instituição (padrão 70%). A frequência já é calculada na leitura em GetClass, a partir das presenças e faltas registradas nas chamadas.' },
      { id: 'dispensado', label: 'Dispensado', sub: 'Dispensado · 3', group: 'missing', x: 796, y: 260, planned: true, note: 'Aproveitamento de estudos — não há endpoint que dispense um aluno de uma turma.' },
    ],
    edges: [
      { from: 'assign', to: 'matriculado', label: 'status inicial' },
      { from: 'assign', to: 'pendente', label: 'nunca', style: 'dotted' },
      { from: 'matriculado', to: 'fechamento', style: 'dotted' },
      { from: 'fechamento', to: 'aprovado', style: 'dashed' },
      { from: 'fechamento', to: 'reprovadoNota', style: 'dashed' },
      { from: 'fechamento', to: 'reprovadoFalta', style: 'dashed' },
      { from: 'fechamento', to: 'dispensado', style: 'dashed' },
    ],
  },
  {
    id: 'execution',
    title: 'O que acontece com a turma iniciada',
    description: 'Depois do start, a turma vira rotina do professor: aulas com chamada e atividades com nota. São esses dois eixos que alimentam a média e a frequência mostradas no detalhe da turma.',
    width: 1000,
    height: 330,
    nodes: [
      { id: 'started', label: 'Turma iniciada', sub: 'Started · 3', group: 'status', x: 16, y: 140, note: 'A partir daqui a turma aparece em GetTeacherCurrentClasses, GetStudentCurrentClasses e nas agendas — todas filtram por ClassStatus.Started.' },
      { id: 'lesson', label: 'Aula', sub: 'ClassLesson · Pending', group: 'effect', x: 256, y: 40, note: 'Criada em lote no start, com número sequencial, data e faixa de horário. Nasce Pending.' },
      { id: 'lessonDone', label: 'Aula concluída', sub: 'ClassLesson · Finalized', group: 'effect', x: 516, y: 40, note: 'CreateLessonAttendance grava a presença de cada aluno matriculado e chama lesson.Finish(). Exige que o professor esteja atribuído à turma e que a aula já tenha acontecido (ClassLessonNotStarted se a data é futura). Rodar de novo atualiza as presenças — não há como reabrir para Pending.' },
      { id: 'activity', label: 'Atividade', sub: 'ClassActivity', group: 'effect', x: 256, y: 220, note: 'CreateClassActivity, feita pelo professor, com tipo de nota (N1, N2, N3), peso de 0 a 100 e prazo. O tipo de nota precisa estar entre os usados pelo ClassGradeRule da instituição. O campo Status (Pending, Published, Finalized) existe na entidade mas nunca é atribuído por nenhum serviço — fica sempre no valor zero.' },
      { id: 'work', label: 'Entrega', sub: 'ClassActivityWork', group: 'effect', x: 516, y: 220, note: 'Uma entrega é criada por aluno matriculado no momento em que a atividade é criada. Pending, vira Delivered quando o aluno anexa o link e Finalized quando o professor lança a nota (AddActivityNote, 0 a 10).' },
      { id: 'grade', label: 'Média e frequência', sub: 'ClassGradeRule', group: 'effect', x: 776, y: 140, note: 'Calculadas na leitura, não gravadas: GetClass aplica o ClassGradeRule do InstitutionConfig sobre as notas por tipo e deriva a frequência das presenças. As cores no detalhe da turma comparam com NoteLimit e FrequencyLimit.' },
    ],
    edges: [
      { from: 'started', to: 'lesson', label: 'geradas no start' },
      { from: 'lesson', to: 'lessonDone', label: 'chamada' },
      { from: 'started', to: 'activity', label: 'professor cria' },
      { from: 'activity', to: 'work', label: '1 por aluno' },
      { from: 'work', to: 'grade', label: 'nota' },
      { from: 'lessonDone', to: 'grade', label: 'presenças' },
    ],
  },
]

interface TransitionRow {
  from: string
  to: string
  endpoint: string
  policy: string
  conditions: string
  errors: string
}

const transitions: TransitionRow[] = [
  {
    from: 'OnPreEnrollment',
    to: 'OnEnrollment',
    endpoint: 'PUT /classes/{classId}/release-for-enrollment',
    policy: 'ReleaseClassForEnrollment',
    conditions: 'Turma da instituição do usuário e status exatamente OnPreEnrollment.',
    errors: 'ClassNotFound · ClassMustBeOnPreEnrollment',
  },
  {
    from: 'OnEnrollment / OnReview',
    to: 'Started',
    endpoint: 'PUT /classes/{classId}/start',
    policy: 'StartClass',
    conditions: 'Status OnEnrollment no banco, ao menos um professor e ao menos um horário. Gera as aulas e soma a carga horária.',
    errors: 'ClassNotFound · ClassMustBeOnEnrollment · ClassWithoutTeachers · ClassWithoutSchedules',
  },
  {
    from: 'Started',
    to: 'Finalized',
    endpoint: '—',
    policy: '—',
    conditions: 'Nenhum endpoint implementa essa transição.',
    errors: '—',
  },
]

interface MatrixRow {
  operation: string
  source: string
  pre: string
  enroll: string
  review: string
  started: string
  finalized: string
}

const matrix: MatrixRow[] = [
  { operation: 'Editar professores', source: 'UpdateClassTeachers', pre: 'sim', enroll: 'sim', review: 'sim', started: 'sim', finalized: 'sim' },
  { operation: 'Editar horários', source: 'UpdateClassSchedules', pre: 'sim', enroll: 'sim', review: 'sim', started: 'não', finalized: 'não' },
  { operation: 'Matricular aluno', source: 'AssignStudentToClass', pre: 'sim', enroll: 'sim', review: 'sim', started: 'sim', finalized: 'não' },
  { operation: 'Liberar para matrícula', source: 'ReleaseClassForEnrollment', pre: 'sim', enroll: 'não', review: 'não', started: 'não', finalized: 'não' },
  { operation: 'Iniciar turma', source: 'StartClass', pre: 'não', enroll: 'sim', review: 'sim', started: 'não', finalized: 'não' },
  { operation: 'Aparecer na agenda / turmas atuais', source: 'GetStudentAgenda, GetTeacherCurrentClasses', pre: 'não', enroll: 'não', review: 'não', started: 'sim', finalized: 'não' },
  { operation: 'Contar na ocupação do campus', source: 'GetCampi, GetCampusOccupancy', pre: 'sim', enroll: 'sim', review: 'sim', started: 'sim', finalized: 'não' },
  { operation: 'Conflitar horário com outra turma', source: 'UpdateClassSchedules', pre: 'sim', enroll: 'sim', review: 'sim', started: 'sim', finalized: 'não' },
]

const matrixColumns: { key: keyof MatrixRow, label: string }[] = [
  { key: 'pre', label: 'Pré-matrícula' },
  { key: 'enroll', label: 'Matrícula' },
  { key: 'review', label: 'Revisão' },
  { key: 'started', label: 'Iniciada' },
  { key: 'finalized', label: 'Finalizada' },
]

interface GapRow {
  title: string
  where: string
  text: string
}

const gaps: GapRow[] = [
  {
    title: 'Liberar para matrícula não checa o período de matrícula',
    where: 'ReleaseClassForEnrollmentService',
    text: 'O comentário do enum e o remarks do controller dizem que a transição só vale dentro do período de matrícula vigente. O serviço só compara o status — nunca consulta EnrollmentPeriods. Dá para liberar uma turma fora de qualquer período, e ela já nasce aparecendo como "Revisão" nas leituras.',
  },
  {
    title: 'Iniciar turma não checa o fim do período de matrícula',
    where: 'StartClassService',
    text: 'O remarks afirma que o período de matrícula precisa estar encerrado. O serviço exige apenas status, professores e horários. Com o período aberto, a turma pula direto de "Matrícula" para "Iniciada" sem nunca passar pela revisão.',
  },
  {
    title: 'Finalized não tem transição',
    where: 'Back/Features/Classes',
    text: 'Não existe FinalizeClass: nem feature, nem policy, nem endpoint. Nenhuma linha do backend atribui ClassStatus.Finalized. Todo o comportamento de turma finalizada (bloquear matrícula, liberar sala e professor para outras turmas, sair da ocupação do campus) está escrito e testado, mas é inalcançável.',
  },
  {
    title: 'Nenhum status de aluno além de Matriculado é gravado',
    where: 'ClassStudent',
    text: 'Aprovado, Reprovado por nota, Reprovado por falta e Dispensado só aparecem em leitura e no mapeamento do histórico. Sem o fechamento da turma, o histórico do aluno mostra toda disciplina como "Cursando".',
  },
  {
    title: 'Professores continuam editáveis depois do start',
    where: 'UpdateClassTeachersService',
    text: 'Diferente dos horários, a troca de professores não tem guarda de status. Remover um professor de uma turma iniciada zera o TeacherId dos horários dele, e as aulas já geradas continuam existindo sem responsável.',
  },
  {
    title: 'A regra do status virtual está copiada em cinco serviços',
    where: 'GetClass, GetClasses, GetDisciplineDetails, GetStudentDetails, GetTeacherDetails',
    text: 'Cada um refaz a consulta de período vigente e a troca de OnEnrollment por OnReview. GetClasses ainda carrega a tradução do filtro. Qualquer mudança na regra precisa ser replicada nos cinco — e as leituras de turma que não fazem isso (GetTeacherClass, GetStudentClass) devolvem OnEnrollment cru.',
  },
  {
    title: 'ClassActivityStatus nunca é atribuído',
    where: 'ClassActivity',
    text: 'A entidade tem Status com Pending, Published e Finalized, e os exemplos da documentação mostram Published, mas nenhum serviço escreve nesse campo — toda atividade fica no valor zero.',
  },
]

const nodeIndex = new Map<string, DiagramNode>()
const neighbors = new Map<string, Set<string>>()

for (const diagram of diagrams) {
  for (const node of diagram.nodes) nodeIndex.set(`${diagram.id}:${node.id}`, node)
  for (const edge of diagram.edges) {
    const from = `${diagram.id}:${edge.from}`
    const to = `${diagram.id}:${edge.to}`
    if (!neighbors.has(from)) neighbors.set(from, new Set())
    if (!neighbors.has(to)) neighbors.set(to, new Set())
    neighbors.get(from)!.add(to)
    neighbors.get(to)!.add(from)
  }
}

const selected = ref<string | null>(null)
const selectedNode = computed(() => selected.value ? nodeIndex.get(selected.value) ?? null : null)

function key(diagram: Diagram, id: string): string {
  return `${diagram.id}:${id}`
}

function widthOf(node: DiagramNode): number {
  return node.w ?? NODE_W
}

function isActive(diagram: Diagram, id: string): boolean {
  return selected.value === key(diagram, id)
}

function nodeDimmed(diagram: Diagram, id: string): boolean {
  const sel = selected.value
  if (!sel) return false
  const self = key(diagram, id)
  if (sel === self) return false
  return !(neighbors.get(sel)?.has(self) ?? false)
}

function edgeActive(diagram: Diagram, edge: DiagramEdge): boolean {
  const sel = selected.value
  if (!sel) return false
  return sel === key(diagram, edge.from) || sel === key(diagram, edge.to)
}

function edgeDimmed(diagram: Diagram, edge: DiagramEdge): boolean {
  if (!selected.value) return false
  return !edgeActive(diagram, edge)
}

interface Geometry {
  path: string
  midX: number
  midY: number
}

function geometryOf(diagram: Diagram, edge: DiagramEdge): Geometry {
  const a = nodeIndex.get(key(diagram, edge.from))!
  const b = nodeIndex.get(key(diagram, edge.to))!

  const aw = widthOf(a)
  const bw = widthOf(b)
  const acx = a.x + aw / 2
  const acy = a.y + NODE_H / 2
  const bcx = b.x + bw / 2
  const bcy = b.y + NODE_H / 2

  const dx = bcx - acx
  const dy = bcy - acy

  if (Math.abs(dx) >= Math.abs(dy)) {
    const sx = dx >= 0 ? a.x + aw : a.x
    const tx = dx >= 0 ? b.x : b.x + bw
    const bend = Math.max(30, Math.abs(tx - sx) / 2)
    const dir = dx >= 0 ? 1 : -1
    return {
      path: `M ${sx} ${acy} C ${sx + bend * dir} ${acy}, ${tx - bend * dir} ${bcy}, ${tx} ${bcy}`,
      midX: (sx + tx) / 2,
      midY: (acy + bcy) / 2,
    }
  }

  const sy = dy >= 0 ? a.y + NODE_H : a.y
  const ty = dy >= 0 ? b.y : b.y + NODE_H
  const bend = Math.max(24, Math.abs(ty - sy) / 2)
  const dir = dy >= 0 ? 1 : -1
  return {
    path: `M ${acx} ${sy} C ${acx} ${sy + bend * dir}, ${bcx} ${ty - bend * dir}, ${bcx} ${ty}`,
    midX: (acx + bcx) / 2,
    midY: (sy + ty) / 2,
  }
}

function dashOf(edge: DiagramEdge): string | undefined {
  if (edge.style === 'dashed') return '6 4'
  if (edge.style === 'dotted') return '2 4'
  return undefined
}

function groupsOf(diagram: Diagram): GroupId[] {
  const found = new Set<GroupId>()
  for (const node of diagram.nodes) found.add(node.group)
  return [...found]
}

function selectNode(diagram: Diagram, id: string) {
  const self = key(diagram, id)
  selected.value = selected.value === self ? null : self
}

function clearSelection() {
  selected.value = null
}
</script>

<template>
  <UDashboardPanel id="dev-class-lifecycle">
    <template #header>
      <UDashboardNavbar title="Ciclo de Vida da Turma">
        <template #leading>
          <PageIcon icon="i-lucide-git-branch" />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="lifecycle space-y-8">
        <div class="space-y-2">
          <p class="text-sm text-muted">
            Todo o caminho de uma turma, do <code class="text-xs">POST /classes</code> até onde o código realmente para.
            São cinco status no enum <code class="text-xs">ClassStatus</code>, mas só três chegam a ser gravados no banco:
            <span class="italic">Revisão</span> é calculado em memória a cada leitura e <span class="italic">Finalizada</span>
            nunca é escrito por nenhum serviço.
          </p>
          <p class="text-sm text-muted">
            Clique numa caixa para destacar suas conexões e ver os detalhes; clique de novo (ou fora dela) para limpar.
          </p>
        </div>

        <UCard :ui="{ body: 'p-3 sm:p-4' }">
          <div v-if="selectedNode" class="space-y-1.5">
            <div class="flex items-center gap-2 flex-wrap">
              <span class="size-2.5 rounded-full shrink-0" :style="{ backgroundColor: `var(--g-${selectedNode.group})` }" />
              <span class="font-semibold text-highlighted">{{ selectedNode.label }}</span>
              <code v-if="selectedNode.sub" class="text-xs text-muted">{{ selectedNode.sub }}</code>
              <UBadge variant="subtle" color="neutral" size="sm">{{ groups[selectedNode.group] }}</UBadge>
              <UBadge v-if="selectedNode.planned" variant="subtle" color="warning" size="sm">não existe no código</UBadge>
            </div>
            <p class="text-sm text-toned">{{ selectedNode.note ?? 'Sem observações.' }}</p>
          </div>
          <p v-else class="text-sm text-muted">
            Nenhuma etapa selecionada. Clique numa caixa dos diagramas para ver os detalhes.
          </p>
        </UCard>

        <section v-for="d in diagrams" :key="d.id" class="space-y-3">
          <div class="space-y-1">
            <h2 class="text-base font-semibold text-highlighted">{{ d.title }}</h2>
            <p class="text-sm text-muted">{{ d.description }}</p>
          </div>

          <div class="flex items-center gap-x-5 gap-y-2 flex-wrap text-xs">
            <div v-for="g in groupsOf(d)" :key="g" class="flex items-center gap-1.5">
              <span class="size-2.5 rounded-full shrink-0" :style="{ backgroundColor: `var(--g-${g})` }" />
              <span class="text-toned">{{ groups[g] }}</span>
            </div>
          </div>

          <div class="overflow-x-auto rounded-lg border border-default bg-default">
            <svg
              :width="d.width"
              :height="d.height"
              :viewBox="`0 0 ${d.width} ${d.height}`"
              role="img"
              :aria-label="d.title"
              @click="clearSelection"
            >
              <defs>
                <marker :id="`arrow-${d.id}`" viewBox="0 0 8 8" refX="7" refY="4" markerWidth="7" markerHeight="7" orient="auto-start-reverse">
                  <path d="M 0 1 L 8 4 L 0 7 z" class="arrow-head" />
                </marker>
                <marker :id="`arrow-hl-${d.id}`" viewBox="0 0 8 8" refX="7" refY="4" markerWidth="7" markerHeight="7" orient="auto-start-reverse">
                  <path d="M 0 1 L 8 4 L 0 7 z" class="arrow-head-hl" />
                </marker>
              </defs>

              <g v-for="(f, i) in d.frames ?? []" :key="`frame-${i}`">
                <rect :x="f.x" :y="f.y" :width="f.w" :height="f.h" rx="12" class="frame-box" />
                <text :x="f.x + 12" :y="f.y + 18" font-size="11" class="frame-label">{{ f.label }}</text>
              </g>

              <g v-for="(e, i) in d.edges" :key="`edge-${i}`">
                <path
                  :d="geometryOf(d, e).path"
                  fill="none"
                  class="edge"
                  :class="{ 'edge-hl': edgeActive(d, e), 'edge-dim': edgeDimmed(d, e) }"
                  :stroke-dasharray="dashOf(e)"
                  :marker-end="edgeActive(d, e) ? `url(#arrow-hl-${d.id})` : `url(#arrow-${d.id})`"
                />
                <text
                  v-if="e.label"
                  :x="geometryOf(d, e).midX"
                  :y="geometryOf(d, e).midY - 6"
                  font-size="10"
                  text-anchor="middle"
                  class="edge-label"
                  :class="{ 'edge-dim': edgeDimmed(d, e) }"
                >{{ e.label }}</text>
              </g>

              <g
                v-for="n in d.nodes"
                :key="n.id"
                class="node cursor-pointer"
                :class="{ 'node-dim': nodeDimmed(d, n.id), 'node-selected': isActive(d, n.id) }"
                :style="{ '--node-color': `var(--g-${n.group})` }"
                :transform="`translate(${n.x}, ${n.y})`"
                @click="(e) => { e.stopPropagation(); selectNode(d, n.id) }"
              >
                <title>{{ n.note ?? n.label }}</title>
                <rect
                  :width="widthOf(n)"
                  :height="NODE_H"
                  rx="8"
                  class="node-box"
                  :stroke-dasharray="n.planned ? '5 4' : undefined"
                />
                <rect x="0" y="10" width="3" :height="NODE_H - 20" rx="1.5" fill="var(--node-color)" />
                <text x="14" y="22" font-size="12.5" font-weight="600" class="node-label">{{ n.label }}</text>
                <text v-if="n.sub" x="14" y="39" font-size="10" class="node-sub">{{ n.sub }}</text>
              </g>
            </svg>
          </div>
        </section>

        <section class="space-y-3">
          <div class="space-y-1">
            <h2 class="text-base font-semibold text-highlighted">Transições</h2>
            <p class="text-sm text-muted">Cada mudança de status persistida, com o endpoint que a dispara e o que ela exige.</p>
          </div>
          <div class="overflow-x-auto rounded-lg border border-default">
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-default bg-elevated/50 text-left text-xs text-muted">
                  <th class="px-3 py-2 font-medium">De</th>
                  <th class="px-3 py-2 font-medium">Para</th>
                  <th class="px-3 py-2 font-medium">Endpoint</th>
                  <th class="px-3 py-2 font-medium">Policy</th>
                  <th class="px-3 py-2 font-medium">Pré-condições</th>
                  <th class="px-3 py-2 font-medium">Erros</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="row in transitions" :key="row.endpoint" class="border-b border-default last:border-b-0">
                  <td class="px-3 py-2 text-muted whitespace-nowrap">{{ row.from }}</td>
                  <td class="px-3 py-2 text-highlighted font-medium whitespace-nowrap">{{ row.to }}</td>
                  <td class="px-3 py-2 text-toned font-mono text-xs">{{ row.endpoint }}</td>
                  <td class="px-3 py-2 text-toned font-mono text-xs whitespace-nowrap">{{ row.policy }}</td>
                  <td class="px-3 py-2 text-muted">{{ row.conditions }}</td>
                  <td class="px-3 py-2 text-muted font-mono text-xs">{{ row.errors }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        <section class="space-y-3">
          <div class="space-y-1">
            <h2 class="text-base font-semibold text-highlighted">O que cada status permite</h2>
            <p class="text-sm text-muted">
              Como Revisão é o mesmo <code class="text-xs">OnEnrollment</code> no banco, as duas colunas se comportam igual —
              a diferença é só o que a API devolve na leitura.
            </p>
          </div>
          <div class="overflow-x-auto rounded-lg border border-default">
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-default bg-elevated/50 text-left text-xs text-muted">
                  <th class="px-3 py-2 font-medium">Operação</th>
                  <th class="px-3 py-2 font-medium">Onde</th>
                  <th v-for="col in matrixColumns" :key="col.key" class="px-3 py-2 font-medium text-center whitespace-nowrap">
                    {{ col.label }}
                  </th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="row in matrix" :key="row.operation" class="border-b border-default last:border-b-0">
                  <td class="px-3 py-2 text-highlighted font-medium">{{ row.operation }}</td>
                  <td class="px-3 py-2 text-muted font-mono text-xs">{{ row.source }}</td>
                  <td v-for="col in matrixColumns" :key="col.key" class="px-3 py-2 text-center">
                    <UIcon
                      :name="row[col.key] === 'sim' ? 'i-lucide-check' : 'i-lucide-x'"
                      class="size-4"
                      :class="row[col.key] === 'sim' ? 'text-success' : 'text-error'"
                    />
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        <section class="space-y-3">
          <div class="space-y-1">
            <h2 class="text-base font-semibold text-highlighted">Onde o código diverge da documentação</h2>
            <p class="text-sm text-muted">
              Pontos em que o comentário do enum, o <code class="text-xs">remarks</code> do controller ou a UI prometem
              uma regra que o serviço não aplica — ou em que o status existe mas ninguém o alcança.
            </p>
          </div>
          <div class="grid gap-3 md:grid-cols-2">
            <UCard v-for="gap in gaps" :key="gap.title" :ui="{ body: 'p-3 sm:p-4' }">
              <div class="space-y-1.5">
                <div class="flex items-start gap-2">
                  <UIcon name="i-lucide-triangle-alert" class="size-4 text-warning shrink-0 mt-0.5" />
                  <span class="text-sm font-semibold text-highlighted">{{ gap.title }}</span>
                </div>
                <code class="text-xs text-muted block">{{ gap.where }}</code>
                <p class="text-sm text-toned">{{ gap.text }}</p>
              </div>
            </UCard>
          </div>
        </section>
      </div>
    </template>
  </UDashboardPanel>
</template>

<style scoped>
.lifecycle {
  --g-action: #2a78d6;
  --g-status: #1baf7a;
  --g-virtual: #eda100;
  --g-effect: #8b5cf6;
  --g-missing: #94a3b8;
  --edge-color: var(--ui-border-accented);
}

.dark .lifecycle {
  --g-action: #3987e5;
  --g-status: #199e70;
  --g-virtual: #c98500;
  --g-effect: #a78bfa;
  --g-missing: #64748b;
}

.edge {
  stroke: var(--edge-color);
  stroke-width: 1.5;
  transition: opacity 0.15s ease, stroke 0.15s ease;
}

.edge-hl {
  stroke: var(--ui-primary);
  stroke-width: 2;
}

.edge-dim {
  opacity: 0.12;
}

.edge-label {
  fill: var(--ui-text-muted);
  stroke: var(--ui-bg);
  stroke-width: 3px;
  paint-order: stroke;
  transition: opacity 0.15s ease;
}

.arrow-head {
  fill: var(--edge-color);
}

.arrow-head-hl {
  fill: var(--ui-primary);
}

.frame-box {
  fill: none;
  stroke: var(--ui-border);
  stroke-width: 1;
  stroke-dasharray: 4 5;
}

.frame-label {
  fill: var(--ui-text-dimmed);
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.node {
  transition: opacity 0.15s ease;
}

.node-dim {
  opacity: 0.2;
}

.node-box {
  fill: var(--ui-bg-elevated);
  stroke: var(--ui-border-accented);
  stroke-width: 1;
  transition: stroke 0.15s ease;
}

.node-selected .node-box {
  stroke: var(--node-color);
  stroke-width: 1.5;
}

.node-label {
  fill: var(--ui-text-highlighted);
}

.node-sub {
  fill: var(--ui-text-muted);
  font-family: var(--font-mono, monospace);
}
</style>
