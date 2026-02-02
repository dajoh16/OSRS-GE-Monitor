<template>
  <div v-if="open" class="settings-panel">
    <div class="settings-header">
      <strong>Settings</strong>
      <button class="ghost" type="button" @click="$emit('close')">Close</button>
    </div>
    <div class="settings-grid">
      <label class="field">
        Standard deviation threshold
        <input v-model.number="threshold" type="number" min="0" step="0.1" />
      </label>
      <label class="field">
        Profit target (%)
        <input v-model.number="profitTargetPercent" type="number" min="0" step="0.1" />
      </label>
      <label class="field">
        Recovery threshold
        <input v-model.number="recoveryThreshold" type="number" min="0" step="0.1" />
      </label>
      <label class="field">
        Rolling window size
        <input v-model.number="rollingWindowSize" type="number" min="5" step="1" />
      </label>
      <label class="field">
        Fetch interval (seconds)
        <input v-model.number="fetchInterval" type="number" min="10" step="5" />
      </label>
      <label class="field">
        API User-Agent
        <input
          v-model.trim="userAgent"
          type="text"
          placeholder="OSRS-GE-Monitor/1.0 (contact: Discord ...)"
        />
        <span v-if="!isUserAgentValid" class="error">
          User-Agent is required to comply with the OSRS Wiki API policy.
        </span>
      </label>
    </div>
    <div class="settings-actions">
      <span v-if="configError" class="error">{{ configError }}</span>
      <button class="primary" :disabled="configLoading || !isUserAgentValid" @click="$emit('save')">
        {{ configLoading ? 'Saving...' : 'Save Settings' }}
      </button>
    </div>
  </div>
</template>

<script setup>
defineProps({
  open: Boolean,
  configLoading: Boolean,
  configError: String,
  isUserAgentValid: Boolean
});

defineEmits(['close', 'save']);

const threshold = defineModel('threshold');
const profitTargetPercent = defineModel('profitTargetPercent');
const recoveryThreshold = defineModel('recoveryThreshold');
const rollingWindowSize = defineModel('rollingWindowSize');
const fetchInterval = defineModel('fetchInterval');
const userAgent = defineModel('userAgent');
</script>
