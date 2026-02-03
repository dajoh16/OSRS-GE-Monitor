import { computed, ref } from 'vue';
import { getConfig, updateConfig } from '../api';

export const useConfig = () => {
  const configLoading = ref(false);
  const configError = ref('');
  const threshold = ref(3.5);
  const profitTargetPercent = ref(2);
  const recoveryThreshold = ref(0);
  const rollingWindowSize = ref(30);
  const fetchInterval = ref(60);
  const userAgent = ref('');
  const discordNotificationsEnabled = ref(false);
  const discordWebhookUrl = ref('');
  const alertGraceMinutes = ref(10);

  const isUserAgentValid = computed(() => userAgent.value.trim().length > 0);
  const isDiscordWebhookValid = computed(() => {
    if (!discordNotificationsEnabled.value) {
      return true;
    }
    const value = discordWebhookUrl.value.trim();
    return (
      value.startsWith('https://discord.com/api/webhooks/') ||
      value.startsWith('https://discordapp.com/api/webhooks/')
    );
  });

  const loadConfig = async () => {
    configError.value = '';
    try {
      const data = await getConfig();
      threshold.value = data.standardDeviationThreshold ?? 3.5;
      profitTargetPercent.value = (data.profitTargetPercent ?? 0.02) * 100;
      recoveryThreshold.value = data.recoveryStandardDeviationThreshold ?? 0;
      rollingWindowSize.value = data.rollingWindowSize ?? 30;
      fetchInterval.value = data.fetchIntervalSeconds ?? 60;
      userAgent.value = data.userAgent ?? '';
      discordNotificationsEnabled.value = data.discordNotificationsEnabled ?? false;
      discordWebhookUrl.value = data.discordWebhookUrl ?? '';
      alertGraceMinutes.value = data.alertGraceMinutes ?? 10;
    } catch (error) {
      configError.value = error.message;
    }
  };

  const saveConfig = async () => {
    configLoading.value = true;
    configError.value = '';
    try {
      await updateConfig({
        standardDeviationThreshold: threshold.value,
        profitTargetPercent: profitTargetPercent.value / 100,
        recoveryStandardDeviationThreshold: recoveryThreshold.value,
        rollingWindowSize: rollingWindowSize.value,
        fetchIntervalSeconds: fetchInterval.value,
        userAgent: userAgent.value,
        discordNotificationsEnabled: discordNotificationsEnabled.value,
        discordWebhookUrl: discordWebhookUrl.value,
        alertGraceMinutes: alertGraceMinutes.value
      });
    } catch (error) {
      configError.value = error.message;
    } finally {
      configLoading.value = false;
    }
  };

  return {
    configLoading,
    configError,
    threshold,
    recoveryThreshold,
    rollingWindowSize,
    fetchInterval,
    userAgent,
    discordNotificationsEnabled,
    discordWebhookUrl,
    alertGraceMinutes,
    profitTargetPercent,
    isUserAgentValid,
    isDiscordWebhookValid,
    loadConfig,
    saveConfig
  };
};
