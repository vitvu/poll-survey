/**
 * usePollHub — SignalR composable
 *
 * Kết nối tới VoteHub, join vào poll room,
 * và expose event "VoteUpdated" cho các component dùng.
 *
 * Kết nối trực tiếp tới VoteService (không qua Ocelot)
 * vì Ocelot không hỗ trợ WebSocket negotiate natively.
 */
import { ref, onUnmounted } from 'vue';
import * as signalR from '@microsoft/signalr';

const VOTE_SERVICE_URL = 'https://localhost:5002';

export function usePollHub(pollCode, onVoteUpdated) {
  const connected  = ref(false);
  const error      = ref(null);
  let   connection = null;

  const start = async () => {
    if (!pollCode) return;

    connection = new signalR.HubConnectionBuilder()
      .withUrl(`${VOTE_SERVICE_URL}/hubs/vote`, {
        // Fallback LongPolling nếu WS bị block
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
        skipNegotiation: false,
      })
      .withAutomaticReconnect([0, 1000, 3000, 5000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Lắng nghe event từ server
    connection.on('VoteUpdated', (data) => {
      if (data.pollCode === pollCode && typeof onVoteUpdated === 'function') {
        onVoteUpdated(data);
      }
    });

    connection.on('JoinedRoom', (code) => {
      console.log(`[SignalR] Joined poll room: ${code}`);
    });

    connection.onreconnecting(() => { connected.value = false; });
    connection.onreconnected(() => {
      connected.value = true;
      // Re-join group sau khi reconnect
      connection.invoke('JoinPollRoom', pollCode).catch(() => {});
    });
    connection.onclose(() => { connected.value = false; });

    try {
      await connection.start();
      await connection.invoke('JoinPollRoom', pollCode);
      connected.value = true;
      error.value     = null;
      console.log(`[SignalR] Connected & joined poll_${pollCode}`);
    } catch (e) {
      error.value     = e.message;
      connected.value = false;
      console.warn('[SignalR] Connection failed:', e.message);
    }
  };

  const stop = async () => {
    if (connection) {
      try {
        if (connection.state !== signalR.HubConnectionState.Disconnected) {
          await connection.invoke('LeavePollRoom', pollCode).catch(() => {});
          await connection.stop();
        }
      } catch {}
      connection = null;
    }
    connected.value = false;
  };

  onUnmounted(stop);

  return { connected, error, start, stop };
}
