import axios from 'axios'

const API_GATEWAY_URL = process.env.VUE_APP_API_BASE_URL || 'http://localhost:5000'

const apiClient = axios.create({
  baseURL: API_GATEWAY_URL,
  headers: {
    'Content-Type': 'application/json'
  },
  timeout: 10000  
})

export async function getPollByCode(pollCode) {
  const response = await apiClient.get(`/api/polls/code/${pollCode}`)
  return response.data
}

export async function createPoll(pollData) {
  const response = await apiClient.post('/api/polls', pollData)
  return response.data
}

export async function updatePoll(pollCode, pollData) {
  const response = await apiClient.put(`/api/polls/code/${pollCode}`, pollData)
  return response.data
}

export async function deletePoll(pollCode) {
  const response = await apiClient.delete(`/api/polls/code/${pollCode}`)
  return response.data
}

export async function submitVote(voteData) {
  const response = await apiClient.post('/api/votes', voteData)
  return response.data
}

export async function getVoteData(pollCode) {
  const response = await apiClient.get(`/api/votes/${pollCode}`)
  return response.data
}

export async function deleteVotes(pollCode) {
  const response = await apiClient.delete('/api/votes', {
    params: { pollCode }
  })
  return response.data
}

export default apiClient
