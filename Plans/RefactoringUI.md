# Refactoring UI, de Adam Wathan e Steve Schoger

Os autores são o criador do Tailwind CSS e um designer. O livro é um guia prático de design para desenvolvedores. Ele não trata de teoria artística. Traz táticas objetivas para deixar uma interface com cara profissional.

## 1. Começando do zero
- **Comece por uma funcionalidade, não pelo layout.** Não desenhe o "shell" (navbar, sidebar) primeiro. Desenhe a tela de "buscar voo", não "o app".
- **Detalhes depois.** Faça rascunhos em baixa fidelidade, sem se prender a fontes, sombras e ícones.
- **Não projete demais.** Entregue uma versão simples que funcione e itere em cima dela.
- **Defina uma personalidade.** Fonte, cor, raio de borda e tom do texto comunicam se o produto é sério, amigável, elegante etc.
- **Limite as escolhas com sistemas.** Tenha escalas fixas de espaçamento, tamanho de fonte, cores, sombras e raios. Escolher entre 5 opções é mais rápido e mais consistente que escolher entre infinitas.

## 2. Hierarquia é tudo
- Nem todo elemento tem a mesma importância, e o design precisa refletir isso.
- **Tamanho não é a única ferramenta.** Use também peso da fonte e cor: texto primário escuro, secundário cinza, terciário cinza mais claro.
- **Enfatize tirando ênfase do resto.** Se algo não se destaca, suavize o que está em volta em vez de aumentar esse elemento.
- **Labels são último recurso.** Em vez de "Email: joao@x.com", às vezes basta mostrar o dado, porque o formato já diz o que ele é.
- **Separe hierarquia visual de semântica.** Um `h1` não precisa ser enorme se não for o foco da tela.
- **Equilibre peso e contraste.** Ícones sólidos "pesam" mais que texto, então suavize a cor deles.
- **Ações por hierarquia.** A ação primária usa botão sólido, a secundária usa outline ou tom mais claro, e a terciária vira link. Ação destrutiva não precisa ser vermelha e chamativa se não for a principal da tela; o vermelho forte fica para a confirmação.

## 3. Layout e espaçamento
- **Comece com espaço demais** e vá reduzindo. O instinto de dev é apertar tudo.
- **Use uma escala de espaçamento** não linear (ex.: 4, 8, 12, 16, 24, 32, 48, 64…). Valores próximos demais geram indecisão.
- **Não precisa ocupar a tela toda.** Um formulário não precisa ter 1200px de largura só porque o monitor tem.
- **Grids são superestimados.** Nem tudo deve ser percentual. Sidebars costumam funcionar melhor com largura fixa.
- **Tamanhos relativos não escalam bem.** Um título grande no desktop não deve simplesmente encolher na mesma proporção no mobile.
- **Evite espaçamento ambíguo.** O espaço entre grupos deve ser maior que o espaço dentro de cada grupo, para que um label fique claramente mais perto do seu input que do anterior.

## 4. Tipografia
- **Escala tipográfica fixa**, definida à mão (ex.: 12, 14, 16, 18, 20, 24, 30, 36, 48…), em vez de razões matemáticas rígidas.
- **Boas fontes:** prefira fontes com vários pesos. Para UI, sans-serif neutra. Copie o que sites populares usam.
- **Comprimento de linha:** 45 a 75 caracteres por linha para leitura confortável.
- **Alinhe pela baseline** elementos de tamanhos diferentes na mesma linha.
- **Line-height é proporcional ao contexto:** texto pequeno e linhas longas pedem mais altura de linha; títulos grandes pedem menos.
- **Nem todo link precisa ser azul e sublinhado**, principalmente em áreas com muitos links.
- **Alinhamento:** texto à esquerda na maioria dos casos e números à direita em tabelas. Centralize só blocos curtos.
- **Letter-spacing:** reduza em títulos grandes e aumente em texto todo em maiúsculas.

## 5. Cores
- **Use HSL** em vez de hex ou RGB, porque é mais intuitivo ajustar matiz, saturação e luminosidade.
- **Você precisa de mais cores do que imagina:** cinzas (8 a 10 tons), cor primária (5 a 10 tons) e cores de apoio (vermelho, amarelo, verde) com vários tons cada.
- **Defina os tons antecipadamente:** escolha a cor base, o tom mais escuro e o mais claro, depois preencha o meio. É a lógica do 50 a 900 do Tailwind.
- **Ajuste a saturação ao clarear ou escurecer**, para a cor não ficar lavada.
- **Cinzas não precisam ser neutros.** Cinzas levemente azulados (frios) ou amarelados (quentes) têm mais personalidade.
- **Acessibilidade:** contraste mínimo de 4.5:1 para texto normal. Em fundo colorido, prefira inverter (texto escuro sobre fundo claro da mesma cor).
- **Não dependa só de cor** para transmitir informação. Use também ícones e texto, pensando em daltonismo.

## 6. Profundidade
- **Simule uma fonte de luz vinda de cima:** borda superior mais clara e sombra embaixo.
- **Sombras comunicam elevação.** Sombra pequena indica elemento próximo da superfície (botões); sombra grande indica elemento "flutuando" (modais). Monte uma escala de cerca de 5 sombras.
- **Sombras em duas camadas:** uma grande e difusa mais uma pequena e escura.
- **Profundidade também vem da cor:** elementos mais claros parecem mais próximos.
- **Sobreponha elementos** para criar camadas.

## 7. Imagens
- **Use fotos boas.** Foto ruim estraga o design, e vale pagar por foto profissional.
- **Texto sobre imagem precisa de contraste consistente:** overlay escuro, reduzir o contraste da imagem ou text-shadow sutil.
- **Não amplie ícones pequenos,** porque eles perdem proporção. Coloque-os dentro de uma forma com fundo.
- **Conteúdo enviado pelo usuário:** controle proporção (`object-fit`) e use sombra interna sutil para imagens com fundo parecido com o da página.

## 8. Toques finais
- **Turbine os padrões:** troque bullets por ícones, destaque citações com aspas grandes, estilize links com sublinhado customizado.
- **Borda colorida de destaque** (accent border) em cards, alertas ou no topo da página dá personalidade com esforço mínimo.
- **Decore fundos** com gradientes sutis, padrões ou formas.
- **Estados vazios merecem design,** porque são a primeira impressão do usuário.
- **Use menos bordas.** Para separar elementos, prefira sombra, cor de fundo diferente ou mais espaço.
- **Pense fora da caixa:** dropdowns podem ter colunas e ícones, tabelas podem combinar dados em células ricas, e radio buttons podem virar cards selecionáveis.

## 9. Evoluindo
- Repare em decisões que você não tomaria em designs que admira e pergunte por que funcionam.
- Recrie interfaces que você gosta. Copiar é a forma mais rápida de aprender.

---

**Resumo:** trabalhe com **sistemas** (escalas de espaço, fonte, cor e sombra), crie **hierarquia** com peso e cor e não só com tamanho, use **mais espaço** do que acha necessário e **menos bordas** do que está acostumado.
