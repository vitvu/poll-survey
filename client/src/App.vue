<!--
  ╔══════════════════════════════════════════════════════════════╗
  ║  APP.VUE — Root Component (Component gốc của toàn bộ app)   ║
  ╚══════════════════════════════════════════════════════════════╝

  File này là "khung xương" của cả ứng dụng Vue.
  Mọi trang (HomeView, VoteView, ...) đều được RENDER BÊN TRONG file này.

  Cấu trúc DOM thực tế khi app chạy:
  ┌─────────────────────────────┐
  │ <div id="app">              │  ← được tạo bởi main.js → app.mount('#app')
  │   <div class="app-wrap">   │  ← wrapper chính (min-height: 100vh, flex)
  │     <main class="page-wrap"> │  ← vùng nội dung chính (có padding)
  │       [NỘI DUNG TRANG]     │  ← HomeView / VoteView / ... thay nhau hiển thị ở đây
  │     </main>                │
  │   </div>                   │
  │ </div>                     │
  └─────────────────────────────┘

  Giải thích các thành phần:

  1. <router-view> — "Cửa sổ hiển thị trang"
     - Đây là nơi Vue Router đặt component tương ứng với URL hiện tại
     - URL = "/"         → render HomeView.vue
     - URL = "/create"   → render CreatePollView.vue
     - URL = "/vote/123" → render VoteView.vue
     - URL = "/analytics"→ render AnalyticsView.vue
     - Khi user click link hay router.push() → nội dung ở đây thay đổi,
       còn App.vue giữ nguyên (không reload toàn trang như website truyền thống)

  2. v-slot="{ Component }" — Lấy component hiện tại từ router để wrap transition
     - Cú pháp scoped slot của Vue 3
     - Cho phép dùng <component :is="Component" /> bên trong transition

  3. <transition name="fade" mode="out-in"> — Hiệu ứng chuyển trang
     - Khi chuyển từ trang này sang trang khác có animation mờ dần
     - name="fade" → dùng class CSS .fade-enter-active / .fade-leave-to (định nghĩa trong main.css)
     - mode="out-in" → trang cũ fade out XONG rồi trang mới mới fade in
       (nếu không có mode, 2 trang sẽ chồng lên nhau trong lúc transition)

  4. <component :is="Component" /> — Render component động
     - :is nhận tên component hoặc component object
     - Tương đương với việc đặt <HomeView /> hay <VoteView /> nhưng động theo route

  TẠI SAO FILE NÀY "KHÔNG CÓ GÌ" MÀ VẪN QUAN TRỌNG?
  - Đây là "shell" (vỏ bọc) cho toàn bộ app
  - Nếu muốn thêm Navbar, Footer dùng chung cho tất cả trang → thêm vào đây
  - Mọi CSS global (như .app-wrap, .page-wrap) được áp dụng từ đây
  - Không có file này thì không có app
-->

<template>
  <!-- Wrapper bao ngoài toàn bộ app — CSS: min-height 100vh, flex column -->
  <div class="min-h-screen flex flex-col">

    <!-- Vùng nội dung chính — CSS: có padding top/bottom, flex: 1 để chiếm hết chiều cao -->
    <main class="page-wrap">

      <!--
        router-view: "Ổ cắm" để Vue Router cắm trang vào đây
        Khi URL thay đổi → component bên trong tự động thay đổi theo
        
        v-slot="{ Component }": lấy ra component hiện tại để truyền vào transition
      -->
      <router-view v-slot="{ Component }">

        <!--
          Hiệu ứng fade khi chuyển trang
          - name="fade"     → tìm CSS class .fade-enter-active, .fade-leave-to trong main.css
          - mode="out-in"   → trang cũ biến mất trước, trang mới xuất hiện sau
        -->
        <transition name="fade" mode="out-in">
          <!-- Render component tương ứng với route hiện tại -->
          <component :is="Component" />
        </transition>

      </router-view>
    </main>

  </div>
</template>
