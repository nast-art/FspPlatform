import { useCallback, useEffect, useState } from 'react'
import { matchingApi } from '../../api/matching.api'
import Spinner from '../../shared/components/Spinner'
import Badge from '../../shared/components/Badge'
import { formatSalaryRange, formatDate } from '../../shared/utils/format'

const STATUS_BADGE = {
  отправлено: { color: 'blue', text: 'отправлено' },
  просмотрено: { color: '', text: 'просмотрено' },
  принято: { color: 'green', text: 'принято' },
  отклонено: { color: 'red', text: 'отклонено' },
  истекло: { color: '', text: 'истекло' },
}

// Подпись канала связи: contactMethod — это канал, по которому РАБОТОДАТЕЛЬ
// свяжется с кандидатом (используя раскрытые контакты профиля), а не адрес работодателя.
const CONTACT_LABEL = {
  почта: 'по почте',
  телефон: 'по телефону',
  телеграм: 'в Telegram',
  через_приложение: 'через приложение',
}

export default function InvitationsPage() {
  const [items, setItems] = useState(null)
  const [error, setError] = useState(null)
  const [busyId, setBusyId] = useState(null)

  const load = useCallback(() => {
    matchingApi
      .myInvitations()
      .then(setItems)
      .catch((e) => setError(e.message))
  }, [])

  useEffect(load, [load])

  async function respond(inv, action) {
    setBusyId(inv.id)
    setError(null)
    try {
      await matchingApi.respondInvitation(inv.id, action)
      load()
    } catch (e) {
      setError(e.message)
    } finally {
      setBusyId(null)
    }
  }

  if (!items) return <Spinner />
  if (error && items == null) return <div className="notice red">{error}</div>

  const active = items.filter((i) => ['отправлено', 'просмотрено'].includes(i.status))

  return (
    <div>
      <div className="h1">Приглашения</div>
      <div className="sub">
        Входящие предложения с зарплатной вилкой — условия видны до начала
        общения. Принятие раскрывает ваши контакты работодателю.
      </div>

      {error && <div className="notice red" style={{ marginBottom: 14 }}>{error}</div>}

      {items.length === 0 && (
        <div className="card">
          <p className="hint">
            Пока приглашений нет. Заполните профиль, пройдите тест — и
            работодатели смогут найти вас в подборке по категории.
          </p>
        </div>
      )}

      {items.map((inv) => {
        const sb = STATUS_BADGE[inv.status] ?? { color: '', text: inv.status }
        const canRespond = ['отправлено', 'просмотрено'].includes(inv.status)
        return (
          <div key={inv.id} className="card invitation-card">
            <div className="row" style={{ justifyContent: 'space-between' }}>
              <div className="row">
                <div className="avatar">{(inv.companyName ?? ' ')[0]}</div>
                <div>
                  <b>{inv.companyName}</b>
                  <div className="hint">
                    {inv.vacancyId ? `по вакансии «${inv.title}»` : 'прямое предложение, без вакансии'}
                  </div>
                </div>
              </div>
              <Badge color={sb.color}>{sb.text}</Badge>
            </div>

            <p style={{ margin: '12px 0', color: 'var(--gray)' }}>{inv.message}</p>

            <div className="row" style={{ justifyContent: 'space-between' }}>
              <div className="row" style={{ gap: 8 }}>
                <span className="tag sel" style={{ fontSize: 14 }}>
                  {formatSalaryRange(inv.salaryFrom, inv.salaryTo)}
                </span>
                <span className="hint">
                  отправлено {formatDate(inv.sentAt)}
                  {inv.expiresAt ? ` · до ${formatDate(inv.expiresAt)}` : ''}
                </span>
              </div>
              {canRespond ? (
                <div className="row" style={{ gap: 8 }}>
                  <button className="btn sm"
                    disabled={busyId === inv.id}
                    onClick={() => respond(inv, 'accept')}>
                    Принять
                  </button>
                  <button className="btn red sm"
                    disabled={busyId === inv.id}
                    onClick={() => respond(inv, 'decline')}>
                    Отклонить
                  </button>
                </div>
              ) : (
                inv.status === 'принято' && (
                  <div style={{ textAlign: 'right', maxWidth: 320 }}>
                    <div style={{ fontSize: 13, color: 'var(--white)' }}>
                      Работодатель свяжется с вами:{' '}
                      <b>{CONTACT_LABEL[inv.contactMethod] ?? inv.contactMethod}</b>
                    </div>
                    <div className="hint" style={{ marginTop: 2 }}>
                      контакты раскрыты работодателю
                    </div>
                  </div>
                )
              )}
            </div>
          </div>
        )
      })}

      {active.length === 0 && items.length > 0 && (
        <p className="hint">Новых приглашений нет — все обработаны.</p>
      )}
    </div>
  )
}