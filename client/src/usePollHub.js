import * as signalR from '@microsoft/signalr'

const VOTE_SERVICE_URL = process.env.VUE_APP_VOTE_SERVICE_URL || 'http://localhost:5002'


export function connectPollHub(pollCode, callbacks) {
  let connection = null
  let stopped = false

  async function joinRoom() {
    if (connection && connection.state === signalR.HubConnectionState.Connected) {
      await connection.invoke('JoinPollRoom', pollCode).catch(function () {})
    }
  }

  async function start() {
    if (!pollCode) return
    stopped = false

    connection = new signalR.HubConnectionBuilder()
      .withUrl(VOTE_SERVICE_URL + '/hubs/vote')
      .withAutomaticReconnect([0, 1000, 3000, 5000])
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    // Server sends this when someone votes
    connection.on('VoteUpdated', function (data) {
      if (data.pollCode === pollCode && callbacks.onUpdate) {
        callbacks.onUpdate(data)
      }
    })

    // Server sends this when admin closes the poll
    connection.on('PollClosed', function (data) {
      if (data.pollCode === pollCode && callbacks.onPollClosed) {
        callbacks.onPollClosed(data)
      }
    })

    // Server confirms we joined the room
    connection.on('UserJoined', function () {
      if (callbacks.onConnected) {
        callbacks.onConnected()
      }
    })

    // Connection dropped, trying to reconnect
    connection.onreconnecting(function () {
      if (callbacks.onDisconnected) {
        callbacks.onDisconnected()
      }
    })

    // Reconnected successfully — rejoin the room
    connection.onreconnected(async function () {
      if (!stopped) {
        await joinRoom()
        if (callbacks.onConnected) {
          callbacks.onConnected()
        }
      }
    })

    connection.onclose(function () {
      if (callbacks.onDisconnected) {
        callbacks.onDisconnected()
      }
    })

    try {
      await connection.start()
      await joinRoom()
      if (callbacks.onConnected) {
        callbacks.onConnected()
      }
    } catch (error) {
      console.error('SignalR failed to connect:', error)
      if (callbacks.onDisconnected) {
        callbacks.onDisconnected()
      }
    }
  }

  async function stop() {
    stopped = true
    if (connection && connection.state !== signalR.HubConnectionState.Disconnected) {
      await connection.invoke('LeavePollRoom', pollCode).catch(function () {})
      await connection.stop().catch(function () {})
    }
    connection = null
    if (callbacks.onDisconnected) {
      callbacks.onDisconnected()
    }
  }

  return { start, stop }
}
