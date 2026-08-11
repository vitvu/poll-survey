<template>
  <div class="container">
    <button class="btn btn-ghost btn-sm mb-5" @click="$router.push('/')">
      <ChevronLeft :size="15" /> Home
    </button>

    <div v-if="accessDenied" class="card text-center max-w-[420px] mx-auto">
      <div class="flex items-center justify-center w-14 h-14 rounded-full bg-[--red-light] border border-[#fca5a5] mx-auto mb-4">
        <Lock :size="26" color="var(--red)" />
      </div>
      <h2 class="text-[17px] font-bold mb-1">Access Denied</h2>
      <p class="text-[14px] text-[--text-3] mb-5">Only the creator can view this page.</p>
      <router-link to="/" class="btn btn-ghost">Go Home</router-link>
    </div>

    <template v-else-if="poll">

      <div class="card mb-3">
        <div class="flex items-start gap-3">
          <div class="flex-1">

            <div class="flex items-center gap-2 mb-2 flex-wrap">
              <span class="text-[22px] font-extrabold text-[--blue] tracking-widest">{{ poll.code }}</span>
              <span v-if="pollIsClosed" class="badge badge-red">Closed</span>
              <span v-else class="badge badge-green"><span class="live-dot"></span> Open</span>
              <span class="badge badge-gray">{{ questionTypeLabel }}</span>
            </div>

            <h1 class="text-[20px] font-extrabold text-[--text] tracking-tight mb-4">{{ poll.question }}</h1>

            <div class="flex items-center gap-2 flex-wrap mb-3">
              <router-link :to="'/vote/' + poll.code" class="btn btn-outline btn-sm" target="_blank">
                <ExternalLink :size="14" /> Vote Page
              </router-link>

              <button v-if="!pollIsClosed" class="btn btn-red btn-sm" @click="stopPoll">
                <StopCircle :size="14" /> Stop
              </button>
              <span v-else class="badge badge-red" style="padding:6px 12px;">Closed</span>

              <button class="btn btn-danger btn-sm" @click="deletePoll">
                <Trash2 :size="14" /> Delete
              </button>
            </div>

            <span v-if="hubConnected" class="badge badge-green">
              <span class="live-dot"></span> Live
            </span>
            <span v-else class="badge badge-gray">
              <span class="live-dot"></span> Offline
            </span>

            <!-- Link -->
            <div class="mt-3">
              <input class="form-control text-[13.5px]" :value="shareLink" readonly style="background:var(--surface-2);" />
            </div>
          </div>

          <div class="w-[100px] h-[100px] shrink-0 rounded-[--radius] overflow-hidden bg-white p-1 shadow-card">
            <canvas ref="qrCanvas" class="w-full h-full"></canvas>
          </div>
        </div>
      </div>

      <div class="card mb-3 text-center">
        <div class="text-[13px] text-[--text-4] font-semibold uppercase tracking-wide mb-1">Total Votes</div>
        <div class="text-[40px] font-extrabold text-[--blue] leading-none">{{ totalVotes }}</div>
      </div>

      <div class="card mb-3">
        <div class="flex items-center text-[14px] font-bold text-[--text-2] pb-2.5 border-b border-[--border] mb-3.5">
          <BarChart2 :size="16" class="mr-1.5" /> Results
        </div>

        <!-- Multiple Choice (type 1) & Yes/No (type 2) -->
        <template v-if="poll.questionType === 1 || poll.questionType === 2">
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
  </div>
</template>

<script>
import { getPollByCode, getVoteData, updatePoll, deletePoll, notifyPollClosed } from '../api'
import { connectPollHub } from '../usePollHub'
import { useToast } from 'vue-toastification'
import QRCode from 'qrcode'
import { ChevronLeft, Lock, ExternalLink, StopCircle, Trash2, BarChart2, Star } from '@lucide/vue'

const QUESTION_TYPE_LABELS = { 1: 'Multiple Choice', 2: 'Yes / No', 3: 'Rating', 4: 'Open Text' }

