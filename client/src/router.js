import { createRouter, createWebHistory } from 'vue-router'

import HomeView from './views/HomeView.vue'
import CreatePollView from './views/CreatePollView.vue'
import VoteView from './views/VoteView.vue'
import AnalyticsView from './views/AnalyticsView.vue'

const routes = [
  { path: '/', name: 'Home', component: HomeView },
  { path: '/create', name: 'CreatePoll', component: CreatePollView },
  { path: '/vote/:code?', name: 'Vote', component: VoteView },
  { path: '/analytics', name: 'Analytics', component: AnalyticsView },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

export default router
