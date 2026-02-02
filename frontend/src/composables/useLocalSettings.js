import { computed, ref, watch } from 'vue';

const STORAGE_KEY = 'osrs-ge-monitor-settings';

const DEFAULTS = {
  searchPageSize: 10,
  watchlistDisplayLimit: 20,
  notificationDisplayLimit: 10,
  positionDisplayLimit: 10
};

const clampInt = (value, min, max, fallback) => {
  const parsed = Number(value);
  if (!Number.isFinite(parsed)) {
    return fallback;
  }
  return Math.min(max, Math.max(min, Math.floor(parsed)));
};

export const useLocalSettings = () => {
  const settings = ref({ ...DEFAULTS });

  const searchPageOptions = [5, 10, 15, 20, 25, 50];
  const watchlistLimitOptions = [10, 20, 30, 50, 75, 100, 150, 200];
  const notificationLimitOptions = [5, 10, 20, 30, 50];
  const positionLimitOptions = [5, 10, 20, 30, 50];

  const searchPageSize = computed({
    get: () => settings.value.searchPageSize,
    set: (value) => {
      settings.value.searchPageSize = clampInt(value, 5, 50, DEFAULTS.searchPageSize);
    }
  });

  const watchlistDisplayLimit = computed({
    get: () => settings.value.watchlistDisplayLimit,
    set: (value) => {
      settings.value.watchlistDisplayLimit = clampInt(value, 5, 200, DEFAULTS.watchlistDisplayLimit);
    }
  });

  const notificationDisplayLimit = computed({
    get: () => settings.value.notificationDisplayLimit,
    set: (value) => {
      settings.value.notificationDisplayLimit = clampInt(value, 5, 50, DEFAULTS.notificationDisplayLimit);
    }
  });

  const positionDisplayLimit = computed({
    get: () => settings.value.positionDisplayLimit,
    set: (value) => {
      settings.value.positionDisplayLimit = clampInt(value, 5, 50, DEFAULTS.positionDisplayLimit);
    }
  });

  const load = () => {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return;
    }
    try {
      const parsed = JSON.parse(raw);
      settings.value = {
        searchPageSize: clampInt(parsed.searchPageSize, 5, 50, DEFAULTS.searchPageSize),
        watchlistDisplayLimit: clampInt(parsed.watchlistDisplayLimit, 5, 200, DEFAULTS.watchlistDisplayLimit),
        notificationDisplayLimit: clampInt(parsed.notificationDisplayLimit, 5, 50, DEFAULTS.notificationDisplayLimit),
        positionDisplayLimit: clampInt(parsed.positionDisplayLimit, 5, 50, DEFAULTS.positionDisplayLimit)
      };
    } catch {
      settings.value = { ...DEFAULTS };
    }
  };

  watch(
    () => settings.value,
    () => {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(settings.value));
    },
    { deep: true }
  );

  return {
    settings,
    load,
    searchPageSize,
    watchlistDisplayLimit,
    notificationDisplayLimit,
    positionDisplayLimit,
    searchPageOptions,
    watchlistLimitOptions,
    notificationLimitOptions,
    positionLimitOptions
  };
};
