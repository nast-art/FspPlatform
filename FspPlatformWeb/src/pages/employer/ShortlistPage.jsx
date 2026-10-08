import { useCallback, useEffect, useMemo, useState } from 'react'
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom'
import { matchingApi } from '../../api/matching.api'
import { useDictionaries } from '../../shared/hooks/useDictionaries'
import Spinner from '../../shared/components/Spinner'
import Badge from '../../shared/components/Badge'

const PAGE_SIZE = 10

// Критерии потребности лежат в sessionStorage рядом с requestId
// (DemandPage сохраняет, здесь читаем — переживает перезагрузку страницы).
const demandKey = (requestId) => `demand:${requestId}`

// Объяснение под именем: ТОЛЬКО то, чего нет в колонках таблицы.
// Категория не дублируется — она и так колонкой справа.
function explainText(ex) {
  if (!ex) return ''
  const parts = []
  if (ex.результат_теста != null) parts.push(`тест ${ex.результат_теста}%`)
  if (ex.вес_достижений_фсп) parts.push(`ФСП +${ex.вес_достижений_фсп}`)
  if (ex.заполненность_профиля) parts.push(`профиль ${ex.заполненность_профиля}%`)
  if (ex.фсп) parts.push('нет истории ФСП')
  return parts.join(' · ')
}

