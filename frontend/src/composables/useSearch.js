import { computed, ref } from 'vue';
import { searchItems } from '../api';

export const useSearch = (searchPageSize) => {
  const searchQuery = ref('');
  const searchResults = ref([]);
  const searchLoading = ref(false);
  const searchError = ref('');
  const searchPage = ref(1);

  const pagedSearchResults = computed(() => {
    const size = searchPageSize.value;
    const start = (searchPage.value - 1) * size;
    return searchResults.value.slice(start, start + size);
  });

  const searchTotalPages = computed(() => {
    return Math.max(1, Math.ceil(searchResults.value.length / searchPageSize.value));
  });

  const runSearch = async () => {
    if (!searchQuery.value) {
      searchResults.value = [];
      searchPage.value = 1;
      return;
    }
    searchLoading.value = true;
    searchError.value = '';
    try {
      searchResults.value = await searchItems(searchQuery.value);
      searchPage.value = 1;
    } catch (error) {
      searchError.value = error.message;
    } finally {
      searchLoading.value = false;
    }
  };

  return {
    searchQuery,
    searchResults,
    searchLoading,
    searchError,
    searchPage,
    pagedSearchResults,
    searchTotalPages,
    runSearch
  };
};
