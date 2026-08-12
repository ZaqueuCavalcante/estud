<script setup lang="ts">
type GroupId = 'client' | 'edge' | 'app' | 'data' | 'external' | 'pipeline' | 'planned'

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
  client: 'Cliente',
  edge: 'Borda & proxy',
  app: 'Aplicação',
  data: 'Dados',
  external: 'Serviços externos',
  pipeline: 'CI/CD & qualidade',
  planned: 'Ainda não existe',
}

const NODE_H = 54
const NODE_W = 160

const diagrams: Diagram[] = [
  {
    id: 'infra',
    title: 'Topologia & infraestrutura',
    description: 'Como uma requisição sai do navegador e chega no banco, onde ela passa por rate limiting e cache, e com quais serviços externos o backend fala.',
    width: 1240,
    height: 520,
    frames: [
      { label: 'Railway', x: 380, y: 24, w: 610, h: 470 },
      { label: 'Serviços externos', x: 1020, y: 24, w: 205, h: 470 },
    ],
    nodes: [
      { id: 'browser', label: 'Navegador', sub: 'Usuário final', group: 'client', x: 16, y: 233, note: 'SPA/SSR do Nuxt. A sessão é um cookie httpOnly, então o navegador nunca guarda o JWT em storage.' },
      { id: 'cloudflare', label: 'Cloudflare', sub: 'DNS · CDN · proxy', group: 'edge', x: 200, y: 233, note: 'Fica na frente do domínio estud.com.br. Termina TLS e repassa para a Railway.' },
      { id: 'caddy', label: 'Caddy', sub: 'Reverse proxy', group: 'edge', x: 400, y: 233, note: 'Serviço próprio na Railway (Dockerfile.caddy). Roteia /api/* para o backend e todo o resto para o Nuxt. Fixa o X-Forwarded-Host para o redirect_uri do OAuth sair como estud.com.br.' },
      { id: 'web', label: 'Nuxt (Web)', sub: 'Vue 3 · SSR · Nuxt UI', group: 'app', x: 600, y: 100, note: 'Node 26, roda o build .output/server. Usa internalBackendUrl na rede privada da Railway para o SSR.' },
      { id: 'ratelimit', label: 'Rate limiting', sub: 'Fixed window · 429', group: 'edge', x: 600, y: 233, note: 'Middleware da própria API (UseRateLimiter), logo depois da autenticação. Limite global particionado por usuário logado ou por IP, mais a política SensitivePolicy (só por IP) nos endpoints de login, registro e reset de senha. Ao estourar responde 429 com o erro TooManyRequests e header Retry-After. O estado é em memória, por instância.' },
      { id: 'back', label: 'API .NET 10', sub: 'ASP.NET Core :5000', group: 'app', x: 790, y: 233, note: 'Vertical slice architecture, Result Pattern (OneOf), Quartz para jobs e Data Protection com chaves no banco.' },
      { id: 'cache', label: 'HybridCache', sub: 'Em memória · 30 min', group: 'app', x: 790, y: 100, note: 'Exposto como ctx.Cache no EstudDbContext. Expiração padrão de 30 min, chave de até 512 caracteres e payload de até 10 MB. Hoje as chaves em uso são a resolução do papel do usuário do request (GetStudentId, GetTeacherId, GetParentId). Só existe o nível local (L1) — não há IDistributedCache registrado, então cada instância tem seu próprio cache.' },
      { id: 'postgres', label: 'PostgreSQL', sub: 'EF Core + Dapper', group: 'data', x: 790, y: 390, note: 'Schema estud, naming snake_case. Guarda também commands, domain events, auditoria e chaves do Data Protection.' },
      { id: 'google', label: 'Google Identity', sub: 'OAuth 2.0 · One Tap', group: 'external', x: 1035, y: 50, w: 175, note: 'Social login (fluxo OAuth com cookie temporário) e One Tap (validação do id_token).' },
      { id: 'oidc', label: 'Provedor OIDC', sub: 'SSO da instituição', group: 'external', x: 1035, y: 134, w: 175, note: 'Cada instituição configura seu próprio provedor (SsoConfiguration + domínios permitidos).' },
      { id: 'brevo', label: 'Brevo', sub: 'Envio de e-mails', group: 'external', x: 1035, y: 218, w: 175, note: 'Em dev e nos testes é trocado pelo FakeEmailsService.' },
      { id: 'otlp', label: 'Collector OTLP', sub: 'Traces e métricas', group: 'external', x: 1035, y: 302, w: 175, note: 'OTEL_EXPORTER_OTLP_ENDPOINT. Hoje só é ligado quando OpenTelemetry:Enabled = true.' },
      { id: 'blob', label: 'Azure Blob Storage', sub: 'Uploads', group: 'planned', x: 1035, y: 386, w: 175, planned: true, note: 'O pacote está no csproj e existe IStorageService, mas o registro atual é o FakeStorageService — não há upload real ainda.' },
    ],
    edges: [
      { from: 'browser', to: 'cloudflare', label: 'https' },
      { from: 'cloudflare', to: 'caddy', label: 'proxy' },
      { from: 'caddy', to: 'web', label: '/ e /api/_*' },
      { from: 'caddy', to: 'ratelimit', label: '/api/*' },
      { from: 'ratelimit', to: 'back', label: 'passou' },
      { from: 'web', to: 'back', label: 'SSR (rede interna)', style: 'dashed' },
      { from: 'back', to: 'cache', label: 'leitura quente' },
      { from: 'back', to: 'postgres', label: 'EF Core · Dapper' },
      { from: 'back', to: 'google' },
      { from: 'back', to: 'oidc' },
      { from: 'back', to: 'brevo' },
      { from: 'back', to: 'otlp' },
      { from: 'back', to: 'blob', style: 'dotted' },
    ],
  },
  {
    id: 'auth',
    title: 'Autenticação & autorização',
    description: 'Todos os caminhos de login convergem para o SignIn, que emite o JWT em cookie httpOnly. A autorização é sempre por policy + permissão. Os endpoints de entrada ficam sob a política de rate limit mais apertada.',
    width: 1045,
    height: 450,
    frames: [
      { label: 'Rate limit sensível (por IP)', x: 8, y: 14, w: 216, h: 414 },
    ],
    nodes: [
      { id: 'pwd', label: 'E-mail + senha', sub: 'EmailPasswordLogin', group: 'client', x: 16, y: 40, w: 200, note: 'Login padrão. Também alimenta o fluxo de recuperação de senha (SendResetPasswordToken / ResetPassword).' },
      { id: 'magic', label: 'Magic Link', sub: 'MagicLinkLogin', group: 'client', x: 16, y: 120, w: 200, note: 'Link de uso único enviado por e-mail.' },
      { id: 'onetap', label: 'Google One Tap', sub: 'GoogleOneTapLogin', group: 'client', x: 16, y: 200, w: 200, note: 'O front recebe o id_token do Google e o backend valida via Google.Apis.Auth.' },
      { id: 'social', label: 'Google Social Login', sub: 'SocialLoginChallenge', group: 'client', x: 16, y: 280, w: 200, note: 'Fluxo OAuth completo, com callback em /identity/social-login/callback/google.' },
      { id: 'sso', label: 'SSO (OIDC)', sub: 'SsoOidcScheme', group: 'client', x: 16, y: 360, w: 200, note: 'Por instituição. A instituição pode exigir SSO obrigatório (RequireSso) e restringir domínios de e-mail.' },
      { id: '2fa', label: 'TwoFactorLogin', sub: 'TOTP · QR Code', group: 'app', x: 286, y: 40, w: 200, note: 'Otp.NET + QRCoder. O segundo fator roda num scheme próprio antes de emitir o JWT definitivo. Também está sob a SensitivePolicy de rate limit.' },
      { id: 'setup2fa', label: 'TwoFactorSetupScheme', sub: '1º acesso com 2FA exigido', group: 'app', x: 286, y: 120, w: 200, note: 'Quando a instituição exige 2FA e o usuário ainda não configurou, ele cai num scheme temporário só para o setup.' },
      { id: 'socialTemp', label: 'SocialTempScheme', sub: 'Cookie temporário', group: 'app', x: 286, y: 280, w: 200, note: 'Guarda a identidade do provedor entre o callback e a emissão do JWT.' },
      { id: 'ssoTemp', label: 'SsoTempScheme', sub: 'Cookie temporário', group: 'app', x: 286, y: 360, w: 200, note: 'Mesmo papel do SocialTemp, para o fluxo de SSO.' },
      { id: 'signin', label: 'SignIn', sub: 'Emite o JWT', group: 'app', x: 556, y: 200, w: 200, note: 'Ponto único de emissão de sessão: resolve usuário, instituição e perfis.' },
      { id: 'jwt', label: 'Cookie JWT httpOnly', sub: 'JwtBearerScheme', group: 'app', x: 556, y: 300, w: 200, note: 'Scheme padrão de challenge. O token nunca é exposto ao JavaScript.' },
      { id: 'policies', label: 'Policies', sub: 'Authorize(Policies.X)', group: 'data', x: 826, y: 200, w: 200, note: 'Uma policy por feature, sempre com o mesmo nome da feature.' },
      { id: 'perms', label: 'Perfis & permissões', sub: 'EstudRole · Permissions', group: 'data', x: 826, y: 300, w: 200, note: 'Permissões agrupadas (PermissionGroup) e perfis por instituição, com papéis padrão (EstudDefaultRoles).' },
    ],
    edges: [
      { from: 'pwd', to: '2fa', label: 'se 2FA ativo' },
      { from: 'pwd', to: 'setup2fa', label: '2FA exigido', style: 'dashed' },
      { from: 'pwd', to: 'signin' },
      { from: 'magic', to: 'signin' },
      { from: 'onetap', to: 'signin' },
      { from: 'social', to: 'socialTemp' },
      { from: 'sso', to: 'ssoTemp' },
      { from: 'socialTemp', to: 'signin' },
      { from: 'ssoTemp', to: 'signin' },
      { from: '2fa', to: 'signin' },
      { from: 'setup2fa', to: 'signin' },
      { from: 'signin', to: 'jwt' },
      { from: 'jwt', to: 'policies' },
      { from: 'policies', to: 'perms' },
    ],
  },
  {
    id: 'async',
    title: 'Processamento assíncrono',
    description: 'Tudo que não precisa acontecer dentro do request vira linha no banco e é processado por um job do Quartz.',
    width: 1100,
    height: 460,
    nodes: [
      { id: 'service', label: 'Service (feature)', sub: 'ctx.AddCommand(...)', group: 'app', x: 16, y: 180, w: 190, note: 'O service persiste o comando na mesma transação da operação de negócio.' },
      { id: 'commands', label: 'Tabela commands', sub: 'Command / CommandBatch', group: 'data', x: 226, y: 90, w: 190, note: 'Suporta parent/child, lotes, retry com backoff exponencial e execução adiada (NotBefore).' },
      { id: 'events', label: 'Domain events', sub: 'Tabela de eventos', group: 'data', x: 226, y: 270, w: 190, note: 'Eventos de domínio publicados pelo SaveChanges e consumidos fora do request.' },
      { id: 'cmdProc', label: 'CommandsProcessor', sub: 'Quartz · 60s', group: 'app', x: 436, y: 90, w: 190, note: 'Job recorrente que pega os comandos pendentes e despacha para os handlers.' },
      { id: 'evtProc', label: 'DomainEventsProcessor', sub: 'Quartz · 60s', group: 'app', x: 436, y: 270, w: 190, note: 'Mesmo padrão do CommandsProcessor, para eventos de domínio.' },
      { id: 'handlers', label: 'Handlers', sub: 'Retry + backoff', group: 'app', x: 646, y: 180, w: 190, note: 'CommandBackoffStrategies define o intervalo entre tentativas.' },
      { id: 'emails', label: 'E-mails', sub: 'Brevo · templates HTML', group: 'external', x: 890, y: 60, w: 190 },
      { id: 'notif', label: 'Notificações', sub: 'Notification / UserNotification', group: 'data', x: 890, y: 150, w: 190 },
      { id: 'webhooks', label: 'Webhooks de saída', sub: 'Call + Attempt', group: 'external', x: 890, y: 240, w: 190, note: 'WebhookSubscription → WebhookCall → WebhookCallAttempt, com histórico de tentativas visível em /integrations. Detalhado no diagrama seguinte.' },
      { id: 'audit', label: 'AuditTrail / AuditChange', sub: 'Interceptor do SaveChanges', group: 'data', x: 226, y: 375, w: 190, note: 'Auditoria automática de tudo que passa pelo SaveChanges (Audit.EntityFramework).' },
    ],
    edges: [
      { from: 'service', to: 'commands' },
      { from: 'service', to: 'events' },
      { from: 'service', to: 'audit', style: 'dashed' },
      { from: 'commands', to: 'cmdProc', label: 'polling' },
      { from: 'events', to: 'evtProc', label: 'polling' },
      { from: 'cmdProc', to: 'handlers' },
      { from: 'evtProc', to: 'handlers' },
      { from: 'handlers', to: 'emails' },
      { from: 'handlers', to: 'notif' },
      { from: 'handlers', to: 'webhooks' },
    ],
  },
  {
    id: 'webhooks',
    title: 'Webhooks & notificações internas',
    description: 'Três trilhas: webhooks de saída (o sistema avisa terceiros), webhooks de entrada (terceiros avisam o sistema) e as notificações que o próprio usuário vê dentro do produto.',
    width: 1080,
    height: 680,
    frames: [
      { label: 'Webhooks de saída', x: 8, y: 24, w: 1064, h: 250 },
      { label: 'Webhooks de entrada', x: 8, y: 290, w: 1064, h: 110 },
      { label: 'Notificações internas', x: 8, y: 416, w: 1064, h: 230 },
    ],
    nodes: [
      { id: 'event', label: 'Evento acadêmico', sub: 'WebhookEventType', group: 'app', x: 20, y: 90, w: 190, note: 'Cada evento de domínio publicável é marcado com [WebhookEventType] e disparado pelo WebhookEventInvoker.' },
      { id: 'subs', label: 'WebhookSubscription', sub: 'URL · eventos · headers', group: 'data', x: 226, y: 90, w: 190, note: 'Por instituição: URL de destino, lista de eventos assinados, headers customizados e flag de ativo/inativo.' },
      { id: 'call', label: 'WebhookCall', sub: 'Payload JSON', group: 'data', x: 436, y: 90, w: 190, note: 'Um registro por evento × inscrição, com o payload congelado no momento do disparo.' },
      { id: 'cmd', label: 'CallWebhookCommand', sub: 'Quartz · retry', group: 'app', x: 646, y: 90, w: 190, note: 'Handler que faz o POST via IHttpClientFactory. Falha de rede vira status 999 e o comando volta para a fila com backoff.' },
      { id: 'endpoint', label: 'Endpoint do cliente', sub: 'POST application/json', group: 'external', x: 856, y: 50, w: 190, note: 'Sistema de terceiro que recebe o payload, com os headers customizados da inscrição.' },
      { id: 'attempt', label: 'WebhookCallAttempt', sub: 'Status + resposta', group: 'data', x: 856, y: 130, w: 190, note: 'Uma linha por tentativa: status HTTP e corpo da resposta, o que dá o histórico visível na UI.' },
      { id: 'integrations', label: '/integrations', sub: 'Inscrições e chamadas', group: 'client', x: 646, y: 195, w: 190, note: 'Telas de integração: cadastro das inscrições e histórico de chamadas em /integrations/calls.' },

      { id: 'extSys', label: 'Sistema externo', sub: 'Dispara evento', group: 'external', x: 20, y: 320, w: 190 },
      { id: 'recv', label: 'ReceivedWebhookEvent', sub: 'Fila de entrada', group: 'data', x: 226, y: 320, w: 190, note: 'O evento recebido é só persistido no request; o processamento acontece fora dele.' },
      { id: 'recvProc', label: 'Processor de entrada', sub: 'ReceivedWebhookEvents', group: 'app', x: 436, y: 320, w: 190, note: 'ReceivedWebhookEventsProcessor consome a fila em intervalo próprio.' },
      { id: 'effect', label: 'Efeito no domínio', sub: 'Service da feature', group: 'app', x: 646, y: 320, w: 190 },

      { id: 'notif', label: 'Notification', sub: 'CreateNotification', group: 'data', x: 20, y: 480, w: 190, note: 'Notificação institucional: tipo, título, descrição e metadata em JSON.' },
      { id: 'userNotif', label: 'UserNotification', sub: 'Entrega por usuário', group: 'data', x: 226, y: 480, w: 190, note: 'Fan-out da notificação para cada destinatário, com controle de visualização (MarkNotificationsAsViewed).' },
      { id: 'api', label: 'API de notificações', sub: 'GetNotifications', group: 'app', x: 436, y: 480, w: 190, note: 'GetNotifications, GetInstitutionNotifications e GetUnreadNotificationsCount.' },
      { id: 'bell', label: 'Sino no header', sub: 'NotificationsSlideover', group: 'client', x: 646, y: 435, w: 190 },
      { id: 'inbox', label: '/notifications', sub: 'Caixa de entrada', group: 'client', x: 646, y: 520, w: 190 },
      { id: 'unread', label: 'Contador de não lidas', sub: 'GetUnreadNotificationsCount', group: 'client', x: 856, y: 480, w: 200, note: 'Hoje o front descobre notificação nova por polling: useNotifications chama /notifications/unread-count a cada 60s (e uma vez no boot e no login). O badge também vai para o título da aba.' },
      { id: 'realtime', label: 'SSE / WebSocket', sub: 'Push em tempo real', group: 'planned', x: 436, y: 560, w: 190, planned: true, note: 'Não existe hoje: nenhuma conexão persistente entre front e API. O caminho natural é um endpoint SSE por usuário (mais simples que WebSocket, e suficiente porque o fluxo é só servidor → cliente) empurrando a notificação assim que o UserNotification é criado, aposentando o polling de 60s. WebSocket só se aparecer fluxo bidirecional de verdade.' },
    ],
    edges: [
      { from: 'event', to: 'subs', label: 'casa com os eventos assinados' },
      { from: 'subs', to: 'call' },
      { from: 'call', to: 'cmd', label: 'comando' },
      { from: 'cmd', to: 'endpoint', label: 'POST' },
      { from: 'cmd', to: 'attempt', label: 'registra' },
      { from: 'attempt', to: 'integrations', label: 'histórico', style: 'dashed' },
      { from: 'subs', to: 'integrations', label: 'cadastro', style: 'dashed' },
      { from: 'extSys', to: 'recv' },
      { from: 'recv', to: 'recvProc', label: 'polling' },
      { from: 'recvProc', to: 'effect' },
      { from: 'notif', to: 'userNotif', label: 'fan-out' },
      { from: 'userNotif', to: 'api' },
      { from: 'api', to: 'bell' },
      { from: 'api', to: 'inbox' },
      { from: 'api', to: 'unread', label: 'polling 60s' },
      { from: 'userNotif', to: 'realtime', label: 'futuro', style: 'dotted' },
      { from: 'realtime', to: 'bell', style: 'dashed' },
      { from: 'realtime', to: 'unread', label: 'sem polling', style: 'dashed' },
    ],
  },
  {
    id: 'cicd',
    title: 'Testes, CI/CD e deploy',
    description: 'PR roda a suíte inteira contra um Postgres real; o merge em master publica cobertura e dispara o deploy.',
    width: 1060,
    height: 340,
    nodes: [
      { id: 'dev', label: 'Push / Pull Request', sub: 'GitHub', group: 'client', x: 16, y: 130, w: 180 },
      { id: 'pr', label: 'PR Tests', sub: 'pr.tests.yml', group: 'pipeline', x: 226, y: 30, w: 180, note: 'Roda em todo PR para master, com permissão de comentar no próprio PR.' },
      { id: 'ci', label: 'CI/CD', sub: 'ci.cd.yml (master)', group: 'pipeline', x: 226, y: 230, w: 180, note: 'Dispara no push em master.' },
      { id: 'tests', label: 'Build + testes', sub: 'NUnit · Postgres service', group: 'pipeline', x: 436, y: 130, w: 180, note: 'Unit e integration separados. Os testes de integração sobem a API via WebApplicationFactory contra um Postgres de serviço do Actions.' },
      { id: 'cov', label: 'Cobertura', sub: 'ReportGenerator', group: 'pipeline', x: 646, y: 30, w: 180, note: 'Coletada com XPlat Code Coverage e transformada em HTML + badges.' },
      { id: 'comment', label: 'Comentário no PR', sub: 'Resumo de cobertura', group: 'pipeline', x: 856, y: 30, w: 180 },
      { id: 'pages', label: 'GitHub Pages', sub: 'branch gh-pages', group: 'pipeline', x: 856, y: 120, w: 180, note: 'Relatório publicado em zaqueucavalcante.github.io/estud.' },
      { id: 'railway', label: 'Deploy Railway', sub: 'Build das imagens Docker', group: 'edge', x: 646, y: 230, w: 180, note: 'Automático a partir do master.' },
      { id: 'services', label: 'back · web · caddy', sub: 'Serviços em produção', group: 'app', x: 856, y: 230, w: 180 },
    ],
    edges: [
      { from: 'dev', to: 'pr' },
      { from: 'dev', to: 'ci' },
      { from: 'pr', to: 'tests' },
      { from: 'ci', to: 'tests' },
      { from: 'tests', to: 'cov' },
      { from: 'cov', to: 'comment', label: 'PR' },
      { from: 'cov', to: 'pages', label: 'master' },
      { from: 'ci', to: 'railway' },
      { from: 'railway', to: 'services' },
    ],
  },
  {
    id: 'docs',
    title: 'Documentação & observabilidade',
    description: 'Duas trilhas de documentação (produto e API) e a instrumentação que sai da API.',
    width: 700,
    height: 350,
    nodes: [
      { id: 'controllers', label: 'Controllers + DTOs', sub: 'XML docs · IApiDto', group: 'app', x: 16, y: 40, w: 190, note: 'Cada action tem summary/remarks e cada DTO expõe exemplos nomeados.' },
      { id: 'swagger', label: 'Swashbuckle', sub: 'swagger.json', group: 'app', x: 250, y: 40, w: 190, note: 'Agrupa endpoints por tag, injeta exemplos de resposta e de erro.' },
      { id: 'scalar', label: 'Scalar', sub: '/api/docs', group: 'external', x: 484, y: 40, w: 190, note: 'Referência interativa da API.' },
      { id: 'markdown', label: 'Markdown', sub: 'Web/content/docs', group: 'data', x: 16, y: 150, w: 190, note: 'Introdução, como começar, funcionalidades e segurança.' },
      { id: 'content', label: '@nuxt/content', sub: 'Coleção docs', group: 'app', x: 250, y: 150, w: 190 },
      { id: 'docsPage', label: '/docs', sub: 'Documentação do produto', group: 'client', x: 484, y: 150, w: 190 },
      { id: 'serilog', label: 'Serilog', sub: 'Logs estruturados', group: 'app', x: 16, y: 260, w: 190, note: 'Hoje só o sink de console em produção. Os sinks de Seq/arquivo/OTLP estão referenciados mas não configurados.' },
      { id: 'otel', label: 'OpenTelemetry', sub: 'Traces + métricas', group: 'app', x: 250, y: 260, w: 190, note: 'Instrumentação de ASP.NET Core, HttpClient, Npgsql e runtime. Logs ainda não passam pelo pipeline OTel.' },
      { id: 'otlpOut', label: 'Exporter OTLP', sub: 'Endpoint configurável', group: 'external', x: 484, y: 260, w: 190 },
    ],
    edges: [
      { from: 'controllers', to: 'swagger' },
      { from: 'swagger', to: 'scalar' },
      { from: 'markdown', to: 'content' },
      { from: 'content', to: 'docsPage' },
      { from: 'serilog', to: 'otel', style: 'dotted', label: 'ainda não' },
      { from: 'otel', to: 'otlpOut' },
    ],
  },
]

