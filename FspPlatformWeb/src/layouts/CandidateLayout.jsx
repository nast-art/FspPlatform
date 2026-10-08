import { useEffect, useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuthStore } from '../stores/authStore'
import { matchingApi } from '../api/matching.api'
import NotificationsBell from '../shared/components/NotificationsBell'
import Logo from '../shared/components/Logo'

const MENU = [
  { to: '/candidate/profile', label: 'Профиль и резюме' },
  { to: '/candidate/category', label: 'Моя категория' },
  { to: '/candidate/test', label: 'Тестирование' },
  { to: '/candidate/invitations', label: 'Приглашения' },
  { to: '/candidate/tasks', label: 'Задания' },
  { to: '/candidate/vacancies', label: 'Вакансии и отклики' },
  { to: '/candidate/settings', label: 'Настройки' },
]

export default function CandidateLayout() {
  const navigate = useNavigate()
  const { email, emailConfirmed, logout } = useAuthStore()
  const [invCount, setInvCount] = useState(0)

  useEffect(() => {
    matchingApi
      .myInvitations()
      .then((list) =>
        setInvCount(
          list.filter(
            (i) => i.status === 'отправлено' || i.status === 'просмотрено'
          ).length
        )
      )
      .catch(() => {})
  }, [])

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
          кабинет кандидата
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
              {m.to === '/candidate/invitations' && invCount > 0 && (
                <span className="menu-badge">{invCount}</span>
              )}
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