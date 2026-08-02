<template>
  <div class="container">
    <!-- Hero -->
    <div class="py-12 text-center">
      <h1 class="text-4xl font-extrabold text-[--text] tracking-tight leading-tight mb-2">
        Create &amp; Share Polls
      </h1>
      <p class="text-[15px] text-[--text-3] max-w-[420px] mx-auto">
        No account needed. Real-time results.
      </p>
    </div>

    <!-- Grid: Join + Create -->
    <div class="grid grid-cols-2 gap-4 max-w-[760px] mx-auto max-sm:grid-cols-1">

      <!-- Card 1: Join Poll -->
      <div class="card">
        <p class="text-[11px] font-bold uppercase tracking-widest text-[--text-4] mb-2">Join Poll</p>
        <h2 class="text-[17px] font-bold mb-1">Enter Room Code</h2>
        <p class="text-[13.5px] text-[--text-3] mb-5">Get the 6-digit code from poll creator</p>

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

          <p v-if="codeError" class="inline-flex items-center gap-1 text-[12.5px] text-[--red] font-semibold mt-2 justify-center">
            <AlertCircle :size="13" /> {{ codeError }}
          </p>

          <button type="submit" class="btn btn-primary btn-lg w-full mt-5" :disabled="joinLoading">
            <span v-if="joinLoading" class="spinner"></span>
            <LogIn v-else :size="15" />
            {{ joinLoading ? 'Joining...' : 'Join Room' }}
          </button>
        </form>
      </div>

      <!-- Card 2: Create Poll -->
      <div class="card !bg-[--blue] !border-[--blue]">
        <p class="text-[11px] font-bold uppercase tracking-widest mb-2" style="color:rgba(255,255,255,.6)">
          Create New Poll
        </p>
        <h2 class="text-[17px] font-bold mb-1" style="color:#fff">Get Started</h2>
        <p class="text-[13.5px] mb-5" style="color:rgba(255,255,255,.7)">
          Questions, options and real-time results
        </p>

        <ul class="list-none flex flex-col gap-1.5 text-[13px] mb-5" style="color:rgba(255,255,255,.85)">
          <li class="flex items-center gap-1.5"><Check :size="13" /> Multiple question types</li>
          <li class="flex items-center gap-1.5"><Check :size="13" /> Real-time result updates</li>
          <li class="flex items-center gap-1.5"><Check :size="13" /> Share via link or code</li>
        </ul>

        <router-link to="/create" class="btn w-full btn-white btn-lg">
          <Plus :size="15" /> Create Poll
        </router-link>
      </div>
    </div>

    <!-- How It Works -->
    <div class="mt-14 pb-5">
      <p class="text-[11px] font-bold uppercase tracking-widest text-[--text-4] text-center mb-5">
        How It Works
      </p>
      <div class="flex items-center justify-center max-w-[620px] mx-auto max-sm:flex-col max-sm:gap-4">
        <div class="flex-1 text-center px-4">
          <div class="w-9 h-9 rounded-full bg-[--blue] text-white text-[15px] font-extrabold flex items-center justify-center mx-auto mb-2.5">1</div>
          <h3 class="text-[14px] font-bold text-[--text] mb-1">Create Question</h3>
          <p class="text-[12.5px] text-[--text-4] leading-relaxed">Fill in question and answer options</p>
        </div>
        <ChevronRight :size="18" class="text-[--border-2] shrink-0 max-sm:rotate-90" />
        <div class="flex-1 text-center px-4">
          <div class="w-9 h-9 rounded-full bg-[--blue] text-white text-[15px] font-extrabold flex items-center justify-center mx-auto mb-2.5">2</div>
          <h3 class="text-[14px] font-bold text-[--text] mb-1">Share Link</h3>
          <p class="text-[12.5px] text-[--text-4] leading-relaxed">Send room code or link to participants</p>
        </div>
        <ChevronRight :size="18" class="text-[--border-2] shrink-0 max-sm:rotate-90" />
        <div class="flex-1 text-center px-4">
          <div class="w-9 h-9 rounded-full bg-[--blue] text-white text-[15px] font-extrabold flex items-center justify-center mx-auto mb-2.5">3</div>
          <h3 class="text-[14px] font-bold text-[--text] mb-1">View Results</h3>
          <p class="text-[12.5px] text-[--text-4] leading-relaxed">Results update instantly when people vote</p>
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
