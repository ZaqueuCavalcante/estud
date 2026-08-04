export default defineAppConfig({
  ui: {
    colors: {
      primary: 'violet',
      neutral: 'zinc'
    },
    dashboardPanel: {
      slots: {
        body: 'flex flex-col gap-4 sm:gap-6 flex-1 overflow-y-auto px-4 py-4 sm:p-6'
      }
    },
    // No mobile o ícone do primeiro item só rouba largura de um breadcrumb que
    // já está apertado — o rótulo sozinho diz a mesma coisa.
    breadcrumb: {
      slots: {
        linkLeadingIcon: 'hidden lg:block'
      }
    },
    // O botão de fechar é `absolute top-4 end-4` e nada reservava espaço pra
    // ele: título longo passava por baixo do X. O espaço vai no `wrapper`, e
    // não no `header`, porque o wrapper só é renderizado no header padrão —
    // assim quem substitui o slot `#header` e traz o próprio botão de fechar
    // não herda um padding fantasma. O `min-w-0` deixa o texto quebrar linha
    // em vez de estourar.
    modal: {
      slots: {
        wrapper: 'min-w-0 flex-1 pe-8',
        title: 'break-words'
      }
    },
    slideover: {
      slots: {
        wrapper: 'min-w-0 flex-1 pe-8',
        title: 'break-words'
      }
    }
  }
})
