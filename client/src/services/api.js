import axios from 'axios';

const apiClient = axios.create({
  baseURL: 'https://localhost:5000',
  headers: { 'Content-Type': 'application/json' },
  timeout: 10000,
});

apiClient.interceptors.response.use(
  res => res,
  err => {
    const msg = err.response?.data?.message || err.message || 'Lỗi kết nối server.';
    return Promise.reject(new Error(msg));
  }
);

export const pollApi = {
  getPollByCode:       (code)     => apiClient.get(`/api/polls/code/${code}`),
  checkPoll:           (code)     => apiClient.get(`/api/polls/check/${code}`),
  createPoll:          (data)     => apiClient.post('/api/polls', data),
  updatePoll:          (id, data) => apiClient.put(`/api/polls/${id}`, data),
  deletePoll:          (id)       => apiClient.delete(`/api/polls/${id}`),

  submitVote:          (data)     => apiClient.post('/api/votes', data),
  getVoteResults:      (code)     => apiClient.get(`/api/votes/result/${code}`),
  getVoteTotal:        (code)     => apiClient.get(`/api/votes/total/${code}`),
  getVoteList:         (code)     => apiClient.get(`/api/votes/list/${code}`),

  getAnalyticsSummary: (code)     => apiClient.get(`/api/analytics/summary/${code}`),
};

export default apiClient;
