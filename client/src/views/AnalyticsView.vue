<template>
  <div class="container">
    <button class="btn btn-ghost btn-sm mb-5" @click="router.push('/')">
      <ChevronLeft :size="15" /> Home
    </button>

    <!-- Access Denied -->
    <div v-if="accessDenied" class="card text-center max-w-[420px] mx-auto">
      <div class="flex items-center justify-center w-14 h-14 rounded-full bg-[--red-light] border border-[#fca5a5] mx-auto mb-4">
        <Lock :size="26" color="var(--red)" />
      </div>
      <h2 class="text-[17px] font-bold mb-1">Access Denied</h2>
      <p class="text-[14px] text-[--text-3] mb-5">Only the creator can view this page.</p>
      <router-link to="/" class="btn btn-ghost">Go Home</router-link>
    </div>

    <!-- Nội dung chính — poll=null thì không hiện gì, tự load xong sẽ hiện -->
    <template v-else-if="poll">

      <!-- Header Card -->
      <div class="card mb-3">
        <div class="flex items-start gap-3">
          <div class="flex-1">

            <div class="flex items-center gap-2 mb-2 flex-wrap">
              <span class="badge badge-blue">{{ poll.code }}</span>
              <span class="badge" :class="isPollClosed() ? 'badge-red' : 'badge-green'">
                <span v-if="!isPollClosed()" class="live-dot"></span>
                {{ isPollClosed() ? 'Closed' : 'Open' }}
              </span>
              <span class="badge badge-gray">{{ poll.questionType }}</span>
            </div>

            <h1 class="text-[20px] font-extrabold text-[--text] tracking-tight mb-5">
              {{ poll.question }}
            </h1>

            <div class="flex items-center gap-2 flex-wrap mb-2">
              <button class="btn btn-outline btn-sm" @click="copyShareLink">
                <Copy :size="14" /> Copy Link
              </button>
              <router-link :to="'/vote/' + poll.code" class="btn btn-outline btn-sm" target="_blank">
                <ExternalLink :size="14" /> Vote Page
              </router-link>
              <button v-if="!isPollClosed()" class="btn btn-red btn-sm" @click="confirmStop = true">
                <StopCircle :size="14" /> Stop
              </button>
              <span v-else class="badge badge-red" style="padding:6px 12px;">Closed</span>
              <button class="btn btn-danger btn-sm" @click="confirmDelete = true">
                <Trash2 :size="14" /> Delete
              </button>
            </div>

            <!-- Trạng thái kết nối realtime -->
            <span class="badge" :class="isHubConnected ? 'badge-green' : 'badge-gray'">
              <span class="live-dot"></span>
              {{ isHubConnected ? 'Live' : 'Connecting...' }}
            </span>

            <!-- Share link -->
            <div class="flex gap-2 mt-3">
              <input class="form-control text-[13.5px]" :value="shareLink()" readonly
                style="background:var(--surface-2);" />
              <button class="btn-icon shrink-0" @click="copyShareLink">
                <Clipboard :size="14" />
              </button>
            </div>

            <div class="flex gap-4 flex-wrap pt-2.5 mt-3 border-t border-[--border] text-[13px] text-[--text-3]">
              <span class="flex items-center gap-1.5">
                <Calendar :size="14" /> {{ new Date(poll.createdAt).toLocaleDateString('en-US') }}
              </span>
              <span class="flex items-center gap-1.5">
                <Clock :size="14" /> {{ new Date(poll.expireAt).toLocaleString('en-US') }}
              </span>
            </div>
          </div>

          <!-- QR — bấm mở modal -->
          <div class="w-[100px] h-[100px] shrink-0 cursor-pointer rounded-[--radius] overflow-hidden bg-white p-1 shadow-card hover:opacity-85 transition-opacity"
            @click="openQRModal">
            <canvas ref="qrThumbnailCanvas" class="w-full h-full"></canvas>
          </div>
        </div>
      </div>

      <!-- Total votes -->
      <div class="card mb-3 text-center">
        <div class="text-[13px] text-[--text-4] font-semibold uppercase tracking-wide mb-1">Total Votes</div>
        <div class="text-[40px] font-extrabold text-[--blue] leading-none">{{ totalVotes }}</div>
      </div>

      <!-- Results -->
      <div class="card mb-3">
        <div class="flex items-center text-[14px] font-bold text-[--text-2] pb-2.5 border-b border-[--border] mb-3.5">
          <BarChart2 :size="16" class="mr-1.5" /> Results
          <span class="live-badge ml-auto"><span class="live-dot"></span>Live</span>
        </div>

        <!-- Multiple Choice / Yes-No -->
        <template v-if="['Multiple Choice', 'Yes / No'].includes(poll.questionType)">
          <div v-if="!choiceResults.length" class="py-6 text-center text-[--text-4] text-[13.5px]">No votes yet.</div>
          <div v-else class="flex flex-col gap-3">
            <div v-for="option in choiceResults" :key="option.optionId">
              <div class="flex items-center justify-between mb-1.5">
                <span class="text-[14px] font-semibold text-[--text]">{{ option.optionText }}</span>
                <strong class="text-[15px] font-extrabold text-[--text-2]">{{ option.count }}</strong>
              </div>
              <div class="bar-track">
                <div class="bar-fill" :style="{ width: totalVotes > 0 ? (option.count / totalVotes * 100) + '%' : '0%' }"></div>
              </div>
            </div>
          </div>
        </template>

        <!-- Rating -->
        <template v-else-if="poll.questionType === 'Rating'">
          <div v-if="!ratingVoteList.length" class="py-6 text-center text-[--text-4] text-[13.5px]">No ratings yet.</div>
          <div v-else class="flex flex-col gap-2">
            <div v-for="(vote, index) in ratingVoteList" :key="index"
              class="flex items-center gap-2 p-2 bg-[--surface-2] border border-[--border] rounded-[--radius]">
              <Star v-for="star in 5" :key="star" :size="16"
                :fill="star <= Number(vote.voteValue) ? 'var(--amber)' : 'transparent'"
                :color="star <= Number(vote.voteValue) ? 'var(--amber)' : 'var(--border-2)'" />
            </div>
          </div>
        </template>

        <!-- Open Text -->
        <template v-else-if="poll.questionType === 'Open Text'">
          <div v-if="!openTextResponses.length" class="py-6 text-center text-[--text-4] text-[13.5px]">No responses yet.</div>
          <div v-for="(text, index) in openTextResponses" :key="index"
            class="p-3 bg-[--surface-2] border border-[--border] rounded-[--radius] text-[14px] text-[--text-3] mb-2">
            {{ text }}
          </div>
        </template>
      </div>
    </template>

    <!-- QR Modal — dùng v-if nên canvas chỉ tồn tại khi modal mở -->
    <div v-if="showQRModal" class="modal-bg" @click.self="showQRModal = false">
      <div class="modal-box">
        <button class="modal-close" @click="showQRModal = false"><X :size="20" /></button>
        <div class="qr-modal-canvas bg-[--surface-2] border border-[--border] rounded-[--radius-lg] p-5 flex items-center justify-center">
          <canvas ref="qrLargeCanvas" class="rounded-[--radius]"></canvas>
        </div>
        <div class="flex items-center justify-center mt-3 p-2.5 bg-[--surface-2] border border-[--border] rounded-[--radius]">
          <span class="text-[20px] font-extrabold text-[--blue] tracking-[2px]">{{ poll?.code }}</span>
        </div>
      </div>
    </div>

    <!-- Confirm Stop Modal -->
    <div v-if="confirmStop" class="modal-bg" @click.self="confirmStop = false">
      <div class="modal-box">
        <div class="flex items-center justify-center w-14 h-14 rounded-full bg-[--red-light] border border-[#fca5a5] mx-auto mb-4">
          <StopCircle :size="26" color="var(--red)" />
        </div>
        <h3 class="text-[17px] font-bold text-[--text] mb-2">Stop Poll?</h3>
        <p class="text-[13.5px] text-[--text-3] mb-5">Users will not be able to vote after closing.</p>
        <div class="flex gap-2 justify-center">
          <button class="btn btn-ghost" @click="confirmStop = false">Cancel</button>
          <button class="btn btn-red" @click="stopPoll"><StopCircle :size="14" /> Stop Now</button>
        </div>
      </div>
    </div>

    <!-- Confirm Delete Modal -->
    <div v-if="confirmDelete" class="modal-bg" @click.self="confirmDelete = false">
      <div class="modal-box">
        <div class="flex items-center justify-center w-14 h-14 rounded-full bg-[--red-light] border border-[#fca5a5] mx-auto mb-4">
          <Trash2 :size="26" color="var(--red)" />
        </div>
        <h3 class="text-[17px] font-bold text-[--text] mb-2">Delete Poll?</h3>
        <p class="text-[13.5px] text-[--text-3] mb-5">This will permanently delete the poll and all its votes.</p>
        <div class="flex gap-2 justify-center">
          <button class="btn btn-ghost" @click="confirmDelete = false">Cancel</button>
          <button class="btn btn-red" @click="deletePoll"><Trash2 :size="14" /> Delete</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
