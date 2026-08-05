import type { GetCampusOpeningHoursOut } from '~/types/campi'

/**
 * Horários de funcionamento de um campus (GET /campi/{id}/opening-hours).
 *
 * A API sempre devolve os 6 dias da semana — dia com `windows` vazia é dia em
 * que o campus não abre.
 */
export function useCampusOpeningHours(campusId: number) {
  const config = useRuntimeConfig()

  const { data, status, error, refresh } = useAsyncData<GetCampusOpeningHoursOut | null>(
    `campus-opening-hours-${campusId}`,
    async () => await $fetch<GetCampusOpeningHoursOut>(
      `${config.public.backendUrl}/campi/${campusId}/opening-hours`,
      { credentials: 'include' },
    ),
    { server: false },
  )

  // `server: false` deixa o status em 'idle' até o onMounted disparar a busca,
  // então o carregando cobre os dois estados. Já com dado em tela o refresh é
  // silencioso — trocar a semana por um spinner a cada volta na aba só pisca.
  const loading = computed(() =>
    (status.value === 'idle' || status.value === 'pending') && data.value === null,
  )

  return { openingHours: data, status, loading, error, refresh }
}
