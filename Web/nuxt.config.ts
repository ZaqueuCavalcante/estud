// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  runtimeConfig: {
    // Server-only. Usada nas chamadas SSR ao backend (ex.: checagem de auth da landing).
    // Se vazia, cai no backendUrl público. Aponte para a URL interna (ex.: rede privada
    // da Railway) para reduzir a latência da checagem no caminho do usuário logado.
    internalBackendUrl: "",
    public: {
      backendUrl: "",
    },
  },

  modules: [
    "@nuxt/ui",
    "@nuxt/fonts",
    "@nuxt/eslint",
    "@vueuse/nuxt",
    "@nuxt/content",
    "@posthog/nuxt",
    "@nuxtjs/sitemap",
  ],

  site: {
    url: "https://estud.com.br",
    name: "Estud - Gestão Educacional",
  },

  sitemap: {
    include: [
      "/",
      "/docs",
      "/docs/**",
      "/contact",
      "/privacy-policy",
      "/terms-of-service",
    ],
  },

  posthogConfig: {
    publicKey: "",
    host: "https://us.i.posthog.com",
    clientConfig: {
      defaults: "2026-06-25",
      person_profiles: "identified_only",
      mask_all_text: true,
      capture_exceptions: true,
    },
    serverConfig: {
      enableExceptionAutocapture: true,
    },
  },

  app: {
    head: {
      title: 'Estud',
      htmlAttrs: { lang: 'pt-BR' },
      meta: [
        { name: 'description', content: 'Organize sua instituição de ensino com excelência!' },
        { name: 'robots', content: 'noindex, nofollow' },
        { property: 'og:site_name', content: 'Estud' },
        { property: 'og:locale', content: 'pt_BR' },
        { property: 'og:type', content: 'website' },
        { property: 'og:title', content: 'Estud - Gestão Educacional' },
        { property: 'og:description', content: 'Organize sua instituição de ensino com excelência!' },
        { property: 'og:image', content: 'https://estud.com.br/images/campus-preview.png' },
        { property: 'og:image:type', content: 'image/png' },
        { property: 'og:image:width', content: '2400' },
        { property: 'og:image:height', content: '1260' },
        { property: 'og:image:alt', content: 'Tela de ocupação de campus do Estud: mapa de uso das salas por dia e turno' },
        { name: 'twitter:card', content: 'summary_large_image' },
        { name: 'twitter:title', content: 'Estud - Gestão Educacional' },
        { name: 'twitter:description', content: 'Organize sua instituição de ensino com excelência!' },
        { name: 'twitter:image', content: 'https://estud.com.br/images/campus-preview.png' },
        { name: 'twitter:image:alt', content: 'Tela de ocupação de campus do Estud: mapa de uso das salas por dia e turno' }
      ]
    }
  },

  content: {
    build: {
      markdown: {
        toc: { depth: 3, searchDepth: 3 },
      },
    },
  },

  devtools: {
    enabled: true,
  },

  css: ["~/assets/css/main.css"],

  sourcemap: {
    server: false,
    client: false,
  },

  routeRules: {
    "/api/**": {
      cors: true,
    },
  },

  vite: {
    optimizeDeps: {
      include: [
        "zod",
        "date-fns",
        "@unovis/vue",
        "@internationalized/date",
      ],
    },
  },

  compatibilityDate: "2024-07-11",

  eslint: {
    config: {
      stylistic: {
        braceStyle: "1tbs",
        commaDangle: "never",
      },
    },
  },

  fonts: {
    families: [
      { name: "Saira", provider: "google", weights: [300, 400, 500, 600, 700, 800] },
      {
        name: "Saira Condensed",
        provider: "google",
        weights: [300, 400, 500, 700],
      },
      { name: "JetBrains Mono", provider: "google", weights: [400, 600] },
    ],
  },
});
