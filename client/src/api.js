import axios from 'axios'

const apiClient = axios.create({
  baseURL: process.env.VUE_APP_API_BASE_URL || 'http://localhost:5000',
  headers: { 'Content-Type': 'application/json' },
  timeout: 10000,
})

// intercept errors and extract message
apiClient.interceptors.response.use(
  res => res,
  err => {
    const msg = err.response?.data?.message
              || err.message
              || 'Server connection error.'
    return Promise.reject(new Error(msg))
  }
)

export const pollApi = {
  getPollByCode: code => apiClient.get(`/api/polls/code/${code}`),
  checkPoll: code => apiClient.get(`/api/polls/check/${code}`),
  createPoll: data => apiClient.post('/api/polls', data),
  updatePoll: (code, data) => apiClient.put(`/api/polls/code/${code}`, data),
  deletePoll: code => apiClient.delete(`/api/polls/code/${code}`),

  submitVote: data => apiClient.post('/api/votes', data),
  getVoteData: pollCode => apiClient.get(`/api/votes/${pollCode}`),
  deleteVotes: pollCode => apiClient.delete('/api/votes', { params: { pollCode } }),
}

export default apiClient
