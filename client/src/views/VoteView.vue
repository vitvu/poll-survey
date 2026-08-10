<template>
  <div class="vote-page">

    <div v-if="pollNotFound" class="card max-w-[440px] w-full mx-auto text-center">
      <div class="w-16 h-16 rounded-full bg-[--red-light] border border-[#fca5a5] flex items-center justify-center mx-auto mb-3">
        <SearchX :size="28" />
      </div>
      <h2 class="text-[18px] font-extrabold text-[--text] mb-1.5">Poll Not Found</h2>
      <p class="text-[14px] text-[--text-3] mb-5">Code <strong>{{ pollCode }}</strong> doesn't exist.</p>
      <router-link to="/" class="btn btn-outline"><ArrowLeft :size="14" /> Go Home</router-link>
    </div>

    <div v-else-if="alreadyVoted" class="card max-w-[440px] w-full mx-auto text-center">
      <div class="w-16 h-16 rounded-full bg-[--green-light] border border-[#86efac] flex items-center justify-center mx-auto mb-3">
        <CheckCircle2 :size="28" />
      </div>
      <h2 class="text-[18px] font-extrabold text-[--text] mb-1.5">Already Voted</h2>
      <p class="text-[14px] text-[--text-3]">You have already participated in this poll.</p>
    </div>

    <div v-else-if="voteSubmitted" class="card max-w-[440px] w-full mx-auto text-center">
      <div class="w-16 h-16 rounded-full bg-[--green-light] border border-[#86efac] flex items-center justify-center mx-auto mb-3">
        <CheckCircle2 :size="28" />
      </div>
      <h2 class="text-[18px] font-extrabold text-[--text] mb-1.5">Vote Recorded!</h2>
      <p class="text-[14px] text-[--text-3]">Thank you for participating.</p>
    </div>

    <div v-else-if="poll" class="vote-card">
      <div class="p-6 pb-5 border-b border-[--border]">
        <div class="flex items-center justify-between mb-2">
          <span class="badge badge-blue">{{ poll.code }}</span>
          <span class="badge" :class="pollIsClosed ? 'badge-red' : 'badge-green'">
            <span v-if="!pollIsClosed" class="live-dot"></span>
            {{ pollIsClosed ? 'Closed' : 'Live' }}
          </span>
        </div>
        <h1 class="text-[19px] font-extrabold text-[--text] tracking-tight leading-snug my-2">{{ poll.question }}</h1>
        <p class="text-[12.5px] text-[--text-4] font-semibold">{{ questionTypeLabel }}</p>
      </div>

      <div v-if="pollIsClosed"
        class="mx-6 mt-4 flex items-center gap-2 p-3 rounded-[--radius] bg-[--red-light] text-[#991b1b] border border-[#fca5a5] text-[13.5px] font-medium">
        <Lock :size="15" /> This poll has ended.
      </div>

      <form v-else class="p-5" @submit.prevent="submitVote">

        <!-- Multiple Choice (type 1) -->
        <div v-if="poll.questionType === 1" class="flex flex-col gap-2 mb-5">
          <label v-for="option in poll.options" :key="option.id"
            class="vote-option"
            :class="{ selected: selectedOptionId === option.id, error: hasError && !selectedOptionId }">
            <input type="radio" :value="option.id" v-model.number="selectedOptionId" class="sr-only" />
            <div class="vote-option-radio">
              <div class="radio-inner" :class="{ filled: selectedOptionId === option.id }"></div>
            </div>
            <span class="flex-1 text-[14.5px] font-medium"
              :class="selectedOptionId === option.id ? 'text-[--blue] font-semibold' : 'text-[--text]'">
              {{ option.text }}
            </span>
            <Check v-if="selectedOptionId === option.id" :size="15" class="text-[--blue]" />
          </label>
          <p v-if="hasError && !selectedOptionId" class="inline-flex items-center gap-1 text-[12.5px] text-[--red] font-semibold">
            <AlertCircle :size="13" /> Please select an option
          </p>
        </div>

        <!-- Yes / No (type 2) -->
        <div v-else-if="poll.questionType === 2" class="flex flex-col gap-2 mb-5">
          <label v-for="option in yesNoOptions" :key="option.value"
            class="vote-option"
            :class="{ selected: voteValue === option.value, error: hasError && !voteValue }">
            <input type="radio" :value="option.value" v-model="voteValue" class="sr-only" />
            <div class="vote-option-radio">
              <div class="radio-inner" :class="{ filled: voteValue === option.value }"></div>
            </div>
            <span class="flex-1 text-[14.5px] font-medium"
              :class="voteValue === option.value ? 'text-[--blue] font-semibold' : 'text-[--text]'">
              {{ option.label }}
            </span>
            <Check v-if="voteValue === option.value" :size="15" class="text-[--blue]" />
          </label>
          <p v-if="hasError && !voteValue" class="inline-flex items-center gap-1 text-[12.5px] text-[--red] font-semibold">
            <AlertCircle :size="13" /> Please select an option
          </p>
        </div>

        <!-- Rating (type 3) -->
        <div v-else-if="poll.questionType === 3" class="text-center py-2 pb-5">
          <div class="flex gap-1 justify-center">
            <button v-for="star in 5" :key="star" type="button"
              class="star-btn" :class="{ on: star <= Number(voteValue) }"
              @click="voteValue = String(star)">
              <Star :size="36" :fill="star <= Number(voteValue) ? 'currentColor' : 'none'" />
            </button>
          </div>
          <p v-if="hasError && !voteValue" class="inline-flex items-center gap-1 text-[12.5px] text-[--red] font-semibold mt-2">
            <AlertCircle :size="13" /> Please select a rating
          </p>
        </div>

        <!-- Open Text (type 4) -->
        <div v-else-if="poll.questionType === 4" class="mb-5">
          <textarea v-model="voteValue" rows="4" class="form-control"
            :class="{ 'is-error': hasError && !voteValue.trim() }"
            placeholder="Enter your response..."></textarea>
          <p v-if="hasError && !voteValue.trim()" class="inline-flex items-center gap-1 text-[12.5px] text-[--red] font-semibold mt-2">
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

  </div>
