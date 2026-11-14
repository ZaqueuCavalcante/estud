# Billing & Financeiro — Plano

Suporte a pagamentos no Estud em duas frentes que **não podem se misturar**, mas que compartilham a mesma
infraestrutura de pagamento:

| Frente | Quem cobra | Quem paga | Dinheiro vai para |
|---|---|---|---|
| **Billing da plataforma** | Estud | Instituição | Conta do Estud |
| **Financeiro acadêmico** | Instituição | Aluno / responsável financeiro | Conta da instituição |

A regra de ouro: **o dinheiro das mensalidades nunca passa pela conta do Estud** (pelo menos no começo). O Estud
orquestra a cobrança usando a conta de gateway da própria instituição. Isso evita que o Estud vire
subcredenciador/instituição de pagamento aos olhos do Banco Central, com toda a carga regulatória e de
responsabilidade sobre o saldo de terceiros que isso traz. Split/marketplace (Stripe Connect, recebedores do
Pagar.me) fica para uma fase posterior, se houver motivo de negócio (ex.: Estud cobrar % sobre a mensalidade).

---

## 1. Perfis de instituição

Hoje o `Institution` (`Back/Domain/Institutions/Institution.cs`) só tem nome e config acadêmica. Os casos que
precisam ser suportados são combinações de duas chaves independentes:

| | Paga o Estud | Não paga o Estud |
|---|---|---|
| **Cobra mensalidade** | Escola/faculdade privada | Parceria/piloto com escola privada |
| **Não cobra mensalidade** | Instituição pública que contratou o Estud; ONG | Instituição pública patrocinada, projeto social, dev/testes |

Por isso **não** amarrar comportamento a um "tipo" de instituição (pública/privada/sem fins lucrativos). O tipo
vira um dado cadastral/fiscal; o comportamento vem de:

1. **Assinatura da plataforma** (`PlatformSubscription`): qual plano, se é cobrado, como é cobrado.
2. **Módulo Financeiro habilitado** (`Entitlement`): se a instituição tem acesso ao financeiro acadêmico.

### Novos dados na instituição

```csharp
public enum InstitutionLegalNature : short   // natureza jurídica — informativo/fiscal
{
    Private = 0,
    Public = 1,
    NonProfit = 2,
}
```

E um `InstitutionBillingProfile` (1:1 com `Institution`, igual ao `InstitutionConfig`):

- Razão social, nome fantasia, CNPJ, inscrição municipal, endereço de cobrança
- E-mails de cobrança (financeiro da instituição recebe as faturas do Estud)
- `LegalNature`
- Para públicas: dados de empenho/contrato administrativo (número do processo, vigência)

O `Institution.NewForUserRegister()` continua criando a instituição no plano gratuito/trial — nada muda pro
fluxo de cadastro atual.

---

## 2. Entitlements (feature flags por plano)

Antes de qualquer pagamento, é preciso ter a noção de **o que a instituição pode usar**. Isso destrava tanto o
"módulo financeiro opcional" quanto os limites dos planos do Estud.

```
Plan ──< PlanEntitlement >── Feature
Institution ──< InstitutionEntitlementOverride   (concessões manuais do admin)
```

- `Feature` é um enum com valor explícito: `FinanceModule = 0`, `Webhooks = 1`, `Sso = 2`, `CustomDomain = 3`,
  `MaxActiveStudents = 100`, `MaxCampi = 101`, `StorageGb = 102`...
- Entitlements booleanos (tem/não tem) e numéricos (limite).
- Overrides permitem ao admin do Estud dar o financeiro pra uma instituição de plano gratuito, estender um
  limite, etc., com data de expiração opcional.
- Resolução: `override ?? plano ?? default`. Cachear com `HybridCache` por instituição, invalidando quando a
  assinatura ou override mudar.

### Como aplicar

- **Backend**: uma policy/attribute `[RequiresFeature(Feature.FinanceModule)]` nos controllers do financeiro,
  devolvendo um `EstudError` próprio (`FeatureNotAvailableInPlan`) — 402 ou 403, a decidir. Limites numéricos
  checados no service (ex.: `CreateStudentService` checa `MaxActiveStudents`).
- **Frontend**: o `GetAuthStatus`/`GetInstitutionConfig` passa a devolver a lista de features ativas; menu
  e páginas do financeiro somem quando a feature está desligada.
- Permissões (RBAC) continuam ortogonais: o entitlement diz se *a instituição* tem a feature; a permissão diz se
  *o usuário* pode usá-la.

---

