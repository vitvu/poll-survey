<template>
  <div class="vote-page">

    <!-- Đang tải -->
    <div v-if="loading" class="state-center">
      <div class="spinner spinner-blue" style="width:28px;height:28px;margin:0 auto 12px;"></div>
      <p class="fs-sm text-3">Loading poll...</p>
    </div>

    <!-- Không tìm thấy poll -->
    <div v-else-if="notFound" class="card state-card text-center">
      <div class="state-icon state-icon--red mb-3">
        <SearchX :size="28" />
      </div>
      <h2 class="state-title">Poll Not Found</h2>
      <p class="page-sub mb-3">Code <strong>{{ code }}</strong> doesn't exist.</p>
      <router-link to="/" class="btn btn-outline">
        <ArrowLeft :size="14" /> Go Home
      </router-link>
    </div>

    <!-- Đã vote rồi -->
    <div v-else-if="alreadyVoted" class="card state-card text-center">
      <div class="state-icon state-icon--green mb-3">
        <CheckCircle2 :size="28" />
      </div>
      <h2 class="state-title">Already Voted</h2>
      <p class="page-sub">You have already participated in this poll.</p>
    </div>

    <!-- Vote thành công -->
    <div v-else-if="voted" class="card state-card text-center">
      <div class="state-icon state-icon--green mb-3">
        <CheckCircle2 :size="28" />
      </div>
      <h2 class="state-title">Vote Recorded!</h2>
      <p class="page-sub">Thank you for participating.</p>
    </div>

    <!-- Poll đang mở hoặc đã đóng -->
    <div v-else-if="poll" class="vote-card">

      <!-- Header: code + trạng thái + câu hỏi -->
      <div class="vote-header">
        <div class="flex items-center justify-between mb-2">
          <span class="badge badge-blue">{{ poll.code }}</span>
          <span class="badge" :class="isExpired ? 'badge-red' : 'badge-green'">
            <span v-if="!isExpired" class="live-dot"></span>
            {{ isExpired ? 'Closed' : 'Live' }}
          </span>
        </div>
        <h1 class="vote-question">{{ poll.question }}</h1>
        <p class="vote-type">{{ poll.questionType }}</p>
      </div>

      <!-- Poll đã đóng -->
      <div v-if="isExpired" class="vote-expired">
        <Lock :size="15" /> This poll has ended.
      </div>

      <!-- Form vote -->
      <form v-else class="vote-body" @submit.prevent="submit">

        <!-- Multiple Choice / Yes-No: danh sách lựa chọn -->
        <div v-if="poll.questionType === 'Multiple Choice' || poll.questionType === 'Yes / No'" class="option-list">
          <label v-for="opt in poll.options" :key="opt.id"
            class="vote-option" :class="{ selected: selectedId === opt.id, error: submitError && !selectedId }">
            <input type="radio" :value="opt.id" v-model="selectedId" class="sr-only" />
            <div class="vote-option-radio">
              <div class="radio-inner" :class="{ filled: selectedId === opt.id }"></div>
            </div>
            <span class="vote-option-text">{{ opt.text }}</span>
            <span v-if="selectedId === opt.id" class="vote-option-check">
              <Check :size="15" />
            </span>
          </label>
          <p v-if="submitError && !selectedId" class="inline-error">
            <AlertCircle :size="13" /> Please select an option
          </p>
        </div>

        <!-- Rating: 5 ngôi sao -->
        <div v-else-if="poll.questionType === 'Rating'" class="rating-wrap">
          <div class="star-row">
            <!-- Click để chọn rating (1–5) -->
            <button v-for="s in 5" :key="s" type="button"
              class="star-btn" :class="{ on: s <= Number(voteValue) }"
              @click="voteValue = String(s)">
              <Star :size="36" :fill="s <= Number(voteValue) ? 'currentColor' : 'none'" />
            </button>
          </div>
          <p v-if="submitError && !voteValue" class="inline-error">
            <AlertCircle :size="13" /> Please select a rating
          </p>
        </div>

        <!-- Open Text: nhập tự do -->
        <div v-else-if="poll.questionType === 'Open Text'">
          <textarea v-model="voteValue" rows="4" class="form-control"
            :class="{ 'is-error': submitError && !voteValue.trim() }"
            placeholder="Enter your response..."></textarea>
          <p v-if="submitError && !voteValue.trim()" class="inline-error">
            <AlertCircle :size="13" /> Please enter a response
          </p>
        </div>

        <button type="submit" class="btn btn-primary btn-lg w-full mt-3" :disabled="submitting">
          <span v-if="submitting" class="spinner"></span>
          <Send v-else :size="15" />
          {{ submitting ? 'Submitting...' : 'Submit Vote' }}
        </button>
      </form>
    </div>

    <!-- Không có code trong URL: form nhập code -->
    <div v-else class="card state-card">
      <p class="label-upper mb-1">Join Poll</p>
      <h1 class="mb-1" style="font-size:18px;font-weight:800;">Enter Room Code</h1>
      <p class="page-sub mb-3">Enter 6-digit code from the creator</p>
      <form @submit.prevent="loadManual">
        <input v-model="manualCode" type="text" inputmode="numeric" maxlength="6"
          placeholder="000000" class="code-input" :class="{ error: manualError }"
          autocomplete="off" />
        <p v-if="manualError" class="inline-error justify-center">
          <AlertCircle :size="13" /> {{ manualError }}
        </p>
        <button type="submit" class="btn btn-primary btn-lg w-full mt-3" :disabled="loadingManual">
          <span v-if="loadingManual" class="spinner"></span>
          <LogIn v-else :size="15" />
          Join Room
        </button>
      </form>
    </div>

  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { CheckCircle2, Check, AlertCircle, Lock, Send, LogIn, Star, ArrowLeft, SearchX } from '@lucide/vue'