</template>

<script>
import { getPollByCode, submitVote } from '../api'
import { connectPollHub } from '../usePollHub'
import { getVoterToken } from '../voterToken'
import { CheckCircle2, Check, AlertCircle, Lock, Send, Star, ArrowLeft, SearchX } from '@lucide/vue'

const QUESTION_TYPE_LABELS = { 1: 'Multiple Choice', 2: 'Yes / No', 3: 'Rating', 4: 'Open Text' }

export default {
  name: 'VoteView',

  components: { CheckCircle2, Check, AlertCircle, Lock, Send, Star, ArrowLeft, SearchX },

  data() {
    return {
      poll:           null,
      pollNotFound:   false,
      alreadyVoted:   false,
      voteSubmitted:  false,
      isSubmitting:   false,
      hasError:       false,
      selectedOptionId: null,
      voteValue:        '',
      hubStop:          null,
      yesNoOptions: [
        { label: 'Yes', value: '1' },
        { label: 'No',  value: '0' },
      ],
    }
  },

  computed: {
    pollCode()         { return this.$route.params.code || '' },
    questionTypeLabel(){ return QUESTION_TYPE_LABELS[this.poll?.questionType] ?? '' },
    pollIsClosed() {
      if (!this.poll) return true
      if (this.poll.status !== 1) return true
      return false
    },
  },

  async created() {
    if (!this.pollCode) { this.$router.push('/'); return }
    await this.loadPoll()
    if (this.poll && !this.pollIsClosed) this.startHub()
  },

  methods: {
    async loadPoll() {
      try {
        const pollData = await getPollByCode(this.pollCode)
        this.poll = pollData
        if (localStorage.getItem(`voted_${this.pollCode}`) === 'true') {
          this.alreadyVoted = true
        }
      } catch {
        this.pollNotFound = true
      }
    },

    startHub() {
      const hub = connectPollHub(this.pollCode, {
        onPollClosed: () => {
          if (this.poll) {
            this.poll.status = 0
          }
          if (!this.voteSubmitted && !this.alreadyVoted) {
            this.$toast.warning('This poll has ended.')
          }
          hub.stop()
        },
      })
      this.hubStop = hub.stop
      hub.start()
    },

    async submitVote() {
      const type = this.poll.questionType
      this.hasError = false

      if (type === 1 && !this.selectedOptionId) { this.hasError = true; return }
      if (type === 2 && !this.voteValue)        { this.hasError = true; return }
      if (type === 3 && !this.voteValue)        { this.hasError = true; return }
      if (type === 4 && !this.voteValue.trim()) { this.hasError = true; return }

      this.isSubmitting = true
      try {
        await submitVote({
          pollCode:   this.poll.code,
          voterToken: getVoterToken(),
          optionId:   type === 1 ? (this.selectedOptionId || 0) : 0,
          voteValue:  type === 1 ? '' : String(this.voteValue),
        })
        localStorage.setItem(`voted_${this.poll.code}`, 'true')
        this.voteSubmitted = true
        this.$toast.success('Your vote has been recorded!')

      } catch (error) {
        const message = error.message || ''
        if (message.includes('already')) {
          this.alreadyVoted = true
          this.$toast.info('You have already voted in this poll.')
        } else if (message.includes('closed')) {
          this.poll.status = 0
          this.$toast.error('This poll has ended.')
        } else {
          this.hasError = true
          this.$toast.error(message || 'Failed to submit vote. Please try again.')
        }
      } finally {
        this.isSubmitting = false
      }
    },
  },
}
</script>
