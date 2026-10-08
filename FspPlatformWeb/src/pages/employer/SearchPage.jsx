import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { matchingApi } from '../../api/matching.api'
import { useDictionaries } from '../../shared/hooks/useDictionaries'
import Spinner from '../../shared/components/Spinner'
import Badge from '../../shared/components/Badge'
import { formatDateTime } from '../../shared/utils/format'

const PAGE_SIZE = 10

export default function SearchPage() {
  const navigate = useNavigate()
  const { dictionaries, loading: dictLoading } = useDictionaries()

  const [filters, setFilters] = useState({
    specializationId: '',
    gradeId: '',
    categoryId: '',
    skillIds: [],
    city: '',
    hasFsp: false,
    query: '',
    page: 1,
  })
  const [result, setResult] = useState(null)
  const [history, setHistory] = useState(null)
  const [searching, setSearching] = useState(false)
  const [error, setError] = useState(null)

  const loadHistory = useCallback(() => {
    matchingApi
      .searchHistory(1, 10)
      .then(setHistory)
      .catch(() => {})
  }, [])

  useEffect(() => {
    loadHistory()
    // Начальная загрузка — БЕЗ сохранения в историю: иначе каждый заход на
    // страницу клонирует запись в «Моих запросах» и захламляет список.
    doSearch({ save: false })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  // Пагинация: все хуки объявлены ДО условного return ниже — иначе ломается
  // порядок хуков между рендерами (React ругается «change in the order of Hooks»).
  const totalPages = useMemo(
    () => (result ? Math.max(1, Math.ceil(result.total / PAGE_SIZE)) : 1),
    [result]
  )

  if (dictLoading || !dictionaries) return <Spinner />

  async function doSearch(overrides) {
    // save — служебный флаг вызова, НЕ часть фильтров: не кладём его в state,
    // иначе следующий поиск по кнопке унаследует save=false и перестанет сохраняться.
    const { save, ...f } = { ...filters, ...overrides }
    // Перелистывание страницы — это тот же запрос, а не новый поиск:
    // историю «Моих запросов» не трогаем, снапшот подборки остаётся прежним.
    const pageChangedOnly =
      Object.keys(overrides).length === 1 && 'page' in overrides
    const page = pageChangedOnly ? f.page : 1

    setSearching(true)
    setError(null)
    try {
      const res = await matchingApi.searchCandidates({
        specializationId: f.specializationId ? Number(f.specializationId) : undefined,
        gradeId: f.gradeId ? Number(f.gradeId) : undefined,
        categoryId: f.categoryId ? Number(f.categoryId) : undefined,
        skillIds: f.skillIds.length ? f.skillIds : undefined,
        city: f.city || undefined,
        hasFsp: f.hasFsp ? true : undefined,
        query: f.query || undefined,
        page,
        pageSize: PAGE_SIZE,
        save: save === false || pageChangedOnly ? false : undefined,
      })
      setResult(res)
      setFilters({ ...f, page })
      // «Мои запросы» обновляются после сохранённых поисков (не при листании)
      if (save !== false && !pageChangedOnly) loadHistory()
    } catch (e) {
      setError(e.message)
    } finally {
      setSearching(false)
    }
  }

  function toggleSkill(id) {
    setFilters((f) => ({
      ...f,
      skillIds: f.skillIds.includes(id)
        ? f.skillIds.filter((x) => x !== id)
        : [...f.skillIds, id],
    }))
  }

  // Человекочитаемые теги активных фильтров для сводки над результатами
  function filterChips() {
    const chips = []
    const spec = dictionaries.specializations.find(
      (s) => String(s.id) === String(filters.specializationId)
    )
    if (spec) chips.push({ key: 'spec', label: spec.name, clear: { specializationId: '', categoryId: '' } })
    const grade = dictionaries.grades.find((g) => String(g.id) === String(filters.gradeId))
    if (grade) chips.push({ key: 'grade', label: grade.name, clear: { gradeId: '', categoryId: '' } })
    const cat = dictionaries.categories.find((c) => String(c.id) === String(filters.categoryId))
    if (cat) chips.push({ key: 'cat', label: cat.displayName, clear: { categoryId: '' } })
    for (const id of filters.skillIds) {
      const sk = dictionaries.skills.find((s) => s.id === id)
      if (sk)
        chips.push({
          key: `sk-${id}`,
          label: sk.name,
          clear: { skillIds: filters.skillIds.filter((x) => x !== id) },
        })
    }
    if (filters.city) chips.push({ key: 'city', label: `г. ${filters.city}`, clear: { city: '' } })
    if (filters.query) chips.push({ key: 'q', label: `«${filters.query}»`, clear: { query: '' } })
    if (filters.hasFsp) chips.push({ key: 'fsp', label: 'с достижениями ФСП', clear: { hasFsp: false } })
    return chips
  }

  function clearAll() {
    const empty = {
      specializationId: '',
      gradeId: '',
      categoryId: '',
      skillIds: [],
      city: '',
      hasFsp: false,
      query: '',
      page: 1,
    }
    setFilters(empty)
    doSearch(empty)
  }

  async function openSaved(requestId) {
    setSearching(true)
    setError(null)
    try {
      const res = await matchingApi.savedResult(requestId)
      if (res) {
        setResult(res)
        // Восстанавливаем панель фильтров в том виде, в каком делалась подборка.
        // Ключи снапшота могут быть в PascalCase (серверный JsonSerializer.Serialize)
        // или camelCase — читаем оба варианта.
        const f = res.filters ?? {}
        const g = (camel) =>
          f[camel] ?? f[camel.charAt(0).toUpperCase() + camel.slice(1)]
        setFilters({
          specializationId: g('specializationId') != null ? String(g('specializationId')) : '',
          gradeId: g('gradeId') != null ? String(g('gradeId')) : '',
          categoryId: g('categoryId') != null ? String(g('categoryId')) : '',
          skillIds: Array.isArray(g('skillIds')) ? g('skillIds').map(Number) : [],
          city: g('city') ?? '',
          hasFsp: Boolean(g('hasFsp')),
          query: g('query') ?? '',
          page: 1,
        })
      }
    } catch (e) {
      setError(e.message)
    } finally {
      setSearching(false)
    }
  }

  const chips = filterChips()
  const categories = dictionaries.categories

  return (
    <div className="search-layout">
      {/* Фильтры */}
      <div className="filter-panel">
        <div className="mono" style={{ marginBottom: 14 }}>ФИЛЬТРЫ</div>

        <div className="field">
          <label>Специализация</label>
          <select
            className="input"
            value={filters.specializationId}
            onChange={(e) =>
              setFilters((f) => ({ ...f, specializationId: e.target.value, categoryId: '', page: 1 }))
            }
          >
            <option value="">Любая</option>
            {dictionaries.specializations.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>
        </div>

        <div className="field">
          <label>Грейд</label>
          <select
            className="input"
            value={filters.gradeId}
            onChange={(e) => setFilters((f) => ({ ...f, gradeId: e.target.value, categoryId: '', page: 1 }))}
          >
            <option value="">Любой</option>
            {dictionaries.grades.map((g) => (
              <option key={g.id} value={g.id}>
                {g.name}
              </option>
            ))}
          </select>
        </div>

        <div className="field">
          <label>Категория</label>
          <select
            className="input"
            value={filters.categoryId}
            onChange={(e) => setFilters((f) => ({ ...f, categoryId: e.target.value, page: 1 }))}
          >
            <option value="">Любая</option>
            {categories
              .filter(
                (c) =>
                  !filters.specializationId ||
                  c.specializationId === Number(filters.specializationId)
              )
              .map((c) => (
                <option key={c.id} value={c.id}>
                  {c.displayName}
                </option>
              ))}
          </select>
        </div>

        <div className="field">
          <label>Стек</label>
          <div>
            {dictionaries.skills.map((s) => (
              <span
                key={s.id}
                className={`tag ${filters.skillIds.includes(s.id) ? 'sel' : ''}`}
                style={{ cursor: 'pointer' }}
                onClick={() => toggleSkill(s.id)}
              >
                {s.name}
              </span>
            ))}
          </div>
        </div>

        <div className="field">
          <label>Город</label>
          <input
            className="input"
            value={filters.city}
            onChange={(e) => setFilters((f) => ({ ...f, city: e.target.value }))}
            onKeyDown={(e) => e.key === 'Enter' && doSearch({ city: filters.city })}
          />
        </div>

        <div className="field">
          <label>Поиск по ФИО / о себе</label>
          <input
            className="input"
            value={filters.query}
            onChange={(e) => setFilters((f) => ({ ...f, query: e.target.value }))}
            onKeyDown={(e) => e.key === 'Enter' && doSearch({ query: filters.query })}
          />
        </div>

        <div className="kv">
          <span>Только с достижениями ФСП</span>
          <span
            className={`toggle ${filters.hasFsp ? '' : 'off'}`}
            onClick={() => setFilters((f) => ({ ...f, hasFsp: !f.hasFsp, page: 1 }))}
          >
            <i />
          </span>
        </div>

        {/* Кнопка поиска — без счётчика; актуальное число найденных — в бейдже над таблицей */}
        <button
          className="btn"
          style={{ width: '100%', marginTop: 14 }}
          onClick={() => doSearch()}
          disabled={searching}
        >
          {searching ? 'Поиск…' : 'Найти'}
        </button>

        {history && history.length > 0 && (
          <>
            <div className="divider" />
            <div className="mono" style={{ marginBottom: 8 }}>МОИ ЗАПРОСЫ</div>
            {history.slice(0, 5).map((h) => (
              <div
                key={h.requestId}
                className="kv"
                style={{ cursor: 'pointer' }}
                onClick={() => openSaved(h.requestId)}
                title="Открыть сохранённую подборку"
              >
                <span className="hint">
                  подборка · {h.total} кандидатов
                  {h.createdAt ? ` · ${formatDateTime(h.createdAt)}` : ''}
                </span>
                <span className="mono">→</span>
              </div>
            ))}
          </>
        )}
      </div>

      {/* Результаты */}
      <div className="content" style={{ paddingTop: 18 }}>
        {/* Компактная шапка: заголовок + счётчик + сброс — одна строка */}
        <div
          className="row"
          style={{ justifyContent: 'space-between', marginBottom: 10, gap: 10 }}
        >
          <div className="row" style={{ gap: 10 }}>
            <span style={{ fontSize: 17, fontWeight: 800 }}>Поиск по банку</span>
            {result && (
              <Badge color="blue">найдено: {result.total}</Badge>
            )}
            {searching && <span className="hint">обновляем…</span>}
          </div>
          {chips.length > 0 && (
            <button className="btn ghost sm" onClick={clearAll}>
              Сбросить всё ✕
            </button>
          )}
        </div>

        {/* Сводка активных фильтров-тегов (удаление каждого в 1 клик) */}
        {chips.length > 0 && (
          <div className="pill-nav" style={{ marginBottom: 12 }}>
            <span className="mono" style={{ alignSelf: 'center' }}>ФИЛЬТРЫ:</span>
            {chips.map((c) => (
              <span
                key={c.key}
                className="tag sel"
                style={{ cursor: 'pointer' }}
                title="Убрать фильтр"
                onClick={() => doSearch({ ...c.clear, page: 1 })}
              >
                {c.label} ✕
              </span>
            ))}
          </div>
        )}

        {error && <div className="notice red" style={{ marginBottom: 14 }}>{error}</div>}

        {searching && !result ? (
          <Spinner />
        ) : !result ? (
          <div className="card">
            <p className="hint">Задайте фильтры слева и нажмите «Найти».</p>
          </div>
        ) : result.items.length === 0 ? (
          <div className="card">
            <p className="hint">
              По этим фильтрам никого не нашлось. Попробуйте убрать часть фильтров —
              например, город или грейд.
            </p>
          </div>
        ) : (
          <>
            <table className="tbl card" style={{ padding: '6px 0' }}>
              <thead>
                <tr>
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
                      <td data-label="Кандидат">
                        <b
                          style={{ cursor: 'pointer' }}
                          onClick={() => navigate(`/employer/candidates/${c.candidateId}`)}
                        >
                          {c.lastName} {c.firstName}
                        </b>
                        {/* Совпадение стека — объяснимость выдачи. Тег с знаменателем:
                            «3/5» читается как «совпало 3 из 5 выбранных навыков».
                            Не показываем, если стек не участвовал в поиске. */}
                        {ex.совпадение_навыков != null && filters.skillIds.length > 0 && (
                          <span
                            className="tag sel"
                            style={{ marginLeft: 8 }}
                            title="Совпавшие навыки из выбранного в фильтрах стека"
                          >
                            стек {ex.совпадение_навыков}/{filters.skillIds.length}
                          </span>
                        )}
                        <div className="explain">
                          {c.skills?.slice(0, 4).join(' · ')}
                          {c.city ? ` · ${c.city}` : ''}
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
                          <Badge color="blue">есть</Badge>
                        ) : (
                          <Badge>нет истории</Badge>
                        )}
                      </td>
                      {/* Рейтинг с расшифровкой по наведению — объяснимость выдачи */}
                      <td className="score" data-label="Рейтинг" title={rankTitle}>
                        {item.rankScore != null ? Number(item.rankScore).toFixed(1) : '—'}
                      </td>
                      <td data-label="">
                        <button
                          className="btn sm"
                          onClick={() => navigate(`/employer/candidates/${c.candidateId}`)}
                        >
                          Профиль
                        </button>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>

            {/* Пагинация: листаем выдачу без потери фильтров; переход по страницам
                не создаёт новых записей в «Моих запросах» (save=false) */}
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
          </>
        )}
      </div>
    </div>
  )
}