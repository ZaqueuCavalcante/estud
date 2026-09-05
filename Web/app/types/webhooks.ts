export interface WebhookCallItem {
  id: number
  eventType: string
  status: string
  attemptsCount: number
  createdAt: string
}

export interface GetWebhookCallsOut {
  total: number
  page: number
  pageSize: number
  items: WebhookCallItem[]
}

export interface WebhookCallRequest {
  method: string
  url: string
  headers: Record<string, string>
  body: string
}

export interface WebhookCallSubscription {
  id: number
  name: string
  url: string
  isActive: boolean
}

export interface WebhookCallAttempt {
  id: number
  status: string
  statusCode: number
  response: string
  durationMs: number
  createdAt: string
}

export interface GetWebhookCallOut {
  id: number
  eventType: string
  status: string
  attemptsCount: number
  createdAt: string
  payload: string
  request: WebhookCallRequest
  subscription: WebhookCallSubscription
  attempts: WebhookCallAttempt[]
}
