<template>
  <div class="container">
    <button class="btn btn-ghost btn-sm mb-5" @click="$router.push('/')">
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

    <template v-else-if="poll">

      <!-- Header Card -->
      <div class="card mb-3">
        <div class="flex items-start gap-3">
          <div class="flex-1">

            <div class="flex items-center gap-2 mb-2 flex-wrap">
              <span class="text-[22px] font-extrabold text-[--blue] tracking-widest">{{ poll.code }}</span>

              <span v-if="pollIsClosed" class="badge badge-red">Closed</span>
              <span v-else class="badge badge-green">
                <span class="live-dot"></span> Open
              </span>

              <span class="badge badge-gray">{{ questionTypeLabel }}</span>
            </div>

            <h1 class="text-[20px] font-extrabold text-[--text] tracking-tight mb-4">{{ poll.question }}</h1>

            <div class="flex items-center gap-2 flex-wrap mb-3">
              <router-link :to="'/vote/' + poll.code" class="btn btn-outline btn-sm" target="_blank">
                <ExternalLink :size="14" /> Vote Page
              </router-link>

              <button v-if="!pollIsClosed" class="btn btn-red btn-sm" @click="confirmStop = true">
                <StopCircle :size="14" /> Stop
              </button>
              <span v-else class="badge badge-red" style="padding:6px 12px;">Closed</span>

              <button class="btn btn-danger btn-sm" @click="confirmDelete = true">
                <Trash2 :size="14" /> Delete
              </button>
            </div>

            <span v-if="hubConnected" class="badge badge-green">
              <span class="live-dot"></span> Live
            </span>
            <span v-else class="badge badge-gray">
              <span class="live-dot"></span> Offline
            </span>

            <!-- Share link (read-only) -->
            <div class="mt-3">
              <input class="form-control text-[13.5px]" :value="shareLink" readonly style="background:var(--surface-2);" />
            </div>
          </div>

          <!-- QR Thumbnail -->
          <div class="w-[100px] h-[100px] shrink-0 cursor-pointer rounded-[--radius] overflow-hidden bg-white p-1 shadow-card hover:opacity-85 transition-opacity"
            @click="openQRModal">
            <canvas ref="qrThumbnailCanvas" class="w-full h-full"></canvas>
          </div>
        </div>
      </div>

      <!-- Total Votes -->
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

        <!-- Multiple Choice (type 1) -->
        <template v-if="poll.questionType === 1">
          <div v-if="choiceResults.length === 0" class="py-6 text-center text-[--text-4] text-[13.5px]">No votes yet.</div>
          <div v-else class="flex flex-col gap-3">
            <div v-for="item in choiceResults" :key="item.label">
              <div class="flex items-center justify-between mb-1.5">
                <span class="text-[14px] font-semibold text-[--text]">{{ item.label }}</span>
                <strong class="text-[15px] font-extrabold text-[--text-2]">{{ item.count }}</strong>
              </div>
              <div class="bar-track">
                <div class="bar-fill" :style="{ width: totalVotes > 0 ? (item.count / totalVotes * 100) + '%' : '0%' }"></div>
              </div>
            </div>
          </div>
        </template>

        <!-- Yes / No (type 2) -->
        <template v-else-if="poll.questionType === 2">
          <div v-if="choiceResults.length === 0" class="py-6 text-center text-[--text-4] text-[13.5px]">No votes yet.</div>
          <div v-else class="flex flex-col gap-3">
            <div v-for="item in choiceResults" :key="item.label">
              <div class="flex items-center justify-between mb-1.5">
                <span class="text-[14px] font-semibold text-[--text]">{{ item.label }}</span>
                <strong class="text-[15px] font-extrabold text-[--text-2]">{{ item.count }}</strong>
              </div>
              <div class="bar-track">
                <div class="bar-fill" :style="{ width: totalVotes > 0 ? (item.count / totalVotes * 100) + '%' : '0%' }"></div>
              </div>
            </div>
          </div>
        </template>

        <!-- Rating (type 3) -->
        <template v-else-if="poll.questionType === 3">
          <div v-if="ratingResponses.length === 0" class="py-6 text-center text-[--text-4] text-[13.5px]">No ratings yet.</div>
          <div v-else class="flex flex-col gap-2">
            <div v-for="(item, index) in ratingResponses" :key="index"
              class="flex items-center gap-2 p-2 bg-[--surface-2] border border-[--border] rounded-[--radius]">
              <Star v-for="star in 5" :key="star" :size="16"
                :fill="star <= Number(item.voteValue) ? 'var(--amber)' : 'transparent'"
                :color="star <= Number(item.voteValue) ? 'var(--amber)' : 'var(--border-2)'" />
            </div>
          </div>
        </template>

        <!-- Open Text (type 4) -->
        <template v-else-if="poll.questionType === 4">
          <div v-if="textResponses.length === 0" class="py-6 text-center text-[--text-4] text-[13.5px]">No responses yet.</div>
          <div v-for="(text, index) in textResponses" :key="index"
            class="p-3 bg-[--surface-2] border border-[--border] rounded-[--radius] text-[14px] text-[--text-3] mb-2">
            {{ text }}
          </div>
        </template>
      </div>
    </template>

    <!-- QR Modal -->
    <div v-if="showQRModal" class="modal-bg" @click.self="showQRModal = false">
      <div class="modal-box">
        <button class="modal-close" @click="showQRModal = false"><X :size="20" /></button>
        <div class="qr-modal-canvas bg-[--surface-2] border border-[--border] rounded-[--radius-lg] p-5 flex items-center justify-center">
          <canvas ref="qrLargeCanvas" class="rounded-[--radius]"></canvas>
        </div>
        <div class="flex items-center justify-center mt-3 p-2.5 bg-[--surface-2] border border-[--border] rounded-[--radius]">
          <span class="text-[20px] font-extrabold text-[--blue] tracking-[2px]">{{ poll && poll.code }}</span>
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

