import { computed, ref } from 'vue';
import { addWatchlistItem, addWatchlistItemsBulk, getWatchlist, removeWatchlistItem } from '../api';

export const useWatchlist = (watchlistDisplayLimit) => {
  const watchlist = ref([]);
  const watchlistQuery = ref('');
  const bulkNames = ref('');
  const bulkResult = ref(null);
  const bulkError = ref('');
  const bulkLoading = ref(false);

  const filteredWatchlist = computed(() => {
    if (!watchlistQuery.value) {
      return watchlist.value;
    }
    const query = watchlistQuery.value.toLowerCase();
    return watchlist.value.filter((item) => item.name.toLowerCase().includes(query));
  });

  const displayedWatchlist = computed(() => {
    return filteredWatchlist.value.slice(0, watchlistDisplayLimit.value);
  });

  const loadWatchlist = async () => {
    watchlist.value = await getWatchlist();
  };

  const addToWatchlist = async (itemId) => {
    await addWatchlistItem(itemId);
    await loadWatchlist();
  };

  const removeFromWatchlist = async (itemId) => {
    await removeWatchlistItem(itemId);
    await loadWatchlist();
  };

  const parseBulkNames = () =>
    bulkNames.value
      .split(/\r?\n|,/g)
      .map((name) => name.trim())
      .filter((name) => name.length > 0);

  const addBulkToWatchlist = async () => {
    const names = parseBulkNames();
    if (!names.length) {
      bulkError.value = 'Enter at least one item name.';
      return;
    }
    bulkLoading.value = true;
    bulkError.value = '';
    bulkResult.value = null;
    try {
      bulkResult.value = await addWatchlistItemsBulk(names);
      bulkNames.value = '';
      await loadWatchlist();
    } catch (error) {
      bulkError.value = error.message;
    } finally {
      bulkLoading.value = false;
    }
  };

  return {
    watchlist,
    watchlistQuery,
    filteredWatchlist,
    displayedWatchlist,
    bulkNames,
    bulkResult,
    bulkError,
    bulkLoading,
    loadWatchlist,
    addToWatchlist,
    removeFromWatchlist,
    addBulkToWatchlist
  };
};
