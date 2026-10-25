import '@fontsource/press-start-2p'
import { createPinia } from 'pinia'
import { createApp } from 'vue'
import App from './App.vue'
import './assets/styles/tokens.css'
import './assets/styles/base.css'
import { createAppRouter } from './router'

createApp(App).use(createPinia()).use(createAppRouter()).mount('#app')
