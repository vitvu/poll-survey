<template>
  <div class="container">
    <button class="btn btn-ghost btn-sm mb-3" @click="router.push('/')">
      <ChevronLeft :size="15" /> Home
    </button>

    <!-- Access Denied -->
    <div v-if="denied" class="card text-center" style="max-width:420px;margin:0 auto;">
      <Lock :size="32" color="var(--red)" class="mb-2" />
      <h2 class="fw-700 mb-1" style="font-size:17px;">Access Denied</h2>
      <p class="page-sub mb-3">Only the creator can view this page.</p>
      <router-link to="/" class="btn btn-ghost">Go Home</router-link>
    </div>

    <!-- Loading -->
    <div v-else-if="loading" class="text-center mt-3">
      <div class="spinner spinner-blue" style="width:28px;height:28px;margin:0 auto 10px;"></div>
      <p class="fs-sm text-3">Loading...</p>
    </div>

    <!-- Content -->
    <template v-else-if="poll">
      <!-- Header Card -->
      <div class="card mb-3">
        <div class="flex items-start gap-3">
          <div class="flex-1">
            <div class="flex items-center gap-2 mb-2 flex-wrap">
              <span class="badge badge-blue">{{ poll.code }}</span>
              <span class="badge" :class="isClosed ? 'badge-red' : 'badge-green'">
                <span v-if="!isClosed" class="live-dot"></span>
                {{ isClosed ? 'Closed' : 'Open' }}
              </span>
              <span class="badge badge-gray">{{ poll.questionType }}</span>
            </div>

            <h1 class="page-title mb-3" style="font-size:20px;">{{ poll.question }}</h1>

            <div class="flex items-center gap-2 flex-wrap mb-2">
              <button class="btn btn-outline" @click="copyLink">
                <Copy :size="14" /> Copy Link
              </button>
              <router-link :to="'/vote/' + poll.code" class="btn btn-outline" target="_blank">
                <ExternalLink :size="14" /> Vote Page
              </router-link>
              <button v-if="!isClosed" class="btn btn-red btn-sm" @click="showConfirm = true">
                <StopCircle :size="14" /> Stop
              </button>
              <span v-else class="badge badge-red" style="padding:7px 12px;">Closed</span>
            </div>

            <span class="badge" :class="hubConnected ? 'badge-green' : 'badge-gray'">
              <span class="live-dot"></span>
              {{ hubConnected ? 'Live' : 'Connecting...' }}
            </span>

            <div class="flex gap-2 mt-3">
              <input class="form-control fs-sm" :value="shareLink" readonly style="background:var(--surface-2);" />
              <button class="btn btn-light shrink-0" @click="copyLink">
                <Clipboard :size="14" />
              </button>
            </div>

            <div class="meta-row mt-3">
              <span><Calendar :size="14" /> {{ createdText }}</span>
              <span><Clock :size="14" /> {{ expireText }}</span>
            </div>
          </div>

          <div class="qr-thumb" @click="showQRModal = true">
            <canvas ref="qrCanvas"></canvas>
            <div class="qr-thumb-overlay"><Maximize2 :size="16" /></div>
          </div>
        </div>
      </div>

      <!-- Stats -->
      <div class="stat-grid mb-3">
        <div class="stat-card">
          <div class="stat-icon-box"><Users :size="16" /></div>
          <div class="stat-num">{{ totalVotes }}</div>
          <div class="stat-label">Total Votes</div>
        </div>
        <div v-if="topText" class="stat-card">
          <div class="stat-icon-box"><Trophy :size="16" /></div>
          <div class="stat-top">{{ topText }}</div>
          <div class="stat-label">Leading</div>
        </div>
      </div>

      <!-- Results -->
      <div class="card mb-3">
        <div class="section-title">
          <BarChart2 :size="16" style="margin-right:6px;" /> Results
          <span class="live-badge" style="margin-left:auto;">
            <span class="live-dot"></span>Live
          </span>
        </div>

        <!-- Multiple Choice / Yes-No -->
        <template v-if="['Multiple Choice', 'Yes / No'].includes(poll.questionType)">
          <div v-if="!totalVotes" class="empty-state"><p class="fs-sm">No votes yet.</p></div>
          <div v-else class="bar-list">
            <div v-for="(opt, i) in choiceResults" :key="opt.id" class="bar-row" :class="{ win: i===0 && opt.count>0 }">
              <div class="bar-meta">
                <span class="bar-label">
                  <Trophy v-if="i===0 && opt.count>0" :size="14" style="color:var(--amber);margin-right:3px;" />
                  {{ opt.text }}
                </span>
                <span class="bar-right">
                  <strong>{{ opt.count }}</strong>
                  <span class="bar-pct">{{ opt.pct }}%</span>
                </span>
              </div>
              <div class="bar-track">
                <div class="bar-fill" :class="{ 'bar-win': i===0 && opt.count>0 }"
                  :style="{ width: opt.pct + '%', transitionDelay: i*50+'ms' }"></div>
              </div>
            </div>
          </div>
        </template>

        <!-- Rating -->
        <template v-else-if="poll.questionType === 'Rating'">
          <div v-if="!totalVotes" class="empty-state"><p class="fs-sm">No ratings yet.</p></div>
          <div v-else>
            <div class="rating-header mb-3">
              <div class="avg-score">{{ avgRating }}</div>
              <div>
                <div class="star-row mb-1">
                  <Star v-for="s in 5" :key="s" :size="20"
                    :color="s <= Math.round(parseFloat(avgRating)) ? 'var(--amber)' : 'var(--border-2)'"
                    :fill="s <= Math.round(parseFloat(avgRating)) ? 'var(--amber)' : 'transparent'" />
                </div>
                <p class="fs-xs text-3">{{ totalVotes }} ratings</p>
              </div>
            </div>
            <div class="bar-list">
              <div v-for="s in [5,4,3,2,1]" :key="s" class="bar-row">
                <div class="bar-meta">
                  <span class="bar-label">
                    <Star :size="14" style="color:var(--amber);margin-right:3px;fill:var(--amber);" />
                    {{ s }} stars
                  </span>
                  <span class="bar-right">
                    <strong>{{ ratingBreakdown[s] }}</strong>
                    <span class="bar-pct">{{ totalVotes > 0 ? Math.round(ratingBreakdown[s]/totalVotes*100) : 0 }}%</span>
                  </span>
                </div>
                <div class="bar-track">
                  <div class="bar-fill"
                    :style="{ width: totalVotes > 0 ? (ratingBreakdown[s]/totalVotes*100)+'%' : '0%' }"></div>
                </div>
              </div>
            </div>
          </div>
        </template>

        <!-- Open Text -->
        <template v-else-if="poll.questionType === 'Open Text'">
          <p class="fs-sm fw-600 mb-2">
            <MessageSquare :size="14" style="display:inline-block;margin-right:4px;vertical-align:text-bottom;" />
            {{ textList.length }} responses
          </p>
          <div v-if="!textList.length" class="empty-state"><p class="fs-sm">No responses yet.</p></div>
          <div v-for="(t, i) in textList" :key="i" class="response-item">{{ t }}</div>
        </template>
      </div>
    </template>

    <!-- QR Modal -->
    <div v-if="showQRModal" class="modal-bg" @click.self="showQRModal = false">
      <div class="modal-box">
        <button class="modal-close" @click="showQRModal = false">
          <X :size="20" />
        </button>
        <div class="qr-modal-canvas">
          <canvas ref="qrCanvasLarge"></canvas>
        </div>
        <div class="flex flex-col gap-2 mt-3">
          <div class="flex items-center justify-center"
            style="padding:10px;background:var(--surface-2);border:1px solid var(--border);border-radius:var(--radius);">
            <span style="font-size:20px;font-weight:800;color:var(--blue);letter-spacing:2px;">
              {{ poll?.code }}
            </span>
          </div>
          <div class="flex gap-2 items-center">
            <input :value="shareLink" readonly class="form-control fs-sm"
              style="background:var(--surface-2);font-family:monospace;"
              @click="$event.target.select()" />
            <button class="btn-icon shrink-0" @click="copyLink">
              <Copy :size="16" />
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Confirm Modal -->
    <div v-if="showConfirm" class="modal-bg" @click.self="showConfirm=false">
      <div class="modal-box">
        <StopCircle :size="32" color="var(--red)" class="mb-2" />
        <h3 class="fw-700 mb-2" style="font-size:17px;">Stop Poll?</h3>
        <p class="fs-sm text-3 mb-3">Users will not be able to vote after closing.</p>
        <div class="flex gap-2 justify-center">
          <button class="btn btn-ghost" @click="showConfirm=false">Cancel</button>
          <button class="btn btn-red" @click="closePoll">
            <StopCircle :size="14" /> Stop Now
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useToast } from 'vue-toastification'
import { pollApi } from '../api'
import { usePollHub } from '../usePollHub'
import QRCode from 'qrcode'
import { ChevronLeft, Lock, Copy, ExternalLink, StopCircle, Calendar, Clock, Users, Trophy, BarChart2, Star, MessageSquare, Clipboard, Maximize2, X } from '@lucide/vue'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const code = route.query.code