## 3. Núcleo de pagamentos (compartilhado)

Infraestrutura em `Back/Payments/` (no mesmo nível de `Back/Auth`, `Back/Emails`, `Back/Storage`), usada pelas
duas frentes.

### 3.1 Abstração de gateway

```csharp
public interface IPaymentGateway
{
    PaymentProvider Provider { get; }
    GatewayCapabilities Capabilities { get; }

    Task<OneOf<GatewayCustomer, EstudError>> UpsertCustomer(GatewayAccountCredentials c, PayerData payer);
    Task<OneOf<GatewayCharge, EstudError>> CreateCharge(GatewayAccountCredentials c, CreateChargeRequest req);
    Task<OneOf<GatewayCharge, EstudError>> GetCharge(GatewayAccountCredentials c, string externalId);
    Task<OneOf<EstudSuccess, EstudError>> CancelCharge(GatewayAccountCredentials c, string externalId);
    Task<OneOf<GatewayRefund, EstudError>> Refund(GatewayAccountCredentials c, string externalId, long amount);
    Task<OneOf<GatewayPaymentMethod, EstudError>> SaveCard(GatewayAccountCredentials c, string customerId, string cardToken);

    WebhookVerification VerifyWebhook(GatewayAccountCredentials c, HttpRequest request, string body);
    IReadOnlyList<NormalizedGatewayEvent> ParseWebhook(string body);
}
```

- `PaymentProvider` enum: `Fake = 0`, `PagarMe = 1`, `Stripe = 2`, `Cielo = 3`, `PagBank = 4`, `Asaas = 5`,
  `MercadoPago = 6`, `Iugu = 7`...
- Uma implementação por provider em `Back/Payments/Providers/{Provider}/`, resolvidas por keyed services
  (`[FromKeyedServices(PaymentProvider.PagarMe)]`) ou por uma `PaymentGatewayFactory`.
- `HttpClient` tipado por provider, com Polly (retry só em erros idempotentes) e OpenTelemetry já instrumentado.
- Os adapters **traduzem tudo para o modelo do Estud**: status, erros e eventos. Nada específico de provider
  vaza pros services de feature.

### 3.2 Capabilities

Os gateways não suportam as mesmas coisas, então cada adapter declara o que faz:

```csharp
[Flags]
public enum GatewayCapabilities : long
{
    None = 0,
    Pix = 1,
    PixAutomatic = 2,        // Pix Automático (recorrência)
    Boleto = 4,
    BolepixHybrid = 8,       // boleto com QR Pix
    CreditCard = 16,
    DebitCard = 32,
    CardInstallments = 64,   // parcelado no cartão
    CardRecurring = 128,     // cobrança recorrente com cartão salvo
    Refund = 256,
    PartialRefund = 512,
    Split = 1024,
    NativeSubscriptions = 2048,
}
```

A UI de configuração e o roteamento (seção 3.4) usam isso pra só oferecer o que o gateway escolhido suporta.

**Decisão importante**: *não* usar as assinaturas nativas dos gateways para mensalidades. O Estud gera cada
cobrança (via job), e o gateway só processa cobranças avulsas. Assim:
- o comportamento é igual em todos os gateways;
- trocar de gateway no meio do ano não exige migrar assinaturas;
- regras do Estud (bolsa, desconto, rematrícula, trancamento) ficam num lugar só.

Exceção: cartão recorrente usa cartão tokenizado salvo (`SaveCard`) + cobrança avulsa disparada pelo Estud.

### 3.3 Contas de gateway (credenciais)

```
GatewayAccount
  Id, PublicId
  InstitutionId?          -- null = conta do próprio Estud (billing da plataforma)
  Provider
  Name                    -- "Pagar.me principal", "Cielo campus Centro"
  Environment             -- Sandbox | Production
  EncryptedCredentials    -- JSON (api key, secret, merchant id...) cifrado
  WebhookSecret (cifrado)
  IsActive, CreatedAt, UpdatedAt, LastHealthCheckAt, LastHealthCheckError
```

- Criptografia com DataProtection, seguindo o padrão do `SsoEncryptionManager`
  (`Back/Auth/Managers/SsoEncryptionManager.cs`) — um `PaymentCredentialsProtector` com purpose próprio.
- O schema das credenciais varia por provider: cada adapter expõe um descritor (campos, labels, quais são
  secretos) usado pelo front pra montar o formulário dinamicamente.
- Na criação/edição, rodar um health check (chamada barata autenticada no gateway) e registrar a URL de webhook
  automaticamente quando o gateway permitir.
