<template>
  <div class="card">
    <div class="card__header">
      <div class="card__title">
        <h2>Notifications</h2>
        <button
          class="ghost ghost-inline"
          type="button"
          @click="$emit('acknowledge-all')"
          :disabled="!notifications.length"
        >
          Acknowledge all
        </button>
      </div>
      <div class="header-actions">
        <button class="secondary action-button" type="button" @click="$emit('open-suppressed')">
          Suppressed
        </button>
        <label class="inline-field">
          Show
          <select v-model.number="notificationDisplayLimit">
            <option v-for="size in notificationLimitOptions" :key="size" :value="size">
              {{ size }}
            </option>
          </select>
        </label>
        <span class="muted">{{ displayedNotifications.length }} of {{ notifications.length }}</span>
      </div>
    </div>
    <div v-if="displayedNotifications.length" class="notification-panel">
      <div
        v-for="note in displayedNotifications"
        :key="note.id"
        class="notification"
        :class="note.type"
      >
        <div>
          <strong>{{ note.title }}</strong>
          <p>{{ note.message }}</p>
        </div>
        <div class="notification-meta">
          <span class="muted">{{ formatTimestamp(note.createdAt) }}</span>
          <button class="ghost" @click="$emit('acknowledge', note.id)">
            Acknowledge
          </button>
        </div>
      </div>
    </div>
    <div v-else class="empty">
      Price drop and recovery notifications will appear here.
    </div>
    <div v-if="suppressedOpen" class="modal-overlay" @click.self="$emit('close-suppressed')">
      <div class="modal-card">
        <div class="modal-header">
          <h3>Suppressed items</h3>
          <button class="ghost" type="button" @click="$emit('close-suppressed')">Close</button>
        </div>
        <div v-if="suppressedLoading" class="muted">Loading...</div>
        <div v-else-if="!suppressedItems.length" class="empty">
          No suppressed items.
        </div>
        <div v-else class="list">
          <div v-for="item in suppressedItems" :key="item.itemId" class="list__item">
            <div>
              <strong>{{ item.itemName }}</strong>
              <p class="muted">Item #{{ item.itemId }}</p>
            </div>
            <button class="secondary danger action-button" @click="$emit('unsuppress', item.itemId)">
              Remove suppression
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  notifications: Array,
  displayedNotifications: Array,
  notificationLimitOptions: Array,
  suppressedOpen: Boolean,
  suppressedItems: Array,
  suppressedLoading: Boolean
});

defineEmits([
  'acknowledge',
  'acknowledge-all',
  'open-suppressed',
  'close-suppressed',
  'unsuppress'
]);

const notificationDisplayLimit = defineModel('notificationDisplayLimit');

const formatTimestamp = (timestamp) => {
  if (!timestamp) {
    return 'just now';
  }
  return new Date(timestamp).toLocaleString();
};
</script>
