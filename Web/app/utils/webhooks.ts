type BadgeColor = 'neutral' | 'primary' | 'success' | 'warning' | 'error' | 'info'

export const webhookEventLabels: Record<string, string> = {
  StudentCreated: 'Aluno criado',
  ClassActivityCreated: 'Atividade publicada',
}

export const webhookCallStatusLabels: Record<string, string> = {
  Pending: 'Pendente',
  Processing: 'Processando',
  Success: 'Sucesso',
  Error: 'Erro',
}

export const webhookCallStatusColors: Record<string, BadgeColor> = {
  Pending: 'neutral',
  Processing: 'info',
  Success: 'success',
  Error: 'error',
}

export const webhookCallAttemptStatusLabels: Record<string, string> = {
  Success: 'Sucesso',
  Error: 'Erro',
}

export const webhookCallAttemptStatusColors: Record<string, BadgeColor> = {
  Success: 'success',
  Error: 'error',
}

export function formatWebhookJson(value: string) {
  if (!value) return ''
  try {
    return JSON.stringify(JSON.parse(value), null, 2)
  } catch {
    return value
  }
}

export function formatWebhookDuration(durationMs: number) {
  if (durationMs < 1000) return `${durationMs} ms`
  return `${(durationMs / 1000).toFixed(2)} s`
}