- Segredos **nunca** voltam na API (devolver mascarado, como `sk_live_****abcd`).
- A conta do Estud (InstitutionId null) vem de settings (`PaymentsSettings`), não do banco, ou é cadastrada só
  pelo admin.

### 3.4 Níveis de configuração e roteamento

"Qual gateway processa esta cobrança, com quais regras" se resolve em cascata, do mais específico pro mais geral:

```
Contrato do aluno  →  Oferta de curso  →  Curso  →  Campus  →  Instituição  →  Default do Estud
```

Na prática, a maioria das instituições só configura o nível Instituição. Os níveis abaixo existem para casos como
"campus X tem CNPJ próprio e outra conta na Cielo" ou "pós-graduação cobra com outro gateway".

`FinanceSettings` (uma linha por nível, campos nulos = herda):

- Gateway por método de pagamento: `PixAccountId`, `BoletoAccountId`, `CardAccountId` (permite, por ex., Pix pelo
  PagBank e cartão pela Cielo)
- Métodos aceitos (Pix, boleto, cartão)
- Dia de vencimento padrão, dias de antecedência pra emissão
- Multa e juros de mora (com validação: multa ≤ 2%, art. 52 §1º CDC)
- Desconto por pontualidade (valor/percentual, até quantos dias antes)
- Validade do boleto após vencimento, expiração do Pix
- Régua de cobrança (ver seção 5.5)
- Instruções/texto do boleto

Um `FinanceSettingsResolver` monta a configuração efetiva para um contrato. Testável unitariamente.

Opcional futuro: **fallback** de gateway (se a criação no gateway A falhar por indisponibilidade, tentar B).

### 3.5 Cobrança e transações

Entidades do núcleo (usadas pelas duas frentes):

```
Charge (cobrança / título)
  Id, PublicId, InstitutionId
  Scope                  -- PlatformBilling | Tuition
  PayerId
  Description, Amount (centavos), Currency
  DueDate, Status        -- Draft, Open, Paid, PartiallyPaid, Overdue, Canceled, Refunded, Renegotiated
  OriginalAmount, DiscountAmount, FineAmount, InterestAmount, PaidAmount
  InvoiceId? / InstallmentId?   -- de onde a cobrança veio
  Attempts[]

ChargeAttempt (tentativa num gateway/método)
  ChargeId, GatewayAccountId, Method, ExternalId
  Status, PixQrCode, PixCopyPaste, BoletoLine, BoletoBarcode, BoletoPdfUrl, CardLast4...
  ExpiresAt, CreatedAt, FailureCode, FailureMessage

Payment (dinheiro efetivamente recebido)
  ChargeId, AttemptId, Amount, Fee (taxa do gateway), NetAmount, PaidAt, SettlementExpectedAt

Refund
  PaymentId, Amount, Reason, Status, ExternalId, RequestedBy
```

- Valores em **centavos (`long`)**. Os gateways trabalham em centavos e evita problema de arredondamento
  ao dividir parcelas. Arredondamento de parcelas: diferença de centavos vai na última (ou primeira) parcela.
- Uma `Charge` pode ter várias tentativas (aluno gera Pix, deixa expirar, gera de novo; ou troca para boleto).
  Só uma pode ser paga; ao confirmar uma, cancelar as outras abertas.
- **Idempotência**: toda chamada de criação no gateway manda `ChargeAttempt.PublicId` como idempotency key (Stripe,
  Pagar.me e Asaas suportam; nos que não suportam, consultar por referência externa antes de recriar).

### 3.6 Webhooks de entrada

```
POST /payments/webhooks/{provider}/{gatewayAccountPublicId}
```

1. Controller anônimo, rate limit próprio, lê o body cru.
2. Verifica assinatura (`VerifyWebhook`) com o segredo da conta. Assinatura inválida → 401, sem gravar nada.
3. Grava o evento cru em `GatewayEvent` (provider, external event id, payload, recebido em) com índice único em
   `(provider, external_event_id)` — deduplicação de reentregas.
4. Cria um `Command` `ProcessGatewayEventCommand` (`ctx.AddCommand(...)`) e responde 200 imediatamente.
5. O handler normaliza o evento (`ParseWebhook`) e aplica a transição na `Charge`/`ChargeAttempt`.

Regras:
- **Webhook não é fonte da verdade**: em eventos de pagamento, o handler consulta o gateway (`GetCharge`) antes de
  confirmar. Protege contra payload forjado em gateways com verificação fraca.