<script>
import { getPollByCode, getVoteData, updatePoll, deletePoll } from '../api'
import { connectPollHub } from '../usePollHub'
import QRCode from 'qrcode'
import { ChevronLeft, Lock, ExternalLink, StopCircle, Trash2, BarChart2, Star, X } from '@lucide/vue'

const QUESTION_TYPE_LABELS = { 1: 'Multiple Choice', 2: 'Yes / No', 3: 'Rating', 4: 'Open Text' }
const YES_NO_LABELS = { '1': 'Yes', '0': 'No' }

export default {
  name: 'AnalyticsView',

  components: { ChevronLeft, Lock, ExternalLink, StopCircle, Trash2, BarChart2, Star, X },

  data() {
    return {
      poll:            null,
      accessDenied:    false,
      confirmStop:     false,
      confirmDelete:   false,
      showQRModal:     false,
      totalVotes:      0,
      choiceResults:   [],  // used for type 1 (Multiple Choice) and type 2 (Yes/No)
      ratingResponses: [],  // used for type 3 (Rating)
      textResponses:   [],  // used for type 4 (Open Text)
      hubConnected:    false,
      fallbackTimer:   null,
      hubStop:         null,
    }
  },

  computed: {
    pollCode()         { return this.$route.query.code },
    shareLink()        { return `${location.origin}/vote/${this.poll?.code}` },
    questionTypeLabel(){ return QUESTION_TYPE_LABELS[this.poll?.questionType] ?? '' },
    pollIsClosed() {
      if (!this.poll) return true
      if (this.poll.status !== 0) return true
      return false
    },
  },

  async created() {
    if (!this.pollCode) return

    const createdPolls = JSON.parse(localStorage.getItem('createdPolls') || '[]')
    if (!createdPolls.includes(this.pollCode)) {
      this.accessDenied = true
      return
    }

    try {
      const { data } = await getPollByCode(this.pollCode)
      this.poll = data
      await this.loadResults()

      if (!this.pollIsClosed) {
        this.startHub()
        this.fallbackTimer = setInterval(() => {
          if (!this.hubConnected) this.loadResults()
        }, 6000)
      }

      setTimeout(() => this.renderQRCode(this.$refs.qrThumbnailCanvas, 100), 100)
    } catch {
      this.$toast.error('Failed to load poll data.')
    }
  },

  unmounted() {
    clearInterval(this.fallbackTimer)
  },

  methods: {
    async loadResults() {
      if (!this.poll) return
      const { data } = await getVoteData(this.pollCode)
      const { total, summary, votes } = data
      this.totalVotes = total
      const type = this.poll.questionType

      if (type === 1) {
        // Multiple Choice: match each summary item to its option label
        this.choiceResults = []
        for (const item of summary) {
          const option = this.poll.options.find(o => o.id === item.optionId)
          this.choiceResults.push({ label: option ? option.text : '(unknown)', count: item.count })
        }

      } else if (type === 2) {
        // Yes / No: convert '1'/'0' to readable label
        this.choiceResults = []
        for (const item of summary) {
          this.choiceResults.push({ label: YES_NO_LABELS[item.voteValue] ?? item.voteValue, count: item.count })
        }

      } else if (type === 3) {
        this.ratingResponses = votes

      } else if (type === 4) {
        // Open Text: collect non-empty responses
        this.textResponses = []
        for (const vote of votes) {
          if (vote.voteValue && vote.voteValue.trim()) {
            this.textResponses.push(vote.voteValue)
          }
        }
      }
    },

    async renderQRCode(canvas, size) {
      if (!canvas) return
      try {
        await QRCode.toCanvas(canvas, this.shareLink, { width: size, margin: 2, color: { dark: '#1e293b', light: '#ffffff' } })
      } catch (error) {
        console.error('QR render failed:', error)
      }
    },

    openQRModal() {
      this.showQRModal = true
      setTimeout(() => this.renderQRCode(this.$refs.qrLargeCanvas, 320), 50)
    },

    startHub() {
      const hub = connectPollHub(this.pollCode, {
        onUpdate: async (data) => {
          this.totalVotes = data.totalVotes ?? data.total ?? 0
          await this.loadResults()
        },
        onPollClosed: () => {
          this.poll.status = 1
          clearInterval(this.fallbackTimer)
          this.fallbackTimer = null
          hub.stop()
        },
        onConnected:    () => { this.hubConnected = true },
        onDisconnected: () => { this.hubConnected = false },
      })

      this.hubStop = hub.stop
      hub.start()
    },

    async stopPoll() {
      this.confirmStop = false
      try {
        await updatePoll(this.pollCode, { ...this.poll, status: 1 })
        this.poll.status = 1
        clearInterval(this.fallbackTimer)
        this.fallbackTimer = null
        if (this.hubStop) this.hubStop()
        await this.broadcastPollClosed()
        this.$toast.success('Poll stopped. All voters notified.')
      } catch {
        this.$toast.error('Failed to stop poll.')
      }
    },

    async broadcastPollClosed() {
      try {
        const url = process.env.VUE_APP_VOTE_SERVICE_URL || 'https://localhost:5002'
        await fetch(`${url}/api/Votes/broadcast-closed`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ pollCode: this.pollCode }),
        })
      } catch (error) {
        console.error('Broadcast failed:', error)
      }
    },

    async deletePoll() {
      this.confirmDelete = false
      try {
        await deletePoll(this.pollCode)
        const saved = JSON.parse(localStorage.getItem('createdPolls') || '[]')
        const updated = []
        for (const code of saved) {
          if (code !== this.pollCode) updated.push(code)
        }
        localStorage.setItem('createdPolls', JSON.stringify(updated))
        this.$toast.success('Poll deleted.')
        this.$router.push('/')
      } catch {
        this.$toast.error('Failed to delete poll.')
      }
    },
  },
}
</script>
