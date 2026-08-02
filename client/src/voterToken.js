/**
 * VOTERTOKEN.JS — Định danh người dùng mà không cần đăng nhập
 *
 * ----------------------------------------------------------------
 * YÊU CẦU ĐỀ BÀI:
 *   "Each respondent can only vote once
 *    (enforce using browser fingerprint or session cookie
 *    — no login required for voters)"
 *
 * GIẢI PHÁP ĐÃ CHỌN: localStorage token
 * ----------------------------------------------------------------
 *
 * Cách hoạt động:
 *   1. Lần đầu tiên người dùng mở app → tạo một chuỗi ngẫu nhiên
 *      duy nhất (gọi là "voter token"), ví dụ: "voter_a3f8b2c1"
 *   2. Lưu chuỗi đó vào localStorage của trình duyệt
 *   3. Mỗi khi người dùng vote → gửi token này lên server cùng phiếu bầu
 *   4. Server kiểm tra: nếu token này đã vote poll đó rồi → từ chối
 *
 * So sánh với các cách khác trong đề:
 *
 *   - Session cookie: cần backend set cookie qua HTTP header,
 *     phải cấu hình CORS, SameSite, HttpOnly... phức tạp hơn nhiều
 *     so với bài tập nhóm này.
 *
 *   - Browser fingerprint: dùng thông tin trình duyệt (user-agent,
 *     canvas, fonts,...) để tạo ID — không ổn định, dễ bị trùng
 *     hoặc sai khi cùng một người dùng nhiều thiết bị.
 *
 *   - localStorage token: đơn giản, không cần đăng nhập,
 *     server vẫn kiểm tra độc lập → đúng yêu cầu "no login required".
 *
 * Hạn chế (chấp nhận được cho bài tập):
 *   - Người dùng xóa cache trình duyệt → mất token → có thể vote lại
 *   - Đổi trình duyệt / thiết bị khác → token khác → vote lại được
 *   → Đây là hạn chế chung của mọi giải pháp không có tài khoản.
 *
 * Dữ liệu lưu trong localStorage:
 *   poll_voter_token  = "voter_a3f8b2c1"   → ID định danh người dùng
 *   voted_123456      = "true"             → đã vote poll có code 123456
 *   createdPolls      = '["123456",...]'   → danh sách poll mình đã tạo
 */

/**
 * Trả về voter token của trình duyệt hiện tại.
 * Nếu chưa có thì tạo mới và lưu vào localStorage.
 *
 * @returns {string} Token dạng "voter_a3f8b2c1"
 */
export function getVoterToken() {

  // Thử đọc token đã lưu từ lần trước
  let token = localStorage.getItem('poll_voter_token')

  if (token === null) {
    // Chưa có token → tạo chuỗi ngẫu nhiên 8 chữ số
    let randomPart = ''
    for (let i = 0; i < 8; i++) {
      // Math.random() trả về số thập phân từ 0 đến 1, ví dụ: 0.7341...
      // nhân 10 → 7.341...
      // Math.floor() lấy phần nguyên → 7
      // Cộng vào chuỗi từng chữ số một
      randomPart += Math.floor(Math.random() * 10)
    }

    token = 'voter_' + randomPart  // Ví dụ: "voter_47291038"

    // Lưu vào localStorage để những lần sau dùng lại cùng token
    localStorage.setItem('poll_voter_token', token)
  }

  return token
}