const gapsDiagram: Diagram = {
  id: 'gaps',
  title: 'O que ainda falta',
  description: 'Área separada de propósito: nada tracejado aqui existe hoje no código. A coluna da esquerda é o que já roda, e as setas mostram onde cada peça nova se encaixa. A busca é para ficar no próprio Postgres (tsvector + índice GIN), sem Elasticsearch nem serviço externo.',
  width: 1060,
  height: 710,
  frames: [
    { label: 'Já existe', x: 16, y: 24, w: 220, h: 660 },
    { label: 'Falta construir', x: 300, y: 24, w: 745, h: 660 },
  ],
  nodes: [
    { id: 'back', label: 'API .NET 10', sub: 'Hoje', group: 'app', x: 36, y: 60, w: 180 },
    { id: 'web', label: 'Nuxt (Web)', sub: 'Hoje', group: 'app', x: 36, y: 175, w: 180 },
    { id: 'otel', label: 'OpenTelemetry', sub: 'Traces + métricas', group: 'app', x: 36, y: 290, w: 180, note: 'Já instrumentado, mas sem destino fixo e sem logs.' },
    { id: 'pg', label: 'PostgreSQL', sub: 'Busca com ILIKE', group: 'data', x: 36, y: 405, w: 180, note: 'A única busca textual hoje é um EF.Functions.ILike com %termo% em GetInstitutions — sem índice, faz varredura na tabela.' },
    { id: 'localState', label: 'Cache + rate limit', sub: 'Por instância', group: 'app', x: 36, y: 500, w: 180, note: 'HybridCache sem L2 e rate limiter em memória: cada réplica tem sua própria contagem e seu próprio cache.' },
    { id: 'polling', label: 'Notificações', sub: 'Polling de 60s', group: 'app', x: 36, y: 595, w: 180, note: 'Todo o tempo real do produto hoje é polling: o sino consulta /notifications/unread-count a cada 60s. Nada é empurrado pelo servidor.' },

    { id: 'admin', label: 'Área de admin', sub: 'Hoje: só listar instituições', group: 'planned', x: 330, y: 60, w: 200, planned: true, note: 'A página /admin/institutions é praticamente tudo que existe. Falta gestão de instituições (suspender, limites, plano), visão de uso por tenant, impersonação com trilha de auditoria, painel de comandos/webhooks com falha e reprocessamento manual.' },
    { id: 'adminOps', label: 'Operação & suporte', sub: 'Impersonar · reprocessar', group: 'planned', x: 580, y: 60, w: 200, planned: true, note: 'Ferramentas de suporte: entrar como usuário (auditado), reprocessar comando/webhook travado, inspecionar jobs do Quartz.' },
    { id: 'adminMetrics', label: 'Métricas de negócio', sub: 'Uso por instituição', group: 'planned', x: 830, y: 60, w: 200, planned: true, note: 'Contadores por tenant (alunos ativos, turmas, matrículas) para suporte e cobrança.' },

    { id: 'posthog', label: 'PostHog', sub: 'Product analytics', group: 'planned', x: 330, y: 175, w: 200, planned: true, note: 'Não existe nenhuma referência a PostHog no repositório hoje.' },
    { id: 'posthogEvents', label: 'Eventos de produto', sub: 'Funil de onboarding', group: 'planned', x: 580, y: 175, w: 200, planned: true, note: 'Instrumentar cadastro, criação da instituição, importação de dados e primeira turma.' },
    { id: 'posthogFlags', label: 'Feature flags', sub: 'Rollout gradual', group: 'planned', x: 830, y: 175, w: 200, planned: true, note: 'Ligar funcionalidade por instituição sem deploy.' },

    { id: 'obs', label: 'Observabilidade completa', sub: 'Backend único', group: 'planned', x: 330, y: 290, w: 200, planned: true, note: 'Hoje o OTLP é opcional e não há destino definido em produção. Falta escolher e ligar um backend (Grafana/Tempo/Loki, SigNoz, Honeycomb...) com retenção e alerta.' },
    { id: 'obsLogs', label: 'Logs', sub: 'Serilog → OTLP', group: 'planned', x: 580, y: 250, w: 200, planned: true, note: 'O sink Serilog.Sinks.OpenTelemetry está no csproj mas não configurado: em produção só existe console.' },
    { id: 'obsMetrics', label: 'Métricas', sub: 'Dashboards + alertas', group: 'planned', x: 580, y: 330, w: 200, planned: true, note: 'As métricas são exportadas, mas não há dashboard nem alerta em cima delas.' },
    { id: 'obsTraces', label: 'Traces', sub: 'Ponta a ponta', group: 'planned', x: 830, y: 290, w: 200, planned: true, note: 'Falta propagar o trace do front (Nuxt) até a API e os jobs, e ligar o Sentry — os pacotes Sentry.AspNetCore/Sentry.OpenTelemetry estão no csproj mas não são usados em nenhum lugar do código.' },

    { id: 'fts', label: 'Full text search', sub: 'No próprio Postgres', group: 'planned', x: 330, y: 405, w: 200, planned: true, note: 'Busca textual usando só o Postgres: tsvector + to_tsquery com dicionário português, sem depender de Elasticsearch, OpenSearch ou qualquer serviço externo. O EF Core já expõe isso via EF.Functions.ToTsVector/Matches e colunas geradas.' },
    { id: 'ftsIndex', label: 'Índice GIN', sub: 'Coluna tsvector gerada', group: 'planned', x: 580, y: 405, w: 200, planned: true, note: 'Coluna tsvector gerada (stored) por entidade pesquisável + índice GIN, mantida pelo próprio banco. Para busca por trecho/typo, pg_trgm com índice GIN cobre o caso do ILIKE atual.' },
    { id: 'ftsUi', label: 'Busca global na UI', sub: 'Alunos · turmas · docs', group: 'planned', x: 830, y: 405, w: 200, planned: true, note: 'Um único endpoint de busca, com ranking (ts_rank) e escopo por instituição, alimentando uma barra de busca global no front.' },

    { id: 'redis', label: 'Estado compartilhado', sub: 'Redis', group: 'planned', x: 330, y: 500, w: 200, planned: true, note: 'Um Redis na Railway resolveria os dois itens ao lado; sem ele, cache e rate limit não sobrevivem a mais de uma réplica.' },
    { id: 'cacheL2', label: 'Cache distribuído', sub: 'L2 do HybridCache', group: 'planned', x: 580, y: 500, w: 200, planned: true, note: 'O HybridCache já suporta segundo nível: basta registrar um IDistributedCache. Hoje não há nenhum, então o cache é sempre local e não é invalidado entre instâncias.' },
    { id: 'rlShared', label: 'Rate limit distribuído', sub: 'Contagem compartilhada', group: 'planned', x: 830, y: 500, w: 200, planned: true, note: 'O limiter de janela fixa conta em memória. Com N réplicas o limite efetivo vira N × o configurado.' },

    { id: 'sse', label: 'SSE / WebSocket', sub: 'Conexão persistente', group: 'planned', x: 330, y: 595, w: 200, planned: true, note: 'Server-Sent Events resolve quase tudo aqui: é HTTP puro, passa pelo Caddy e pelo Cloudflare sem configuração extra, reconecta sozinho e o fluxo é só servidor → cliente. WebSocket fica reservado para quando aparecer interação bidirecional de verdade. Atenção: conexão persistente precisa de estado compartilhado entre réplicas (o mesmo Redis do item acima) para saber em qual instância o usuário está pendurado.' },
    { id: 'ssePush', label: 'Push de notificações', sub: 'Adeus polling de 60s', group: 'planned', x: 580, y: 595, w: 200, planned: true, note: 'Quando o UserNotification é criado, o evento vai direto para o sino do usuário conectado. O polling vira só fallback de reconexão.' },
    { id: 'sseLive', label: 'Telas ao vivo', sub: 'Agenda · turmas · chamadas', group: 'planned', x: 830, y: 595, w: 200, planned: true, note: 'Mesmo canal serve para atualizar agenda e turmas sem refresh, acompanhar chamadas de webhook em /integrations e mostrar o resultado de comandos assíncronos assim que processam.' },
  ],
  edges: [
    { from: 'back', to: 'admin', style: 'dotted' },
    { from: 'web', to: 'admin', style: 'dotted' },
    { from: 'admin', to: 'adminOps', style: 'dashed' },
    { from: 'admin', to: 'adminMetrics', style: 'dashed' },
    { from: 'web', to: 'posthog', style: 'dotted' },
    { from: 'posthog', to: 'posthogEvents', style: 'dashed' },
    { from: 'posthog', to: 'posthogFlags', style: 'dashed' },
    { from: 'otel', to: 'obs', style: 'dotted' },
    { from: 'obs', to: 'obsLogs', style: 'dashed' },
    { from: 'obs', to: 'obsMetrics', style: 'dashed' },
    { from: 'obs', to: 'obsTraces', style: 'dashed' },
    { from: 'pg', to: 'fts', style: 'dotted' },
    { from: 'fts', to: 'ftsIndex', style: 'dashed' },
    { from: 'ftsIndex', to: 'ftsUi', style: 'dashed' },
    { from: 'localState', to: 'redis', style: 'dotted' },
    { from: 'redis', to: 'cacheL2', style: 'dashed' },
    { from: 'cacheL2', to: 'rlShared', style: 'dashed' },
    { from: 'polling', to: 'sse', style: 'dotted' },
    { from: 'sse', to: 'ssePush', style: 'dashed' },
    { from: 'ssePush', to: 'sseLive', style: 'dashed' },
    { from: 'redis', to: 'sse', label: 'depende de', style: 'dotted' },
  ],
}

