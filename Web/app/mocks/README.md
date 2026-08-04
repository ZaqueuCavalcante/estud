# Mocks

TODO: Remove this mocks folder after tests

Respostas de API gravadas em JSON, pra testar telas sem depender do backend.

Cada arquivo `.json` desta pasta tem exatamente o shape que o endpoint devolve.
Os arquivos são descobertos automaticamente (`app/utils/mocks.ts`), então pra
adicionar um cenário novo basta criar o arquivo aqui — não precisa registrar
nada em lugar nenhum.

## Como usar

Abra a tela com o nome do arquivo (sem o `.json`) na query string:

```
/campi/1?mock=campus-occupancy
/campi/1?mock=campus-occupancy-vazio
/campi/1?mock=campus-occupancy-diurno
```

`?mock` sozinho (sem valor) pega o primeiro arquivo em ordem alfabética. Sem o
parâmetro, a tela vai na API normalmente.

## Cenários atuais

| Arquivo | Endpoint | Cenário |
|---|---|---|
| `campus-occupancy.json` | `GET /campi/{id}/occupancy` | Campus com 6 salas e ocupação variada ao longo da semana |
| `campus-occupancy-vazio.json` | `GET /campi/{id}/occupancy` | Campus sem nenhuma sala cadastrada |
| `campus-occupancy-diurno.json` | `GET /campi/{id}/occupancy` | Campus que abre só de manhã, de segunda a sexta: 5 células abertas e 13 fechadas |
