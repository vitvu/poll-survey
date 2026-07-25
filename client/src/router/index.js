import { createRouter, createWebHistory } from 'vue-router';
import HomeView from '../views/HomeView.vue';
import CreatePollView from '../views/CreatePollView.vue';
import VoteView from '../views/VoteView.vue';
import AnalyticsView from '../views/AnalyticsView.vue';

const routes = [
  { path: '/',          name: 'Home',       component: HomeView,       meta: { title: 'PollBuilder' } },
  { path: '/create',    name: 'CreatePoll', component: CreatePollView, meta: { title: 'Tạo bình chọn' } },
  { path: '/vote/:code?', name: 'Vote',     component: VoteView,       meta: { title: 'Tham gia bình chọn' } },
  { path: '/analytics', name: 'Analytics',  component: AnalyticsView,  meta: { title: 'Kết quả & Quản lý' } },
  // Redirect cũ về analytics
  { path: '/result/:code', redirect: to => ({ name: 'Analytics', query: { code: to.params.code } }) },
  { path: '/:pathMatch(.*)*', redirect: '/' },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior() {
    return { top: 0 };
  }
});

router.beforeEach((to) => {
  document.title = to.meta.title || 'Poll Survey Builder';
  return true;
});

export default router;
