import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import Toast from 'vue-toastification'
import 'vue-toastification/dist/index.css'
import './assets/main.css'

const app = createApp(App)

app.use(router)

app.use(Toast, {
  position: 'bottom-right',
  timeout: 2500,
  closeOnClick: true,
  pauseOnHover: true,
  draggable: false,
  hideProgressBar: true,
  closeButton: false,
})

app.mount('#app')
