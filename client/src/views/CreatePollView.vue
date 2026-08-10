<template>
  <div class="container">
    <div class="mb-4">
      <button class="btn btn-ghost btn-sm" @click="$router.back()">
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

            <div v-for="type in questionTypes" :key="type.value"
              class="type-card" :class="{ active: form.questionType === type.value }"
              @click="form.questionType = type.value">
              <div class="type-card-icon" :class="{ active: form.questionType === type.value }">
                <component :is="type.icon" :size="18" :color="form.questionType === type.value ? '#fff' : 'currentColor'" />
              </div>
              <div>
                <div class="text-[13px] font-bold text-[--text]"
                  :class="form.questionType === type.value ? 'text-[--blue]' : ''">
                  {{ type.label }}
                </div>
                <div class="text-[11.5px] text-[--text-4] mt-0.5">{{ type.description }}</div>
              </div>
              <CheckCircle2 v-if="form.questionType === type.value"
                :size="15" class="absolute top-2 right-2 text-[--blue]" />
            </div>

          </div>
        </div>

        <!-- Options — only for Multiple Choice (type 1) -->
        <div v-if="form.questionType === 1">
          <hr class="border-none border-t border-[--border] my-5" />
          <div class="flex items-center justify-between mb-2">
            <label class="text-[13.5px] font-semibold text-[--text-2]">Options</label>
            <span class="text-[12px] text-[--text-3]">{{ validOptionCount }} / 6</span>
          </div>
          <div class="flex flex-col gap-1.5">
            <div v-for="(option, index) in form.options" :key="index" class="flex items-center gap-2">
              <span class="w-6 h-6 rounded-full shrink-0 bg-[--surface-3] border border-[--border]
                           text-[12px] font-bold text-[--text-4] flex items-center justify-center">
                {{ index + 1 }}
              </span>
              <input v-model="option.text" type="text" class="form-control flex-1"
                :placeholder="'Option ' + (index + 1)" />
              <button type="button" class="btn btn-danger btn-sm"
                :disabled="form.options.length <= 2" @click="removeOption(index)">
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

<script>
import { createPoll } from '../api'
import { ChevronLeft, CheckCircle2, Plus, X, BarChart2, ToggleLeft, Star, MessageSquare } from '@lucide/vue'

export default {
  name: 'CreatePollView',

  components: { ChevronLeft, CheckCircle2, Plus, X },

  data() {
    return {
      isLoading:  false,

      form: {
        question:     '',
        questionType: 1,  // 1=Multiple Choice, 2=Yes/No, 3=Rating, 4=Open Text
        options:      [{ text: '' }, { text: '' }],
      },

      // Question type options shown in the type-card grid
      questionTypes: [
        { value: 1, label: 'Multiple Choice', description: 'Choose one from many', icon: BarChart2 },
        { value: 2, label: 'Yes / No',        description: 'Only 2 options',       icon: ToggleLeft },
        { value: 3, label: 'Star Rating',     description: 'Choose 1–5 stars',     icon: Star },
        { value: 4, label: 'Open Text',       description: 'Free text response',   icon: MessageSquare },
      ],
    }
  },

  computed: {
    validOptionCount() {
      let count = 0
      for (const option of this.form.options) {
        if (option.text.trim() !== '') count++
      }
      return count
    },
  },

  methods: {
    addOption() {
      if (this.form.options.length < 6) {
        this.form.options.push({ text: '' })
      }
    },

    removeOption(index) {
      if (this.form.options.length > 2) {
        this.form.options.splice(index, 1)
      }
    },

    async submit() {
      if (!this.form.question.trim()) {
        this.$toast.error('Please enter a question.')
        return
      }
      if (this.form.questionType === 1 && this.validOptionCount < 2) {
        this.$toast.error('Need at least 2 valid options.')
        return
      }

      this.isLoading = true
      try {
        // Build options list — only used for Multiple Choice
        const options = []
        if (this.form.questionType === 1) {
          for (const option of this.form.options) {
            if (option.text.trim()) options.push({ text: option.text.trim() })
          }
        }

        const payload = {
          question:     this.form.question.trim(),
          questionType: this.form.questionType,
          options,
        }

        const response = await createPoll(payload)
        // Backend returns the poll object directly, wrapped in { poll } or as the root response
        const createdPoll = response?.poll || response || {}
        
        if (!createdPoll.code) {
          throw new Error('Invalid poll response from server')
        }

        // Remember this poll code so the creator can access analytics
        const saved = JSON.parse(localStorage.getItem('createdPolls') || '[]')
        if (!saved.includes(createdPoll.code)) {
          saved.push(createdPoll.code)
          localStorage.setItem('createdPolls', JSON.stringify(saved))
        }

        this.$toast.success('Poll created!')
        this.$router.push({ name: 'Analytics', query: { code: createdPoll.code } })

      } catch (error) {
        this.$toast.error(error.message || 'Failed to create poll.')
      } finally {
        this.isLoading = false
      }
    },
  },
}
</script>