- Transições de status são uma máquina de estados no domínio (`Charge.MarkAsPaid(...)` etc.), idempotentes e
  tolerantes a eventos fora de ordem (ex.: `paid` chegando antes de `created`).
- Gateways sem webhook confiável (ou falha de entrega): job de **polling** consulta tentativas abertas há mais de
  X minutos.

### 3.7 Ledger (extrato)

Extrato confiável exige um livro-razão imutável, em vez de calcular saldo a partir dos status das cobranças:

```
LedgerEntry
  Id, InstitutionId, Scope, AccountType   -- Receivable, Cash, GatewayFees, Discounts, Refunds, Writeoffs...
  PayerId?, ChargeId?, PaymentId?
  Amount (com sinal), OccurredAt, Description, CreatedAt
```

- Partidas dobradas simplificadas: emitir cobrança = débito em Receivable; pagamento = crédito em Receivable +
  débito em Cash; taxa do gateway = lançamento em GatewayFees; estorno, desconto, perdão de dívida etc.
- **Append-only**: nada é editado ou apagado; correção é um lançamento de estorno.
- A partir do ledger saem: extrato do aluno/responsável, saldo devedor, extrato da instituição, relatórios de
  receita, inadimplência e taxas. Relatórios pesados podem ir pra Dapper com SQL à mão.

### 3.8 Conciliação

- **Liquidação/repasses**: job diário consulta o gateway (quando a API permite) pelos recebíveis/payouts e marca
  `Payment.SettledAt`, guardando o id do repasse. Divergências (pago no gateway e aberto no Estud, ou o contrário)
  geram um item de `ReconciliationIssue` pro financeiro olhar.
- **Pagamentos fora do gateway** (dinheiro no balcão, transferência, depósito): baixa manual na cobrança, com
  anexo de comprovante (`IStorageService`), motivo e usuário — tudo auditado.

---

## 4. Billing da plataforma (Estud → instituição)

### 4.1 Modelo

```
Plan
  Id, Code ("free", "basic", "pro", "enterprise"), Name, IsPublic, IsActive
  Entitlements[]
  Prices[]

PlanPrice
  PlanId, Interval (Monthly | Yearly), Model (Flat | PerActiveStudent | Tiered), Amount, TiersJson

PlatformSubscription (1 por instituição)
  InstitutionId, PlanId, PlanPriceId
  Status       -- Trialing, Active, PastDue, Suspended, Canceled, Exempt
  BillingMode  -- Automatic (cartão/Pix/boleto via gateway) | Manual (fatura + NF, ex.: órgão público) | Exempt
  CurrentPeriodStart, CurrentPeriodEnd, TrialEndsAt, CancelAt
  CustomAmount?      -- preço negociado (enterprise / contrato público)
  DefaultPaymentMethodId?

PlatformInvoice
  InstitutionId, SubscriptionId, PeriodStart, PeriodEnd
  Lines[] (plano, excedente de alunos, add-ons, crédito, desconto, pro-rata)
  Total, Status, ChargeId?, FiscalDocumentId?

UsageSnapshot
  InstitutionId, Metric (ActiveStudents, StorageBytes...), Value, MeasuredAt
```

### 4.2 Cobrança por aluno ativo

Métrica natural pra um SGA. Um job diário grava `UsageSnapshot` de alunos ativos; no fechamento do ciclo a fatura
usa o **pico** (ou a média) do período — definir regra e documentar nos termos de uso. Definir o que é "ativo"
(aluno com `StudentCourseEnrollment` sem `LeftAt`, por exemplo).

### 4.3 Ciclo de vida

- **Cadastro** (`RegisterUser`) → assinatura `Trialing` no plano padrão, `TrialEndsAt = +30 dias`.
- **Upgrade** → imediato, pro-rata cobrado na hora ou na próxima fatura. **Downgrade** → no fim do ciclo, validando
  se a instituição cabe nos limites do plano novo.
- **Inadimplência (dunning)**: fatura vence → lembretes D+1, D+3, D+7 → `PastDue` com banner no app → após N dias
  `Suspended`.
- **Suspensão = modo somente leitura**, nunca bloqueio total nem exclusão: os dados são da instituição (LGPD) e
  alunos/professores não podem perder acesso a notas e documentos por causa de uma fatura. Exportação de dados
  continua sempre disponível.
- **Cancelamento** → fim do ciclo; dados retidos por período definido nos termos, depois anonimização/exclusão.

### 4.4 Instituições públicas e isentas

