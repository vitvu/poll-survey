import axios from 'axios'

// ──────────────────────────────────────────────────────────────
// API Client cho Poll & Vote Services
// Base URL: API Gateway (http://localhost:5000)
// ──────────────────────────────────────────────────────────────

const API_GATEWAY_URL = process.env.VUE_APP_API_BASE_URL || 'http://localhost:5000'

// Tạo Axios instance với default configuration
const apiClient = axios.create({
  baseURL: API_GATEWAY_URL,
  headers: {
    'Content-Type': 'application/json'
  },
  timeout: 10000  // 10 giây timeout cho mỗi request
})

// ──────────────────────────────────────────────────────────────
// POLL API FUNCTIONS
// ──────────────────────────────────────────────────────────────

/**
 * Lấy thông tin poll từ code
 * @param {string} pollCode - 8-digit poll code (ví dụ: "12345678")
 * @returns {Promise<Object>} Poll object chứa question, options, status
 */
export async function getPollByCode(pollCode) {
  const response = await apiClient.get(`/api/polls/code/${pollCode}`)
  return response.data
}

/**
 * Tạo poll mới
 * @param {Object} pollData - {question, questionType, options}
 *   - question (string): Nội dung câu hỏi
 *   - questionType (number): 1=Multiple Choice, 2=Yes/No, 3=Star Rating, 4=Open Text
 *   - options (Array): [{text: "..."}, ...] for type 1 only
 * @returns {Promise<Object>} Newly created poll với poll code
 */
export async function createPoll(pollData) {
  const response = await apiClient.post('/api/polls', pollData)
  return response.data
}

/**
 * Cập nhật thông tin poll (chủ yếu dùng để đóng poll)
 * @param {string} pollCode - 8-digit poll code
 * @param {Object} pollData - {question, status}
 *   - status: 0=active, 1=closed
 * @returns {Promise<void>}
 */
export async function updatePoll(pollCode, pollData) {
  const response = await apiClient.put(`/api/polls/code/${pollCode}`, pollData)
  return response.data
}

/**
 * Xóa poll cùng tất cả votes liên quan
 * @param {string} pollCode - 8-digit poll code
 * @returns {Promise<void>}
 */
export async function deletePoll(pollCode) {
  const response = await apiClient.delete(`/api/polls/code/${pollCode}`)
  return response.data
}

// ──────────────────────────────────────────────────────────────
// VOTE API FUNCTIONS
// ──────────────────────────────────────────────────────────────

/**
 * Gửi một phiếu bình chọn
 * @param {Object} voteData - {pollCode, optionId, voteValue, voterToken}
 *   - pollCode (string): 8-digit poll code
 *   - optionId (number): ID của option được chọn (for Multiple Choice)
 *   - voteValue (string): Giá trị vote (for Star Rating: "1-5", for Open Text: text content)
 *   - voterToken (string): Browser fingerprint để prevent duplicate votes
 * @returns {Promise<Object>} Vote confirmation
 */
export async function submitVote(voteData) {
  const response = await apiClient.post('/api/votes', voteData)
  return response.data
}

/**
 * Lấy thống kê vote cho một poll
 * @param {string} pollCode - 8-digit poll code
 * @returns {Promise<Object>} Vote analytics {pollCode, total, summary, votes}
 */
export async function getVoteData(pollCode) {
  const response = await apiClient.get(`/api/votes/${pollCode}`)
  return response.data
}

/**
 * Xóa tất cả votes cho một poll (dùng khi poll bị xóa)
 * @param {string} pollCode - 8-digit poll code
 * @returns {Promise<void>}
 */
export async function deleteVotes(pollCode) {
  const response = await apiClient.delete('/api/votes', {
    params: { pollCode }
  })
  return response.data
}

// ──────────────────────────────────────────────────────────────
// EXPORT: API client instance (advanced usage)
// ──────────────────────────────────────────────────────────────
export default apiClient
