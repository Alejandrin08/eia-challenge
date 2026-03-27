import { Navigate } from 'react-router-dom';
import { ROUTES } from '../config';

export default function ProtectedRoute({ children }) {
  const token = localStorage.getItem('token');

  if (!token) {
    return <Navigate to={ROUTES?.LOGIN || '/login'} replace />;
  }

  return children;
}