const allDiagrams = [...diagrams, gapsDiagram]

interface StackRow {
  area: string
  item: string
  tech: string
  note: string
}

const stack: StackRow[] = [
  { area: 'Frontend', item: 'Web', tech: 'Nuxt · Vue 3 · TypeScript · Nuxt UI', note: 'SSR em Node, Tailwind via Nuxt UI, @nuxt/content para a documentação.' },
  { area: 'Backend', item: 'API', tech: 'ASP.NET Core (.NET 10)', note: 'Vertical slice em Back/Features, Result Pattern com OneOf, FluentValidation.' },
  { area: 'Backend', item: 'Persistência', tech: 'PostgreSQL · EF Core · Dapper', note: 'Schema estud, snake_case, migrations por EF. Dapper nas consultas de leitura mais pesadas.' },
  { area: 'Backend', item: 'Cache', tech: 'HybridCache', note: 'ctx.Cache, entrada padrão de 30 min, payload de até 10 MB. Só nível local — sem IDistributedCache registrado.' },
  { area: 'Backend', item: 'Rate limiting', tech: 'Fixed window (ASP.NET Core)', note: 'Limite global por usuário/IP + SensitivePolicy por IP nos endpoints de login, registro e reset. Estado em memória.' },
  { area: 'Backend', item: 'Jobs', tech: 'Quartz.NET', note: 'CommandsProcessor e DomainEventsProcessor, polling a cada 60s.' },
  { area: 'Backend', item: 'PDF', tech: 'QuestPDF', note: 'Relatórios. O CI instala libfontconfig1 por causa disso.' },
  { area: 'Integrações', item: 'Webhooks', tech: 'Inscrições por instituição · retry com backoff', note: 'Saída (WebhookCall + Attempt) e entrada (ReceivedWebhookEvent), com histórico em /integrations.' },
  { area: 'Integrações', item: 'Notificações internas', tech: 'Notification / UserNotification', note: 'Sino no header, caixa de entrada em /notifications e contador de não lidas por polling de 60s — sem push (SSE/WebSocket) ainda.' },
  { area: 'Auth', item: 'Sessão', tech: 'JWT em cookie httpOnly', note: 'Data Protection com chaves no banco.' },
  { area: 'Auth', item: 'Métodos', tech: 'Senha · Magic Link · Google One Tap · Google OAuth · SSO OIDC', note: '2FA por TOTP, opcionalmente obrigatório por instituição.' },
  { area: 'Auth', item: 'Autorização', tech: 'Policies + permissões por perfil', note: 'Uma policy por feature, permissões agrupadas, perfis por instituição.' },
  { area: 'Infra', item: 'Hospedagem', tech: 'Railway (Docker)', note: 'Três serviços: back, web e caddy, além do Postgres.' },
  { area: 'Infra', item: 'Borda', tech: 'Cloudflare + Caddy', note: 'Cloudflare no domínio, Caddy roteando /api/* para o backend.' },
  { area: 'Qualidade', item: 'Testes', tech: 'NUnit · FluentAssertions · WebApplicationFactory', note: 'Integração contra um Postgres real, cenário montado pelos próprios endpoints.' },
  { area: 'Qualidade', item: 'CI/CD', tech: 'GitHub Actions', note: 'pr.tests.yml em PR, ci.cd.yml em master, cobertura no GitHub Pages.' },
  { area: 'Observabilidade', item: 'Logs e telemetria', tech: 'Serilog · OpenTelemetry · OTLP', note: 'Traces e métricas instrumentados; logs ainda só no console em produção.' },
  { area: 'Documentação', item: 'Produto e API', tech: '@nuxt/content (/docs) · Scalar (/api/docs)', note: 'Docs da API geradas dos comentários XML e dos exemplos dos DTOs.' },
]

