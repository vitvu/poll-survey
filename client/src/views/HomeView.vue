<template>
  <div class="container">
    <!-- Hero section -->
    <div class="hero">
      <h1 class="hero-title">Create & Share Polls</h1>
      <p class="hero-sub">No account needed. Real-time results.</p>
    </div>

    <!-- Grid 2 cột: Join + Create -->
    <div class="home-grid">
      <!-- Card 1: Join Poll -->
      <div class="card">
        <p class="label-upper mb-2">Join Poll</p>
        <h2 class="card-h mb-1">Enter Room Code</h2>
        <p class="fs-sm text-3 mb-3">Get the 6-digit code from poll creator</p>

        <form @submit.prevent="joinPoll">
          <input
            v-model="code"
            type="text"
            inputmode="numeric"
            maxlength="6"
            placeholder="000000"
            class="code-input" :class="{ error: codeError }"
            autocomplete="off"
          />

          <!-- Error message -->
          <p v-if="codeError" class="inline-error justify-center">
            <AlertCircle :size="13" /> {{ codeError }}
          </p>

          <!-- Submit button -->
          <button type="submit" class="btn btn-primary btn-lg w-full mt-3" :disabled="joinLoading">
            <span v-if="joinLoading" class="spinner"></span>
            <LogIn v-else :size="15" />
            {{ joinLoading ? 'Joining...' : 'Join Room' }}
          </button>
        </form>
      </div>

      <!-- Card 2: Create Poll -->
      <div class="card create-card">
        <p class="label-upper mb-2" style="color:rgba(255,255,255,.6);">Create New Poll</p>
        <h2 class="card-h mb-1" style="color:#fff;">Get Started</h2>
        <p class="fs-sm mb-3" style="color:rgba(255,255,255,.7);">Questions, options and real-time results</p>

        <ul class="feature-list mb-3">
          <li><Check :size="13" /> Multiple question types</li>
          <li><Check :size="13" /> Real-time result updates</li>
          <li><Check :size="13" /> Share via link or code</li>
        </ul>

        <router-link to="/create" class="btn w-full btn-white btn-lg">
          <Plus :size="15" /> Create Poll
        </router-link>
      </div>
    </div>

    <!-- How It Works -->
    <div class="how-section">
      <p class="label-upper text-center mb-3">How It Works</p>
      <div class="steps-row">
        <div class="step-card">
          <div class="step-num">1</div>
          <h3 class="step-title">Create Question</h3>
          <p class="step-desc">Fill in question and answer options</p>
        </div>
        <ChevronRight :size="18" class="step-arrow" />
        <div class="step-card">
          <div class="step-num">2</div>
          <h3 class="step-title">Share Link</h3>
          <p class="step-desc">Send room code or link to participants</p>
        </div>
        <ChevronRight :size="18" class="step-arrow" />
        <div class="step-card">
          <div class="step-num">3</div>
          <h3 class="step-title">View Results</h3>
          <p class="step-desc">Results update instantly when people vote</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { AlertCircle, LogIn, Plus, Check, ChevronRight } from '@lucide/vue'
import { pollApi } from '../api'

const router = useRouter()
const code = ref('')
const codeError = ref('')
const joinLoading = ref(false)

const joinPoll = async () => {
  if (code.value.length < 6) {
    codeError.value = 'Please enter all 6 digits'
    return
  }

  joinLoading.value = true
  try {
    await pollApi.checkPoll(code.value)
    router.push(`/vote/${code.value}`)
  } catch {
    codeError.value = 'Poll not found'
  } finally {
    joinLoading.value = false
  }
}
</script>
