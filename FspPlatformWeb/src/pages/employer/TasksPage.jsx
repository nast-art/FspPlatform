import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { tasksApi } from '../../api/tasks.api'
import { matchingApi } from '../../api/matching.api'
import { useDictionaries } from '../../shared/hooks/useDictionaries'
import Field from '../../shared/components/Field'
import Modal from '../../shared/components/Modal'
import Badge from '../../shared/components/Badge'
import Spinner from '../../shared/components/Spinner'
import { toast } from '../../shared/components/toast'
import { formatDate, isFresh } from '../../shared/utils/format'

const EMPTY = {
  title: '',
  description: '',
  specializationId: 1,
  gradeId: '',
  maxScore: '',
}

const STATUS_BADGE = {
  назначено: { color: 'blue', text: 'назначено' },
  в_работе: { color: '', text: 'в работе' },
  выполнено: { color: 'green', text: 'выполнено' },
  пропущено: { color: '', text: 'пропущено' },
  срок_истек: { color: 'red', text: 'истёк срок' },
  отменено: { color: '', text: 'отменено' },
}

// Инициалы для аватарки из строки «Фамилия Имя [Отчество]»
function initialsOf(fullName) {
  const parts = (fullName ?? '').trim().split(/\s+/).filter(Boolean)
  return ((parts[0]?.[0] ?? '') + (parts[1]?.[0] ?? '')).toUpperCase()
}

