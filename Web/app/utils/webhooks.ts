type BadgeColor = 'neutral' | 'primary' | 'success' | 'warning' | 'error' | 'info'

export const webhookEventLabels: Record<string, string> = {
  StudentCreated: 'Aluno criado',
  TeacherCreated: 'Professor criado',
  ClassActivityPublished: 'Atividade publicada',
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

export const webhookBackoffStrategyLabels: Record<string, string> = {
  None: 'Sem backoff',
  Fixed: 'Fixo',
  Linear: 'Linear',
  Exponential: 'Exponencial',
}

export const webhookBackoffStrategyIcons: Record<string, string> = {
  None: 'i-lucide-zap',
  Fixed: 'i-lucide-minus',
  Linear: 'i-lucide-trending-up',
  Exponential: 'i-lucide-chart-spline',
}

export function webhookRetryDelays(maxRetries: number, baseDelaySeconds: number, backoffStrategy: string) {
  return Array.from({ length: maxRetries }, (_, i) => {
    const attempt = i + 1
    switch (backoffStrategy) {
      case 'Exponential': return baseDelaySeconds * 2 ** (attempt - 1)
      case 'Linear': return baseDelaySeconds * attempt
      case 'Fixed': return baseDelaySeconds
      default: return 0
    }
  })
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
