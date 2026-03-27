import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { outageService } from '../services/outageService';
import { authService } from '../services/authService';
import { useOutages } from '../hooks/useOutages';
import { createPortal } from 'react-dom';
import { ROUTES } from '../config';

const IconBolt = () => (
  <svg viewBox="0 0 24 24" width="17" height="17" stroke="#fff" fill="none" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
    <polygon points="13 2 3 14 12 14 11 22 21 10 12 10 13 2"/>
  </svg>
);

const IconRefresh = () => (
  <svg viewBox="0 0 24 24"><polyline points="1 4 1 10 7 10"/><path d="M3.51 15a9 9 0 1 0 .49-4"/></svg>
);

const IconLogout = () => (
  <svg viewBox="0 0 24 24"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>
);

const IconChevLeft = () => (
  <svg viewBox="0 0 24 24"><polyline points="15 18 9 12 15 6"/></svg>
);

const IconChevRight = () => (
  <svg viewBox="0 0 24 24"><polyline points="9 18 15 12 9 6"/></svg>
);

const IconInbox = () => (
  <svg viewBox="0 0 24 24"><polyline points="22 12 16 12 14 15 10 15 8 12 2 12"/><path d="M5.45 5.11L2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z"/></svg>
);

const IconAlert = () => (
  <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
);

const IconCheck = () => (
  <svg viewBox="0 0 24 24"><polyline points="20 6 9 17 4 12"/></svg>
);


function SortTh({ col, active, dir, onSort, children }) {
  return (
    <th className={active ? 'active' : ''} onClick={() => onSort(col)}>
      {children}
      <span className="sort-icon">{active ? (dir === 'desc' ? '▼' : '▲') : '⇅'}</span>
    </th>
  );
}

function Badge({ value }) {
  const pct = parseFloat(value);
  if (pct > 15) return <span className="badge badge-danger">{value}%</span>;
  if (pct > 8)  return <span className="badge badge-warning">{value}%</span>;
  return <span className="badge badge-success">{value}%</span>;
}

function StatsRow({ outages, totalRecords }) {
  if (!outages.length) return null;
  const latest    = outages[0];
  const avgPct    = (outages.reduce((s, o) => s + parseFloat(o.percentOutage), 0) / outages.length).toFixed(1);
  const maxOutage = Math.max(...outages.map((o) => o.outageMw)).toLocaleString('en-US');

  return (
    <div className="stats-row">
      <div className="stat-card teal">
        <span className="stat-label">Total Registros</span>
        <span className="stat-value">{totalRecords.toLocaleString()}</span>
        <span className="stat-sub">en la base de datos</span>
      </div>
    </div>
  );
}

