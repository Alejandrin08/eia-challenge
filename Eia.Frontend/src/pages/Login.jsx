import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { authService } from '../services/authService';
import { ROUTES } from '../config';

export default function Login() {
  const [email, setEmail]       = useState('admin@eia.local');
  const [password, setPassword] = useState('Admin1234!');
  const [error, setError]       = useState('');
  const [loading, setLoading]   = useState(false);
  const navigate = useNavigate();

  const handleLogin = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const token = await authService.login(email, password);
      localStorage.setItem('token', token);
      navigate(ROUTES.DASHBOARD);
    } catch (err) {
      setError(err.response?.data?.error || 'Credenciales incorrectas. Intenta de nuevo.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <div className="login-panel-left">
        <div className="deco-grid" />
        <div className="panel-content">
          <div className="panel-badge">
            <span className="dot" />
            EIA Data 
          </div>
          <h2>Monitor de Cortes<br />Nucleares EIA</h2>
          <p>
            Datos diarios de capacidad y cortes de generacion nuclear en
            Estados Unidos. 
          </p>
        </div>
      </div>

      <div className="login-panel-right">
        <div className="login-form-wrap">
          <div className="form-header">
            <h3>Iniciar sesion</h3>
            <p>Ingresa tus credenciales para continuar</p>
          </div>

          {error && (
            <div className="alert alert-danger">
              <svg viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>
              {error}
            </div>
          )}

          <form onSubmit={handleLogin}>
            <div className="form-field">
              <label>Email</label>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="usuario@dominio.com"
                required
              />
            </div>
            <div className="form-field">
              <label>Contrasena</label>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                required
              />
            </div>
            <button type="submit" className="login-submit" disabled={loading}>
              {loading ? (
                <>
                  <span className="spinner spinner-sm" />
                  Autenticando...
                </>
              ) : 'Entrar'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}