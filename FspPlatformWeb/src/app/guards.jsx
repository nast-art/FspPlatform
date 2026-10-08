import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { ROLES, useAuthStore } from '../stores/authStore'

// Защита маршрутов: требует авторизации и (опционально) конкретной роли
export function RequireAuth({ roles }) {
  const { token, role } = useAuthStore()
  const location = useLocation()

  if (!token) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }
  if (roles && !roles.includes(role)) {
    return (
      <Navigate
        to={role === ROLES.EMPLOYER ? '/employer' : '/candidate'}
        replace
      />
    )
  }
  return <Outlet />
}