import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useToastStore = defineStore('toast', () => {
  const toasts = ref([]);

  function addToast(message, type = 'info', duration = 3000) {
    // Dedupe: không thêm cùng message đang hiện
    if (toasts.value.some(t => t.message === message)) return;

    const id = Date.now() + Math.random();
    toasts.value.push({ id, message, type, leaving: false });

    // Auto remove with leave animation
    setTimeout(() => dismiss(id), duration);
  }

  function dismiss(id) {
    const t = toasts.value.find(t => t.id === id);
    if (!t) return;
    t.leaving = true;               // trigger leave animation
    setTimeout(() => {
      toasts.value = toasts.value.filter(t => t.id !== id);
    }, 280);
  }

  const success = (msg, dur) => addToast(msg, 'success', dur);
  const error   = (msg, dur) => addToast(msg, 'error',   dur ?? 4000);
  const warning = (msg, dur) => addToast(msg, 'warning', dur);
  const info    = (msg, dur) => addToast(msg, 'info',    dur);

  return { toasts, success, error, warning, info, dismiss };
});
