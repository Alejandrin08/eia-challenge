import { useNavigate } from 'react-router-dom';
import { ROUTES } from '../config';

const IconCompass = () => (
  <svg viewBox="0 0 24 24" width="48" height="48" stroke="var(--border-strong)" fill="none" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" style={{ marginBottom: '24px' }}>
    <circle cx="12" cy="12" r="10"/>
    <polygon points="16.24 7.76 14.12 14.12 7.76 16.24 9.88 9.88 16.24 7.76"/>
  </svg>
);

const IconArrowLeft = () => (
  <svg viewBox="0 0 24 24" width="14" height="14" stroke="currentColor" fill="none" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <line x1="19" y1="12" x2="5" y2="12"/>
    <polyline points="12 19 5 12 12 5"/>
  </svg>
);

export default function NotFound() {
  const navigate = useNavigate();

  return (
    <div className="not-found-page">
      <div className="not-found-card">
        <IconCompass />
        <div className="not-found-code">404</div>
        <h2 className="not-found-title">Página no encontrada</h2>
        <p className="not-found-text">
          La ruta que intentas consultar no existe o fue movida. Verifica la URL o regresa al sistema principal.
        </p>
        <button 
          className="btn btn-accent" 
          style={{ width: '100%', padding: '12px' }}
          onClick={() => navigate(ROUTES?.DASHBOARD || '/dashboard')}
        >
          <IconArrowLeft /> Volver al Dashboard
        </button>
      </div>
    </div>
  );
}