export default {
  name: 'AnalyticsView',

  components: {
    ChevronLeft,
    Lock,
    ExternalLink,
    StopCircle,
    Trash2,
    BarChart2,
    Star,
  },

  data() {
    return {
      poll: null,
      accessDenied: false,
      totalVotes: 0,
      choiceResults: [],
      ratingResponses: [],
      textResponses: [],
      hubConnected: false,
      hubStop: null,
      toast: useToast(),
    }
  },

  computed: {
    pollCode() {
      return this.$route.query.code
    },

    shareLink() {
      return `${location.origin}/vote/${this.poll?.code}`
    },

    questionTypeLabel() {
      return QUESTION_TYPE_LABELS[this.poll?.questionType] ?? ''
    },

    pollIsClosed() {
      return !this.poll || this.poll.status !== 1
    },
  },

  async created() {
    if (!this.pollCode) {
      return
    }

    try {
      this.poll = await getPollByCode(this.pollCode)

      const createdPolls = JSON.parse(localStorage.getItem('createdPolls') || '[]')
      if (!createdPolls.includes(this.pollCode)) {
        this.accessDenied = true
        return
      }

      await this.loadResults()

      if (!this.pollIsClosed) {
        this.startHub()
      }

      setTimeout(() => this.renderQR(), 100)
    } catch (error) {
      this.toast.error('Failed to load poll data.')
    }
  },

  unmounted() {
    // Dừng kết nối SignalR khi rời khỏi component
    if (this.hubStop) {
      this.hubStop()
    }
  },

  methods: {
    async renderQR() {
      const canvas = this.$refs.qrCanvas
      if (!canvas) {
        return
      }

      try {
        await QRCode.toCanvas(canvas, this.shareLink, {
          width: 100,
          margin: 2,
          color: {
            dark: '#1e293b',
            light: '#ffffff',
          },
        })
      } catch (error) {
        console.error('QR render failed:', error)
      }
    },

    async loadResults() {
      if (!this.poll) {
        return
      }

      try {
        const { total, votes } = await getVoteData(this.pollCode)
        this.totalVotes = total
        const questionType = this.poll.questionType

        if (questionType === 1) {
          this.processMultipleChoiceResults(votes)
        } else if (questionType === 2) {
          this.processYesNoResults(votes)
        } else if (questionType === 3) {
          this.ratingResponses = votes || []
        } else if (questionType === 4) {
          this.textResponses = votes
            .filter(vote => vote.voteValue && vote.voteValue.trim())
            .map(vote => vote.voteValue)
        }
      } catch (error) {
        this.toast.error('Failed to load results.')
      }
    },

    processMultipleChoiceResults(votes) {
      // Đếm số phiếu cho từng optionId
      const countByOption = {}
      
      for (const vote of votes) {
        if (!vote.optionId) {
          continue
        }
        
        const optionId = vote.optionId
        if (countByOption[optionId] === undefined) {
          countByOption[optionId] = 0
        }
        countByOption[optionId] += 1
      }

      // Tạo mảng kết quả với label từ poll.options
      this.choiceResults = []

      if (!this.poll.options || !Array.isArray(this.poll.options)) {
        return
      }

      for (const option of this.poll.options) {
        const count = countByOption[option.id] || 0
        
        this.choiceResults.push({
          label: option.text,
          count: count,
        })
      }
    },

    processYesNoResults(votes) {
      let countYes = 0
      let countNo = 0

      for (const vote of votes) {
        const answer = vote.voteValue ?? ''
        
        if (answer === '1') {
          countYes += 1
        } else if (answer === '0') {
          countNo += 1
        }
      }

      this.choiceResults = [
        {
          label: 'Yes',
          count: countYes,
        },
        {
          label: 'No',
          count: countNo,
        },
      ]
    },

    startHub() {
      const hub = connectPollHub(this.pollCode, {
        onUpdate: async (data) => {
          if (data.totalVotes !== undefined) {
            this.totalVotes = data.totalVotes
          } else if (data.total !== undefined) {
            this.totalVotes = data.total
          } else {
            this.totalVotes = 0
          }

          await this.loadResults()
        },

        onPollClosed: () => {
          this.poll.status = 0
          hub.stop()
        },

        onConnected: () => {
          this.hubConnected = true
        },

        onDisconnected: () => {
          this.hubConnected = false
        },
      })

      this.hubStop = hub.stop

      hub.start()
    },

    async stopPoll() {
      if (!this.poll) {
        return
      }

      const userConfirmed = confirm('Stop this poll? Voters will no longer be able to vote.')
      if (!userConfirmed) {
        return
      }

      try {
        await updatePoll(this.pollCode, { status: 0 })

        this.poll.status = 0

        if (this.hubStop) {
          this.hubStop()
        }

        await notifyPollClosed(this.pollCode)

        this.toast.success('Poll stopped. All voters notified.')
      } catch (error) {
        this.toast.error('Failed to stop poll.')
      }
    },

    async deletePoll() {
      const userConfirmed = confirm('Delete this poll? This cannot be undone.')
      if (!userConfirmed) {
        return
      }

      try {
        await deletePoll(this.pollCode)

        const createdPollsJson = localStorage.getItem('createdPolls')
        const createdPolls = createdPollsJson ? JSON.parse(createdPollsJson) : []

        const updatedPolls = []
        for (const pollCode of createdPolls) {
          if (pollCode !== this.pollCode) {
            updatedPolls.push(pollCode)
          }
        }

        localStorage.setItem('createdPolls', JSON.stringify(updatedPolls))

        this.toast.success('Poll deleted.')

        this.$router.push('/')
      } catch (error) {
        this.toast.error('Failed to delete poll.')
      }
    },
  },
}
</script>
