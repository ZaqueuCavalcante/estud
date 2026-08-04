import type { GetCampusOccupancyOut } from '~/types/campi'

/**
 * Ocupação de um campus (GET /campi/{id}/occupancy).
 *
 * Com `?mock=<arquivo>` na URL os dados saem de `app/Mocks/` em vez da API —
 * veja `~/utils/mocks`.
 */
export function useCampusOccupancy(campusId: number) {
  const route = useRoute()
  const config = useRuntimeConfig()

  const mock = computed(() => resolveMockName(route.query.mock))

  const { data, status, error, refresh } = useAsyncData<GetCampusOccupancyOut | null>(
    `campus-occupancy-${campusId}`,
    async () => {
      if (mock.value) return await loadMock<GetCampusOccupancyOut>(mock.value)

      return await $fetch<GetCampusOccupancyOut>(
        `${config.public.backendUrl}/campi/${campusId}/occupancy`,
        { credentials: 'include' }
      )
    },
    { watch: [mock], server: false }
  )

  // `server: false` deixa o status em 'idle' até o onMounted disparar a busca,
  // então o carregando cobre os dois estados.
  const loading = computed(() => status.value === 'idle' || status.value === 'pending')

  return { occupancy: data, status, loading, error, refresh, mock, mockNames }
}