const nodeIndex = new Map<string, DiagramNode>()
const neighbors = new Map<string, Set<string>>()

for (const diagram of allDiagrams) {
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
  <UDashboardPanel id="dev-overview">
    <template #header>
      <UDashboardNavbar title="Visão Geral do Sistema">
        <template #leading>
          <PageIcon icon="i-lucide-network" />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="overview space-y-8">
        <div class="space-y-2">
          <p class="text-sm text-muted">
            Mapa de alto nível de tudo que o Estud tem hoje: infraestrutura, autenticação, processamento assíncrono,
            pipeline de testes e deploy, documentação e observabilidade. A última seção é separada de propósito e lista
            o que ainda <span class="italic">não</span> existe.
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
              <UBadge v-if="selectedNode.planned" variant="subtle" color="warning" size="sm">ainda não existe</UBadge>
            </div>
            <p class="text-sm text-toned">{{ selectedNode.note ?? 'Sem observações.' }}</p>
          </div>
          <p v-else class="text-sm text-muted">
            Nenhum componente selecionado. Clique numa caixa dos diagramas para ver os detalhes.
          </p>
        </UCard>

        <section v-for="d in allDiagrams" :key="d.id" class="space-y-3">
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
            <h2 class="text-base font-semibold text-highlighted">Stack</h2>
            <p class="text-sm text-muted">Resumo do que está em uso, por área.</p>
          </div>
          <div class="overflow-x-auto rounded-lg border border-default">
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-default bg-elevated/50 text-left text-xs text-muted">
                  <th class="px-3 py-2 font-medium">Área</th>
                  <th class="px-3 py-2 font-medium">Componente</th>
                  <th class="px-3 py-2 font-medium">Tecnologia</th>
                  <th class="px-3 py-2 font-medium">Observações</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="row in stack" :key="`${row.area}-${row.item}`" class="border-b border-default last:border-b-0">
                  <td class="px-3 py-2 text-muted whitespace-nowrap">{{ row.area }}</td>
                  <td class="px-3 py-2 text-highlighted font-medium whitespace-nowrap">{{ row.item }}</td>
                  <td class="px-3 py-2 text-toned">{{ row.tech }}</td>
                  <td class="px-3 py-2 text-muted">{{ row.note }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>
      </div>
    </template>
  </UDashboardPanel>
</template>

<style scoped>
.overview {
  --g-client: #2a78d6;
  --g-edge: #eda100;
  --g-app: #1baf7a;
  --g-data: #e87ba4;
  --g-external: #8b5cf6;
  --g-pipeline: #0891b2;
  --g-planned: #94a3b8;
  --edge-color: var(--ui-border-accented);
}

.dark .overview {
  --g-client: #3987e5;
  --g-edge: #c98500;
  --g-app: #199e70;
  --g-data: #d55181;
  --g-external: #a78bfa;
  --g-pipeline: #22b8cf;
  --g-planned: #64748b;
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
