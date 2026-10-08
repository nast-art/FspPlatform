import { useCallback, useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuthStore } from '../../stores/authStore'
import { notificationsApi } from '../../api/notifications.api'
import { formatRelativeTime } from '../utils/format'

/**
 * Колокольчик уведомлений для топбара обоих кабинетов.
 * - опрос раз в 30 секунд;
 * - выпадающая панель со списком, клик по уведомлению — переход + прочтение;
 * - «отметить все прочитанными».
 * Маршрут перехода зависит от роли и типа уведомления (payload подставляется из API).
 * Deep-link: работодателю «новый отклик» ведёт сразу на вкладку откликов с фильтром
 * по вакансии (?tab=applications&vacancy=...), «ответ на приглашение» — на вкладку
 * исходящих приглашений.
 */
export default function NotificationsBell() {
  const navigate = useNavigate()
  const role = useAuthStore((s) => s.role)
  const [items, setItems] = useState([])
  const [open, setOpen] = useState(false)
  const rootRef = useRef(null)

  const load = useCallback(() => {
    notificationsApi
      .list()
      .then((list) => setItems(list ?? []))
      .catch(() => {})
  }, [])

  useEffect(() => {
    load()
    const t = setInterval(load, 30000)
    return () => clearInterval(t)
  }, [load])

  useEffect(() => {
    if (!open) return undefined
    const onDoc = (e) => {
      if (rootRef.current && !rootRef.current.contains(e.target)) setOpen(false)
    }
    document.addEventListener('mousedown', onDoc)
    return () => document.removeEventListener('mousedown', onDoc)
  }, [open])

  const unread = items.filter((n) => !n.readAt).length

  /** Кодирует значение для query-string: кириллица в названиях вакансий ломала бы URL. */
  const enc = (v) => encodeURIComponent(v ?? '')

  /** Маршрут перехода по типу уведомления и роли пользователя. */
  function linkFor(n) {
    const p = n.payload ?? {}
    const t = n.type
    if (role === 'кандидат') {
      if (t === 'получено_приглашение') return '/candidate/invitations'
      if (t === 'смена_статуса_отклика') return '/candidate/vacancies'
      if (t === 'новое_задание' || t === 'оценка_задания' || t === 'задание_отменено')
        return '/candidate/tasks'
      return '/candidate/invitations'
    }
    if (t === 'ответ_на_приглашение') return '/employer/invitations?tab=invitations'
    if (t === 'новый_отклик')
      return `/employer/invitations?tab=applications&vacancy=${enc(p.vacancy)}`
    return '/employer/invitations'
  }

  /** Человекочитаемый текст уведомления. Ключи payload — контракт с бэкендом:
   *  ApplicationService/InvitationService/EmployerTaskService (NotificationService.CreateAsync). */
  function textFor(n) {
    const p = n.payload ?? {}
    switch (n.type) {
      case 'получено_приглашение':
        return `Новое приглашение от «${p.company ?? 'компании'}»: ${p.title ?? ''}`
      case 'ответ_на_приглашение': {
        const who = p.candidate ?? 'Кандидат'
        const what = p.title ? ` на «${p.title}»` : ''
        return `${who} ${p.status === 'принято' ? 'принял' : 'отклонил'} приглашение${what}`
      }
      case 'новый_отклик':
        return `Новый отклик на вакансию «${p.vacancy ?? ''}» от ${p.candidate ?? 'кандидата'}`
      case 'смена_статуса_отклика': {
        const who = p.company ? ` от «${p.company}»` : ''
        return `Отклик на «${p.vacancy ?? ''}»${who}: ${p.status ?? ''}`
      }
      case 'новое_задание':
        return `Новое задание от «${p.company ?? ''}»: ${p.title ?? ''}`
      case 'оценка_задания':
        return `Оценка задания «${p.title ?? ''}»: ${p.score ?? '—'} баллов`
      case 'задание_отменено':
        return `Задание «${p.title ?? ''}» отменено`
      default:
        return n.type
    }
  }

  async function openItem(n) {
    if (!n.readAt) {
      notificationsApi.markRead(n.id).catch(() => {})
      setItems((prev) => prev.map((x) => (x.id === n.id ? { ...x, readAt: new Date().toISOString() } : x)))
    }
    setOpen(false)
    navigate(linkFor(n))
  }

  async function markAll() {
    const unreadIds = items.filter((n) => !n.readAt).map((n) => n.id)
    await Promise.allSettled(unreadIds.map((id) => notificationsApi.markRead(id)))
    load()
  }

  return (
    <div className="bell" ref={rootRef}>
           <button
        type="button"
        className="bell-btn"
        aria-label="Уведомления"
        onClick={() => setOpen((v) => !v)}
      >
        <svg
          width="17"
          height="17"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
          aria-hidden="true"
        >
          <path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9" />
          <path d="M13.7 21a2 2 0 0 1-3.4 0" />
        </svg>
        {unread > 0 && <span className="bell-badge">{unread > 9 ? '9+' : unread}</span>}
      </button>

      {open && (
        <div className="bell-panel">
          <div className="bell-head">
            <span className="mono">Уведомления</span>
            {unread > 0 && (
              <button type="button" className="btn ghost sm" onClick={markAll}>
                Прочитать все
              </button>
            )}
          </div>
          {items.length === 0 && <p className="hint">Уведомлений пока нет.</p>}
          {items.slice(0, 15).map((n) => (
            <div
              key={n.id}
              className={`bell-item ${n.readAt ? '' : 'unread'}`}
              onClick={() => openItem(n)}
            >
              <div className="bell-item-text">{textFor(n)}</div>
              <div className="hint">{formatRelativeTime(n.createdAt)}</div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}