// useToast: hiện thông báo nhỏ góc màn hình. Dùng: toast.success('...') / toast.error('...')
import { useToast } from 'vue-toastification'
// pollApi: object chứa tất cả hàm gọi HTTP đến backend — định nghĩa trong src/api.js
import { pollApi } from '../api'
// usePollHub: hook kết nối SignalR để nhận kết quả vote realtime — định nghĩa trong src/usePollHub.js
import { usePollHub } from '../usePollHub'
// QRCode: thư viện bên ngoài, vẽ mã QR lên phần tử <canvas>
import QRCode from 'qrcode'
import {
  ChevronLeft, Lock, Copy, ExternalLink, StopCircle, Trash2,
  Calendar, Clock, BarChart2, Star, Clipboard, X,
} from '@lucide/vue'

const route  = useRoute()   // đọc thông tin URL hiện tại (query string, params,...)
const router = useRouter()  // dùng để chuyển trang: router.push('/')
const toast  = useToast()   // hiện toast thông báo

// Lấy code poll từ query string trong URL
// Ví dụ: /analytics?code=123456  →  pollCode = '123456'
const pollCode = route.query.code

// =================================================================
// BIẾN TRẠNG THÁI GIAO DIỆN
// ref(giá_trị_ban_đầu) tạo "hộp" chứa giá trị reactive
// Đọc: biến.value  |  Ghi: biến.value = ...
// Khi .value thay đổi → Vue tự cập nhật lại phần template dùng biến đó
// =================================================================

