import axios from 'axios'

const POLL_SERVICE_URL = process.env.VUE_APP_API_BASE_URL || 'http://localhost:5000'

const http = axios.create({
  baseURL: POLL_SERVICE_URL,
  headers: { 'Content-Type': 'application/json' },
  timeout: 10000,
})

// Extract a readable error message from the server response
http.interceptors.response.use(
  function (response) {
    return response
  },
  function (error) {
    const message = error.response?.data?.message || error.message || 'Server connection error.'
    return Promise.reject(new Error(message))
  }
)

// --- Polls ---

export const getPollByCode = async (code) => {
  return await http.get('/api/polls/code/' + code)
}

export const createPoll = async (pollData) => {
  return await http.post('/api/polls', pollData)
}

export const updatePoll = async (code, pollData) => {
  return await http.put('/api/polls/code/' + code, pollData)
}

export const deletePoll = async (code) => {
  return await http.delete('/api/polls/code/' + code)
}

// --- Votes ---

export const submitVote = async (voteData) => {
  return await http.post('/api/votes', voteData)
}

export const getVoteData = async (pollCode) => {
  return await http.get('/api/votes/' + pollCode)
}

export const deleteVotes = async (pollCode) => {
  return await http.delete('/api/votes', { params: { pollCode } })
}
