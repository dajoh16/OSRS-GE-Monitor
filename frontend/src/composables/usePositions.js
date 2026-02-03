import { computed, ref } from 'vue';
import { getPositions, removePosition } from '../api';

export const usePositions = (positionDisplayLimit) => {
  const positions = ref([]);

  const openPositions = computed(() => {
    return positions.value.filter((position) => !position.soldAt);
  });

  const displayedPositions = computed(() => {
    return openPositions.value.slice(0, positionDisplayLimit.value);
  });

  const loadPositions = async () => {
    const data = await getPositions();
    positions.value = data.map((position) => {
      const isRecovered = Boolean(position.recoveredAt || position.recoveryPrice);
      const isSold = Boolean(position.soldAt);
      return {
        ...position,
        recoveryPrice: position.recoveryPrice ?? 'N/A',
        status: isSold ? 'sold' : isRecovered ? 'recovered' : 'open',
        statusLabel: isSold ? 'Sold' : isRecovered ? 'Recovered' : 'Open'
      };
    });
  };

  const removePositionItem = async (positionId) => {
    await removePosition(positionId);
    await loadPositions();
  };

  return {
    positions,
    openPositions,
    displayedPositions,
    loadPositions,
    removePositionItem
  };
};