const loading = ref(false)
const denied = ref(false)
const poll = ref(null)
const totalVotes = ref(0)
const choiceResults = ref([])
const textList = ref([])
const ratingVotes = ref([])
const showConfirm = ref(false)
const showQRModal = ref(false)
const qrCanvas = ref(null)
const qrCanvasLarge = ref(null)

const isClosed = computed(() =>
  !poll.value || poll.value.status !== 'Active' || new Date(poll.value.expireAt) <= new Date()
)

const shareLink = computed(() => `${location.origin}/vote/${poll.value?.code}`)
const topText = computed(() => choiceResults.value[0]?.count > 0 ? choiceResults.value[0].text : '')
const avgRating = computed(() => {
  const nums = ratingVotes.value.map(v => parseFloat(v.voteValue)).filter(n => n > 0)
  return nums.length ? (nums.reduce((a, b) => a + b, 0) / nums.length).toFixed(1) : '0.0'
})

const ratingBreakdown = computed(() => {
  const counts = { 1: 0, 2: 0, 3: 0, 4: 0, 5: 0 }
  ratingVotes.value.forEach(v => {
    const s = parseInt(v.voteValue)
    if (s >= 1 && s <= 5) counts[s]++
  })
  return counts
})

const createdText = computed(() => poll.value ? new Date(poll.value.createdAt).toLocaleDateString('en-US') : '')
const expireText = computed(() => {
  if (!poll.value) return ''
  const d = new Date(poll.value.expireAt)
  return d.getFullYear() > new Date().getFullYear() + 50 ? 'No limit' : d.toLocaleString('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' })
})

