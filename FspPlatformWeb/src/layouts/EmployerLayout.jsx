import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuthStore } from '../stores/authStore'
import NotificationsBell from '../shared/components/NotificationsBell'
import Logo from '../shared/components/Logo'

const MENU = [
  { to: '/employer/company', label: 'Профиль компании' },
  { to: '/employer/demand', label: 'Потребность' },
  { to: '/employer/shortlist', label: 'Подборка кандидатов' },
  { to: '/employer/search', label: 'Поиск по банку' },
  { to: '/employer/invitations', label: 'Приглашения' },
  { to: '/employer/vacancies', label: 'Вакансии' },
  { to: '/employer/tasks', label: 'Задания кандидатам' },
  { to: '/employer/ats', label: 'Интеграции ATS' },
]

export default function EmployerLayout() {
  const navigate = useNavigate()
  const { email, emailConfirmed, logout } = useAuthStore()

  function handleLogout() {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <div>
      <div className="topnav">
        <Logo variant="inline" size={52} withText={false} />
        <span style={{ fontWeight: 800, fontSize: 15 }}>
          ФСП · Платформа ИТ-вакансий
        </span>
        <span className="badge blue" style={{ marginLeft: 8 }}>
          <span className="d" />
          кабинет работодателя
        </span>
        <span className="spacer" />
        <NotificationsBell />
        <span className="mono">{email}</span>
        <button className="btn ghost sm" onClick={handleLogout}>
          Выйти
        </button>
      </div>

      {/* Баннер: почта не подтверждена */}
      {emailConfirmed === false && (
        <div className="notice" style={{ margin: '0 28px 14px' }}>
          Подтвердите адрес электронной почты — письмо со ссылкой отправлено
          при регистрации (проверьте папку «Спам»).
        </div>
      )}

      <div className="layout">
        <div className="sidenav">
          {MENU.map((m) => (
            <NavLink
              key={m.to}
              to={m.to}
              className={({ isActive }) => (isActive ? 'on' : '')}
            >
              {m.label}
            </NavLink>
          ))}
        </div>
        <div className="content">
          <Outlet />
        </div>
      </div>
    </div>
  )
}