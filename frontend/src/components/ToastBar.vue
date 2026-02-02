<template>
  <div class="toast-stack" aria-live="polite" aria-atomic="true">
    <div v-for="toast in toasts" :key="toast.id" class="toast" :class="toast.type">
      <span>{{ toast.message }}</span>
      <button class="ghost" type="button" @click="$emit('dismiss', toast.id)">Close</button>
    </div>
  </div>
</template>

<script setup>
defineProps({
  toasts: Array
});

defineEmits(['dismiss']);
</script>
