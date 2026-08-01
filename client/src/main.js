/**
 * ╔══════════════════════════════════════════════════════════════╗
 * ║  MAIN.JS — Điểm khởi đầu (Entry Point) của toàn bộ app     ║
 * ╚══════════════════════════════════════════════════════════════╝
 *
 * Đây là file đầu tiên được chạy khi browser load app.
 * Nhiệm vụ: Lắp ráp tất cả các mảnh ghép lại và "bật" app lên.
 *
 * Thứ tự thực thi:
 *   1. Import các thư viện và file cần thiết
 *   2. Tạo Vue app instance
 *   3. Cài các plugin (Router, Toast)
 *   4. Mount app vào trang HTML
 */

import { createApp } from 'vue'        // Hàm tạo Vue app — cốt lõi của Vue 3
import App from './App.vue'            // Root component (xem giải thích trong App.vue)
import router from './router'          // Cấu hình routing (xem router/index.js)
import Toast from 'vue-toastification' // Thư viện hiển thị thông báo nổi (toast)
import 'vue-toastification/dist/index.css' // CSS của Toast (bắt buộc phải import)
import './assets/main.css'             // CSS toàn app (màu sắc, layout, buttons,...)

// ── Bước 1: Tạo Vue app từ component gốc ─────────────────────────
// createApp(App) = "Xây ngôi nhà từ bản thiết kế App.vue"
const app = createApp(App)

// ── Bước 2: Cài Vue Router ────────────────────────────────────────
// router xử lý việc chuyển URL → hiển thị đúng trang
// Sau khi use(router), trong mọi component có thể dùng:
//   - useRouter()  → điều hướng (router.push('/vote/123'))
//   - useRoute()   → đọc URL hiện tại (route.params.code)
//   - <router-link> → thẻ <a> thông minh (không reload trang)
app.use(router)

// ── Bước 3: Cài Toast notification ───────────────────────────────
// Toast = thông báo nhỏ xuất hiện góc màn hình, tự biến mất
// Ví dụ: toast.success('Tạo poll thành công!') → hiện hộp xanh góc phải
// Sau khi use(Toast), trong mọi component có thể dùng:
//   const toast = useToast()
//   toast.success('...')  → thông báo xanh
//   toast.error('...')    → thông báo đỏ
app.use(Toast, {
  position: 'bottom-right', // Vị trí: góc dưới bên phải màn hình
  timeout: 2500,            // Tự động biến mất sau 2.5 giây
  closeOnClick: true,       // Click vào toast → đóng ngay
  pauseOnHover: true,       // Di chuột vào → tạm dừng đếm ngược
  draggable: false,         // Không cho kéo toast
  hideProgressBar: true,    // Ẩn thanh đếm ngược (cho gọn)
  closeButton: false,       // Ẩn nút X (đóng bằng cách click vào toast)
})

// ── Bước 4: Mount app vào HTML ────────────────────────────────────
// Tìm element có id="app" trong file index.html rồi đặt toàn bộ Vue app vào đó
// Sau lệnh này, App.vue bắt đầu render và user thấy được giao diện
//
// Trong index.html có:  <div id="app"></div>
//                                  ↑ Vue sẽ "chiếm" element này
app.mount('#app')
