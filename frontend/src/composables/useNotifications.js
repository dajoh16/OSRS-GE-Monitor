import { computed, ref } from 'vue';
import { clearNotifications, getNotifications, removeNotification } from '../api';

export const useNotifications = (notificationDisplayLimit) => {
  const notifications = ref([]);

  const displayedNotifications = computed(() => {
    return notifications.value.slice(0, notificationDisplayLimit.value);
  });

  const loadNotifications = async () => {
    notifications.value = await getNotifications();
  };

  const clearAllNotifications = async () => {
    await clearNotifications();
    notifications.value = [];
  };

  const removeNotificationItem = async (notificationId) => {
    await removeNotification(notificationId);
    await loadNotifications();
  };

  return {
    notifications,
    displayedNotifications,
    loadNotifications,
    clearAllNotifications,
    removeNotificationItem
  };
};