export default function Dashboard() {
  const navigate = useNavigate();
  const [refreshing, setRefreshing]         = useState(false);
  const [refreshMessage, setRefreshMessage] = useState(null);

  const {
    outages, totalRecords, page, setPage,
    filters, setFilters, resetFilters, handleSort,
    loading, error, refetch,
    totalPages,
  } = useOutages();

  const handleLogout = () => {
    authService.logout();
    navigate(ROUTES.LOGIN);
  };

const handleRefresh = async () => {
    setRefreshing(true);
    setRefreshMessage(null);
    
    try {
      await outageService.refreshPipeline();
      setRefreshMessage({ type: 'success', text: 'Datos extraídos y actualizados correctamente' });
      setPage(1);
      refetch();
    } catch (err) {
      let errorText = 'Hubo un problema al conectar con el servidor. Inténtalo más tarde.';
      const errMsg = (JSON.stringify(err.response?.data) || err.message || '').toLowerCase();
      
      if (errMsg.includes('api key') || err.response?.status === 401 || err.response?.status === 403) {
        errorText = 'Verifica la configuración del servidor.';
      } else if (err.response?.status === 429) {
        errorText = 'Espera un momento.';
      }

      setRefreshMessage({ type: 'danger', text: errorText });
    } finally {
      setRefreshing(false);
      
      setTimeout(() => {
        setRefreshMessage(null);
      }, 5000);
    }
  };

  return (
    <>
      <header className="topbar">
        <div className="topbar-brand">
          <div className="brand-icon"><IconBolt /></div>
          <div>
            <h1>EIA Nuclear Outages</h1>
          </div>
        </div>
        <div className="topbar-actions">
          <button
            className="btn btn-ghost-white"
            onClick={handleRefresh}
            disabled={refreshing || loading}
          >
            {refreshing ? (
              <><span className="spinner spinner-sm" style={{ borderTopColor: '#fff', borderColor: 'rgba(255,255,255,0.3)' }} /> Actualizando...</>
            ) : (
              <><IconRefresh /> Extraer Datos Nuevos</>
            )}
          </button>
          <button className="btn btn-danger-topbar" onClick={handleLogout}>
            <IconLogout /> Salir
          </button>
        </div>
      </header>

      <main className="page-content">
      
        {refreshMessage && createPortal(
        <div className="notif-container">
          <div className={`notif notif-${refreshMessage.type}`}>
              <div className="toast-icon">
                {refreshMessage.type === 'success' ? <IconCheck /> : <IconAlert />}
              </div>
              <div className="toast-message">{refreshMessage.text}</div>
            </div>
          </div>,
          document.body 
        )}
        {error && (
          <div className="alert alert-danger">
            <IconAlert /> {error}
          </div>
        )}

        <StatsRow outages={outages} totalRecords={totalRecords} />

        <div className="filter-bar">
          <div className="filter-group">
            <label className="filter-label">Desde</label>
            <input
              type="date" className="filter-input"
              value={filters.dateFrom}
              onChange={(e) => setFilters({ ...filters, dateFrom: e.target.value })}
            />
          </div>
          <div className="filter-group">
            <label className="filter-label">Hasta</label>
            <input
              type="date" className="filter-input"
              value={filters.dateTo}
              onChange={(e) => setFilters({ ...filters, dateTo: e.target.value })}
            />
          </div>
          <div className="filter-group">
            <label className="filter-label">Corte Min. (MW)</label>
            <input
              type="number" className="filter-input"
              placeholder="Ej. 5000"
              value={filters.minOutage}
              onChange={(e) => setFilters({ ...filters, minOutage: e.target.value })}
            />
          </div>
          <div className="filter-group">
            <label className="filter-label">Corte Max. (MW)</label>
            <input
              type="number" className="filter-input"
              placeholder="Ej. 20000"
              value={filters.maxOutage}
              onChange={(e) => setFilters({ ...filters, maxOutage: e.target.value })}
            />
          </div>
          <div className="filter-actions">
            <button className="btn btn-ghost" onClick={resetFilters}>Limpiar filtros</button>
          </div>
        </div>

        <div className="table-card">
          <table className="data-table">
            <thead>
              <tr>
                <SortTh col="period"   active={filters.sortBy === 'period'}   dir={filters.sortDir} onSort={handleSort}>Periodo</SortTh>
                <SortTh col="capacity" active={filters.sortBy === 'capacity'} dir={filters.sortDir} onSort={handleSort}>Capacidad Total (MW)</SortTh>
                <SortTh col="outage"   active={filters.sortBy === 'outage'}   dir={filters.sortDir} onSort={handleSort}>Corte (MW)</SortTh>
                <SortTh col="percent"  active={filters.sortBy === 'percent'}  dir={filters.sortDir} onSort={handleSort}>% Corte</SortTh>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={4}>
                    <div className="state-cell">
                      <div className="spinner" />
                      <div className="state-label">Cargando datos</div>
                      <p>Obteniendo registros del servidor...</p>
                    </div>
                  </td>
                </tr>
              ) : outages.length === 0 ? (
                <tr>
                  <td colSpan={4}>
                    <div className="state-cell">
                      <div className="state-icon-box"><IconInbox /></div>
                      <div className="state-label">Sin resultados</div>
                      <p>Ajusta los filtros o extrae nuevos datos.</p>
                    </div>
                  </td>
                </tr>
              ) : (
                outages.map((o, idx) => (
                  <tr key={idx}>
                    <td className="cell-period">{o.period}</td>
                    <td className="cell-mw">{o.capacityMw.toLocaleString('en-US')} MW</td>
                    <td className="cell-outage">{o.outageMw.toLocaleString('en-US')} MW</td>
                    <td><Badge value={o.percentOutage} /></td>
                  </tr>
                ))
              )}
            </tbody>
          </table>

          {!loading && outages.length > 0 && (
            <div className="table-footer">
              <span>
                Pagina <strong>{page}</strong> de <strong>{totalPages}</strong>
                &nbsp;&middot;&nbsp;
                <strong>{totalRecords.toLocaleString()}</strong> registros en total
              </span>
              <div className="pagination">
                <button
                  className="btn-page"
                  disabled={page === 1}
                  onClick={() => setPage(page - 1)}
                >
                  <IconChevLeft />
                  Anterior
                </button>
                <button
                  className="btn-page"
                  disabled={page >= totalPages}
                  onClick={() => setPage(page + 1)}
                >
                  Siguiente
                  <IconChevRight />
                </button>
              </div>
            </div>
          )}
        </div>
      </main>
    </>
  );
}