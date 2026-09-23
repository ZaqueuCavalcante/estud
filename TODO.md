# TODO

## Foco atual


## Próximo

- notificar quando um usuário for marcado em algum lugar (Basecamp like, o clique na notificação vai pro local da marcação)
- notificar aluno quando o professor adicionar nota de alguma atividade que ele entregou ou quando o professor comentar na entrega, mesmo sem mencao
- professor e apenas o professor pode marcar todo mundo da turma usando o atalho @turma

## Planejado

- notas parciais
- test containers
- elaboração de horários
- lista de alunos em risco
- coeficiente de rendimento
- header X-Estud-Signature nos webhooks
- webhook conditional send (custom rules)
- backoffice (eventos, comandos, auditoria)
- mural da turma com avisos e comentários da turma
- ajustar usos do DateTime.UtcNow e fusos horários
- guia de contribuição no github, padronização de issues e PRs
- colocar os nomes inteiros no breadcrumbs, apenas reduzir no mobile
- aluno e professor criados pelo gestor devem receber convite por email
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
