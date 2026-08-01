/**
 * ROUTER — Điều hướng giữa các trang
 *
 * Vue Router là gì?
 *   App này chỉ có 1 file HTML duy nhất (index.html).
 *   Router "giả lập" chuyển trang bằng cách ẩn/hiện component theo URL,
 *   không reload lại trang như website truyền thống.
 *
 * Các trang:
 *   /              → HomeView.vue       (trang chủ)
 *   /create        → CreatePollView.vue (tạo poll)
 *   /vote/123456   → VoteView.vue       (bỏ phiếu — code lấy từ URL)
 *   /vote          → VoteView.vue       (bỏ phiếu — không có code, hiện form nhập)
 *   /analytics     → AnalyticsView.vue  (xem kết quả)
 *   /* (khác)      → redirect về /
 */

import { createRouter, createWebHistory } from 'vue-router'

const routes = [
  {
    path: '/',
    name: 'Home',
    // Lazy loading: chỉ tải file khi user thực sự vào trang này
    // Giúp app khởi động nhanh hơn vì không tải hết mọi thứ ngay từ đầu
    component: () => import('../views/HomeView.vue'),
    meta: { title: 'PollBuilder' }, // Tiêu đề tab trình duyệt
  },
  {
    path: '/create',
    name: 'CreatePoll',
    component: () => import('../views/CreatePollView.vue'),
    meta: { title: 'Create Poll' },
  },
  {
    // :code?  → tham số tùy chọn trong URL
    // /vote/123456 → code = "123456"
    // /vote        → code = undefined → VoteView hiện form nhập code
    path: '/vote/:code?',
    name: 'Vote',
    component: () => import('../views/VoteView.vue'),
    meta: { title: 'Vote' },
  },
  {
    // Query string: /analytics?code=123456
    // Đọc trong component bằng: route.query.code
    path: '/analytics',
    name: 'Analytics',
    component: () => import('../views/AnalyticsView.vue'),
    meta: { title: 'Analytics & Results' },
  },
  {
    // Bắt tất cả URL không khớp → về trang chủ
    path: '/:pathMatch(.*)*',
    redirect: '/',
  },
]

const router = createRouter({
  // createWebHistory: URL dạng /vote/123 (không có dấu #)
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  // Cuộn về đầu trang mỗi khi chuyển trang
  scrollBehavior() {
    return { top: 0 }
  },
})

// Chạy trước mỗi lần chuyển trang → cập nhật tiêu đề tab trình duyệt
router.beforeEach(to => {
  document.title = to.meta.title || 'Poll Survey'
})

export default router