- **Públicas**: geralmente não pagam com cartão. Pagam após empenho, contra nota fiscal, por boleto ou
  transferência, muitas vezes anual. `BillingMode = Manual`: o Estud gera a fatura e a NFS-e, e o admin dá
  baixa manual quando o pagamento entra. Precisa suportar preço negociado (`CustomAmount`) e vigência de contrato.
- **Isentas** (patrocinadas, projetos sociais, parcerias): `Status = Exempt`, sem faturas. Os entitlements vêm
  do plano atribuído pelo admin.

### 4.5 Nota fiscal do Estud

O Estud, como prestador de serviço, precisa emitir **NFS-e** de cada fatura paga (ou emitida, conforme o regime).
Integrar com um emissor (NFE.io, Focus NFe, eNotas, PlugNotas) por trás de um `IFiscalDocumentProvider`, com o
mesmo padrão de adapter e Command assíncrono. Atenção à reforma tributária (IBS/CBS, a partir de 2026) — o
emissor terceirizado absorve a maior parte disso, o que é mais um motivo pra não emitir direto com as prefeituras.

### 4.6 Telas

- **Instituição** (`/billing`): plano atual, uso vs. limites, faturas e notas (download), forma de pagamento,
  upgrade/downgrade, dados de cobrança. Permissão nova `ManagePlatformSubscription` (diretor).
- **Admin do Estud** (`/admin/plans`, `/admin/subscriptions`, ao lado de `Web/app/pages/admin/institutions.vue`):
  CRUD de planos, atribuir plano/isenção/preço negociado, overrides de entitlement, baixa manual, MRR, churn,
  inadimplência. Features em `Back/Features/Admin/`.

---

## 5. Financeiro acadêmico (instituição → aluno)

Só existe para instituições com `Feature.FinanceModule`.

### 5.1 Responsável financeiro

```
Payer
  Id, InstitutionId, Type (Person | Company), Name, Document (CPF/CNPJ), Email, Phone, Address
  UserId?            -- se o responsável também tem login no Estud
  GatewayCustomers[] -- id do customer em cada GatewayAccount
```

O pagador nem sempre é o aluno: pais (aluno menor de idade), empresa que paga a pós de um funcionário, convênio.
Um aluno pode ter vários pagadores (ex.: 50% empresa, 50% aluno) e um pagador pode cobrir vários alunos (irmãos).
Boleto registrado e NFS-e exigem CPF/CNPJ e endereço do pagador — tratar como dado pessoal sob LGPD (base legal:
execução de contrato).

### 5.2 Tabela de preços e plano de pagamento

```
PriceTable                       -- "Tabela 2027 — ADS noturno"
  InstitutionId, Name, ValidFrom, ValidTo
  Scope: CourseOffering | Course | Campus | Institution
  Items[]: tipo (Matrícula, Mensalidade, Material, Taxa de documento, Taxa de reposição...), valor

PaymentPlan                      -- como o valor é parcelado
  PriceTableId, Name ("12x", "6x semestral", "À vista com 10%")
  InstallmentsCount, FirstDueDate rule, DueDay, Discount
```

Vincular à `CourseOffering` (`Back/Domain/CourseOfferings/CourseOffering.cs`), que já é período + campus + curso
+ currículo + turno: é exatamente a granularidade em que o preço costuma variar.

Cobranças avulsas (2ª via de documento, segunda chamada de prova, material) também precisam existir, sem contrato.

### 5.3 Contrato

```
StudentContract
  Id, PublicId, InstitutionId, StudentId
  StudentCourseEnrollmentId      -- vínculo com a matrícula existente
  PaymentPlanId, Status (Draft, PendingSignature, Active, Suspended, Terminated, Completed)
  Payers[] (PayerId, Percentage)
  Discounts[] (bolsa, convênio, irmão, funcionário — tipo, %, valor, vigência, cumulativo?)
  StartDate, EndDate, SignedAt, DocumentFileId
  Installments[]

ContractInstallment
  ContractId, Number, DueDate, GrossAmount, DiscountAmount, NetAmount, Status, ChargeId?
```

- Ao ativar o contrato, as parcelas são **planejadas** (`ContractInstallment`), mas as `Charge`s só são criadas
  X dias antes do vencimento (seção 5.4). Assim, mudanças de desconto/bolsa no meio do semestre afetam as parcelas
  futuras sem cancelar cobranças já emitidas.
- Bolsas e financiamentos de terceiros (ProUni, FIES, bolsa da própria instituição) são descontos ou um pagador
  "terceiro" — modelar de forma genérica, sem integração com o governo no início.
