/**
 * ╔══════════════════════════════════════════════════════════════╗
 * ║  API.JS — Cầu nối giữa Frontend (Vue) và Backend (.NET)     ║
 * ╚══════════════════════════════════════════════════════════════╝
 *
 * File này dùng thư viện AXIOS để gọi HTTP request đến server.
 *
 * AXIOS là gì?
 * - Là thư viện JavaScript giúp gửi request lên server (giống fetch nhưng tiện hơn)
 * - Thay vì viết fetch('/api/polls/123') nhiều lần,
 *   ta tạo 1 instance có sẵn baseURL, header, timeout → gọi ngắn hơn
 *
 * Luồng request trong app này:
 *   Vue component → gọi pollApi.xxx() → axios gửi HTTP request
 *       → OcelotGateway (port 5000) → forward đến đúng service
 *       → PollService (port 5001) hoặc VoteService (port 5002)
 *       → trả response về → Vue cập nhật UI
 *
 *   Ví dụ: pollApi.checkPoll('123456')
 *     → GET https://localhost:5000/api/polls/check/123456
 *     → Gateway forward → PollService xử lý → trả về thông tin poll
 */

import axios from 'axios'

// ── Tạo axios instance với cấu hình mặc định ─────────────────────
const apiClient = axios.create({
  baseURL: 'https://localhost:5000', // Mọi request đều bắt đầu từ địa chỉ này (OcelotGateway)
  headers: { 'Content-Type': 'application/json' }, // Nói với server: "tôi gửi dữ liệu dạng JSON"
  timeout: 10000, // Nếu server không trả lời trong 10 giây → báo lỗi (tránh chờ mãi mãi)
})

// ── INTERCEPTOR: Bắt lỗi từ mọi response ─────────────────────────
//
// Interceptor = "lính gác" đứng giữa axios và component
// Mọi response trước khi về component đều đi qua đây
//
// Tại sao cần?
// - Không phải component nào cũng xử lý lỗi tốt
// - Ở đây ta chuẩn hóa error thành 1 object Error có .message rõ ràng
// - Component chỉ cần catch(e) → e.message là đủ
apiClient.interceptors.response.use(
  res => res, // Response thành công (status 2xx) → trả nguyên về component

  err => {
    // Response lỗi (status 4xx, 5xx) hoặc không kết nối được server
    // Ưu tiên lấy message từ backend, nếu không có thì dùng message của axios
    const msg = err.response?.data?.message  // Message từ backend (ví dụ: "Poll not found")
              || err.message                  // Message của axios (ví dụ: "timeout of 10000ms exceeded")
              || 'Server connection error.'   // Fallback nếu cả 2 đều không có

    // Trả về Promise rejected với Error object chuẩn
    // → Component dùng catch(e) → e.message là chuỗi có thể hiển thị cho user
    return Promise.reject(new Error(msg))
  }
)

// ── POLL API: Tập hợp tất cả hàm gọi API ─────────────────────────
//
// Mỗi hàm là 1 arrow function trả về Promise (axios luôn trả Promise)
// Cách dùng trong component:
//   const res = await pollApi.checkPoll('123456')
//   console.log(res.data) // dữ liệu từ server
//
// Cấu trúc URL (theo cấu hình OcelotGateway):
//   /api/polls/* → PollService (quản lý thông tin poll)
//   /api/votes/* → VoteService (quản lý phiếu bầu & kết quả)
export const pollApi = {

  // ── Poll APIs ─────────────────────────────────────────────────

  // Lấy toàn bộ thông tin poll (kèm danh sách options) — dùng ở AnalyticsView
  getPollByCode: code => apiClient.get(`/api/polls/code/${code}`),

  // Kiểm tra poll có tồn tại không (nhẹ hơn, không cần trả về options) — dùng ở HomeView, VoteView
  checkPoll: code => apiClient.get(`/api/polls/check/${code}`),

  // Tạo poll mới — dùng ở CreatePollView, gửi toàn bộ form data lên
  createPoll: data => apiClient.post('/api/polls', data),

  // Cập nhật poll (chủ yếu để đóng poll: status: 'Closed') — dùng ở AnalyticsView
  updatePoll: (id, data) => apiClient.put(`/api/polls/${id}`, data),

  // Xóa poll — dùng ở AnalyticsView khi bấm Delete
  deletePoll: id => apiClient.delete(`/api/polls/${id}`),

  // ── Vote APIs ─────────────────────────────────────────────────

  // Gửi vote của user — dùng ở VoteView khi bấm Submit
  // body gồm: { pollCode, voterToken, optionId, voteValue }
  submitVote: data => apiClient.post('/api/votes', data),

  // Lấy kết quả tổng hợp theo option (Multiple Choice, Yes/No)
  // Trả về: [{ optionId, count }, ...] — dùng ở AnalyticsView để vẽ bar chart
  getVoteResults: code => apiClient.get(`/api/votes/result/${code}`),

  // Lấy tổng số phiếu bầu — dùng ở AnalyticsView để hiển thị stat card
  getVoteTotal: code => apiClient.get(`/api/votes/total/${code}`),

  // Lấy danh sách từng phiếu (Rating/Open Text)
  // Trả về: [{ voteValue }, ...] — dùng ở AnalyticsView để tính avg rating hoặc hiển thị text responses
  getVoteList: code => apiClient.get(`/api/votes/list/${code}`),
}

export default apiClient
