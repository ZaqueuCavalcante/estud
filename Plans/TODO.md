# TODO

## Responder

- O que o Estud faz?
- Por que o Estud é importante?

## Foco atual

- 

## Próximo

- aluno precisa ver sua frequencia em cada turma (hoje ele so ve as notas)
- notificar quando um usuário for marcado em algum lugar (Basecamp like, o clique na notificação vai pro local da marcação)
- notificar aluno quando o professor adicionar nota de alguma atividade que ele entregou ou quando o professor comentar na entrega, mesmo sem mencao
- professor e apenas o professor pode marcar todo mundo da turma usando o atalho @turma

## Planejado

- notas parciais
- diário de classe
- Diploma Digital (MEC)
- elaboração de horários
- lista de alunos em risco
- coeficiente de rendimento
- importação de dados em massa
- refatorar queries para Dapper
- SSE vs polling for notifications
- histórico escolar e diploma digital
- header X-Estud-Signature nos webhooks
- webhook conditional send (custom rules)
- backoffice (eventos, comandos, auditoria)
- mural da turma com avisos e comentários da turma
- ajustar usos do DateTime.UtcNow e fusos horários
- Censo da Educação Superior e Censo Escolar (INEP)
- Assinatura eletrônica (Clicksign, D4Sign, ZapSign)
- onboarding guiado (checklist no canto inferior direito)
- guia de contribuição no github, padronização de issues e PRs
- ClamAV ou VirusTotal pra encontrar virus nos PDFs dos enviados
- visão de opções/conflitos na hora de definir horários (professor e sala)

---------------------------------------------------------------------------------------------------

### Dada uma turma com 3 alunos (Ana, Bia e Carlos)

- Professor faz a chamada das 4 primeiras aulas
    - Ana vai em todas, 100% de frequência
    - Bia falta 1 dia, 70% de frequência
    - Carlos só foi dia, 25% de frequência

### Dada uma turma com 3 alunos (Ana, Bia e Carlos)

- Professor cria a primeira atividade da turma (um trabalho), valendo 40% da N1
    - Ana tira 10
    - Bia tira 6
    - Carlos não entrega o trabalho

- Resultados esperados
    - Aproveitamento dos 3 alunos deve ser diferente
    - Nota final dos 3 alunos deve ser diferente

---------------------------------------------------------------------------------------------------

## Caso de Uso de Ponta a Ponta

- ✅ Registrar no sistema

- ✅ Criar campus e salas

- ✅ Criar cursos, disciplinas e seus vínculos

- ✅ Montar as grades curriculares

- ✅ Ofertar cursos

- Criar professores e alunos

- Matricular alunos nas ofertas de curso

- Vincular professores com campus e disciplinas

- Abrir turmas

- Definir horários, professores e salas das turmas

- Lidar com os conflitos de horários (professores, salas e alunos)

- Matricular alunos nas turmas

- Professor numa turma
    - Criar planos de aula
    - Faz chamadas
    - Passa atividades
    - Corrige atividades e atribui notas

- Aluno numa turma
    - Vai pras aulas (frequência)
    - Entrega atividades
    - Recebe correções e notas

- Ao final do semestre as turmas são finalizadas e os alunos aprovados/reprovados

- Isso se repete até o aluno finalizar o curso e receber seu certificado/diploma