- **Eventos acadêmicos que afetam o financeiro**: trancamento, cancelamento, transferência, conclusão. Ouvir via
  domain events (`Back/DomainEvents/`) — o fluxo acadêmico dispara o evento, o financeiro reage (suspender
  parcelas futuras, calcular multa rescisória, gerar crédito). O acadêmico nunca chama o financeiro diretamente.
- Assinatura eletrônica do contrato (Clicksign, D4Sign, ZapSign) — fase posterior, mesmo padrão de adapter.

### 5.4 Geração de cobranças

Job Quartz diário (`GenerateTuitionChargesJob`) → para cada instituição com o módulo ativo, um Command por lote:

1. Busca parcelas com vencimento dentro da janela de emissão e sem `Charge`.
2. Resolve a configuração efetiva (`FinanceSettingsResolver`).
3. Cria `Charge` + `ChargeAttempt` no gateway configurado (boleto/bolepix geralmente já na emissão; Pix pode ser
   gerado sob demanda quando o aluno clica em "pagar", pra não expirar).
4. Notifica o pagador (e-mail via `IEmailsService`, notificação in-app via `Notification`).

Sempre um Command por cobrança (ou lote pequeno), com retry e backoff (`Back/Commands`): uma falha no gateway não
pode travar o lote inteiro.

### 5.5 Régua de cobrança e inadimplência

- Configurável por instituição: lembrete D-5, D-1, no vencimento, D+1, D+7, D+15...
- Canais: e-mail, notificação in-app; WhatsApp/SMS em fase posterior.
- Multa e juros calculados **no momento do pagamento** (boleto com instrução de multa/juros no gateway;
  Pix gerado depois do vencimento já com o valor atualizado).
- **Renegociação/acordo**: junta N cobranças vencidas numa nova série de parcelas; as originais ficam
  `Renegotiated` (não `Paid`), pra relatório de inadimplência ser honesto.
- **Limites legais** (Lei 9.870/99, art. 6º): a instituição **não pode** suspender provas, reter documentos
  escolares nem aplicar penalidade pedagógica por inadimplência; pode negar a rematrícula. Então o sistema **não**
  deve oferecer "bloquear acesso do aluno por inadimplência" a notas/aulas/documentos. Pode oferecer bloqueio da
  **rematrícula** (`EnrollStudentInCourseOffering`), como configuração.

### 5.6 Portal do aluno / responsável

- Lista de cobranças (abertas, pagas, vencidas), extrato, saldo devedor.
- Pagar: Pix (QR + copia e cola, com polling/SSE de confirmação), boleto (linha digitável + PDF), cartão.
- **Cartão**: o número **nunca** chega ao backend do Estud. Tokenização no browser com o SDK de cada gateway
  (Stripe Elements, tokenizecard do Pagar.me, Cielo, PagBank) → o backend recebe só o token. Isso mantém o Estud
  no escopo PCI DSS SAQ A (ou A-EP). O front precisa de um componente de cartão que carrega o SDK do gateway
  configurado (o backend devolve provider + public key).
- Salvar cartão pra débito recorrente (opt-in explícito).
- Documentos: recibo, **declaração anual de quitação de débitos** (obrigatória — Lei 12.007/2009, até maio do
  ano seguinte) e informe de pagamentos pra declaração de IR (despesas com educação). Geração em PDF reaproveitando
  a infra do `GenerateEnrollmentProof`.

### 5.7 Back-office da instituição

- Dashboard: a receber, recebido, inadimplência (valor e %), previsão de caixa, taxas pagas aos gateways.
- Cobranças: filtros, 2ª via, cancelar, baixa manual, estorno (parcial/total), concessão de desconto pontual.
- Contratos: criar/editar, aplicar bolsa, simular, renegociar.
- Tabelas de preço e planos de pagamento.
- Configurações: contas de gateway, `FinanceSettings` por nível, régua de cobrança.
- Conciliação: repasses, divergências.
- Exportações (CSV/Excel) e integração contábil — o contador da instituição vai pedir.

### 5.8 NFS-e das mensalidades

Instituições privadas emitem NFS-e das mensalidades. Mesmo `IFiscalDocumentProvider` da seção 4.5, mas com as
credenciais/certificado **da instituição**. Opcional por instituição. Fase posterior.

---

## 6. Permissões, auditoria e segurança

### Permissões

