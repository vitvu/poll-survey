<template>
  <div class="vote-page">

    <!-- Poll không tìm thấy -->
    <div v-if="pollNotFound" class="card max-w-[440px] w-full mx-auto text-center">
      <div class="w-16 h-16 rounded-full bg-[--red-light] border border-[#fca5a5]
                  flex items-center justify-center mx-auto mb-3">
        <SearchX :size="28" />
      </div>
      <h2 class="text-[18px] font-extrabold text-[--text] mb-1.5">Poll Not Found</h2>
      <p class="text-[14px] text-[--text-3] mb-5">
        Code <strong>{{ pollCodeFromUrl }}</strong> doesn't exist.
      </p>
      <router-link to="/" class="btn btn-outline">
        <ArrowLeft :size="14" /> Go Home
      </router-link>
    </div>

    <!-- Đã vote rồi (phát hiện qua localStorage hoặc lỗi từ server) -->
    <div v-else-if="alreadyVoted" class="card max-w-[440px] w-full mx-auto text-center">
      <div class="w-16 h-16 rounded-full bg-[--green-light] border border-[#86efac]
                  flex items-center justify-center mx-auto mb-3">
        <CheckCircle2 :size="28" />
      </div>
      <h2 class="text-[18px] font-extrabold text-[--text] mb-1.5">Already Voted</h2>
      <p class="text-[14px] text-[--text-3]">You have already participated in this poll.</p>
    </div>

    <!-- Vote thành công -->
    <div v-else-if="voteSubmitted" class="card max-w-[440px] w-full mx-auto text-center">
      <div class="w-16 h-16 rounded-full bg-[--green-light] border border-[#86efac]
                  flex items-center justify-center mx-auto mb-3">
        <CheckCircle2 :size="28" />
      </div>
      <h2 class="text-[18px] font-extrabold text-[--text] mb-1.5">Vote Recorded!</h2>
      <p class="text-[14px] text-[--text-3]">Thank you for participating.</p>
    </div>

    <!-- Form vote — hiện khi đã load được poll -->
    <div v-else-if="poll" class="vote-card">

      <!-- Header: code + trạng thái + câu hỏi -->
      <div class="p-6 pb-5 border-b border-[--border]">
        <div class="flex items-center justify-between mb-2">
          <span class="badge badge-blue">{{ poll.code }}</span>
          <span class="badge" :class="isPollExpired() ? 'badge-red' : 'badge-green'">
            <span v-if="!isPollExpired()" class="live-dot"></span>
            {{ isPollExpired() ? 'Closed' : 'Live' }}
          </span>
        </div>
        <h1 class="text-[19px] font-extrabold text-[--text] tracking-tight leading-snug my-2">
          {{ poll.question }}
        </h1>
        <p class="text-[12.5px] text-[--text-4] font-semibold">{{ poll.questionType }}</p>
      </div>

      <!-- Thông báo poll đã đóng -->
      <div v-if="isPollExpired()"
        class="mx-6 mt-4 flex items-center gap-2 p-3 rounded-[--radius]
               bg-[--red-light] text-[#991b1b] border border-[#fca5a5] text-[13.5px] font-medium">
        <Lock :size="15" /> This poll has ended.
      </div>

      <!-- Form vote — chỉ hiện khi poll chưa đóng -->
      <form v-else class="p-5" @submit.prevent="submitVote">

        <!-- Multiple Choice / Yes-No: danh sách lựa chọn dạng radio -->
        <div v-if="poll.questionType === 'Multiple Choice' || poll.questionType === 'Yes / No'"
          class="flex flex-col gap-2 mb-5">
          <label
            v-for="option in poll.options"
            :key="option.id"
            class="vote-option"
            :class="{
              selected: selectedOptionId === option.id,
              error: hasSubmitError && !selectedOptionId
            }"
          >
            <!-- input radio ẩn, Vue dùng v-model để theo dõi option nào được chọn -->
            <input type="radio" :value="option.id" v-model="selectedOptionId" class="sr-only" />

            <!-- Vòng tròn radio tự vẽ (không dùng radio mặc định của trình duyệt) -->
            <div class="vote-option-radio">
              <div class="radio-inner" :class="{ filled: selectedOptionId === option.id }"></div>
            </div>

            <!-- Tên option -->
            <span class="flex-1 text-[14.5px] font-medium"
              :class="selectedOptionId === option.id ? 'text-[--blue] font-semibold' : 'text-[--text]'">
              {{ option.text }}
            </span>

            <!-- Dấu check khi được chọn -->
            <span v-if="selectedOptionId === option.id" class="text-[--blue]">
              <Check :size="15" />
            </span>
          </label>

          <p v-if="hasSubmitError && !selectedOptionId"
            class="inline-flex items-center gap-1 text-[12.5px] text-[--red] font-semibold">
            <AlertCircle :size="13" /> Please select an option
          </p>
        </div>

        <!-- Rating: 5 ngôi sao, bấm để chọn điểm từ 1-5 -->
        <div v-else-if="poll.questionType === 'Rating'" class="text-center py-2 pb-5">
          <div class="flex gap-1 justify-center">
            <button
              v-for="starNumber in 5"
              :key="starNumber"
              type="button"
              class="star-btn"
              :class="{ on: starNumber <= Number(voteValue) }"
              @click="voteValue = String(starNumber)"
            >
              <!-- fill='currentColor' khi sao được chọn (tô màu), 'none' khi chưa chọn (rỗng) -->
              <Star :size="36" :fill="starNumber <= Number(voteValue) ? 'currentColor' : 'none'" />
            </button>
          </div>

          <p v-if="hasSubmitError && !voteValue"
            class="inline-flex items-center gap-1 text-[12.5px] text-[--red] font-semibold mt-2">
            <AlertCircle :size="13" /> Please select a rating
          </p>
        </div>

        <!-- Open Text: textarea nhập tự do -->
        <div v-else-if="poll.questionType === 'Open Text'" class="mb-5">
          <textarea
            v-model="voteValue"
            rows="4"
            class="form-control"
            :class="{ 'is-error': hasSubmitError && !voteValue.trim() }"
            placeholder="Enter your response..."
          ></textarea>

          <p v-if="hasSubmitError && !voteValue.trim()"
            class="inline-flex items-center gap-1 text-[12.5px] text-[--red] font-semibold mt-2">
            <AlertCircle :size="13" /> Please enter a response
          </p>
        </div>

        <button type="submit" class="btn btn-primary btn-lg w-full mt-3" :disabled="isSubmitting">
          <span v-if="isSubmitting" class="spinner"></span>
          <Send v-else :size="15" />
          {{ isSubmitting ? 'Submitting...' : 'Submit Vote' }}
        </button>
      </form>
    </div>

    <!-- Nhập code thủ công — hiện khi URL không có code (vào thẳng /vote) -->
    <div v-else class="card max-w-[440px] w-full mx-auto">
      <p class="text-[11px] font-bold uppercase tracking-widest text-[--text-4] mb-1">Join Poll</p>
      <h1 class="text-[18px] font-extrabold mb-1">Enter Room Code</h1>
      <p class="text-[14px] text-[--text-3] mb-5">Enter 6-digit code from the creator</p>

      <form @submit.prevent="loadPollByManualCode">
        <input
          v-model="manualCode"
          type="text"
          inputmode="numeric"
          maxlength="6"
          placeholder="000000"
          class="code-input"
          :class="{ error: manualCodeError }"
          autocomplete="off"
        />

        <p v-if="manualCodeError"
          class="inline-flex items-center gap-1 text-[12.5px] text-[--red] font-semibold mt-2 justify-center">
          <AlertCircle :size="13" /> {{ manualCodeError }}
        </p>

        <button type="submit" class="btn btn-primary btn-lg w-full mt-5" :disabled="isLoadingManual">
          <span v-if="isLoadingManual" class="spinner"></span>
          <LogIn v-else :size="15" />
          Join Room
        </button>
      </form>
    </div>

  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { CheckCircle2, Check, AlertCircle, Lock, Send, LogIn, Star, ArrowLeft, SearchX } from '@lucide/vue'