const buildBars = (results, total) => {
  if (!poll.value) return
  const counts = Object.fromEntries(results.map(r => [r.optionId, r.count]))
  choiceResults.value = poll.value.options
    .map(o => ({
      id: o.id,
      text: o.text,
      count: counts[o.id] || 0,
      pct: total > 0 ? Math.round(((counts[o.id] || 0) / total) * 100) : 0,
    }))
    .sort((a, b) => b.count - a.count)
}

const loadResults = async () => {
  if (!poll.value) return
  const totalRes = await pollApi.getVoteTotal(code)
  totalVotes.value = totalRes.data.totalVotes
  const type = poll.value.questionType

  if (['Multiple Choice', 'Yes / No'].includes(type)) {
    const res = await pollApi.getVoteResults(code)
    buildBars(res.data.map(r => ({ optionId: r.optionId, count: r.count })), totalVotes.value)
  } else {
    const res = await pollApi.getVoteList(code)
    if (type === 'Rating') ratingVotes.value = res.data
    else if (type === 'Open Text') textList.value = res.data.map(v => v.voteValue).filter(Boolean)
  }
}

const generateQR = async (canvas, size) => {
  if (!canvas) return
  try {
    await QRCode.toCanvas(canvas, shareLink.value, {
      width: size,
      margin: 2,
      color: { dark: '#1e293b', light: '#ffffff' },
      errorCorrectionLevel: 'H',
    })
  } catch (err) {
    console.error('QR failed:', err)
  }
}

const { connected: hubConnected, start: hubStart } = usePollHub(code, async data => {
  totalVotes.value = data.total
  if (['Multiple Choice', 'Yes / No'].includes(poll.value?.questionType)) {
    buildBars(data.results, data.total)
  } else {
    await loadResults()
  }
})

let fallback = null

onMounted(async () => {
  if (!code) return

  const saved = JSON.parse(localStorage.getItem('createdPolls') || '[]')
  if (!saved.includes(code)) {
    denied.value = true
    return
  }

  loading.value = true
  try {
    const [pollRes, totalRes] = await Promise.all([
      pollApi.getPollByCode(code),
      pollApi.getVoteTotal(code),
    ])

    poll.value = pollRes.data
    totalVotes.value = totalRes.data.totalVotes

    await loadResults()
    hubStart()

    fallback = setInterval(() => {
      if (!hubConnected.value) loadResults()
    }, 6000)

    setTimeout(() => {
      if (qrCanvas.value) generateQR(qrCanvas.value, 100)
    }, 100)
  } catch {
    toast.error('Failed to load data.')
  } finally {
    loading.value = false
  }
})

watch(showQRModal, async isOpen => {
  if (isOpen) {
    await new Promise(r => setTimeout(r, 50))
    if (qrCanvasLarge.value) generateQR(qrCanvasLarge.value, 320)
  }
})

onUnmounted(() => {
  clearInterval(fallback)
})

const closePoll = async () => {
  showConfirm.value = false
  try {
    await pollApi.updatePoll(poll.value.id, { ...poll.value, status: 'Closed' })
    poll.value.status = 'Closed'
    toast.success('Poll stopped.')
  } catch {
    toast.error('Failed to close poll.')
  }
}

const copyLink = async () => {
  try {
    await navigator.clipboard.writeText(shareLink.value)
    toast.success('Link copied!')
  } catch {
    toast.error('Failed to copy.')
  }
}
</script>

<style scoped>
.qr-thumb {
  position: relative;
  width: 100px; height: 100px;
  flex-shrink: 0;
  cursor: pointer;
  border-radius: var(--radius);
  overflow: hidden;
  background: #fff; padding: 4px;
  box-shadow: var(--shadow);
  transition: transform .2s, box-shadow .2s;
}
.qr-thumb:hover { transform: scale(1.05); box-shadow: 0 4px 12px rgba(0,0,0,.15); }
.qr-thumb canvas { width: 100%; height: 100%; }
.qr-thumb-overlay {
  position: absolute; inset: 0;
  background: rgba(15,23,42,.75);
  display: flex; align-items: center; justify-content: center;
  color: #fff; opacity: 0; transition: opacity .2s;
}
.qr-thumb:hover .qr-thumb-overlay { opacity: 1; }
.qr-modal-canvas {
  background: var(--surface-2);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: 20px;
  display: flex; align-items: center; justify-content: center;
}
.qr-modal-canvas canvas { border-radius: var(--radius); }

@media (max-width: 600px) {
  .qr-thumb { width: 72px; height: 72px; }
}
</style>
