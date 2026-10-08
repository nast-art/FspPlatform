import { useCallback, useEffect, useMemo, useState } from 'react'
import { vacanciesApi } from '../../api/vacancies.api'
import { useDictionaries } from '../../shared/hooks/useDictionaries'
import Spinner from '../../shared/components/Spinner'
import Badge from '../../shared/components/Badge'
import { formatSalaryShort, formatDate, isFresh } from '../../shared/utils/format'

const APP_BADGE = {
  отправлен: { color: 'blue', text: 'отправлен' },
  просмотрен: { color: '', text: 'просмотрен' },
  принят: { color: 'green', text: 'принят' },
  отклонен: { color: 'red', text: 'отклонён' },
  отозван: { color: '', text: 'отозван' },
}

// Активные отклики — всё, КРОМЕ отозванных: отозванный не блокирует повторный
// отклик (см. ApplicationService.ApplyAsync — реактивация той же записи).
const ACTIVE_APP_STATUSES = ['отправлен', 'просмотрен', 'принят', 'отклонен']

export default function VacanciesPage() {
  const { dictionaries, loading: dictLoading } = useDictionaries()
  const [tab, setTab] = useState('all') // all | mine
  const [vacancies, setVacancies] = useState(null)
  const [applications, setApplications] = useState([])
  const [error, setError] = useState(null)
  const [busy, setBusy] = useState(null)

  // Фильтры списка вакансий
  const [query, setQuery] = useState('')
  const [specId, setSpecId] = useState(0) // 0 — все специализации

  const load = useCallback(() => {
    Promise.all([
      vacanciesApi.published({ page: 1, pageSize: 50 }),
      vacanciesApi.myApplications(),
    ])
      .then(([v, a]) => {
        setVacancies(v)
        setApplications(a)
      })
      .catch((e) => setError(e.message))
  }, [])

  useEffect(load, [load])

  // Отклик по vacancyId — используется и из списка вакансий, и из карточки
  // отозванного отклика (реактивация записи на бэкенде).
  async function applyByVacancyId(vacancyId) {
    setBusy(vacancyId)
    setError(null)
    try {
      await vacanciesApi.apply(vacancyId, '')
      load()
    } catch (e) {
      setError(e.message)
    } finally {
      setBusy(null)
    }
  }

  async function withdraw(appId) {
    setBusy(appId)
    setError(null)
    try {
      await vacanciesApi.withdraw(appId)
      load()
    } catch (e) {
      setError(e.message)
    } finally {
      setBusy(null)
    }
  }

  // Выбранная специализация (объект из словаря).
  // ВАЖНО: VacancyDto не содержит specializationId — только specializationName,
  // поэтому сравниваем по имени, а не по id.
  const selectedSpec =
    specId > 0
      ? (dictionaries?.specializations ?? []).find((s) => s.id === specId)
      : null

  // Применённые фильтры: текст (название/описание/компания) + специализация
  const filtered = useMemo(() => {
    const list = vacancies ?? []
    const q = query.trim().toLowerCase()
    return list.filter((v) => {
      if (selectedSpec && v.specializationName !== selectedSpec.name) return false
      if (!q) return true
      return (
        (v.title ?? '').toLowerCase().includes(q) ||
        (v.description ?? '').toLowerCase().includes(q) ||
        (v.companyName ?? '').toLowerCase().includes(q)
      )
    })
  }, [vacancies, query, selectedSpec])

  const hasFilters = query.trim() !== '' || specId > 0

  function resetFilters() {
    setQuery('')
    setSpecId(0)
  }

  if (!vacancies || dictLoading) return <Spinner />

  // Вкладка «Все вакансии» считает ТОЛЬКО активные отклики.
  const activeApplications = applications.filter((a) =>
    ACTIVE_APP_STATUSES.includes(a.status)
  )
  const appliedVacancyIds = new Set(activeApplications.map((a) => a.vacancyId))

  return (
    <div>
      <div className="page-head">
        <div>
          <div className="h1">Вакансии и отклики</div>
          <div className="sub">
            Классический сценарий: просмотр опубликованных вакансий и
            самостоятельный отклик — дополнение к основной механике подбора,
            где инициативу проявляет работодатель.
          </div>
        </div>
      </div>

      <div className="pill-nav" style={{ marginBottom: 14 }}>
        <span
          className={`tag ${tab === 'all' ? 'sel' : ''}`}
          onClick={() => setTab('all')}
        >
          Все вакансии
        </span>
        {/* Счётчик — по ВСЕМ записям истории (включая отозванные),
            иначе число не совпадает с числом карточек в списке. */}
        <span
          className={`tag ${tab === 'mine' ? 'sel' : ''}`}
          onClick={() => setTab('mine')}
        >
          Мои отклики ({applications.length})
        </span>
      </div>

      {error && (
        <div className="notice red" style={{ marginBottom: 14 }}>
          {error}
        </div>
      )}

      {tab === 'all' && (
        <>
          {/* Строка фильтров: поиск + специализация + сброс */}
          <div className="filter-bar">
            <input
              className="input"
              placeholder="Поиск по названию, описанию, компании…"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
            />
            <select
              className="input"
              value={specId}
              onChange={(e) => setSpecId(Number(e.target.value))}
            >
              <option value={0}>Все специализации</option>
              {(dictionaries?.specializations ?? []).map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name}
                </option>
              ))}
            </select>
            {hasFilters && (
              <button className="btn ghost sm" onClick={resetFilters}>
                Сбросить
              </button>
            )}
          </div>

          {filtered.length === 0 ? (
            <div className="card" style={{ textAlign: 'center', padding: 28 }}>
              <p className="hint" style={{ marginBottom: 12 }}>
                {hasFilters
                  ? 'По заданным фильтрам ничего не найдено.'
                  : 'Опубликованных вакансий пока нет.'}
              </p>
              {hasFilters && (
                <button className="btn ghost sm" onClick={resetFilters}>
                  Сбросить фильтры
                </button>
              )}
            </div>
          ) : (
            filtered.map((v) => (
              <div key={v.id} className="card vac-card" style={{ marginBottom: 10 }}>
                <div className="vac-head">
                  <span className="vac-title">{v.title}</span>
                  <span className="vac-salary">
                    {formatSalaryShort(v.salaryFrom, v.salaryTo)}
                  </span>
                </div>
                <div className="vac-meta">
                  {v.companyName} · {v.location ?? 'локация не указана'} ·{' '}
                  {v.specializationName} · {v.gradeRange}
                </div>
                {v.skills?.length > 0 && (
                  <div className="vac-tags">
                    {v.skills.map((s) => (
                      <span key={s} className="tag">
                        {s}
                      </span>
                    ))}
                  </div>
                )}
                <div className="vac-foot">
                  <span className="hint">
                    {isFresh(v.publishedAt) && (
                      <>
                        <Badge color="blue">новая</Badge>{' '}
                      </>
                    )}
                    опубликована {formatDate(v.publishedAt)}
                  </span>
                  {appliedVacancyIds.has(v.id) ? (
                    <Badge color="green">вы откликнулись</Badge>
                  ) : (
                    <button
                      className="btn sm"
                      disabled={busy === v.id}
                      onClick={() => applyByVacancyId(v.id)}
                    >
                      {busy === v.id ? 'Отправка…' : 'Откликнуться'}
                    </button>
                  )}
                </div>
              </div>
            ))
          )}
        </>
      )}

      {tab === 'mine' && (
        <>
          {applications.length === 0 && (
            <div className="card">
              <p className="hint">
                Вы ещё не откликались ни на одну вакансию. Отклик — дополнительный
                сценарий: основная механика платформы — приглашения от
                работодателей.
              </p>
            </div>
          )}
          {applications.map((a) => {
            const sb = APP_BADGE[a.status] ?? { color: '', text: a.status }
            const isWithdrawn = a.status === 'отозван'
            return (
              <div key={a.id} className="card vac-card" style={{ marginBottom: 10 }}>
                <div className="vac-head">
                  <span className="vac-title">{a.vacancyTitle}</span>
                  <span className="vac-salary">
                    {formatSalaryShort(a.salaryFrom, a.salaryTo)}
                  </span>
                </div>
                <div className="vac-meta">
                  {a.companyName} · отклик от {formatDate(a.submittedAt)}
                  {isWithdrawn && ' · отозван — можно откликнуться заново'}
                </div>
                <div className="vac-foot">
                  <Badge color={sb.color}>{sb.text}</Badge>
                  {isWithdrawn ? (
                    // Отозванный отклик не тупик: реактивация той же записи.
                    <button
                      className="btn sm"
                      disabled={busy === a.vacancyId}
                      onClick={() => applyByVacancyId(a.vacancyId)}
                    >
                      {busy === a.vacancyId ? 'Отправка…' : 'Откликнуться снова'}
                    </button>
                  ) : (
                    ['отправлен', 'просмотрен'].includes(a.status) && (
                      <button
                        className="btn ghost sm"
                        disabled={busy === a.id}
                        onClick={() => withdraw(a.id)}
                      >
                        Отозвать
                      </button>
                    )
                  )}
                </div>
              </div>
            )
          })}
        </>
      )}
    </div>
  )
}