import { pollApi } from '../api'
import { getVoterToken } from '../voterToken'  // hàm trả về token định danh thiết bị (tránh vote 2 lần)

// Đọc thông tin URL hiện tại
// Ví dụ URL: /vote/123456 → route.params.code = '123456'
// Ví dụ URL: /vote        → route.params.code = undefined
const route = useRoute()

// Lấy code từ URL params, nếu không có thì để chuỗi rỗng
const pollCodeFromUrl = route.params.code || ''

// =================================================================
// BIẾN TRẠNG THÁI
// =================================================================

// Dữ liệu poll nhận từ API: { id, code, question, questionType, options, status, expireAt }
// null = chưa load, template dùng v-else-if="poll" để chờ
const poll = ref(null)

// true = poll không tồn tại (API trả về 404 hoặc lỗi)
const pollNotFound = ref(false)

// true = người dùng đã vote rồi (phát hiện qua localStorage hoặc lỗi "already voted" từ server)
const alreadyVoted = ref(false)

// true = vừa vote thành công → hiện màn hình cảm ơn
const voteSubmitted = ref(false)

// =================================================================
// BIẾN FORM VOTE
// =================================================================

// ID của option được chọn (dùng cho Multiple Choice và Yes/No)
// null = chưa chọn gì
const selectedOptionId = ref(null)

