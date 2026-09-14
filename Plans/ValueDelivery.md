# Value Delivery

Principais entregas de valor do Estud para cada perfil de usuário.

Legenda: ✅ já existe · 🟡 existe pela metade · 🔲 pode trazer

## Gestor

| | Entrega | Valor |
|---|---|---|
| ✅ | Estrutura acadêmica: cursos, disciplinas, grades curriculares, ofertas e turmas | Toda a oferta da instituição num lugar só, sem planilha |
| ✅ | Horários com detecção de conflito de professor e de sala | Acaba o retrabalho de todo semestre montando horário |
| ✅ | Calendário institucional (feriados e recessos) que gera as aulas e a carga horária | Aula e carga horária saem certas sem conta manual |
| ✅ | Mapa de ocupação do campus: uso de tempo e de capacidade por sala e turno | Base para decidir abrir turma, liberar sala ou alugar espaço |
| ✅ | Regras de aprovação configuráveis: nota mínima, frequência mínima e regra de média | Cada instituição usa o próprio regimento |
| ✅ | Ciclo da turma (pré-matrícula → matrícula → iniciada) e períodos de matrícula | Controle do que pode ser editado em cada fase |
| ✅ | Visão da turma com média e frequência de cada aluno | Acompanhamento sem pedir planilha ao professor |
| ✅ | Perfis e permissões, SSO, 2FA obrigatório e trilha de auditoria | Segurança e resposta clara a "quem mudou isso?" |
| ✅ | API e webhooks | Integra com os sistemas que a instituição já usa |
| 🟡 | Fechamento de turma | Hoje só muda o status; não define aprovado ou reprovado |
| 🔲 | Horário gerado automaticamente (o `TimetablingSolver` existe, mas nenhuma feature usa) | Monta o semestre em minutos, não em semanas |
| 🔲 | Alunos em risco: frequência ou média projetada abaixo do limite | Agir antes da reprovação e da evasão |
| 🔲 | Indicadores acionáveis: aprovação por disciplina, evasão, carga dos professores | Decisão com dado, não só contagem |
| 🔲 | Importação por planilha | Adoção em dias, não em meses |
| 🔲 | Documentos da secretaria: histórico, declarações, diploma digital, Censo | Tira a secretaria do trabalho manual e cumpre o que o MEC exige |
| 🔲 | Rematrícula online | Menos fila e menos trabalho da secretaria no início do semestre |
| 🔲 | Financeiro, via integração com um gateway de pagamento | É o que mais pesa na decisão de compra |

## Professor

| | Entrega | Valor |
|---|---|---|
| ✅ | Agenda com todas as aulas | Sabe onde e quando dá aula, sem consultar a secretaria |
| ✅ | Aulas já geradas pelo calendário | Nada para montar no início do semestre |
| ✅ | Turmas e alunos com média e frequência de cada um | Situação da turma num olhar |
| ✅ | Chamada por aula (aula futura fica bloqueada) | Frequência registrada e calculada automaticamente |
| ✅ | Atividades com tipo de nota (N1, N2, N3), peso e prazo; entrega criada para cada aluno | Controle das entregas sem ferramenta paralela |
| ✅ | Lançamento de nota com média calculada pela regra da instituição | Sem planilha e sem conta de média |
| 🔲 | Registro do conteúdo dado em cada aula (hoje a chamada guarda só os presentes) | Diário de classe completo, com validade legal |
| 🔲 | Nota lançada já vale como oficial (depende do fechamento de turma) | Lança uma vez, sem redigitar em outro sistema |
| 🔲 | Chamada rápida no celular, funcionando mesmo com Wi-Fi ruim | Chamada em segundos, dentro da sala |
| 🔲 | Entrega com arquivo e com correção ou comentário | Substitui de verdade o Google Classroom |
| 🔲 | Alerta de aluno em risco na turma | Intervir enquanto ainda dá tempo |
| 🔲 | Plano de ensino e cronograma | Planejamento e execução no mesmo lugar |

## Aluno

| | Entrega | Valor |
|---|---|---|
| ✅ | Agenda de aulas | Rotina organizada |
| ✅ | Turmas atuais com horário, sala e professores | Sabe para onde ir |
| ✅ | Atividades com prazo, peso e nota recebida; entrega por link | Sabe o que entregar e quanto valeu |
| ✅ | Calendário de frequência dia a dia | Enxerga as próprias faltas |
| ✅ | Comprovante de matrícula em PDF com validação pública | Estágio, transporte e meia-entrada sem ir à secretaria |
| ✅ | Detalhes do curso: grade, disciplinas por período, créditos e carga horária | Entende o caminho do curso |
| ✅ | Notificações dentro do sistema | Comunicados da instituição |
| 🟡 | Situação por disciplina no histórico | Hoje aparece sempre "Cursando" |
| 🟡 | Coeficiente de rendimento (CR) | Hoje é sempre 0 |
| 🔲 | Média atual e percentual de frequência por turma (o professor vê, o aluno não) | "Como estou?" respondido na hora |
| 🔲 | Simulador: quanto precisa tirar na N3 e quantas faltas ainda pode ter | O que o aluno mais quer saber, e os dados já existem |
| 🔲 | Lembrete de prazo por push ou e-mail | Não perder entrega |
| 🔲 | Progresso no curso: quanto já cursou e o que falta | Planejar a formatura |
| 🔲 | Pedido de documentos: histórico e declarações | Autoatendimento, sem fila |
| 🔲 | Rematrícula online e escolha de disciplinas | Rematrícula pelo celular |

As entregas 🔲 que dão mais retorno pelo esforço são as que usam dados que o sistema já calcula: a média e a frequência para o aluno, o simulador e os alunos em risco.
