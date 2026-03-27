import { useState, useEffect, useCallback } from 'react';
import { outageService } from '../services/outageService';
import { PAGINATION } from '../config';

const DEFAULT_FILTERS = {
  dateFrom: '',
  dateTo: '',
  minOutage: '',
  maxOutage: '',
  sortBy: 'period',
  sortDir: 'desc',
};

export function useOutages() {
  const [outages, setOutages]           = useState([]);
  const [totalRecords, setTotalRecords] = useState(0);
  const [page, setPage]                 = useState(1);
  const [filters, setFilters]           = useState(DEFAULT_FILTERS);
  const [loading, setLoading]           = useState(true);
  const [error, setError]               = useState(null);

  const limit = PAGINATION.DEFAULT_LIMIT;

  const fetchOutages = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await outageService.getOutages({ page, limit, filters });
      setOutages(result.data);
      setTotalRecords(result.total);
    } catch (err) {
      if (err.response?.status === 401) throw err; 
      setError('Error al cargar los datos. Intenta de nuevo más tarde.');
    } finally {
      setLoading(false);
    }
  }, [page, filters, limit]);

  useEffect(() => { fetchOutages(); }, [fetchOutages]);

  const resetFilters = () => {
    setFilters(DEFAULT_FILTERS);
    setPage(1);
  };

  /**
   * Updates sort column and toggles direction to 'asc' if the same column is
   * selected again while descending; otherwise defaults to 'desc'.
 */
  const handleSort = (column) => {
    setFilters((prev) => ({
      ...prev,
      sortBy: column,
      sortDir: prev.sortBy === column && prev.sortDir === 'desc' ? 'asc' : 'desc',
    }));
  };

  return {
    outages, totalRecords, page, setPage,
    filters, setFilters, resetFilters, handleSort,
    loading, error, refetch: fetchOutages,
    totalPages: Math.ceil(totalRecords / limit),
  };
}