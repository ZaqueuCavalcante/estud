# TODO

- Exibir os detalhes de uma sala como agenda (GetClassroomController + tela)
    - Semana com dias, cada dia com turma
    - O importante eh ter claro os dados de ocupacao de tempo e de espaço
    - Pode ter formas mais interessantes de mostrar os dados

- Keycloak Testcontainer nos testes de SSO

- Ciclo de vida de uma turma

- Remover usos do GetDbContext


- ❌ GetStudentDetailsController (falta nota)
- Test cases novos no GetClassIntegrationTests.cs


- ❌ GetClassController (falta nota do aluno)
- ❌ GetTeacherClassStudentsController (falta nota)





O metodo deve lancar exceptions nos casos de valores/inputs claramente errados
Isso vai alertar o time rapido e deixar claro onde ta errado sem a gente conviver com um bug silecioso

- Weight negativo ou maior que 100 -> Exception
- Note negativo ou maior que 10 -> Exception
- A soma dos Weights em uma nota deu maior que 100 -> Exception

Tem mais algum?

Crie testes unitarios pra os casos de lancamento de Exception tbm