// Giá trị nhập tự do (dùng cho Rating và Open Text)
// Rating: '1' đến '5' (chuỗi số)
// Open Text: chuỗi văn bản người dùng nhập
const voteValue = ref('')

// true = người dùng bấm Submit nhưng chưa chọn/nhập gì → hiện thông báo lỗi đỏ
const hasSubmitError = ref(false)

// true = đang gọi API submit vote → disable nút Submit và hiện spinner
const isSubmitting = ref(false)

// =================================================================
// BIẾN FORM NHẬP CODE THỦ CÔNG
// =================================================================

// Code người dùng gõ vào ô input (khi vào /vote không có code trong URL)
const manualCode = ref('')

// Thông báo lỗi khi nhập code sai hoặc poll không tìm thấy
const manualCodeError = ref('')

// true = đang gọi API kiểm tra code thủ công → disable nút Join
const isLoadingManual = ref(false)

// =================================================================
// HÀM TIỆN ÍCH
// =================================================================

// Kiểm tra poll có hết hạn/đóng chưa
// Trả về true nếu poll đã đóng hoặc quá thời hạn
const isPollExpired = () => {
  if (!poll.value) return true
  if (poll.value.status !== 'Active') return true
  if (new Date(poll.value.expireAt) <= new Date()) return true
  return false
}

// =================================================================
// loadPoll — gọi API kiểm tra và lấy thông tin poll
// =================================================================
// Được gọi từ onMounted (nếu URL có code) hoặc từ loadPollByManualCode
const loadPoll = async (pollCode) => {
  pollNotFound.value = false  // reset lỗi cũ trước khi gọi mới

  try {
    // GET /api/polls/check/{pollCode}
    // Backend kiểm tra poll có tồn tại, còn active, chưa hết hạn không
    // Nếu hợp lệ → trả về thông tin poll (kèm options)
    // Nếu không → trả về 404 hoặc 400 → catch bên dưới xử lý
    const response = await pollApi.checkPoll(pollCode)
    poll.value = response.data  // lưu thông tin poll vào ref → template tự hiện form vote

    // Kiểm tra localStorage xem người dùng đã vote poll này chưa
    // Key lưu dạng: "voted_123456" = "true"
    const hasVotedBefore = localStorage.getItem(`voted_${pollCode}`) === 'true'
    if (hasVotedBefore) {
      alreadyVoted.value = true  // đã vote → hiện màn hình "Already Voted"
    }

  } catch {
    // API trả lỗi (poll không tồn tại, đã đóng, hết hạn,...) → hiện màn hình "Poll Not Found"
    pollNotFound.value = true
  }
}

// =================================================================
// loadPollByManualCode — xử lý form nhập code thủ công
// =================================================================
const loadPollByManualCode = async () => {
  // Validate: code phải đủ 6 chữ số
  if (manualCode.value.length < 6) {
    manualCodeError.value = 'Please enter all 6 digits'
    return
  }

  isLoadingManual.value = true
  manualCodeError.value = ''  // xóa lỗi cũ

  await loadPoll(manualCode.value)  // gọi hàm load poll với code vừa nhập

  // Nếu load xong mà poll không tìm thấy → hiện lỗi trong form nhập code
  if (pollNotFound.value) {
    manualCodeError.value = 'Poll not found'
  }

  isLoadingManual.value = false
}