const accessDenied  = ref(false)   // true = hiện màn hình "Access Denied"
const confirmStop   = ref(false)   // true = hiện modal xác nhận Stop
const confirmDelete = ref(false)   // true = hiện modal xác nhận Delete
const showQRModal   = ref(false)   // true = hiện modal QR phóng to

// poll: object poll đầy đủ { id, code, question, options, status, expireAt, createdAt, ... }
// Ban đầu null → template dùng v-else-if="poll" để chờ load xong mới hiện
const poll              = ref(null)
const totalVotes        = ref(0)     // tổng số người đã vote
const choiceResults     = ref([])    // kết quả Multiple Choice/Yes-No: [{ optionId, optionText, count }]
const openTextResponses = ref([])    // câu trả lời Open Text: ['Tôi thích Vue', 'React tốt hơn', ...]
const ratingVoteList    = ref([])    // danh sách phiếu Rating: [{ voteValue: '4' }, ...]

// Canvas refs: Vue tự gán phần tử <canvas> vào đây sau khi DOM mount
// Trước mount: null  |  Sau mount: phần tử <canvas> thật trong trang
const qrThumbnailCanvas = ref(null)  // canvas nhỏ trong header card
const qrLargeCanvas     = ref(null)  // canvas lớn trong modal QR

// =================================================================
// HÀM TIỆN ÍCH (không dùng computed để đơn giản hơn)
// =================================================================

// Kiểm tra poll đã đóng chưa — trả về true/false
// Poll coi là đóng nếu THỎA MỘT TRONG BA điều kiện:
//   1. poll.value là null (chưa load xong)
//   2. status không phải 'Active'
//   3. expireAt đã qua thời hạn
const isPollClosed = () => {
  if (!poll.value) return true
  if (poll.value.status !== 'Active') return true
  if (new Date(poll.value.expireAt) <= new Date()) return true
  return false
}

