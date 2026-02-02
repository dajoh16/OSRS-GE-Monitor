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

  if (response.status === 204) {
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

export const searchItems = (query) =>
  request(`/api/items?query=${encodeURIComponent(query)}`);

export const getWatchlist = () => request('/api/watchlist');

export const addWatchlistItem = (itemId) =>
  request('/api/watchlist', {
    method: 'POST',
    body: JSON.stringify({ itemId })
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

export const getPositions = () => request('/api/positions');

export const getNotifications = () => request('/api/notifications');