import { pollApi } from '../api'
import { getVoterToken } from '../voterToken'

const route = useRoute()
const code = computed(() => route.params.code || '')

// State cho UI (cần reactive để hiển thị màn hình khác nhau)
const loading = ref(false)       // Đang load poll
const poll = ref(null)           // Data poll từ API
const notFound = ref(false)      // Poll không tồn tại
const alreadyVoted = ref(false)  // Đã vote rồi
const voted = ref(false)         // Vừa vote xong

// State cho form vote
const selectedId = ref(null)    // ID option đã chọn
const voteValue = ref('')       // Text/rating đã nhập
const submitError = ref(false)  // Lỗi validate → hiện thông báo lỗi
const submitting = ref(false)   // Đang submit → disable button + spinner

// Form nhập code thủ công (khi không có code trong URL)
const manualCode = ref('')
const manualError = ref('')
const loadingManual = ref(false)

const isExpired = computed(() =>
  !poll.value || poll.value.status !== 'Active' || new Date(poll.value.expireAt) <= new Date()
)

onMounted(() => {
  if (code.value) loadPoll(code.value)
})

const loadPoll = async pollCode => {
  loading.value = true
  notFound.value = false
  try {
    const r = await pollApi.checkPoll(pollCode)
    poll.value = r.data
    if (localStorage.getItem(`voted_${pollCode}`) === 'true') alreadyVoted.value = true
  } catch {
    notFound.value = true
  } finally {
    loading.value = false
  }
}

const loadManual = async () => {
  if (manualCode.value.length < 6) {
    manualError.value = 'Please enter all 6 digits'
    return
  }
  loadingManual.value = true
  await loadPoll(manualCode.value)
  if (notFound.value) manualError.value = 'Poll not found'
  loadingManual.value = false
}

const submit = async () => {
  const type = poll.value.questionType
  const invalid =
    ((type === 'Multiple Choice' || type === 'Yes / No') && !selectedId.value) ||
    ((type === 'Rating' || type === 'Open Text') && !voteValue.value)

  if (invalid) { submitError.value = true; return }
  submitError.value = false
  submitting.value = true

  try {
    await pollApi.submitVote({
      pollCode: poll.value.code,
      voterToken: getVoterToken(),
      optionId: selectedId.value || 0,
      voteValue: voteValue.value,
    })
    localStorage.setItem(`voted_${poll.value.code}`, 'true')
    voted.value = true
  } catch (e) {
    submitError.value = true
    if (e.message?.includes('already')) alreadyVoted.value = true
  } finally {
    submitting.value = false
  }
}
</script>
