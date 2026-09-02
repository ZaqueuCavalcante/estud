import { createSharedComposable } from '@vueuse/core'

export interface CurrentClassItem {
  id: number
  name: string
}

interface GetCurrentClassesOut {
  classes: CurrentClassItem[]
}

const _useCurrentClasses = () => {
  const config = useRuntimeConfig()
  const { can } = usePolicy()

  const canSeeTeacherClasses = can('GetTeacherCurrentClasses')
  const canSeeStudentClasses = can('GetStudentCurrentClasses')
  const canSeeClasses = computed(() => canSeeTeacherClasses.value || canSeeStudentClasses.value)

  const classes = ref<CurrentClassItem[]>([])

  async function fetchClasses() {
    if (!canSeeClasses.value) {
      classes.value = []
      return
    }
    const path = canSeeTeacherClasses.value ? 'teachers' : 'students'
    try {
      const data = await $fetch<GetCurrentClassesOut>(
        `${config.public.backendUrl}/${path}/current-classes`,
        { credentials: 'include' }
      )
      classes.value = data.classes
    } catch { /* ignore */ }
  }

  return { classes, canSeeClasses, fetchClasses }
}

export const useCurrentClasses = createSharedComposable(_useCurrentClasses)
