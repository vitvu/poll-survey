/**
 * VOTERTOKEN.JS — Tạo ID ẩn cho người dùng (không cần đăng nhập)
 *
 * Vấn đề: App không có tài khoản, nhưng vẫn cần chặn vote nhiều lần.
 * Giải pháp: Tạo 1 chuỗi ngẫu nhiên, lưu vào localStorage của browser.
 *   Mỗi lần vote → gửi chuỗi này lên server → server biết ai đã vote.
 *
 * localStorage là gì?
 *   - Kho lưu trữ nhỏ ngay trong browser (không phải server)
 *   - Dữ liệu tồn tại cho đến khi user xóa cache
 *   - Chỉ lưu cặp key = value kiểu string
 *   - localStorage.setItem('key', 'value')  → lưu
 *   - localStorage.getItem('key')           → đọc
 *
 * Dữ liệu app này lưu trong localStorage:
 *   poll_voter_token  = "voter_xxxxxxxx"   → ID người dùng
 *   voted_123456      = "true"             → đã vote poll 123456
 *   createdPolls      = '["123456",...]'   → danh sách poll mình tạo
 *
 * Hạn chế: Xóa cache hoặc đổi browser → mất token → có thể vote lại.
 * Nhưng đủ tốt cho app đơn giản không cần bảo mật cao.
 */

/**
 * Lấy token của người dùng, tạo mới nếu chưa có.
 * Token là chuỗi ngẫu nhiên dạng: "voter_a3f8b2c1"
 */
export function getVoterToken() {
  // Đọc token đã lưu từ lần trước
  let token = localStorage.getItem('poll_voter_token')

  if (!token) {
    // Chưa có → tạo chuỗi ngẫu nhiên 8 ký tự
    // Cách tạo: lấy 8 chữ số ngẫu nhiên rồi ghép thành token
    let random = ''
    for (let i = 0; i < 8; i++) {
      random += Math.floor(Math.random() * 10) // Mỗi lần thêm 1 chữ số 0-9
    }
    token = 'voter_' + random

    localStorage.setItem('poll_voter_token', token) // Lưu lại để dùng lần sau
  }

  return token
}
