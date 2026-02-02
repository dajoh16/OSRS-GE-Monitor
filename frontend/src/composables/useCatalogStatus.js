import { computed, ref } from 'vue';
import { getCatalogStatus } from '../api';

export const useCatalogStatus = () => {
  const catalogStatus = ref(null);

  const loadCatalogStatus = async () => {
    try {
      catalogStatus.value = await getCatalogStatus();
    } catch (error) {
      catalogStatus.value = { count: 0, lastError: error.message };
    }
  };

  const catalogStatusLabel = computed(() => {
    if (!catalogStatus.value) {
      return 'Loading';
    }
    if (catalogStatus.value.count > 0 && !catalogStatus.value.lastError) {
      return 'OK';
    }
    return 'Empty';
  });

  const catalogStatusClass = computed(() => {
    if (!catalogStatus.value) {
      return 'pending';
    }
    if (catalogStatus.value.count > 0 && !catalogStatus.value.lastError) {
      return 'ok';
    }
    return 'error';
  });

  return {
    catalogStatus,
    catalogStatusLabel,
    catalogStatusClass,
    loadCatalogStatus
  };
};
