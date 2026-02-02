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
        Discord alerts
        <select v-model="discordNotificationsEnabled">
          <option :value="false">Disabled</option>
          <option :value="true">Enabled</option>
        </select>
      </label>
      <label class="field">
        Discord webhook URL
        <input
          v-model.trim="discordWebhookUrl"
          type="url"
          placeholder="https://discord.com/api/webhooks/..."
        />
        <span v-if="!isDiscordWebhookValid" class="error">
          Enter a valid Discord webhook URL to enable alerts.
        </span>
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
      <button
        class="secondary"
        type="button"
        :disabled="configLoading || !isUserAgentValid || !isDiscordWebhookValid || !discordNotificationsEnabled"
        @click="$emit('test-discord')"
      >
        Send test alert
      </button>
      <button
        class="primary"
        :disabled="configLoading || !isUserAgentValid || !isDiscordWebhookValid"
        @click="$emit('save')"
      >
        {{ configLoading ? 'Saving...' : 'Save Settings' }}
      </button>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';

defineProps({
  open: Boolean,
  configLoading: Boolean,
  configError: String,
  isUserAgentValid: Boolean
});

defineEmits(['close', 'save', 'test-discord']);

const threshold = defineModel('threshold');
const profitTargetPercent = defineModel('profitTargetPercent');
const recoveryThreshold = defineModel('recoveryThreshold');
const rollingWindowSize = defineModel('rollingWindowSize');
const fetchInterval = defineModel('fetchInterval');
const userAgent = defineModel('userAgent');
const discordNotificationsEnabled = defineModel('discordNotificationsEnabled');
const discordWebhookUrl = defineModel('discordWebhookUrl');

const isDiscordWebhookValid = computed(() => {
  if (!discordNotificationsEnabled.value) {
    return true;
  }
  const value = (discordWebhookUrl.value ?? '').trim();
  return (
    value.startsWith('https://discord.com/api/webhooks/') ||
    value.startsWith('https://discordapp.com/api/webhooks/')
  );
});
</script>