Novos `PermissionGroup`s (próximos valores livres em `Back/Auth/Permissions/PermissionGroup.cs`): `Finance = 15`,
`PlatformBilling = 16`. Novas `EstudPermission`s em `EstudPermissions.cs`, por exemplo:

| Permissão | Descrição |
|---|---|
| `ViewFinance` | Ver dashboard, cobranças e extratos |
| `ManageContracts` | Criar/editar contratos, descontos e renegociações |
| `ManageCharges` | Emitir, cancelar, dar baixa manual |
| `RefundPayments` | Estornar pagamentos — sensível |
| `ManageFinanceSettings` | Gateways, credenciais, tabelas de preço, régua |
| `ManagePlatformSubscription` | Plano do Estud, faturas, forma de pagamento |

- Estorno, troca de credencial e baixa manual: considerar exigir 2FA recente (já existe enforcement de 2FA por
  perfil em `SetTwoFactorEnforcement`).
- Policies em `Back/Auth/Policies/Policies.Finance.cs` e `Policies.Billing.cs`.

### Auditoria

Toda mudança em cobrança, contrato, conta de gateway e assinatura passa pelo `AuditSaveChangesInterceptor`
(`Back/Audit/`). Garantir que `EncryptedCredentials` seja **excluído** do audit trail.

### Segurança

- Credenciais cifradas (DataProtection), mascaradas na API, nunca logadas (revisar Serilog destructuring e
  atributos de span do OpenTelemetry nos HttpClients dos gateways).
- Webhook: assinatura obrigatória, dedup, confirmação via API do gateway.
- Isolamento multi-tenant: toda query filtra por `ctx.RequestUser.InstitutionId`; o endpoint de webhook resolve a
  instituição pela `GatewayAccount`, nunca pelo payload.
- Valores sempre calculados no backend; o front nunca manda "quanto cobrar".

---

## 7. Integrações de saída

Novos `WebhookEventType` (`Back/Domain/Enums/WebhookEventType.cs`), num bloco novo de valores explícitos:

```csharp
[Description("Cobrança criada")]      ChargeCreated = 300,
[Description("Cobrança paga")]        ChargePaid = 301,
[Description("Cobrança vencida")]     ChargeOverdue = 302,
[Description("Cobrança cancelada")]   ChargeCanceled = 303,
[Description("Pagamento estornado")]  PaymentRefunded = 304,
[Description("Contrato ativado")]     ContractActivated = 400,
[Description("Contrato encerrado")]   ContractTerminated = 401,
```

Permite que a instituição integre com ERP/contabilidade própria usando a infra de webhooks que já existe.

---

## 8. Testes

- **Fake gateway** no projeto `Fakes/` (como já existe pra Brevo, OIDC, DNS e webhooks): `Fakes/Payments/` com
  endpoints que imitam um gateway (criar cobrança, consultar) e um controller de controle pros testes
  ("marcar como pago", "expirar", "recusar cartão", "reenviar webhook", "enviar webhook duplicado/fora de ordem").
  O `PaymentProvider.Fake` aponta pra ele. Testes de integração montam tudo via API (conta de gateway, tabela de
  preço, contrato) e usam `_back.AwaitCommandsProcessing()`.
- **Testes de contrato por adapter** (PagarMe, Stripe...): rodar contra o sandbox de cada gateway, fora do CI
  padrão (categoria separada/manual), validando o mapeamento de status e de webhooks com payloads reais gravados.
  Payloads de exemplo versionados como fixtures pros testes unitários de `ParseWebhook`.
- **Unitários**: `FinanceSettingsResolver`, cálculo de parcelas/arredondamento, multa/juros, pro-rata, descontos
  cumulativos, máquina de estados da `Charge`, lançamentos do ledger (soma sempre fecha).
- Cenários críticos: webhook duplicado não paga duas vezes; pagamento após cancelamento; dois Pix pagos da mesma
  cobrança (gera crédito/estorno); troca de gateway com cobranças abertas no antigo.

---

## 9. Organização no código

