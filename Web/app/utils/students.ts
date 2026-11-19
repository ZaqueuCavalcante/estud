type BadgeColor = 'neutral' | 'primary' | 'success' | 'warning' | 'error' | 'info'

export const studentStatusLabels: Record<string, string> = {
  Enrolled: 'Matriculado',
  Transferred: 'Transferido',
  Deferred: 'Trancado',
  Completed: 'Concluído',
}

export const studentStatusColors: Record<string, BadgeColor> = {
  Enrolled: 'info',
  Transferred: 'warning',
  Deferred: 'neutral',
  Completed: 'success',
}

export const courseSessionLabels: Record<string, string> = {
  Morning: 'Matutino',
  Afternoon: 'Vespertino',
  Evening: 'Noturno',
}

export const studentDisciplineStatusLabels: Record<string, string> = {
  NaoCursada: 'A cursar',
  Cursando: 'Cursando',
  Aprovada: 'Aprovada',
  Dispensada: 'Dispensada',
  Reprovada: 'Reprovada',
}

export const studentDisciplineStatusColors: Record<string, BadgeColor> = {
  NaoCursada: 'neutral',
  Cursando: 'info',
  Aprovada: 'success',
  Dispensada: 'warning',
  Reprovada: 'error',
}

// Cor do "ponto" de status na grade curricular (classes estáticas p/ o Tailwind não purgar)
export const studentDisciplineStatusDot: Record<string, string> = {
  NaoCursada: 'bg-accented',
  Cursando: 'bg-info',
  Aprovada: 'bg-success',
  Dispensada: 'bg-warning',
  Reprovada: 'bg-error',
}
