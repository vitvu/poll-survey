<template>
  <div class="container">
    <div class="mb-4">
      <button class="btn btn-ghost btn-sm" @click="router.back()">
        <ChevronLeft :size="15" /> Back
      </button>
    </div>

    <div class="card">
      <p class="text-[11px] font-bold uppercase tracking-widest text-[--text-4] mb-1">Create New</p>
      <h1 class="text-[20px] font-extrabold mb-5">Configure Your Poll</h1>

      <form @submit.prevent="submit">

        <div class="mb-4">
          <label class="block text-[13.5px] font-semibold text-[--text-2] mb-1.5">
            Question <span class="text-[--red]">*</span>
          </label>
          <input v-model="form.question" type="text" class="form-control"
            placeholder="e.g., What's your favorite programming language?" />
        </div>

        <!-- Question Type -->
        <div class="mb-4">
          <label class="block text-[13.5px] font-semibold text-[--text-2] mb-1.5">Question Type</label>
          <div class="grid grid-cols-2 gap-2">
            <div class="type-card" :class="{ active: form.questionType === 'Multiple Choice' }"
              @click="form.questionType = 'Multiple Choice'">
              <div class="type-card-icon" :class="{ active: form.questionType === 'Multiple Choice' }">
                <BarChart2 :size="18" :color="form.questionType === 'Multiple Choice' ? '#fff' : 'currentColor'" />
              </div>
              <div>
                <div class="text-[13px] font-bold text-[--text]"
                  :class="form.questionType === 'Multiple Choice' ? 'text-[--blue]' : ''">
                  Multiple Choice
                </div>
                <div class="text-[11.5px] text-[--text-4] mt-0.5">Choose one from many</div>
              </div>
              <CheckCircle2 v-if="form.questionType === 'Multiple Choice'"
                :size="15" class="absolute top-2 right-2 text-[--blue]" />
            </div>

            <div class="type-card" :class="{ active: form.questionType === 'Yes / No' }"
              @click="form.questionType = 'Yes / No'">
              <div class="type-card-icon" :class="{ active: form.questionType === 'Yes / No' }">
                <ToggleLeft :size="18" :color="form.questionType === 'Yes / No' ? '#fff' : 'currentColor'" />
              </div>
              <div>
                <div class="text-[13px] font-bold text-[--text]"
                  :class="form.questionType === 'Yes / No' ? 'text-[--blue]' : ''">
                  Yes / No
                </div>
                <div class="text-[11.5px] text-[--text-4] mt-0.5">Only 2 options</div>
              </div>
              <CheckCircle2 v-if="form.questionType === 'Yes / No'"
                :size="15" class="absolute top-2 right-2 text-[--blue]" />
            </div>

            <div class="type-card" :class="{ active: form.questionType === 'Rating' }"
              @click="form.questionType = 'Rating'">
              <div class="type-card-icon" :class="{ active: form.questionType === 'Rating' }">
                <Star :size="18" :color="form.questionType === 'Rating' ? '#fff' : 'currentColor'" />
              </div>
              <div>
                <div class="text-[13px] font-bold text-[--text]"
                  :class="form.questionType === 'Rating' ? 'text-[--blue]' : ''">
                  Star Rating
                </div>
                <div class="text-[11.5px] text-[--text-4] mt-0.5">Choose 1–5 stars</div>
              </div>
              <CheckCircle2 v-if="form.questionType === 'Rating'"
                :size="15" class="absolute top-2 right-2 text-[--blue]" />
            </div>

            <div class="type-card" :class="{ active: form.questionType === 'Open Text' }"
              @click="form.questionType = 'Open Text'">
              <div class="type-card-icon" :class="{ active: form.questionType === 'Open Text' }">
                <MessageSquare :size="18" :color="form.questionType === 'Open Text' ? '#fff' : 'currentColor'" />
              </div>
              <div>
                <div class="text-[13px] font-bold text-[--text]"
                  :class="form.questionType === 'Open Text' ? 'text-[--blue]' : ''">
                  Open Text
                </div>
                <div class="text-[11.5px] text-[--text-4] mt-0.5">Free text response</div>
              </div>
              <CheckCircle2 v-if="form.questionType === 'Open Text'"
                :size="15" class="absolute top-2 right-2 text-[--blue]" />
            </div>
          </div>
        </div>

        <!-- Poll Duration -->
        <div class="mb-4">
          <label class="block text-[13.5px] font-semibold text-[--text-2] mb-1.5">Poll Duration</label>
          <div class="flex gap-2">
            <label class="expire-opt" :class="{ active: expireMode === 'none' }" @click="expireMode = 'none'">
              <Infinity :size="15" /> No Limit
            </label>
            <label class="expire-opt" :class="{ active: expireMode === 'custom' }" @click="expireMode = 'custom'">
              <CalendarClock :size="15" /> Set Deadline
            </label>
          </div>
          <div v-if="expireMode === 'custom'" class="mt-2">
            <input v-model="form.expireAt" type="datetime-local" class="form-control max-w-[280px]" />
          </div>
        </div>

        <!-- Options (Multiple Choice only) -->
        <div v-if="form.questionType === 'Multiple Choice'">
          <hr class="border-none border-t border-[--border] my-5" />
          <div class="flex items-center justify-between mb-2">
            <label class="text-[13.5px] font-semibold text-[--text-2]">Options</label>
            <span class="text-[12px] text-[--text-3]">{{ countValidOptions() }} / 6</span>
          </div>
          <div class="flex flex-col gap-1.5">
            <div v-for="(opt, i) in form.options" :key="i" class="flex items-center gap-2">
              <span class="w-6 h-6 rounded-full shrink-0 bg-[--surface-3] border border-[--border]
                           text-[12px] font-bold text-[--text-4] flex items-center justify-center">
                {{ i + 1 }}
              </span>
              <input v-model="opt.text" type="text" class="form-control flex-1"
                :placeholder="'Option ' + (i + 1)" />
              <button type="button" class="btn btn-danger btn-sm"
                :disabled="form.options.length <= 2" @click="removeOption(i)">
                <X :size="13" />
              </button>
            </div>
          </div>
          <button v-if="form.options.length < 6" type="button"
            class="btn btn-ghost btn-sm mt-2" @click="addOption">
            <Plus :size="14" /> Add Option
          </button>
        </div>

        <hr class="border-none border-t border-[--border] my-5" />
        <button type="submit" class="btn btn-primary btn-lg w-full" :disabled="isLoading">
          <span v-if="isLoading" class="spinner"></span>
          {{ isLoading ? 'Creating...' : 'Create Poll' }}
        </button>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useToast } from 'vue-toastification'
