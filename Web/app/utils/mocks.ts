// Mocks locais de resposta da API, pra testar telas sem depender do backend.
//
// Qualquer .json solto em `app/Mocks/` vira uma fonte de dados alternativa: os
// arquivos são descobertos automaticamente, então pra adicionar um cenário novo
// basta criar o arquivo (no mesmo shape que o endpoint devolve).
//
// Uso: abra a tela com `?mock=<nome-do-arquivo>` (sem o `.json`), ou só `?mock`
// pra pegar o primeiro em ordem alfabética.

// Glob relativo (e não via alias `~`) porque é o que o Vite resolve sem depender
// da config de alias. Lazy: os mocks só entram no bundle quando alguém pede.
const loaders = import.meta.glob('../Mocks/*.json')

const byName: Record<string, () => Promise<unknown>> = Object.fromEntries(
  Object.entries(loaders).map(([path, load]) => [
    path.split('/').pop()!.replace(/\.json$/, ''),
    load
  ])
)

/** Nomes dos mocks disponíveis, em ordem alfabética. */
export const mockNames: string[] = Object.keys(byName).sort()

/**
 * Traduz o valor de `?mock=` da URL no nome de um mock existente.
 * Retorna `null` quando não há mock a usar — aí a tela vai na API.
 */
export function resolveMockName(param: unknown): string | null {
  // Ausente na URL. (`?mock`, sem valor, chega como `null` — esse conta.)
  if (param === undefined) return null

  const value = Array.isArray(param) ? param[0] : param
  const name = typeof value === 'string' ? value.trim() : ''

  // `?mock`, `?mock=`, `?mock=1` e `?mock=true` pegam o primeiro disponível.
  if (name === '' || name === '1' || name === 'true') return mockNames[0] ?? null

  if (!byName[name]) {
    console.warn(`[mocks] "${name}" não existe em app/Mocks/. Disponíveis: ${mockNames.join(', ') || '(nenhum)'}`)
    return null
  }

  return name
}

/** Carrega o conteúdo de um mock pelo nome do arquivo (sem o `.json`). */
export async function loadMock<T>(name: string): Promise<T | null> {
  const load = byName[name]
  if (!load) return null

  const mod = await load() as T | { default: T }
  return (mod && typeof mod === 'object' && 'default' in mod ? mod.default : mod) as T
}
