# TODO

## Entrega de valor mais simples possível

- Criar campus com sala
- Criar disciplina, período acadêmico e turma
- Dentro da turma, definir horário e sala

Com esses dados já é possível montar o mapa de ocupação do campus.
É possível criar qualquer variação de dados pra testar o mapa? Apenas responda n altere nada ainda.





Manhã 06h–12h

Tarde 12h–18h

Noite 18h–24h


- permitir apenas um range de funcionamento por turno

- cada sala vai ter, para cada turno, dentro de cada range, seus horarios
    - dado um dia e um turno, todas as salas vao ter o mesmo range
    - basta representar em cada sala quanto desse range elas estao ocupando (horizontalmente)
    - para cada sala, na direita, pode ter um termometro que indica a ocupacao de cadeiras/assentos de cada sala no range do dia e turno selecionado

- nos detalhes de uma sala
    - exibir agenda completa da sala, com todos os horarios e ocupacoes de assentos



agora altere o frontend, a tela de detalhes de um campus, tab ocupacao
quando um celula (dia+turno) for selecionada, deve mostrar os cards com as salas
em cada card de sala deve mostrar duas informacoes:
    - porcentagem de tempo (UsedMinutesRate) que a sala esta ocupada no turno selecionado
    - porcentagem de assentos ocupados (UsedCapacityRate) da sala no turno selecionado

o UsedMinutesRate deve ser mostrado num componente novo, parecido com o ClassesRingStat, vai ser o ClassroomUsedMinutesRingStat
no meio dele vai ter dois ponteiros, um grande de minutos e um menor de horas
a ideia eh que o de minutos sempre fique na vertical pra cima
e o de horas fique no ponto do valor do UsedMinutesRate (se UsedMinutesRate=25% o ponteiro fica apontando pra direita, por exemplo)

abaixo do ClassroomUsedMinutesRingStat vai ter a representacao do valor UsedCapacityRate
pra ele, siga a ideia que ja existe hoje, use os 10 quadradinhos onde cada um representa 10%