```
Back/
  Payments/                     -- infra: IPaymentGateway, providers, factory, protector, settings
    Providers/{Fake,PagarMe,Stripe,Cielo,PagBank}/
  Fiscal/                       -- IFiscalDocumentProvider (fase posterior)
  Domain/
    Payments/                   -- GatewayAccount, Charge, ChargeAttempt, Payment, Refund, GatewayEvent, LedgerEntry
    Billing/                    -- Plan, PlanPrice, PlatformSubscription, PlatformInvoice, UsageSnapshot, Entitlement
    Finance/                    -- Payer, PriceTable, PaymentPlan, StudentContract, ContractInstallment, FinanceSettings
  Database/
    EstudDbContext.Payments.cs, .Billing.cs, .Finance.cs + configs
  Features/
    Payments/                   -- ReceiveGatewayWebhook, CreateGatewayAccount, TestGatewayAccount...
    Billing/                    -- GetSubscription, ChangePlan, GetPlatformInvoices...
    Finance/                    -- CreateContract, GetCharges, PayCharge, RefundPayment, ManualSettlement...
    Admin/                      -- CreatePlan, AssignPlan, SetEntitlementOverride, GetMrr...
Web/app/pages/
  billing/                      -- assinatura do Estud
  finance/                      -- back-office financeiro
  payments/                     -- portal do aluno/responsável
  admin/plans.vue, admin/subscriptions.vue
```

Talvez valha um schema Postgres próprio (`finance`) pra separar fisicamente — decidir junto com o padrão de
`DbSchemas.cs`.

---

## 10. Fases

Cada fase entrega algo usável sozinho.

### Fase 0 — Fundação (sem dinheiro)
- `InstitutionBillingProfile`, `LegalNature`.
- `Plan`, `PlatformSubscription` (status/modos, sem cobrança), entitlements + overrides + `[RequiresFeature]`.
- Todas as instituições existentes migradas para um plano com `Exempt`/`Trialing`.
- Admin: atribuir plano, isentar, override.
- **Resultado**: dá pra ligar/desligar o módulo financeiro e limitar planos, mesmo cobrando por fora.

### Fase 1 — Núcleo de pagamentos + um gateway
- `IPaymentGateway`, `GatewayAccount` (cifrado), fake gateway, webhook de entrada, `Charge`/`Attempt`/`Payment`,
  ledger, polling.
- Primeiro adapter: **um gateway que cubra Pix + boleto + cartão no Brasil** (Pagar.me ou Asaas são os candidatos
  mais diretos; Stripe tem Pix/boleto no BR, mas com menos opções pra boleto).

### Fase 2 — Billing da plataforma
- Faturas do Estud, cobrança automática pela conta do Estud, modo manual (públicas), dunning, somente leitura,
  tela `/billing`, admin de assinaturas. NFS-e via emissor terceirizado.
- **Resultado**: o Estud começa a faturar.

### Fase 3 — Financeiro acadêmico MVP
- Payer, tabela de preço, plano de pagamento, contrato, parcelas, geração diária de cobranças, portal do aluno
  (Pix + boleto), baixa manual, extrato, régua de cobrança por e-mail.

### Fase 4 — Financeiro completo
- Cartão (tokenização no front, recorrência), estornos, renegociação, descontos/bolsas avançados, reação a
  trancamento/cancelamento, conciliação de repasses, declarações anuais, relatórios, webhooks de saída.

### Fase 5 — Multi-gateway e níveis de configuração
- Adapters adicionais (Stripe, Cielo, PagBank...), roteamento por método e por nível (campus/curso/oferta),
  fallback, troca de gateway com cobranças em aberto.

### Fase 6 — Extras
- NFS-e das mensalidades, assinatura eletrônica de contrato, WhatsApp/SMS, Pix Automático, split/marketplace
  (Estud ficando com % das mensalidades), integração contábil.

---

## 11. Decisões em aberto

1. **Modelo de preço do Estud**: por aluno ativo, faixas, ou flat por plano? Pico ou média do mês?
2. **Primeiro gateway**: Pagar.me, Asaas ou Stripe? Pesa: Pix/boleto/cartão no mesmo lugar, taxa, qualidade da
   API e do sandbox, idempotência, webhooks assinados.
3. **Estud intermedia dinheiro algum dia?** Se sim (split, % da mensalidade), o desenho de `GatewayAccount` precisa
   prever subcontas desde a Fase 1, e é preciso consultoria jurídica/regulatória antes.
4. **Erro de feature fora do plano**: 402 Payment Required ou 403 com código próprio?
5. **Retenção de dados** após cancelamento e o que exatamente fica disponível em modo somente leitura.
6. **Schema separado** (`finance`) ou tudo em `estud`?
7. **Contrato acadêmico**: cobrança semestral/anual com parcelas (modelo escolar clássico) é suficiente, ou
   precisa também de cobrança por disciplina/crédito (modelo de algumas faculdades)? Muda o modelo de
   `PriceTable`.
8. **Quem emite a NFS-e da mensalidade**: o Estud, em nome da instituição (com certificado dela), ou deixar fora
   do escopo e só exportar os dados?
