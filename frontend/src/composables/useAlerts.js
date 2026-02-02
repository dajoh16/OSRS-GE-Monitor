import { ref } from 'vue';
import { acknowledgeAlert, getAlerts, removeAlert } from '../api';

export const useAlerts = () => {
  const alerts = ref([]);
  const alertQuantities = ref({});

  const loadAlerts = async () => {
    const data = await getAlerts();
    alerts.value = data.map((alert) => {
      const currentPrice = alert.currentPrice ?? alert.triggerPrice ?? 0;
      const dropPercent =
        alert.mean && currentPrice
          ? (((alert.mean - currentPrice) / alert.mean) * 100).toFixed(1)
          : '0.0';
      return {
        ...alert,
        currentPrice,
        dropPercent
      };
    });
  };

  const acknowledge = async (alert) => {
    const quantity = alertQuantities.value[alert.id] ?? 1;
    await acknowledgeAlert(alert.id, quantity);
  };

  const removeAlertItem = async (alertId) => {
    await removeAlert(alertId);
  };

  return {
    alerts,
    alertQuantities,
    loadAlerts,
    acknowledge,
    removeAlertItem
  };
};
