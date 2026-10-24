import { ViteSSG } from 'vite-ssg'
import { createPinia } from 'pinia'
import App from './App.vue'
import { routes } from './router/routes'
import '@fontsource/press-start-2p'
import './assets/styles/tokens.css'
import './assets/styles/base.css'

export const createApp = ViteSSG(App, { routes }, ({ app, initialState }) => {
  const pinia = createPinia()
  app.use(pinia)
  if (import.meta.env.SSR) {
    initialState.pinia = pinia.state.value
  } else {
    pinia.state.value = initialState.pinia || {}
  }
})