export default function TasksPage() {
  const { dictionaries, loading: dictLoading } = useDictionaries()

  const [tab, setTab] = useState('tasks')

  // Данные
  const [tasks, setTasks] = useState([])
  const [completions, setCompletions] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  // Inline-форма создания (как на странице «Вакансии»)
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState(EMPTY)
  const [saving, setSaving] = useState(false)

  // Модал назначения кандидатам (picker — остаётся модалом)
  const [assignFor, setAssignFor] = useState(null)
  const [filters, setFilters] = useState({ specializationId: '', gradeId: '' })
  const [candidates, setCandidates] = useState([])
  const [searchBusy, setSearchBusy] = useState(false)
  const [selected, setSelected] = useState(new Set())
  const [assignBusy, setAssignBusy] = useState(false)

  // Архив / восстановление
  const [archiveBusy, setArchiveBusy] = useState(null)
  const [restoreBusy, setRestoreBusy] = useState(null)

  // Отзыв назначения
  const [unassignBusy, setUnassignBusy] = useState(null)

  // Оценка ответов
  const [scoreDrafts, setScoreDrafts] = useState({})
  const [scoreBusy, setScoreBusy] = useState(null)

  const load = useCallback(() => {
    setLoading(true)
    setError(null)
    Promise.all([tasksApi.myTasks(), tasksApi.completions()])
      .then(([t, c]) => {
        setTasks(t)
        setCompletions(c)
      })
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false))
  }, [])

  useEffect(load, [load])

  // Ответы сортируем «очередью на ревью»: сначала выполненные без оценки
  // (по свежести), затем остальные — тоже по свежести. Без этого карточка
  // «на оценке» тонет под «назначено/в работе», когда ответов станет много.
  // ВАЖНО: хук объявлен ДО условных return'ов ниже — иначе ломается порядок хуков.
  const sortedCompletions = useMemo(() => {
    const reviewRank = (c) => (c.statusCode === 'выполнено' && c.score == null ? 0 : 1)
    const ts = (c) => new Date(c.completedAt ?? c.assignedAt ?? 0).getTime()
    return [...completions].sort((a, b) => reviewRank(a) - reviewRank(b) || ts(b) - ts(a))
  }, [completions])

  if (dictLoading || !dictionaries) return <Spinner />
  if (loading) return <Spinner text="Загружаем задания…" />

  const activeTasks = tasks.filter((t) => t.isActive)
  const archivedTasks = tasks.filter((t) => !t.isActive)
  const awaitingReview = completions.filter((c) => c.statusCode === 'выполнено' && c.score == null)

  // ---------------- Создание задачи (inline-форма на странице) ----------------

  function startCreate() {
    setForm(EMPTY)
    setShowForm(true)
    setError(null)
  }

  async function handleSave(e) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    try {
      await tasksApi.createTask({
        title: form.title.trim(),
        description: form.description.trim(),
        specializationId: Number(form.specializationId),
        gradeId: form.gradeId ? Number(form.gradeId) : null,
        maxScore: form.maxScore ? Number(form.maxScore) : null,
      })
      setShowForm(false)
      toast.success('Задача создана')
      load()
    } catch (e2) {
      setError(e2.message)
    } finally {
      setSaving(false)
    }
  }

  // ---------------- Назначение кандидатам ----------------

  function openAssign(task) {
    setAssignFor(task)
    const specId = dictionaries.specializations.find((s) => s.name === task.specializationName)?.id
    const gradeId = dictionaries.grades.find((g) => g.name === task.gradeName)?.id
    setFilters({
      specializationId: specId ? String(specId) : '',
      gradeId: gradeId ? String(gradeId) : '',
    })
    setCandidates([])
    setSelected(new Set())
  }

  async function searchCandidates() {
    setSearchBusy(true)
    try {
      const res = await matchingApi.searchCandidates({
        specializationId: filters.specializationId ? Number(filters.specializationId) : undefined,
        gradeId: filters.gradeId ? Number(filters.gradeId) : undefined,
        // уже активно назначенные на эту задачу из списка исключаем;
        // отменённые остаются — их можно назначить повторно
        excludeTaskId: assignFor?.id,
        page: 1,
        pageSize: 20,
      })
      setCandidates(res.items ?? [])
      if (!res.items?.length) toast.error('По этим фильтрам кандидатов не найдено')
    } catch (e) {
      toast.error(e.message)
    } finally {
      setSearchBusy(false)
    }
  }

  function toggleCandidate(id) {
    setSelected((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  async function submitAssign() {
    if (!selected.size) {
      toast.error('Отметьте хотя бы одного кандидата')
      return
    }
    setAssignBusy(true)
    try {
      const assigned = await tasksApi.assignTaskBulk(assignFor.id, [...selected])
      toast.success(`Назначено кандидатов: ${assigned}`)
      setAssignFor(null)
      load()
    } catch (e) {
      toast.error(e.message)
    } finally {
      setAssignBusy(false)
    }
  }

  // ---------------- Архив / восстановление ----------------

  async function archiveTask(task) {
    setArchiveBusy(task.id)
    try {
      await tasksApi.archiveTask(task.id)
      toast.success('Задача архивирована — уже выданные задания продолжают действовать')
      load()
    } catch (e) {
      toast.error(e.message)
    } finally {
      setArchiveBusy(null)
    }
  }

  async function restoreTask(task) {
    setRestoreBusy(task.id)
    try {
      await tasksApi.restoreTask(task.id)
      toast.success('Задача возвращена из архива')
      load()
    } catch (e) {
      toast.error(e.message)
    } finally {
      setRestoreBusy(null)
    }
  }

  // ---------------- Отзыв назначения ----------------

  async function unassignTask(c) {
    setUnassignBusy(c.candidateTaskId)
    try {
      await tasksApi.unassignTask(c.candidateTaskId)
      toast.success(`Назначение отменено: ${c.candidateName}`)
      load()
    } catch (e) {
      toast.error(e.message)
    } finally {
      setUnassignBusy(null)
    }
  }

  // ---------------- Оценка ответов ----------------

  async function saveScore(c) {
    const raw = scoreDrafts[c.candidateTaskId]
    const value = Number(raw)
    if (raw === undefined || raw === '' || Number.isNaN(value) || value < 0) {
      toast.error('Введите балл от 0 до ' + c.maxScore)
      return
    }
    setScoreBusy(c.candidateTaskId)
    try {
      await tasksApi.scoreTask(c.candidateTaskId, value)
      toast.success(`Оценка сохранена: ${c.candidateName}`)
      load()
    } catch (e) {
      toast.error(e.message)
    } finally {
      setScoreBusy(null)
    }
  }

  // ---------------- Рендер ----------------

  return (
    <div>
      {/* Шапка — только заголовок и описание */}
      <div style={{ marginBottom: 14 }}>
        <div className="h1">Короткие задания кандидатам</div>
        <div className="sub">
          Регулярные задания поддерживают профиль кандидата в актуальном состоянии и дают
          работодателю свежий сигнал (ТЗ п.2, дополнительный функционал).
        </div>
      </div>

      {/* Табы; кнопка создания — только на вкладке «Мои задачи» */}
      <div className="pill-nav">
        <span
          className={`tag ${tab === 'tasks' ? 'sel' : ''}`}
          onClick={() => setTab('tasks')}
        >
          Мои задачи ({activeTasks.length})
        </span>
        <span
          className={`tag ${tab === 'answers' ? 'sel' : ''}`}
          onClick={() => setTab('answers')}
        >
          Ответы кандидатов ({completions.length})
          {awaitingReview.length > 0 ? ` · на оценке: ${awaitingReview.length}` : ''}
        </span>
        {tab === 'tasks' && (
          <>
            <span style={{ flex: 1 }} />
            <button className="btn sm" onClick={startCreate}>
              + Создать задачу
            </button>
          </>
        )}
      </div>

      {error && (
        <div className="notice red" style={{ marginBottom: 14 }}>
          {error}{' '}
          <button className="btn ghost sm" style={{ marginLeft: 10 }} onClick={load}>
            Повторить
          </button>
        </div>
      )}

      {/* ---------- Вкладка «Мои задачи» ---------- */}
      {tab === 'tasks' && (
        <>
          {/* Inline-форма создания задачи (как у вакансий) */}
          {showForm && (
            <div className="card" style={{ marginBottom: 16 }}>
              <div className="h1" style={{ fontSize: 16 }}>
                Новая задача кандидатам
              </div>
              <form onSubmit={handleSave}>
                <Field label="Название задачи">
                  <input
                    className="input"
                    value={form.title}
                    onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))}
                    placeholder="Например: Оптимизация SQL-запроса"
                    required
                  />
                </Field>
                <Field label="Описание задачи / кейса">
                  <textarea
                    className="input"
                    rows={4}
                    value={form.description}
                    onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
                    placeholder="Короткая задача: решить или предложить подход к решению"
                    required
                  />
                </Field>
                <div className="grid2" style={{ gap: 12 }}>
                  <Field label="Специализация">
                    <select
                      className="input"
                      value={form.specializationId}
                      onChange={(e) =>
                        setForm((f) => ({ ...f, specializationId: e.target.value }))
                      }
                    >
                      {dictionaries.specializations.map((s) => (
                        <option key={s.id} value={s.id}>
                          {s.name}
                        </option>
                      ))}
                    </select>
                  </Field>
                  <Field label="Грейд (необязательно)">
                    <select
                      className="input"
                      value={form.gradeId}
                      onChange={(e) => setForm((f) => ({ ...f, gradeId: e.target.value }))}
                    >
                      <option value="">— любой —</option>
                      {dictionaries.grades.map((g) => (
                        <option key={g.id} value={g.id}>
                          {g.name}
                        </option>
                      ))}
                    </select>
                  </Field>
                </div>
                <Field label="Максимальный балл (по умолчанию 10)">
                  <input
                    className="input"
                    type="number"
                    min="1"
                    value={form.maxScore}
                    onChange={(e) => setForm((f) => ({ ...f, maxScore: e.target.value }))}
                    placeholder="10"
                  />
                </Field>
                <div className="row" style={{ gap: 10 }}>
                  <button className="btn" type="submit" disabled={saving}>
                    {saving ? 'Сохранение…' : 'Создать задачу'}
                  </button>
                  <button className="btn ghost" type="button" onClick={() => setShowForm(false)}>
                    Отмена
                  </button>
                </div>
              </form>
            </div>
          )}

          {activeTasks.length === 0 && archivedTasks.length === 0 && !showForm && (
            <div className="card">
              <p className="hint">
                Задач пока нет. Нажмите «+ Создать задачу» — например, это может быть
                маленький кейс из вашей практики, который кандидат решит или предложит
                подход к решению.
              </p>
            </div>
          )}

          {activeTasks.map((t) => (
            <div key={t.id} className="card" style={{ marginBottom: 12 }}>
              <div className="row" style={{ justifyContent: 'space-between' }}>
                <b>{t.title}</b>
                <span className="tag">макс. балл: {t.maxScore}</span>
              </div>
              <div className="hint" style={{ margin: '6px 0' }}>
                {t.specializationName ?? 'специализация не указана'}
                {t.gradeName ? ` · ${t.gradeName}` : ''} · создана {formatDate(t.createdAt)}
              </div>
              <div className="hint" style={{ margin: '6px 0', whiteSpace: 'pre-wrap' }}>
                {t.description}
              </div>

              <div className="task-stats" style={{ margin: '8px 0 12px' }}>
                <span>
                  Назначено: <b>{t.assignedCount}</b>
                </span>
                <span>
                  Ответили: <b>{t.answeredCount}</b>
                </span>
                <span>
                  Средний балл:{' '}
                  <b>
                    {t.avgScore != null
                      ? `${Number(t.avgScore).toFixed(1)} / ${t.maxScore}`
                      : '—'}
                  </b>
                </span>
              </div>

              <div className="row" style={{ gap: 8 }}>
                <button
                  className="btn sm"
                  disabled={archiveBusy === t.id}
                  onClick={() => openAssign(t)}
                >
                  Назначить кандидатам
                </button>
                <button
                  className="btn ghost sm"
                  disabled={archiveBusy === t.id}
                  onClick={() => archiveTask(t)}
                >
                  В архив
                </button>
              </div>
            </div>
          ))}

          {archivedTasks.length > 0 && (
            <>
              <div className="sub" style={{ margin: '18px 0 10px' }}>
                В архиве ({archivedTasks.length}) — уже выданные задания продолжают действовать
              </div>
              {archivedTasks.map((t) => (
                <div key={t.id} className="card" style={{ marginBottom: 12, opacity: 0.6 }}>
                  <div className="row" style={{ justifyContent: 'space-between' }}>
                    <b>
                      {t.title} <Badge color="">в архиве</Badge>
                    </b>
                    <button
                      className="btn ghost sm"
                      disabled={restoreBusy === t.id}
                      onClick={() => restoreTask(t)}
                    >
                      Вернуть
                    </button>
                  </div>
                  <div className="task-stats" style={{ marginTop: 8 }}>
                    <span>
                      Назначено: <b>{t.assignedCount}</b>
                    </span>
                    <span>
                      Ответили: <b>{t.answeredCount}</b>
                    </span>
                  </div>
                </div>
              ))}
            </>
          )}
        </>
      )}

      {/* ---------- Вкладка «Ответы кандидатов» (без кнопки создания) ---------- */}
      {tab === 'answers' && (
        <>
          {sortedCompletions.length === 0 && (
            <div className="card">
              <p className="hint">
                Ответов пока нет. Назначьте задачу кандидатам на вкладке «Мои задачи» — как
                только кандидат решит задачу или предложит подход, ответ появится здесь,
                и вы сможете его оценить.
              </p>
            </div>
          )}

          {/* Рендерим sortedCompletions — «на оценке» всегда сверху */}
          {sortedCompletions.map((c) => {
            const sb = STATUS_BADGE[c.statusCode] ?? { color: '', text: c.status }
            const fresh = isFresh(c.completedAt, 3)
            const needScore = c.statusCode === 'выполнено' && c.score == null
            return (
              <div key={c.candidateTaskId} className="card" style={{ marginBottom: 12 }}>
                {/* Шапка карточки: аватар + имя-ссылка слева, бейджи и дата справа */}
                <div className="answer-card-head">
                  <div className="avatar">{initialsOf(c.candidateName)}</div>
                  <Link to={`/employer/candidates/${c.candidateId}`} className="cand-link">
                    {c.candidateName}
                  </Link>
                  {c.categoryName && <span className="tag">{c.categoryName}</span>}
                  <div className="head-meta">
                    {/* Один статусный бейдж: пока нет оценки — «на оценке» (акцентный
                        фиолетовый, тон бренда), после оценки — «выполнено» (зелёный).
                        Два бейджа рядом («выполнено» + «на оценке») читались
                        как противоречие, жёлтый под гамму ФСП не подходил. */}
                    <Badge color={needScore ? 'accent' : sb.color}>
                      {needScore ? 'на оценке' : sb.text}
                    </Badge>
                    {fresh && <Badge color="blue">свежий ответ</Badge>}
                    <span className="hint">
                      {c.completedAt
                        ? formatDate(c.completedAt)
                        : 'назначено ' + formatDate(c.assignedAt)}
                    </span>
                    {c.statusCode === 'назначено' && (
                      <button
                        className="btn quiet-danger sm"
                        disabled={unassignBusy === c.candidateTaskId}
                        onClick={() => unassignTask(c)}
                      >
                        Отменить назначение
                      </button>
                    )}
                  </div>
                </div>

                <div className="hint" style={{ margin: '10px 0' }}>
                  Задача: <b>{c.taskTitle}</b>
                </div>

                {c.answerText && <div className="answer-text">{c.answerText}</div>}

                {/* Подвал карточки: оценка отделена разделителем */}
                {c.statusCode === 'выполнено' &&
                  (needScore ? (
                    <form
                      className="score-row"
                      onSubmit={(e) => {
                        e.preventDefault()
                        saveScore(c)
                      }}
                    >
                      <span className="hint">Оценка (0–{c.maxScore}):</span>
                      <input
                        className="input"
                        type="number"
                        min="0"
                        max={c.maxScore}
                        step="0.5"
                        value={scoreDrafts[c.candidateTaskId] ?? ''}
                        placeholder={`0–${c.maxScore}`}
                        onChange={(e) =>
                          setScoreDrafts((prev) => ({
                            ...prev,
                            [c.candidateTaskId]: e.target.value,
                          }))
                        }
                      />
                      <button
                        className="btn sm"
                        type="submit"
                        disabled={scoreBusy === c.candidateTaskId}
                      >
                        Сохранить оценку
                      </button>
                    </form>
                  ) : (
                    <div className="score-row">
                      <Badge color="green">
                        оценено: {Number(c.score).toFixed(1)} / {c.maxScore}
                      </Badge>
                    </div>
                  ))}
              </div>
            )
          })}
        </>
      )}

      {/* ---------- Модал: назначение кандидатам (picker) ---------- */}
      {assignFor && (
        <Modal title={`Назначить: ${assignFor.title}`} onClose={() => setAssignFor(null)}>
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginBottom: 12 }}>
            <select
              className="input"
              style={{ flex: 1, minWidth: 180 }}
              value={filters.specializationId}
              onChange={(e) => setFilters((p) => ({ ...p, specializationId: e.target.value }))}
            >
              <option value="">Любая специализация</option>
              {dictionaries.specializations.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name}
                </option>
              ))}
            </select>
            <select
              className="input"
              style={{ flex: 1, minWidth: 150 }}
              value={filters.gradeId}
              onChange={(e) => setFilters((p) => ({ ...p, gradeId: e.target.value }))}
            >
              <option value="">Любой грейд</option>
              {dictionaries.grades.map((g) => (
                <option key={g.id} value={g.id}>
                  {g.name}
                </option>
              ))}
            </select>
            <button className="btn sm" onClick={searchCandidates} disabled={searchBusy}>
              {searchBusy ? 'Ищем…' : 'Найти кандидатов'}
            </button>
          </div>

          {candidates.length > 0 && (
            <>
              <div className="hint" style={{ marginBottom: 8 }}>
                Отмечено: {selected.size} из {candidates.length}
              </div>
              {candidates.map((item) => {
                const c = item.candidate
                return (
                  <label key={c.candidateId} className="pick-row">
                    <input
                      type="checkbox"
                      checked={selected.has(c.candidateId)}
                      onChange={() => toggleCandidate(c.candidateId)}
                    />
                    <div className="pick-info">
                      <div className="pick-name">
                        {c.lastName} {c.firstName}
                      </div>
                      <div className="pick-sub">
                        {c.categoryName ?? 'категория не присвоена'}
                        {c.bestAttemptPercent != null ? ` · тест: ${c.bestAttemptPercent}%` : ''}
                        {c.confirmed === false ? ' · не подтверждён' : ''}
                      </div>
                    </div>
                    <span className="hint">#{item.position}</span>
                  </label>
                )
              })}
            </>
          )}

          {candidates.length === 0 && !searchBusy && (
            <p className="hint" style={{ marginBottom: 12 }}>
              Задайте фильтры и нажмите «Найти кандидатов» — выбор будет из категоризированной
              базы, как в подборке на странице поиска.
            </p>
          )}

          <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
            <button className="btn ghost sm" onClick={() => setAssignFor(null)}>
              Отмена
            </button>
            <button
              className="btn sm"
              disabled={assignBusy || selected.size === 0}
              onClick={submitAssign}
            >
              {assignBusy ? 'Назначаем…' : `Назначить выбранным (${selected.size})`}
            </button>
          </div>
        </Modal>
      )}
    </div>
  )
}