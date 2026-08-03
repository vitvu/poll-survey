import { createRouter, createWebHistory } from 'vue-router'

const routes = [
  {
    path: '/',
    name: 'Home',
    component: () => import('../views/HomeView.vue'),
    meta: { title: 'PollBuilder' },
  },
  {
    path: '/create',
    name: 'CreatePoll',
    component: () => import('../views/CreatePollView.vue'),
    meta: { title: 'Create Poll' },
  },
  {
    path: '/vote/:code?',
    name: 'Vote',
    component: () => import('../views/VoteView.vue'),
    meta: { title: 'Vote' },
  },
  {
    path: '/analytics',
    name: 'Analytics',
    component: () => import('../views/AnalyticsView.vue'),
    meta: { title: 'Analytics & Results' },
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/',
  },
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  scrollBehavior() {
    return { top: 0 }
  },
})

router.beforeEach(to => {
  document.title = to.meta.title || 'Poll Survey'
})

export default router
