<template>
  <div class="vote-page">

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

    <div v-else-if="alreadyVoted" class="card max-w-[440px] w-full mx-auto text-center">
      <div class="w-16 h-16 rounded-full bg-[--green-light] border border-[#86efac]
                  flex items-center justify-center mx-auto mb-3">
        <CheckCircle2 :size="28" />
      </div>
      <h2 class="text-[18px] font-extrabold text-[--text] mb-1.5">Already Voted</h2>
      <p class="text-[14px] text-[--text-3]">You have already participated in this poll.</p>
    </div>

    <div v-else-if="voteSubmitted" class="card max-w-[440px] w-full mx-auto text-center">
      <div class="w-16 h-16 rounded-full bg-[--green-light] border border-[#86efac]
                  flex items-center justify-center mx-auto mb-3">
        <CheckCircle2 :size="28" />
      </div>
      <h2 class="text-[18px] font-extrabold text-[--text] mb-1.5">Vote Recorded!</h2>
      <p class="text-[14px] text-[--text-3]">Thank you for participating.</p>
    </div>

    <div v-else-if="poll" class="vote-card">

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

      <div v-if="isPollExpired()"
        class="mx-6 mt-4 flex items-center gap-2 p-3 rounded-[--radius]
               bg-[--red-light] text-[#991b1b] border border-[#fca5a5] text-[13.5px] font-medium">
        <Lock :size="15" /> This poll has ended.
      </div>

      <form v-else class="p-5" @submit.prevent="submitVote">

        <div v-if="poll.questionType === 'Multiple Choice'"
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
            <input type="radio" :value="option.id" v-model="selectedOptionId" class="sr-only" />

            <div class="vote-option-radio">
              <div class="radio-inner" :class="{ filled: selectedOptionId === option.id }"></div>
            </div>

            <span class="flex-1 text-[14.5px] font-medium"
              :class="selectedOptionId === option.id ? 'text-[--blue] font-semibold' : 'text-[--text]'">
              {{ option.text }}
            </span>

            <span v-if="selectedOptionId === option.id" class="text-[--blue]">
              <Check :size="15" />
            </span>
          </label>

          <p v-if="hasSubmitError && !selectedOptionId"
            class="inline-flex items-center gap-1 text-[12.5px] text-[--red] font-semibold">
            <AlertCircle :size="13" /> Please select an option
          </p>
        </div>

        <!-- yes/no uses votevalue instead of options from backend -->
        <div v-else-if="poll.questionType === 'Yes / No'"
          class="flex flex-col gap-2 mb-5">
          <label
            v-for="option in [{ label: 'Yes', value: '1' }, { label: 'No', value: '0' }]"
            :key="option.value"
            class="vote-option"
            :class="{
              selected: voteValue === option.value,
              error: hasSubmitError && !voteValue
            }"
          >
            <input type="radio" :value="option.value" v-model="voteValue" class="sr-only" />

            <div class="vote-option-radio">
              <div class="radio-inner" :class="{ filled: voteValue === option.value }"></div>
            </div>

            <span class="flex-1 text-[14.5px] font-medium"
              :class="voteValue === option.value ? 'text-[--blue] font-semibold' : 'text-[--text]'">
              {{ option.label }}
            </span>

            <span v-if="voteValue === option.value" class="text-[--blue]">
              <Check :size="15" />
            </span>
          </label>

          <p v-if="hasSubmitError && !voteValue"
            class="inline-flex items-center gap-1 text-[12.5px] text-[--red] font-semibold">
            <AlertCircle :size="13" /> Please select an option
          </p>
        </div>

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
              <Star :size="36" :fill="starNumber <= Number(voteValue) ? 'currentColor' : 'none'" />
            </button>
          </div>

          <p v-if="hasSubmitError && !voteValue"
            class="inline-flex items-center gap-1 text-[12.5px] text-[--red] font-semibold mt-2">
            <AlertCircle :size="13" /> Please select a rating
          </p>
        </div>

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

    <!-- manual code entry form -->
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
import { getVoterToken } from '../voterToken'

const route = useRoute()
const pollCodeFromUrl = route.params.code || ''

const poll = ref(null)
const pollNotFound = ref(false)
const alreadyVoted = ref(false)
const voteSubmitted = ref(false)

const selectedOptionId = ref(null)
const voteValue = ref('')
const hasSubmitError = ref(false)
const isSubmitting = ref(false)

const manualCode = ref('')
const manualCodeError = ref('')
const isLoadingManual = ref(false)

const isPollExpired = () => {
  if (!poll.value) return true
  if (poll.value.status !== 'Active') return true
  if (new Date(poll.value.expireAt) <= new Date()) return true
  return false
}

const loadPoll = async (pollCode) => {
  pollNotFound.value = false

  try {
    const response = await pollApi.checkPoll(pollCode)
    poll.value = response.data

    const hasVotedBefore = localStorage.getItem(`voted_${pollCode}`) === 'true'
    if (hasVotedBefore) {
      alreadyVoted.value = true
    }

  } catch {
    pollNotFound.value = true
  }
}

const loadPollByManualCode = async () => {
  if (manualCode.value.length < 6) {
    manualCodeError.value = 'Please enter all 6 digits'
    return
  }

  isLoadingManual.value = true
  manualCodeError.value = ''

  await loadPoll(manualCode.value)

  if (pollNotFound.value) {
    manualCodeError.value = 'Poll not found'
  }

  isLoadingManual.value = false
}

const submitVote = async () => {
  const questionType = poll.value.questionType

  // validate based on question type
  if (questionType === 'Multiple Choice') {
    if (selectedOptionId.value === null) {
      hasSubmitError.value = true
      return
    }
  } else if (questionType === 'Yes / No') {
    if (voteValue.value === '') {
      hasSubmitError.value = true
      return
    }
  } else if (questionType === 'Rating') {
    if (voteValue.value === '') {
      hasSubmitError.value = true
      return
    }
  } else if (questionType === 'Open Text') {
    if (voteValue.value.trim() === '') {
      hasSubmitError.value = true
      return
    }
  }

  hasSubmitError.value = false
  isSubmitting.value = true

  try {
    await pollApi.submitVote({
      pollCode: poll.value.code,
      voterToken: getVoterToken(),
      optionId: questionType === 'Multiple Choice' ? (selectedOptionId.value || 0) : 0,
      voteValue: questionType === 'Multiple Choice' ? '' : voteValue.value,
    })

    localStorage.setItem(`voted_${poll.value.code}`, 'true')
    voteSubmitted.value = true

  } catch (error) {
    if (error.message && error.message.includes('already')) {
      alreadyVoted.value = true
    } else {
      hasSubmitError.value = true
    }
  } finally {
    isSubmitting.value = false
  }
}

onMounted(() => {
  if (pollCodeFromUrl) {
    loadPoll(pollCodeFromUrl)
  }
})
</script>