export default function ShortlistPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const [params] = useSearchParams()
  const { dictionaries } = useDictionaries()

  const [result, setResult] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  // Критерии потребности, по которым открыта подборка (чипы сверху)
  const [criteria, setCriteria] = useState(null)
  const [criteriaVisible, setCriteriaVisible] = useState(true)

  const [filters, setFilters] = useState({
    categoryId: '',
    specializationId: '',
    gradeId: '',
    city: '',
    hasFsp: false,
    query: '',
    page: 1,
  })

  const doSearch = useCallback(
    async (overrides) => {
      const f = { ...filters, ...overrides }
      const pageChangedOnly = Object.keys(overrides).length === 1 && 'page' in overrides
      const page = pageChangedOnly ? f.page : 1

      setLoading(true)
      setError(null)
      try {
        const res = await matchingApi.searchCandidates({
          specializationId: f.specializationId ? Number(f.specializationId) : undefined,
          gradeId: f.gradeId ? Number(f.gradeId) : undefined,
          categoryId: f.categoryId ? Number(f.categoryId) : undefined,
          city: f.city || undefined,
          hasFsp: f.hasFsp ? true : undefined,
          query: f.query || undefined,
          page,
          pageSize: PAGE_SIZE,
          // Уточнения фильтров на подборке НЕ пишутся в историю: фильтры здесь
          // применяются автоматически, и каждый пересчёт клонировал бы запись
          // в «Моих запросах». Сама подборка сохранена снапшотом — этого
          // достаточно по ТЗ («уточнение без потери подборки»).
          save: false,
        })
        setResult(res)
        setFilters((prev) => ({ ...prev, ...overrides, page }))
      } catch (e) {
        setError(e.message)
      } finally {
        setLoading(false)
      }
    },
    [filters]
  )

  useEffect(() => {
    const requestId = params.get('requestId')
    if (requestId) {
      setLoading(true)
      setError(null)

      // Критерии: сначала из state навигации, иначе из sessionStorage по requestId
      let crit = location.state?.criteria ?? null
      if (!crit) {
        try {
          const raw = sessionStorage.getItem(demandKey(requestId))
          if (raw) crit = JSON.parse(raw)
        } catch { /* критериев нет — покажем без чипов */ }
      }
      setCriteria(crit)

      matchingApi
        .savedResult(requestId)
        .then((res) => {
          if (!res) throw new Error('Подборка не найдена. Сформируйте новую.')
          setResult(res)

          // Предзаполняем фильтр-бар критериями потребности — применённые условия
          // видны в интерфейсе, а не только в чипах.
          if (crit) {
            setFilters((prev) => ({
              ...prev,
              specializationId: crit.specializationId ? String(crit.specializationId) : '',
              gradeId: crit.gradeFromId ? String(crit.gradeFromId) : '',
              city: crit.city ?? '',
              page: 1,
            }))
          }
        })
        .catch((e) => setError(e.message))
        .finally(() => setLoading(false))
    } else {
      doSearch({ page: 1 })
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [params])

  function openCategory(cat) {
    doSearch({ categoryId: cat.id, page: 1 })
  }

  function resetFilters() {
    setFilters({
      categoryId: '',
      specializationId: '',
      gradeId: '',
      city: '',
      hasFsp: false,
      query: '',
      page: 1,
    })
    doSearch({
      categoryId: undefined,
      specializationId: '',
      gradeId: '',
      city: '',
      hasFsp: false,
      query: '',
      page: 1,
    })
  }

  const totalPages = useMemo(
    () => (result ? Math.max(1, Math.ceil(result.total / PAGE_SIZE)) : 1),
    [result]
  )
  const activeFiltersCount =
    (filters.categoryId ? 1 : 0) +
    (filters.specializationId ? 1 : 0) +
    (filters.gradeId ? 1 : 0) +
    (filters.city ? 1 : 0) +
    (filters.hasFsp ? 1 : 0) +
    (filters.query ? 1 : 0)

  // Чипы критериев для карточки сверху
  const criteriaChips = useMemo(() => {
    if (!criteria) return []
    const chips = []
    if (criteria.specializationName) chips.push(criteria.specializationName)
    if (criteria.gradeFromName || criteria.gradeToName)
      chips.push(
        `грейд: ${criteria.gradeFromName ?? '…'} – ${criteria.gradeToName ?? '…'}`
      )
    if (criteria.skillNames?.length)
      chips.push(`стек: ${criteria.skillNames.join(', ')}`)
    if (criteria.city) chips.push(`город: ${criteria.city}`)
    if (criteria.salaryFrom || criteria.salaryTo)
      chips.push(
        `вилка: ${
          criteria.salaryFrom
            ? `${criteria.salaryFrom.toLocaleString('ru-RU')} ₽`
            : '…'
        } – ${
          criteria.salaryTo
            ? `${criteria.salaryTo.toLocaleString('ru-RU')} ₽`
            : '…'
        }`
      )
    if (criteria.teamActivity)
      chips.push(
        `задача: ${criteria.teamActivity.slice(0, 80)}${
          criteria.teamActivity.length > 80 ? '…' : ''
        }`
      )
    return chips
  }, [criteria])

  // Знаменатель для тега совпадения стека: выбранные в критериях навыки
  const criteriaSkillCount = criteria?.skillNames?.length ?? 0

  if (loading && !result) return <Spinner text="Формируем подборку…" />

  return (
    <div>
      {/* КАРТОЧКА ПРИМЕНЁННЫХ КРИТЕРИЕВ — работодатель видит, что искалось */}
      {criteria && criteriaVisible && criteriaChips.length > 0 && (
        <div className="card" style={{ marginBottom: 14, borderColor: 'var(--blue)' }}>
          <div
            className="row"
            style={{ justifyContent: 'space-between', alignItems: 'center', marginBottom: 10 }}
          >
            <span className="mono">ПОДБОРКА ПО ВАШЕЙ ПОТРЕБНОСТИ</span>
            <button
              className="btn ghost sm"
              onClick={() => setCriteriaVisible(false)}
              title="Скрыть критерии"
            >
              ✕
            </button>
          </div>
          <div style={{ marginBottom: 10 }}>
            {criteriaChips.map((c) => (
              <span key={c} className="tag sel">
                {c}
              </span>
            ))}
          </div>
          <div
            className="row"
            style={{ justifyContent: 'space-between', alignItems: 'center' }}
          >
            <span className="hint">
              Найдено кандидатов: <b>{result?.total ?? '—'}</b>. Критерии
              предзаполнены в фильтрах ниже — уточнение запроса не теряет подборку.
            </span>
            <button
              className="btn ghost sm"
              onClick={() => navigate('/employer/demand')}
            >
              Изменить критерии
            </button>
          </div>
        </div>
      )}

      {dictionaries && (
        <div className="pill-nav" style={{ marginBottom: 12 }}>
          <span className="mono" style={{ alignSelf: 'center', marginRight: 6 }}>
            РЕКОМЕНДОВАННЫЕ КАТЕГОРИИ:
          </span>
          <span
            className={`tag ${!filters.categoryId ? 'sel' : ''}`}
            style={{ cursor: 'pointer' }}
            onClick={() => doSearch({ categoryId: undefined, page: 1 })}
          >
            Все категории
          </span>
          {dictionaries.categories
            .filter((c) => [1, 2, 4, 5].includes(c.specializationId) && c.gradeId <= 4)
            .slice(0, 8)
            .map((c) => (
              <span
                key={c.id}
                className={`tag ${String(filters.categoryId) === String(c.id) ? 'sel' : ''}`}
                style={{ cursor: 'pointer' }}
                onClick={() => openCategory(c)}
              >
                {c.displayName}
              </span>
            ))}
        </div>
      )}

      <div className="row" style={{ justifyContent: 'space-between', marginBottom: 14 }}>
        <div>
          <div className="h1">Подборка кандидатов</div>
          <div className="sub">
            {result
              ? `Найдено: ${result.total}${
                  criteria ? ' · критерии потребности применены' : ''
                }${activeFiltersCount ? ` · фильтров: ${activeFiltersCount}` : ''}`
              : 'Опишите потребность, чтобы получить подборку'}
          </div>
        </div>
        <div className="row" style={{ gap: 8 }}>
          {activeFiltersCount > 0 && (
            <button className="btn ghost sm" onClick={resetFilters}>
              Сбросить фильтры ✕
            </button>
          )}
          <button className="btn ghost sm" onClick={() => navigate('/employer/demand')}>
            Уточнить фильтры
          </button>
        </div>
      </div>

      <div className="card" style={{ marginBottom: 14 }}>
        <div className="grid2" style={{ gap: 12 }}>
          <div className="row" style={{ gap: 8 }}>
            <select
              className="input"
              style={{ flex: 1 }}
              value={filters.specializationId}
              onChange={(e) =>
                doSearch({ specializationId: e.target.value, categoryId: undefined })
              }
            >
              <option value="">Специализация: любая</option>
              {dictionaries?.specializations.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name}
                </option>
              ))}
            </select>
            <select
              className="input"
              style={{ flex: 1 }}
              value={filters.gradeId}
              onChange={(e) => doSearch({ gradeId: e.target.value, categoryId: undefined })}
            >
              <option value="">Грейд: любой</option>
              {dictionaries?.grades.map((g) => (
                <option key={g.id} value={g.id}>
                  {g.name}
                </option>
              ))}
            </select>
          </div>
          <div className="row" style={{ gap: 8 }}>
            <input
              className="input"
              style={{ flex: 1 }}
              placeholder="Город"
              value={filters.city}
              onChange={(e) => setFilters((f) => ({ ...f, city: e.target.value }))}
              onKeyDown={(e) => e.key === 'Enter' && doSearch({ city: filters.city, page: 1 })}
            />
            <input
              className="input"
              style={{ flex: 1 }}
              placeholder="Поиск по ФИО / о себе"
              value={filters.query}
              onChange={(e) => setFilters((f) => ({ ...f, query: e.target.value }))}
              onKeyDown={(e) => e.key === 'Enter' && doSearch({ query: filters.query, page: 1 })}
            />
            <label
              className="check-row"
              style={{ whiteSpace: 'nowrap', margin: 0 }}
              title="Только с достижениями ФСП"
            >
              <input
                type="checkbox"
                checked={filters.hasFsp}
                onChange={(e) => doSearch({ hasFsp: e.target.checked, page: 1 })}
              />
              <span>ФСП</span>
            </label>
          </div>
        </div>
        <p className="hint" style={{ margin: '8px 0 0' }}>
          Фильтры применяются автоматически. В текстовых полях — Enter.
        </p>
      </div>

      {error && <div className="notice red" style={{ marginBottom: 14 }}>{error}</div>}
      {loading && result && <Spinner text="Обновляем…" />}

      {!loading && (!result || result.items.length === 0) ? (
        <div className="card">
          <p className="hint">
            Пока нет кандидатов по выбранным фильтрам. Неподтверждённые тестом кандидаты
            тоже показываются — со статусом «грейд не подтверждён» и ниже в выдаче.
          </p>
        </div>
      ) : (
        !loading && (
          <>
            <table className="tbl card" style={{ padding: '6px 0' }}>
              <thead>
                <tr>
                  <th>#</th>
                  <th>Кандидат</th>
                  <th>Категория</th>
                  <th>Тест</th>
                  <th>ФСП</th>
                  <th>Рейтинг</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {result.items.map((item) => {
                  const c = item.candidate
                  const ex = item.explanation ?? {}
                  // Расшифровка рейтинга для tooltip — объяснимость выдачи (ТЗ, критерий 1)
                  const testPart = ex['результат_теста'] != null
                    ? (0.64 * Number(ex['результат_теста'])).toFixed(1)
                    : null
                  const fspPart = ex['вес_достижений_фсп'] != null
                    ? Math.min(Number(ex['вес_достижений_фсп']), 27).toFixed(1)
                    : null
                  const complPart = ex['заполненность_профиля'] != null
                    ? Math.min(Number(ex['заполненность_профиля']) * 0.09, 9).toFixed(1)
                    : null
                  const rankTitle =
                    `Рейтинг ${Number(item.rankScore).toFixed(1)} из 100 = ` +
                    `0.64 × тест (${testPart ?? '—'}) + ` +
                    `ФСП до 27 (${fspPart ?? '—'}) + ` +
                    `полнота до 9 (${complPart ?? '—'})`
                  return (
                    <tr key={c.candidateId}>
                      <td className="rank" data-label="#">
                        {(filters.page - 1) * PAGE_SIZE + item.position}
                      </td>
                      <td data-label="Кандидат">
                        <b
                          style={{ cursor: 'pointer' }}
                          onClick={() => navigate(`/employer/candidates/${c.candidateId}`)}
                        >
                          {c.lastName} {c.firstName}
                        </b>
                        {/* Совпадение стека из критериев потребности: «3/5» =
                            совпало 3 из 5 выбранных навыков. Только когда стек
                            реально участвовал в подборке. */}
                        {ex.совпадение_навыков != null && criteriaSkillCount > 0 && (
                          <span
                            className="tag sel"
                            style={{ marginLeft: 8 }}
                            title="Совпавшие навыки из стека, указанного в потребности"
                          >
                            стек {ex.совпадение_навыков}/{criteriaSkillCount}
                          </span>
                        )}
                        <div className="explain">
                          {c.skills?.slice(0, 4).join(' · ')}
                          {c.city ? ` · ${c.city}` : ''}
                          {explainText(ex) && (
                            <>
                              {' '}
                              · <b>{explainText(ex)}</b>
                            </>
                          )}
                        </div>
                      </td>
                      <td data-label="Категория">
                        {c.categoryName ?? '—'}
                        {c.confirmed === false && (
                          <>
                            {' '}
                            <Badge>не подтверждён</Badge>
                          </>
                        )}
                      </td>
                      <td className="score" data-label="Тест">
                        {c.bestAttemptPercent != null ? `${c.bestAttemptPercent}%` : '—'}
                        {c.confirmed === false && c.bestAttemptPercent != null && (
                          <span className="hint"> (не пройден)</span>
                        )}
                      </td>
                      <td data-label="ФСП">
                        {c.hasFspHistory ? (
                          <Badge color="blue">
                            {c.fspAchievements?.[0]?.resultPlace
                              ? `${c.fspAchievements[0].resultPlace} место`
                              : 'участник ФСП'}
                          </Badge>
                        ) : (
                          <Badge>нет истории ФСП</Badge>
                        )}
                      </td>
                      {/* Рейтинг с расшифровкой по наведению — объяснимость выдачи */}
                      <td className="score" data-label="Рейтинг" title={rankTitle}>
                        {item.rankScore != null ? Number(item.rankScore).toFixed(1) : '—'}
                      </td>
                      <td data-label="">
                        <button
                          className="btn sm"
                          onClick={() => navigate(`/employer/invite/${c.candidateId}`)}
                        >
                          Пригласить
                        </button>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>

            {totalPages > 1 && (
              <div className="pager" style={{ marginTop: 16 }}>
                <span
                  className={`nav ${filters.page <= 1 ? 'disabled' : ''}`}
                  onClick={() => filters.page > 1 && doSearch({ page: filters.page - 1 })}
                >
                  ← Назад
                </span>
                {Array.from({ length: totalPages }, (_, i) => i + 1)
                  .filter(
                    (p) => p === 1 || p === totalPages || Math.abs(p - filters.page) <= 2
                  )
                  .reduce((acc, p, idx, arr) => {
                    if (idx > 0 && p - arr[idx - 1] > 1) acc.push('…')
                    acc.push(p)
                    return acc
                  }, [])
                  .map((p, i) =>
                    p === '…' ? (
                      <span key={`dots-${i}`} className="dots">
                        …
                      </span>
                    ) : (
                      <span
                        key={p}
                        className={p === filters.page ? 'cur' : ''}
                        style={{ cursor: p === filters.page ? 'default' : 'pointer' }}
                        onClick={() => p !== filters.page && doSearch({ page: p })}
                      >
                        {p}
                      </span>
                    )
                  )}
                <span
                  className={`nav ${filters.page >= totalPages ? 'disabled' : ''}`}
                  onClick={() =>
                    filters.page < totalPages && doSearch({ page: filters.page + 1 })
                  }
                >
                  Вперёд →
                </span>
              </div>
            )}

            <p className="hint" style={{ marginTop: 12 }}>
              Почему этот порядок: рейтинг 0–100 = 0.64 × тест + до 27 баллов ФСП
              + до 9 за полноту профиля (наведите на колонку «Рейтинг»). Контакты
              откроются после принятия приглашения кандидатом. Каждый поиск на
              странице «Поиск по банку» сохраняется — подборку можно вернуть через
              «Мои запросы».
            </p>
          </>
        )
      )}
    </div>
  )
}