const DEFAULT_BASE_URL = '';

const getBaseUrl = () => import.meta.env.VITE_API_BASE_URL ?? DEFAULT_BASE_URL;

const request = async (path, options = {}) => {
  const response = await fetch(`${getBaseUrl()}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...(options.headers ?? {})
    },
    ...options
  });

  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || `Request failed: ${response.status}`);
  }

  if (response.status === 204 || response.status === 202) {
    return null;
  }

  return response.json();
};

export const getConfig = () => request('/api/config');

export const updateConfig = (payload) =>
  request('/api/config', {
    method: 'PUT',
    body: JSON.stringify(payload)
  });

export const sendDiscordTest = (message) =>
  request('/api/config/discord-test', {
    method: 'POST',
    body: JSON.stringify({ message })
  });

export const searchItems = (query) =>
  request(`/api/items?query=${encodeURIComponent(query)}`);

export const getCatalogStatus = () => request('/api/items/status');

export const getWatchlist = () => request('/api/watchlist');

export const getWatchlistItem = (itemId) =>
  request(`/api/watchlist/${itemId}`);

export const addWatchlistItem = (itemId) =>
  request('/api/watchlist', {
    method: 'POST',
    body: JSON.stringify({ itemId })
  });

export const addWatchlistItemsBulk = (names) =>
  request('/api/watchlist/bulk', {
    method: 'POST',
    body: JSON.stringify({ names })
  });

export const removeWatchlistItem = (itemId) =>
  request(`/api/watchlist/${itemId}`, {
    method: 'DELETE'
  });

export const getAlerts = () => request('/api/alerts');

export const acknowledgeAlert = (alertId, quantity) =>
  request(`/api/alerts/${alertId}/acknowledge`, {
    method: 'POST',
    body: JSON.stringify({ quantity })
  });

export const removeAlert = (alertId) =>
  request(`/api/alerts/${alertId}`, {
    method: 'DELETE'
  });

export const getPositions = () => request('/api/positions');

export const removePosition = (positionId) =>
  request(`/api/positions/${positionId}`, {
    method: 'DELETE'
  });

export const getNotifications = () => request('/api/notifications');

export const clearNotifications = () =>
  request('/api/notifications', {
    method: 'DELETE'
  });

export const removeNotification = (notificationId) =>
  request(`/api/notifications/${notificationId}`, {
    method: 'DELETE'
  });

export const getPriceHistory = (itemId, from, to, maxPoints) => {
  const params = new URLSearchParams();
  if (from) params.set('from', from);
  if (to) params.set('to', to);
  if (maxPoints) params.set('maxPoints', String(maxPoints));
  return request(`/api/prices/${itemId}/history?${params.toString()}`);
};