import { ChevronLeft, CheckCircle2, Plus, X, Infinity, CalendarClock, BarChart2, ToggleLeft, Star, MessageSquare } from '@lucide/vue'
import { pollApi } from '../api'

const router = useRouter()
const toast  = useToast()

const isLoading = ref(false)
const expireMode = ref('none')

// default datetime for input: 5 minutes from now in local timezone
const getDefaultExpireDate = () => {
  const now = new Date()
  const fiveMinutesLater = new Date(now.getTime() + 5 * 60 * 1000)
  const localTime = new Date(fiveMinutesLater.getTime() - fiveMinutesLater.getTimezoneOffset() * 60000)
  return localTime.toISOString().slice(0, 16)
}

const form = ref({
  question:     '',
  questionType: 'Multiple Choice',
  expireAt:     getDefaultExpireDate(),
  options:      [{ text: '' }, { text: '' }],
})

const countValidOptions = () => {
  return form.value.options.filter(option => option.text.trim() !== '').length
}

const addOption = () => {
  if (form.value.options.length < 6) {
    form.value.options.push({ text: '' })
  }
}

const removeOption = (index) => {
  if (form.value.options.length > 2) {
    form.value.options.splice(index, 1)
  }
}

const saveCreatedPollCode = (pollCode) => {
  const savedCodes = localStorage.getItem('createdPolls')
  const existingCodes = JSON.parse(savedCodes || '[]')

  if (!existingCodes.includes(pollCode)) {
    existingCodes.push(pollCode)
    localStorage.setItem('createdPolls', JSON.stringify(existingCodes))
  }
}

// convert local datetime string to utc iso
const localDateTimeToUtcIso = (localDateTimeString) => {
  const date = new Date(localDateTimeString)
  return date.toISOString()
}

const submit = async () => {
  if (!form.value.question.trim()) {
    toast.error('Please enter a question.')
    return
  }
  if (form.value.questionType === 'Multiple Choice' && countValidOptions() < 2) {
    toast.error('Need at least 2 valid options.')
    return
  }

  isLoading.value = true

  try {
    const roomCode = Math.floor(100000 + Math.random() * 900000).toString()

    const noLimitExpireDate = new Date()
    noLimitExpireDate.setFullYear(noLimitExpireDate.getFullYear() + 100)

    const optionsToSend = []
    if (form.value.questionType === 'Multiple Choice') {
      for (const option of form.value.options) {
        if (option.text.trim() !== '') {
          optionsToSend.push({ text: option.text.trim() })
        }
      }
    }

    const payload = {
      code:         roomCode,
      question:     form.value.question.trim(),
      questionType: form.value.questionType,
      expireAt:     expireMode.value === 'custom'
                      ? localDateTimeToUtcIso(form.value.expireAt)
                      : noLimitExpireDate.toISOString(),
      options:      optionsToSend,
    }

    const response = await pollApi.createPoll(payload)
    const createdPoll = response.data.poll || response.data

    saveCreatedPollCode(createdPoll.code)
    toast.success('Poll created!')

    router.push({ name: 'Analytics', query: { code: createdPoll.code } })

  } catch (error) {
    toast.error(error.message || 'Failed to create poll.')
  } finally {
    isLoading.value = false
  }
}
</script>
