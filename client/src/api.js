import axios from 'axios'

const SERVER_URL = process.env.VUE_APP_API_BASE_URL || 'http://localhost:5000'
const VOTE_SERVICE_URL = process.env.VUE_APP_VOTE_SERVICE_URL || 'https://localhost:5002'


export const getPollByCode = async (pollCode) => {
  try {
    const response = await axios.get(SERVER_URL + '/api/polls/code/' + pollCode)
    return response.data
  } catch (err) {
    console.error(err)
    return null
  }
}

export const createPoll = async (pollData) => {
  try {
    const response = await axios.post(SERVER_URL + '/api/polls', pollData)
    return response.data
  } catch (err) {
    console.error(err)
    return null
  }
}

export const updatePoll = async (pollCode, pollData) => {
  try {
    const response = await axios.put(SERVER_URL + '/api/polls/code/' + pollCode, pollData)
    return response.data
  } catch (err) {
    console.error(err)
    return null
  }
}

export const deletePoll = async (pollCode) => {
  try {
    const response = await axios.delete(SERVER_URL + '/api/polls/code/' + pollCode)
    return response.data
  } catch (err) {
    console.error(err)
    return null
  }
}


export const submitVote = async (voteData) => {
  try {
    const response = await axios.post(SERVER_URL + '/api/votes', voteData)
    return response.data
  } catch (err) {
    console.error(err)
    return null
  }
}

export const getVoteData = async (pollCode) => {
  try {
    const response = await axios.get(SERVER_URL + '/api/votes/' + pollCode)
    return response.data
  } catch (err) {
    console.error(err)
    return null
  }
}

export const deleteVotes = async (pollCode) => {
  try {
    const response = await axios.delete(SERVER_URL + '/api/votes', {
      params: { pollCode }
    })
    return response.data
  } catch (err) {
    console.error(err)
    return null
  }
}

export const notifyPollClosed = async (pollCode) => {
  try {
    const broadcastUrl = VOTE_SERVICE_URL + '/api/Votes/broadcast-closed'

    const response = await axios.post(broadcastUrl, {
      pollCode: pollCode,
    })

    return response.data
  } catch (err) {
    console.error('Notify poll closed error:', err)
    return null
  }
}
