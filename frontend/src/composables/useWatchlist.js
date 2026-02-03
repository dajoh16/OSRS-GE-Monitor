import { computed, ref } from 'vue';
import {
  addWatchlistItem,
  addWatchlistItemsBulk,
  getWatchlist,
  removeWatchlistItem,
  getWatchlistMarket
} from '../api';

export const useWatchlist = (watchlistDisplayLimit) => {
  const watchlist = ref([]);
  const watchlistQuery = ref('');
  const bulkNames = ref('');
  const bulkResult = ref(null);
  const bulkError = ref('');
  const bulkLoading = ref(false);
  const marketById = ref({});
  const watchlistSortField = ref('name');
  const watchlistSortDirection = ref('asc');

  const filteredWatchlist = computed(() => {
    if (!watchlistQuery.value) {
      return watchlist.value;
    }
    const query = watchlistQuery.value.toLowerCase();
    return watchlist.value.filter((item) => item.name.toLowerCase().includes(query));
  });

  const displayedWatchlist = computed(() => {
    const enriched = filteredWatchlist.value.map((item) => {
      const market = marketById.value[item.id];
      return {
        ...item,
        marketHigh: market?.high ?? null,
        marketLow: market?.low ?? null,
        marketHighTime: market?.highTime ?? null,
        marketLowTime: market?.lowTime ?? null,
        buyLimit: market?.buyLimit ?? null
      };
    });

    const direction = watchlistSortDirection.value === 'desc' ? -1 : 1;
    const sorted = [...enriched].sort((a, b) => {
      const field = watchlistSortField.value;
      const valueA = getSortValue(a, field);
      const valueB = getSortValue(b, field);

      if (valueA === null && valueB !== null) return 1;
      if (valueA !== null && valueB === null) return -1;
      if (valueA === null && valueB === null) return 0;

      if (typeof valueA === 'string' && typeof valueB === 'string') {
        return valueA.localeCompare(valueB) * direction;
      }
      return (valueA - valueB) * direction;
    });

    return sorted.slice(0, watchlistDisplayLimit.value);
  });

  const loadWatchlist = async () => {
    watchlist.value = await getWatchlist();
    await loadMarket();
  };

  const loadMarket = async () => {
    const data = await getWatchlistMarket();
    marketById.value = data.reduce((acc, entry) => {
      acc[entry.itemId] = entry;
      return acc;
    }, {});
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
    marketById,
    watchlistSortField,
    watchlistSortDirection,
    bulkNames,
    bulkResult,
    bulkError,
    bulkLoading,
    loadWatchlist,
    loadMarket,
    addToWatchlist,
    removeFromWatchlist,
    addBulkToWatchlist
  };
};

const getSortValue = (item, field) => {
  switch (field) {
    case 'id':
      return item.id ?? null;
    case 'high':
      return item.marketHigh ?? null;
    case 'low':
      return item.marketLow ?? null;
    case 'limit':
      return item.buyLimit ?? null;
    case 'afterTax':
      if (item.marketHigh == null || item.marketLow == null) {
        return null;
      }
      const tax = item.marketHigh < 100 ? 0 : Math.floor(item.marketHigh * 0.02);
      return item.marketHigh - item.marketLow - tax;
    default:
      return item.name?.toLowerCase() ?? null;
  }
};