// Tạo link chia sẻ để gửi cho người tham gia vote
// location.origin = phần gốc URL hiện tại, ví dụ: http://localhost:8080
// Kết quả ví dụ: http://localhost:8080/vote/123456
const shareLink = () => `${location.origin}/vote/${poll.value?.code}`

// =================================================================
// loadResults — load vote data from API
// =================================================================
const loadResults = async () => {
  if (!poll.value) return

  const response = await pollApi.getVoteData(pollCode)
  const { total, summary, votes } = response.data

  totalVotes.value = total

  const questionType = poll.value.questionType

  if (questionType === 'Multiple Choice' || questionType === 'Yes / No') {
    const resultsWithName = summary.map(item => ({
      optionId: item.optionId,
      optionText: poll.value.options.find(o => o.id === item.optionId)?.text || '(unknown)',
      count: item.count,
    }))
    resultsWithName.sort((a, b) => b.count - a.count)
    choiceResults.value = resultsWithName

  } else if (questionType === 'Rating') {
    ratingVoteList.value = votes

  } else if (questionType === 'Open Text') {
    openTextResponses.value = votes
      .map(v => v.voteValue)
      .filter(v => v && v.trim())
  }
}

// =================================================================
// renderQRCode — vẽ mã QR lên phần tử <canvas>
// Tham số:
//   canvasElement: phần tử <canvas> DOM thật (lấy từ ref.value)
//   size: kích thước pixel muốn vẽ (100 = nhỏ, 320 = lớn)
// =================================================================
const renderQRCode = async (canvasElement, size) => {
  // Nếu canvas chưa tồn tại trong DOM thì bỏ qua (tránh lỗi)
  if (!canvasElement) return
  try {
    await QRCode.toCanvas(canvasElement, shareLink(), {
      width: size,    // chiều rộng và cao của ảnh QR (px)
      margin: 2,      // số ô trắng viền xung quanh QR
      color: {
        dark: '#1e293b',   // màu của các ô trong QR
        light: '#ffffff',  // màu nền
      },
    })
  } catch (error) {
    console.error('QR render failed:', error)
  }
}

// =================================================================
// openQRModal — mở modal QR và vẽ QR lớn vào canvas
// =================================================================
// Lý do cần setTimeout 50ms:
//   showQRModal.value = true → Vue cần một "tick" để tạo <canvas ref="qrLargeCanvas">
//   Nếu gọi renderQRCode ngay lập tức thì qrLargeCanvas.value vẫn còn null
//   → phải chờ Vue render canvas xong mới vẽ được
const openQRModal = () => {
  showQRModal.value = true  // bật v-if="showQRModal" → Vue render canvas trong modal
  setTimeout(() => {
    renderQRCode(qrLargeCanvas.value, 320)  // sau 50ms canvas đã có, vẽ QR lớn
  }, 50)
}

// =================================================================
// usePollHub — kết nối SignalR nhận kết quả vote realtime
// =================================================================
// Truyền vào: pollCode + callback chạy khi server push dữ liệu mới
//   hubData = { total: 42, results: [...], pollCode: '123456' }
// Nhận về:
//   isHubConnected: ref(true/false) hiển thị badge "Live" / "Connecting..."
//   startHub: hàm bắt đầu kết nối, gọi sau khi đã load poll xong
const { connected: isHubConnected, start: startHub } = usePollHub(pollCode, async (hubData) => {
  totalVotes.value = hubData.total  // cập nhật tổng phiếu ngay
  await loadResults()               // load lại kết quả chi tiết
})

// Lưu ID của setInterval để clearInterval khi unmount (không dùng ref vì không cần reactive)
let fallbackInterval = null