// =================================================================
// submitVote — gửi phiếu bầu lên server
// =================================================================
const submitVote = async () => {

  // Lấy loại câu hỏi của poll hiện tại để biết cần validate gì
  // Ví dụ: 'Multiple Choice', 'Yes / No', 'Rating', 'Open Text'
  const questionType = poll.value.questionType

  // ---------------------------------------------------------------
  // Bước 1: Kiểm tra người dùng đã chọn / nhập câu trả lời chưa
  // ---------------------------------------------------------------

  if (questionType === 'Multiple Choice' || questionType === 'Yes / No') {

    // Với loại Multiple Choice và Yes/No:
    // người dùng phải bấm chọn một trong các option
    // selectedOptionId = null có nghĩa là chưa chọn gì
    if (selectedOptionId.value === null) {
      // Chưa chọn option → bật cờ lỗi để template hiện thông báo đỏ
      hasSubmitError.value = true
      return  // dừng lại, không gửi lên server
    }

  } else if (questionType === 'Rating') {

    // Với loại Rating:
    // người dùng phải bấm chọn số sao (1 đến 5)
    // voteValue = '' có nghĩa là chưa bấm sao nào
    if (voteValue.value === '') {
      // Chưa chọn số sao → bật cờ lỗi
      hasSubmitError.value = true
      return  // dừng lại, không gửi lên server
    }

  } else if (questionType === 'Open Text') {

    // Với loại Open Text:
    // người dùng phải nhập nội dung câu trả lời
    // .trim() loại bỏ khoảng trắng đầu/cuối trước khi kiểm tra
    // Ví dụ: '   ' (toàn khoảng trắng) → sau trim() = '' → coi là chưa nhập
    if (voteValue.value.trim() === '') {
      // Chưa nhập nội dung → bật cờ lỗi
      hasSubmitError.value = true
      return  // dừng lại, không gửi lên server
    }
  }

  // Đến đây nghĩa là đã qua validate → reset cờ lỗi
  hasSubmitError.value = false

  // Bật trạng thái đang gửi → nút Submit bị disable + hiện spinner
  isSubmitting.value = true

  // ---------------------------------------------------------------
  // Bước 2: Gửi phiếu bầu lên server
  // ---------------------------------------------------------------

  try {
    // Gọi POST /api/votes — gửi phiếu bầu lên VoteService
    await pollApi.submitVote({
      pollCode: poll.value.code,        // code của poll đang vote

      voterToken: getVoterToken(),      // token định danh thiết bị (tạo trong voterToken.js)
                                        // server dùng token này để chặn cùng 1 người vote 2 lần

      optionId: selectedOptionId.value || 0,
      // Nếu là Multiple Choice / Yes-No → gửi id option được chọn (ví dụ: 3)
      // Nếu là Rating / Open Text → không có option → gửi 0 (backend bỏ qua field này)

      voteValue: voteValue.value,
      // Nếu là Rating → gửi chuỗi số sao ví dụ '4'
      // Nếu là Open Text → gửi nội dung người dùng nhập
      // Nếu là Multiple Choice / Yes-No → gửi '' (backend bỏ qua field này)
    })

    // Vote gửi thành công:
    // Lưu vào localStorage để lần sau vào lại trang biết đã vote rồi
    // Key: "voted_123456" = "true"
    localStorage.setItem(`voted_${poll.value.code}`, 'true')

    // Bật cờ → template hiện màn hình "Vote Recorded!"
    voteSubmitted.value = true

  } catch (error) {

    // Server trả về lỗi — xử lý từng trường hợp

    if (error.message && error.message.includes('already')) {
      // Lỗi "already voted" = server phát hiện voterToken này đã vote rồi
      // (trường hợp localStorage bị xóa nhưng server vẫn còn ghi nhận)
      alreadyVoted.value = true  // hiện màn hình "Already Voted"

    } else {
      // Lỗi khác (mất mạng, server lỗi,...) → bật cờ lỗi trong form
      hasSubmitError.value = true
    }

  } finally {
    // Dù thành công hay lỗi đều tắt spinner
    isSubmitting.value = false
  }
}

// =================================================================
// onMounted — chạy một lần sau khi component hiện lên trang
// =================================================================
onMounted(() => {
  // Nếu URL có code (ví dụ /vote/123456) thì tự động load poll luôn
  // Nếu không có code thì hiện form nhập code thủ công
  if (pollCodeFromUrl) {
    loadPoll(pollCodeFromUrl)
  }
})
</script>
