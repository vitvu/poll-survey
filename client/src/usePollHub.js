import { ref, onUnmounted } from 'vue'
import * as signalR from '@microsoft/signalr'

const VOTE_SERVICE_URL = process.env.VUE_APP_VOTE_SERVICE_URL || 'http://localhost:5002'

export function usePollHub(pollCode, onVoteUpdated) {
  const connected = ref(false)
  let connection = null

  const notify = data => {
    if (data.pollCode === pollCode && typeof onVoteUpdated === 'function') {
      onVoteUpdated(data)
    }
  }

  const start = async () => {
    if (!pollCode) return

    connection = new signalR.HubConnectionBuilder()
      .withUrl(`${VOTE_SERVICE_URL}/hubs/vote`)
      .withAutomaticReconnect([0, 1000, 3000, 5000])
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    connection.on('VoteUpdated', notify)
    connection.on('PollClosed', notify)

    connection.onreconnecting(() => { connected.value = false })
    connection.onreconnected(() => {
      connected.value = true
      connection.invoke('JoinPollRoom', pollCode).catch(() => {})
    })
    connection.onclose(() => { connected.value = false })

    try {
      await connection.start()
      await connection.invoke('JoinPollRoom', pollCode)
      connected.value = true
    } catch (e) {
      connected.value = false
    }
  }

  const stop = async () => {
    if (connection) {
      try {
        if (connection.state !== signalR.HubConnectionState.Disconnected) {
          await connection.invoke('LeavePollRoom', pollCode).catch(() => {})
          await connection.stop()
        }
      } catch (_) { /* ignore disconnect errors */ }
      connection = null
    }
    connected.value = false
  }

  onUnmounted(stop)

  return { connected, start, stop }
}
