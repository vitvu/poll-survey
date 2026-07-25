import axios from 'axios';

// Ocelot Gateway Base URL
const API_BASE_URL = 'http://localhost:5019';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000,
});

// Response interceptor for error handling
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    let errorMessage = 'Đã xảy ra lỗi khi kết nối với máy chủ Ocelot Gateway.';
    if (error.response && error.response.data && error.response.data.message) {
      errorMessage = error.response.data.message;
    } else if (error.message) {
      errorMessage = error.message;
    }
    return Promise.reject(new Error(errorMessage));
  }
);

export const pollApi = {
  // 1. PollService APIs via Gateway
  getPolls: () => apiClient.get('/api/polls'),
  getPollById: (id) => apiClient.get(`/api/polls/${id}`),
  getPollByCode: (code) => apiClient.get(`/api/polls/code/${code}`),
  checkPoll: (code) => apiClient.get(`/api/polls/check/${code}`),
  checkOption: (optionId) => apiClient.get(`/api/polls/check-option/${optionId}`),
  createPoll: (pollData) => apiClient.post('/api/polls', pollData),
  updatePoll: (id, pollData) => apiClient.put(`/api/polls/${id}`, pollData),
  deletePoll: (id) => apiClient.delete(`/api/polls/${id}`),
  verifyCreator: (verifyData) => apiClient.post('/api/polls/verify-creator', verifyData),

  // 2. VoteService APIs via Gateway
  submitVote:     (voteData)  => apiClient.post('/api/votes', voteData),
  getVoteResults: (pollCode)  => apiClient.get(`/api/votes/result/${pollCode}`),
  getVoteTotal:   (pollCode)  => apiClient.get(`/api/votes/total/${pollCode}`),
  getVoteList:    (pollCode)  => apiClient.get(`/api/votes/list/${pollCode}`),
  getVoteByValue: (pollCode)  => apiClient.get(`/api/votes/byvalue/${pollCode}`),

  // 3. AnalyticsService APIs via Gateway
  getAnalyticsSummary: (pollCode) => apiClient.get(`/api/analytics/summary/${pollCode}`),
};

export default apiClient;