// =================================================================
// onMounted — chạy một lần ngay sau khi component hiện lên trang
// Flow: kiểm tra quyền → load poll → load kết quả → kết nối SignalR → vẽ QR
// =================================================================
onMounted(async () => {
  // Nếu URL không có ?code=... thì không làm gì
  if (!pollCode) return

  // --- Kiểm tra quyền truy cập ---
  // Khi tạo poll thành công (CreatePollView), code được lưu vào localStorage
  // localStorage['createdPolls'] = '["123456","789012"]'  (mảng JSON string)
  const savedCodes = localStorage.getItem('createdPolls')   // lấy chuỗi JSON
  const createdPollCodes = JSON.parse(savedCodes || '[]')   // parse thành mảng JS

  if (!createdPollCodes.includes(pollCode)) {
    // pollCode không nằm trong danh sách đã tạo → không phải creator
    accessDenied.value = true  // bật cờ → template hiện màn hình "Access Denied"
    return                     // dừng ở đây, không load gì thêm
  }

  try {
    // --- Load thông tin poll ---
    // GET /api/polls/code/{pollCode} → { id, code, question, questionType, options, status, expireAt, ... }
    const pollResponse = await pollApi.getPollByCode(pollCode)
    poll.value = pollResponse.data  // lưu vào ref → v-else-if="poll" trong template tự hiện

    // --- Load kết quả vote lần đầu ---
    await loadResults()

    // --- Bắt đầu kết nối SignalR ---
    // Từ đây server sẽ push dữ liệu mới mỗi khi có người vote
    startHub()

    // --- Fallback polling ---
    // Cứ 6 giây kiểm tra: nếu SignalR vẫn offline thì tự gọi API để lấy kết quả mới
    fallbackInterval = setInterval(() => {
      if (!isHubConnected.value) {
        loadResults()
      }
    }, 6000)

    // --- Vẽ QR thumbnail ---
    // Delay 100ms vì Vue cần một "tick" để render <canvas ref="qrThumbnailCanvas">
    // sau khi poll.value có giá trị và template v-else-if="poll" hiện ra
    setTimeout(() => {
      renderQRCode(qrThumbnailCanvas.value, 100)
    }, 100)

  } catch {
    // Bất kỳ lỗi nào ở trên (network, server lỗi,...) → hiện toast lỗi
    toast.error('Failed to load data.')
  }
})

// =================================================================
// onUnmounted — chạy khi user rời trang (component bị gỡ khỏi DOM)
// =================================================================
// Phải clearInterval để dừng fallbackInterval
// Nếu không: interval tiếp tục chạy ngầm dù đã rời trang → memory leak
onUnmounted(() => {
  clearInterval(fallbackInterval)
})

// =================================================================
// stopPoll — đóng poll, không cho vote thêm
// =================================================================
const stopPoll = async () => {
  confirmStop.value = false  // đóng modal trước khi gọi API

  try {
    // PUT /api/polls/code/{code}: gửi toàn bộ data poll, chỉ ghi đè status = 'Closed'
    // Spread {...poll.value} sao chép tất cả field hiện tại, sau đó ghi đè status
    // Ví dụ gửi lên: { id:1, code:'123456', question:'...', status:'Closed', ... }
    await pollApi.updatePoll(pollCode, { ...poll.value, status: 'Closed' })

    // Cập nhật local ngay (không gọi API lại) để badge đổi tức thì không cần chờ
    poll.value.status = 'Closed'

    toast.success('Poll stopped.')
  } catch {
    toast.error('Failed to stop poll.')
  }
}

// =================================================================
// deletePoll — xóa poll vĩnh viễn
// =================================================================
const deletePoll = async () => {
  confirmDelete.value = false  // đóng modal trước khi gọi API

  try {
    // DELETE /api/polls/code/{code}: xóa poll và tất cả vote liên quan trong DB
    await pollApi.deletePoll(pollCode)

    // Xóa code này khỏi localStorage để trang analytics không còn truy cập được
    const savedCodes = localStorage.getItem('createdPolls')
    const createdPollCodes = JSON.parse(savedCodes || '[]')
    // filter trả về mảng mới, chỉ giữ lại các code KHÁC với pollCode vừa xóa
    const updatedCodes = createdPollCodes.filter(code => code !== pollCode)
    localStorage.setItem('createdPolls', JSON.stringify(updatedCodes))

    toast.success('Poll deleted.')
    router.push('/')  // chuyển về trang chủ
  } catch {
    toast.error('Failed to delete poll.')
  }
}

// =================================================================
// copyShareLink — copy link vào clipboard của trình duyệt
// =================================================================
const copyShareLink = async () => {
  try {
    // navigator.clipboard là Web API của trình duyệt để thao tác clipboard
    await navigator.clipboard.writeText(shareLink())
    toast.success('Link copied!')
  } catch {
    toast.error('Failed to copy.')
  }
}
</script>

