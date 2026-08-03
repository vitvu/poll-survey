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
                <Clock :size="14" /> {{ isNoLimit(poll.expireAt) ? 'No Limit' : new Date(poll.expireAt).toLocaleString('en-US') }}
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
import { useToast } from 'vue-toastification'
import { pollApi } from '../api'
import { usePollHub } from '../usePollHub'
import QRCode from 'qrcode'
import {
  ChevronLeft, Lock, Copy, ExternalLink, StopCircle, Trash2,
  Calendar, Clock, BarChart2, Star, Clipboard, X,
} from '@lucide/vue'

const route  = useRoute()
const router = useRouter()
const toast  = useToast()

const pollCode = route.query.code

const accessDenied  = ref(false)
const confirmStop   = ref(false)
const confirmDelete = ref(false)
const showQRModal   = ref(false)

const poll              = ref(null)
const totalVotes        = ref(0)
const choiceResults     = ref([])
const openTextResponses = ref([])
const ratingVoteList    = ref([])

const qrThumbnailCanvas = ref(null)
const qrLargeCanvas     = ref(null)

const isPollClosed = () => {
  if (!poll.value) return true
  if (poll.value.status !== 'Active') return true
  if (new Date(poll.value.expireAt) <= new Date()) return true
  return false
}

const isNoLimit = (expireAtString) => {
  const expireDate = new Date(expireAtString)
  const now = new Date()
  const yearsUntilExpire = expireDate.getFullYear() - now.getFullYear()
  return yearsUntilExpire > 50
}

const shareLink = () => `${location.origin}/vote/${poll.value?.code}`

const loadResults = async () => {
  if (!poll.value) return

  const response = await pollApi.getVoteData(pollCode)
  const { total, summary, votes } = response.data

  totalVotes.value = total

  const questionType = poll.value.questionType

  if (questionType === 'Multiple Choice') {
    const resultsWithName = summary.map(item => ({
      optionId: item.optionId,
      optionText: poll.value.options.find(o => o.id === item.optionId)?.text || '(unknown)',
      count: item.count,
    }))
    resultsWithName.sort((a, b) => b.count - a.count)
    choiceResults.value = resultsWithName

  } else if (questionType === 'Yes / No') {
    // map votevalue to yes/no labels
    const yesNoMap = { '1': 'Yes', '0': 'No' }
    const resultsWithName = summary.map(item => ({
      optionId: 0,
      optionText: yesNoMap[item.voteValue] || item.voteValue,
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

const renderQRCode = async (canvasElement, size) => {
  if (!canvasElement) return
  try {
    await QRCode.toCanvas(canvasElement, shareLink(), {
      width: size,
      margin: 2,
      color: {
        dark: '#1e293b',
        light: '#ffffff',
      },
    })
  } catch (error) {
    console.error('QR render failed:', error)
  }
}

// wait for vue to render canvas before drawing qr
const openQRModal = () => {
  showQRModal.value = true
  setTimeout(() => {
    renderQRCode(qrLargeCanvas.value, 320)
  }, 50)
}

const { connected: isHubConnected, start: startHub } = usePollHub(pollCode, async (hubData) => {
  totalVotes.value = hubData.totalVotes ?? hubData.total ?? 0
  await loadResults()
})

let fallbackInterval = null

onMounted(async () => {
  if (!pollCode) return

  // check access permission
  const savedCodes = localStorage.getItem('createdPolls')
  const createdPollCodes = JSON.parse(savedCodes || '[]')

  if (!createdPollCodes.includes(pollCode)) {
    accessDenied.value = true
    return
  }

  try {
    const pollResponse = await pollApi.getPollByCode(pollCode)
    poll.value = pollResponse.data

    await loadResults()

    startHub()

    // fallback polling when signalr disconnected
    fallbackInterval = setInterval(() => {
      if (!isHubConnected.value) {
        loadResults()
      }
    }, 6000)

    setTimeout(() => {
      renderQRCode(qrThumbnailCanvas.value, 100)
    }, 100)

  } catch {
    toast.error('Failed to load data.')
  }
})

onUnmounted(() => {
  clearInterval(fallbackInterval)
})

const stopPoll = async () => {
  confirmStop.value = false

  try {
    await pollApi.updatePoll(pollCode, { ...poll.value, status: 'Closed' })
    poll.value.status = 'Closed'
    toast.success('Poll stopped.')
  } catch {
    toast.error('Failed to stop poll.')
  }
}

const deletePoll = async () => {
  confirmDelete.value = false

  try {
    await pollApi.deletePoll(pollCode)

    const savedCodes = localStorage.getItem('createdPolls')
    const createdPollCodes = JSON.parse(savedCodes || '[]')
    const updatedCodes = createdPollCodes.filter(code => code !== pollCode)
    localStorage.setItem('createdPolls', JSON.stringify(updatedCodes))

    toast.success('Poll deleted.')
    router.push('/')
  } catch {
    toast.error('Failed to delete poll.')
  }
}

const copyShareLink = async () => {
  try {
    await navigator.clipboard.writeText(shareLink())
    toast.success('Link copied!')
  } catch {
    toast.error('Failed to copy.')
  }
}
</script>

