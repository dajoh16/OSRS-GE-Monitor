import { ref } from 'vue';

let nextId = 1;

export const useToasts = () => {
  const toasts = ref([]);

  const removeToast = (id) => {
    toasts.value = toasts.value.filter((toast) => toast.id !== id);
  };

  const pushToast = (message, type = 'success', durationMs = 2800) => {
    const id = nextId;
    nextId += 1;
    toasts.value = [...toasts.value, { id, message, type }];
    if (durationMs > 0) {
      setTimeout(() => removeToast(id), durationMs);
    }
  };

  return {
    toasts,
    pushToast,
    removeToast
  };
};
