import { useCallback, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { matchingApi } from '../../api/matching.api'
import { employerApi } from '../../api/employer.api'
import Spinner from '../../shared/components/Spinner'
import Badge from '../../shared/components/Badge'
import ApplicationCandidateCell from '../../shared/components/ApplicationCandidateCell'
import { formatSalaryRange, formatDate } from '../../shared/utils/format'

const INV_BADGE = {
  отправлено: { color: 'blue', text: 'отправлено' },
  просмотрено: { color: '', text: 'просмотрено' },
  принято: { color: 'green', text: 'принято' },
  отклонено: { color: 'red', text: 'отклонено' },
  истекло: { color: '', text: 'истекло' },
  отозвано: { color: '', text: 'отозвано' },
}

const APP_BADGE = {
  отправлен: { color: 'blue', text: 'отправлен' },
  просмотрен: { color: '', text: 'просмотрен' },
  принят: { color: 'green', text: 'принят' },
  отклонен: { color: 'red', text: 'отклонён' },
  отозван: { color: '', text: 'отозван' },
}

// Параметры приходят из колокольчика уведомлений (NotificationsBell):
// ?tab=applications — открыть вкладку откликов, ?vacancy=Название — отфильтровать по вакансии.
const VALID_TABS = ['invitations', 'applications']

export default function SentInvitationsPage() {
  const [searchParams, setSearchParams] = useSearchParams()

  const initialTab = VALID_TABS.includes(searchParams.get('tab') ?? '')
    ? searchParams.get('tab') ?? 'invitations'
    : 'invitations'

  const [tab, setTab] = useState(initialTab)
  const [invitations, setInvitations] = useState([])
  const [applications, setApplications] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [busyId, setBusyId] = useState(null)      // отзыв приглашения
  const [busyAppId, setBusyAppId] = useState(null) // принять/отклонить отклик

  // Фильтр по вакансии из query-string; null — без фильтра.
  const vacancyFilter = searchParams.get('vacancy')

  const load = useCallback(() => {
    setLoading(true)
    setError(null)

    // Приглашения — критичны для страницы
    matchingApi
      .companyInvitations()
      .then((list) => {
        setInvitations(list)
        setLoading(false)
      })
      .catch((e) => {
        setError(e.message)
        setLoading(false)
      })

    // Отклики — один агрегирующий запрос вместо цикла по всем вакансиям платформы
    employerApi
      .companyApplications()
      .then((list) => setApplications(list))
      .catch((e) => {
        // Нет компании / другая проблема — показываем, но не ломаем страницу
        setApplications([])
        setError((prev) => prev ?? e.message)
      })
  }, [])

  useEffect(load, [load])

  async function revoke(inv) {
    setBusyId(inv.id)
    setError(null)
    try {
      await matchingApi.revokeInvitation(inv.id)
      load()
    } catch (e) {
      setError(e.message)
    } finally {
      setBusyId(null)
    }
  }

  // Тот же API, что и на странице «Вакансии» (VacancyApplicationsController.UpdateStatus):
  // PATCH /api/vacancies/{vacancyId}/applications/{applicationId} со статусом «принят»/«отклонен».
  async function setAppStatus(app, status) {
    setBusyAppId(app.id)
    setError(null)
    try {
      await employerApi.updateApplicationStatus(app.vacancyId, app.id, status)
      const list = await employerApi.companyApplications()
      setApplications(list)
    } catch (e) {
      setError(e.message)
    } finally {
      setBusyAppId(null)
    }
  }

  // Переключение вкладки синхронизирует URL: чтобы обновление страницы (F5)
  // не сбрасывало пользователя на первую вкладку после перехода из уведомления.
  function switchTab(next) {
    setTab(next)
    setSearchParams(
      (prev) => {
        const p = new URLSearchParams(prev)
        p.set('tab', next)
        if (next !== 'applications') p.delete('vacancy')
        return p
      },
      { replace: true }
    )
  }

  // Отклики с учётом фильтра по вакансии из уведомления
  const visibleApplications = vacancyFilter
    ? applications.filter(
        (a) => a.vacancyTitle?.toLowerCase() === vacancyFilter.toLowerCase()
      )
    : applications

  function clearVacancyFilter() {
    setSearchParams(
      (prev) => {
        const p = new URLSearchParams(prev)
        p.delete('vacancy')
        return p
      },
      { replace: true }
    )
  }

  if (loading) return <Spinner text="Загружаем приглашения…" />

  return (
    <div>
      <div className="h1">Приглашения и отклики</div>
      <div className="sub">
        Статусы видят обе стороны. Контакты раскрываются только в статусе «принято».
      </div>

      <div className="pill-nav">
        <span
          className={`tag ${tab === 'invitations' ? 'sel' : ''}`}
          onClick={() => switchTab('invitations')}
        >
          Исходящие приглашения ({invitations.length})
        </span>
        <span
          className={`tag ${tab === 'applications' ? 'sel' : ''}`}
          onClick={() => switchTab('applications')}
        >
          Отклики на вакансии ({applications.length})
        </span>
      </div>

      {error && (
        <div className="notice red" style={{ marginBottom: 14 }}>
          {error}{' '}
          <button className="btn ghost sm" style={{ marginLeft: 10 }} onClick={load}>
            Повторить
          </button>
        </div>
      )}

      {tab === 'invitations' &&
        (invitations.length === 0 ? (
          <div className="card">
            <p className="hint">
              Приглашений пока нет. Найдите кандидатов в подборке и направьте
              предложение — инициатива у работодателя.
            </p>
          </div>
        ) : (
          <table className="tbl card" style={{ padding: '6px 0' }}>
            <thead>
              <tr>
                <th>Кандидат</th>
                <th>Вилка</th>
                <th>Отправлено</th>
                <th>Статус</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {invitations.map((inv) => {
                const sb = INV_BADGE[inv.status] ?? { color: '', text: inv.status }
                return (
                  <tr key={inv.id}>
                    <td data-label="Кандидат">
                      <b>{inv.candidateName}</b>
                      <div className="hint">{inv.title}</div>
                    </td>
                    <td data-label="Вилка">
                      {formatSalaryRange(inv.salaryFrom, inv.salaryTo)}
                    </td>
                    <td data-label="Отправлено">{formatDate(inv.sentAt)}</td>
                    <td data-label="Статус">
                      <Badge color={sb.color}>{sb.text}</Badge>
                    </td>
                    <td data-label="">
                      {['отправлено', 'просмотрено'].includes(inv.status) && (
                        <button
                          className="btn ghost sm"
                          disabled={busyId === inv.id}
                          onClick={() => revoke(inv)}
                        >
                          Отозвать
                        </button>
                      )}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        ))}

      {tab === 'applications' && (
        <>
          {vacancyFilter && (
            <div className="card" style={{ marginBottom: 14, display: 'flex', alignItems: 'center', gap: 12 }}>
              <span>
                Фильтр: вакансия «<b>{vacancyFilter}</b>» — {visibleApplications.length}{' '}
                из {applications.length}
              </span>
              <button className="btn ghost sm" onClick={clearVacancyFilter}>
                Сбросить фильтр
              </button>
            </div>
          )}

          {visibleApplications.length === 0 ? (
            <div className="card">
              <p className="hint">
                {vacancyFilter
                  ? 'Откликов по этой вакансии нет.'
                  : 'Откликов на вакансии пока нет. Опубликуйте вакансию — кандидаты смогут проявить инициативу.'}
              </p>
            </div>
          ) : (
            <table className="tbl card" style={{ padding: '6px 0' }}>
              <thead>
                <tr>
                  <th>Вакансия</th>
                  <th>Кандидат</th>
                  <th>Отклик</th>
                  <th>Статус</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {visibleApplications.map((app) => {
                  const sb = APP_BADGE[app.status] ?? { color: '', text: app.status }
                  const open = ['отправлен', 'просмотрен'].includes(app.status)
                  return (
                    <tr key={app.id}>
                      <td data-label="Вакансия">
                        <b>{app.vacancyTitle}</b>
                        <div className="hint">{app.companyName}</div>
                      </td>
                      <ApplicationCandidateCell app={app} />
                      <td data-label="Отклик">{formatDate(app.submittedAt)}</td>
                      <td data-label="Статус">
                        <Badge color={sb.color}>{sb.text}</Badge>
                      </td>
                      <td data-label="">
                        {open && (
                          <div className="row" style={{ gap: 6 }}>
                            <button
                              className="btn sm"
                              disabled={busyAppId === app.id}
                              onClick={() => setAppStatus(app, 'принят')}
                            >
                              Принять
                            </button>
                            <button
                              className="btn red sm"
                              disabled={busyAppId === app.id}
                              onClick={() => setAppStatus(app, 'отклонен')}
                            >
                              Отклонить
                            </button>
                          </div>
                        )}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          )}
        </>
      )}
    </div>
  )
}