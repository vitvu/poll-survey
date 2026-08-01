<template>
  <div class="container">
    <div class="page-nav">
      <button class="btn btn-ghost btn-sm" @click="router.back()">
        <ChevronLeft :size="15" /> Back
      </button>
    </div>

    <div class="create-layout">
      <div class="card">
        <p class="label-upper mb-1">Create New</p>
        <h1 class="page-title mb-3" style="font-size:20px;">Configure Your Poll</h1>

        <form @submit.prevent="submit">
          <div class="form-group">
            <label class="form-label">
              Question
              <span style="color:var(--red)">*</span>
            </label>
            <input v-model="form.question" type="text" class="form-control"
              placeholder="e.g., What's your favorite programming language?" />
          </div>

          <div class="form-group">
            <label class="form-label">Question Type</label>
            <div class="type-grid">
              <div class="type-card" :class="{ active: form.questionType === 'Multiple Choice' }" @click="form.questionType = 'Multiple Choice'">
                <div class="type-card-icon" :class="{ active: form.questionType === 'Multiple Choice' }">
                  <BarChart2 :size="18" :color="form.questionType === 'Multiple Choice' ? '#fff' : 'currentColor'" />
                </div>
                <div>
                  <div class="type-card-name">Multiple Choice</div>
                  <div class="type-card-desc">Choose one from many</div>
                </div>
                <div v-if="form.questionType === 'Multiple Choice'" class="type-check">
                  <CheckCircle2 :size="15" />
                </div>
              </div>

              <div class="type-card" :class="{ active: form.questionType === 'Yes / No' }" @click="form.questionType = 'Yes / No'">
                <div class="type-card-icon" :class="{ active: form.questionType === 'Yes / No' }">
                  <ToggleLeft :size="18" :color="form.questionType === 'Yes / No' ? '#fff' : 'currentColor'" />
                </div>
                <div>
                  <div class="type-card-name">Yes / No</div>
                  <div class="type-card-desc">Only 2 options</div>
                </div>
                <div v-if="form.questionType === 'Yes / No'" class="type-check">
                  <CheckCircle2 :size="15" />
                </div>
              </div>

              <div class="type-card" :class="{ active: form.questionType === 'Rating' }" @click="form.questionType = 'Rating'">
                <div class="type-card-icon" :class="{ active: form.questionType === 'Rating' }">
                  <Star :size="18" :color="form.questionType === 'Rating' ? '#fff' : 'currentColor'" />
                </div>
                <div>
                  <div class="type-card-name">Star Rating</div>
                  <div class="type-card-desc">Choose 1–5 stars</div>
                </div>
                <div v-if="form.questionType === 'Rating'" class="type-check">
                  <CheckCircle2 :size="15" />
                </div>
              </div>

              <div class="type-card" :class="{ active: form.questionType === 'Open Text' }" @click="form.questionType = 'Open Text'">
                <div class="type-card-icon" :class="{ active: form.questionType === 'Open Text' }">
                  <MessageSquare :size="18" :color="form.questionType === 'Open Text' ? '#fff' : 'currentColor'" />
                </div>
                <div>
                  <div class="type-card-name">Open Text</div>
                  <div class="type-card-desc">Free text response</div>
                </div>
                <div v-if="form.questionType === 'Open Text'" class="type-check">
                  <CheckCircle2 :size="15" />
                </div>
              </div>
            </div>
          </div>

          <div class="form-group">
            <label class="form-label">Poll Duration</label>
            <div class="expire-row">
              <label class="expire-opt" :class="{ active: expireMode === 'none' }" @click="expireMode = 'none'">
                <Infinity :size="15" /> No Limit
              </label>
              <label class="expire-opt" :class="{ active: expireMode === 'custom' }" @click="expireMode = 'custom'">
                <CalendarClock :size="15" /> Set Deadline
              </label>
            </div>
            <div v-if="expireMode === 'custom'" class="mt-2">
              <input v-model="form.expireAt" type="datetime-local" class="form-control"
                style="max-width:280px;" />
            </div>
          </div>

          <div v-if="form.questionType === 'Multiple Choice'">
            <hr class="divider" />
            <div class="flex items-center justify-between mb-2">
              <label class="form-label mb-0">Options</label>
              <span class="fs-xs text-3">{{ validOptions }} / 6</span>
            </div>
            <div class="flex-col gap-1">
              <div v-for="(opt, i) in form.options" :key="i" class="flex items-center gap-2">
                <span class="opt-num">{{ i + 1 }}</span>
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

          <hr class="divider" />
          <button type="submit" class="btn btn-primary btn-lg w-full" :disabled="loading">
            <span v-if="loading" class="spinner"></span>
            {{ loading ? 'Creating...' : 'Create Poll' }}
          </button>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useToast } from 'vue-toastification'
import { ChevronLeft, CheckCircle2, Plus, X, Infinity, CalendarClock, BarChart2, ToggleLeft, Star, MessageSquare } from '@lucide/vue'
import { pollApi } from '../api'

const router = useRouter()
const toast = useToast()
const loading = ref(false)
const expireMode = ref('none')

const getDefaultExpire = () => {
  const d = new Date()
  d.setDate(d.getDate() + 1)
  return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 16)
}

const form = ref({
  question: '',
  questionType: 'Multiple Choice',
  expireAt: getDefaultExpire(),
  options: [{ text: '' }, { text: '' }],
})

const validOptions = computed(() => form.value.options.filter(o => o.text.trim()).length)

const addOption = () => {
  if (form.value.options.length < 6) form.value.options.push({ text: '' })
}

const removeOption = i => {
  if (form.value.options.length > 2) form.value.options.splice(i, 1)
}

const saveCreatorCode = code => {
  try {
    const list = JSON.parse(localStorage.getItem('createdPolls') || '[]')
    if (!list.includes(code)) {
      list.push(code)
      localStorage.setItem('createdPolls', JSON.stringify(list))
    }
  } catch (_) { /* localStorage parse error, ignore */ }
}

const submit = async () => {
  if (!form.value.question.trim()) {
    toast.error('Please enter a question.')
    return
  }

  if (form.value.questionType === 'Multiple Choice' && validOptions.value < 2) {
    toast.error('Need at least 2 valid options.')
    return
  }

  loading.value = true
  try {
    const code = Math.floor(100000 + Math.random() * 900000).toString()
    const far = new Date()
    far.setFullYear(far.getFullYear() + 100)

    const payload = {
      code,
      question: form.value.question.trim(),
      questionType: form.value.questionType,
      expireAt: expireMode.value === 'custom'
        ? new Date(form.value.expireAt).toISOString()
        : far.toISOString(),
      options: form.value.questionType === 'Multiple Choice'
        ? form.value.options
          .filter(o => o.text.trim())
          .map(o => ({ text: o.text.trim() }))
        : [],
    }

    const res = await pollApi.createPoll(payload)
    const pollData = res.data.poll || res.data

    saveCreatorCode(pollData.code)
    toast.success('Poll created!')
    router.push({ name: 'Analytics', query: { code: pollData.code } })
  } catch (e) {
    toast.error(e.message || 'Failed to create poll.')
  } finally {
    loading.value = false
  }
}
</script>
