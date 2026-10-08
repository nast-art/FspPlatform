import { useCallback, useEffect, useState } from 'react'
import { employerApi } from '../../api/employer.api'
import { useDictionaries } from '../../shared/hooks/useDictionaries'
import Field from '../../shared/components/Field'
import Spinner from '../../shared/components/Spinner'
import Badge from '../../shared/components/Badge'
import ApplicationCandidateCell from '../../shared/components/ApplicationCandidateCell'
import { formatSalaryRange, formatDate } from '../../shared/utils/format'

const EMPTY = {
  title: '',
  description: '',
  specializationId: 1,
  gradeFromId: 2,
  gradeToId: 3,
  salaryFrom: '',
  salaryTo: '',
  employmentType: 'remote',
  location: '',
  skillIds: [],
}

export default function VacanciesPage() {
  const { dictionaries, loading: dictLoading } = useDictionaries()
  const [myCompany, setMyCompany] = useState(null) // ← фильтр «только свои»
  const [vacancies, setVacancies] = useState([])
  const [loading, setLoading] = useState(true)
  const [form, setForm] = useState(EMPTY)
  const [editingId, setEditingId] = useState(null)
  const [showForm, setShowForm] = useState(false)
  const [appsFor, setAppsFor] = useState(null)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState(null)
  const [notice, setNotice] = useState(null)

  const load = useCallback(() => {
    setLoading(true)
    Promise.all([employerApi.myCompany(), employerApi.publishedVacancies({ page: 1, pageSize: 100 })])
      .then(([company, all]) => {
        setMyCompany(company)
        // GET /api/vacancies публичный и отдаёт вакансии ВСЕХ компаний —
        // в кабинете работодателя показываем только свои.
        setVacancies(all.filter((v) => v.companyName === company.name))
        setLoading(false)
      })
      .catch((e) => {
        setError(e.message)
        setLoading(false)
      })
  }, [])

  useEffect(load, [load])

  if (dictLoading || !dictionaries) return <Spinner />
  if (loading) return <Spinner text="Загружаем вакансии…" />

  const grades = dictionaries.grades.filter((g) => g.rank <= 4)

  function startEdit(v) {
    setEditingId(v.id)
    setForm({
      title: v.title,
      description: v.description,
      specializationId:
        dictionaries.specializations.find((s) => s.name === v.specializationName)?.id ?? 1,
      gradeFromId: grades.find((g) => v.gradeRange.startsWith(g.name))?.id ?? 2,
      gradeToId:
        grades.find((g) => v.gradeRange.endsWith(g.name))?.id ??
        grades.find((g) => v.gradeRange.startsWith(g.name))?.id ??
        3,
      salaryFrom: v.salaryFrom,
      salaryTo: v.salaryTo,
      employmentType: v.employmentType ?? 'remote',
      location: v.location ?? '',
      skillIds: [],
    })
    setShowForm(true)
    setNotice(null)
  }

  function startCreate() {
    setEditingId(null)
    setForm(EMPTY)
    setShowForm(true)
    setNotice(null)
  }

  function closeForm() {
    setShowForm(false)
    setEditingId(null)
    setForm(EMPTY)
  }

  function toggleSkill(id) {
    setForm((f) => ({
      ...f,
      skillIds: f.skillIds.includes(id)
        ? f.skillIds.filter((x) => x !== id)
        : [...f.skillIds, id],
    }))
  }

  async function handleSave(e) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    setNotice(null)
    try {
      const dto = {
        title: form.title.trim(),
        description: form.description.trim(),
        specializationId: Number(form.specializationId),
        gradeFromId: Number(form.gradeFromId),
        gradeToId: Number(form.gradeToId),
        salaryFrom: Number(form.salaryFrom),
        salaryTo: Number(form.salaryTo),
        employmentType: form.employmentType || undefined,
        location: form.location || undefined,
        skillIds: form.skillIds.length ? form.skillIds : undefined,
      }
      if (editingId) {
        await employerApi.updateVacancy(editingId, dto)
        setNotice('Вакансия обновлена.')
      } else {
        const v = await employerApi.createVacancy(dto)
        await employerApi.publishVacancy(v.id)
        setNotice('Вакансия опубликована.')
      }
      closeForm()
      load()
    } catch (e2) {
      // теперь это 400 с текстом — покажется здесь, без разлогина
      setError(e2.message)
    } finally {
      setSaving(false)
    }
  }

  async function closeVacancy(v) {
    setError(null)
    try {
      await employerApi.closeVacancy(v.id)
      load()
    } catch (e) {
      setError(e.message)
    }
  }

  async function showApplications(v) {
    if (appsFor?.vacancyId === v.id) {
      setAppsFor(null)
      return
    }
    try {
      const apps = await employerApi.vacancyApplications(v.id)
      setAppsFor({ vacancyId: v.id, items: apps })
    } catch (e) {
      setError(e.message)
    }
  }

  async function setAppStatus(vacancyId, app, status) {
    setError(null)
    try {
      await employerApi.updateApplicationStatus(vacancyId, app.id, status)
      const apps = await employerApi.vacancyApplications(vacancyId)
      setAppsFor({ vacancyId, items: apps })
    } catch (e) {
      setError(e.message)
    }
  }

  return (
    <div>
      <div className="row" style={{ justifyContent: 'space-between', marginBottom: 14 }}>
        <div>
          <div className="h1">Вакансии</div>
          <div className="sub">
            {myCompany
              ? `${myCompany.name} · публикация вакансий — дополнительный сценарий (ТЗ): кандидат без приглашений сохраняет возможность проявить инициативу.`
              : 'Публикация вакансий — дополнительный сценарий (ТЗ).'}
          </div>
        </div>
        <button className="btn sm" onClick={startCreate}>
          + Новая вакансия
        </button>
      </div>

      {notice && <div className="notice" style={{ marginBottom: 14 }}>{notice}</div>}
      {error && <div className="notice red" style={{ marginBottom: 14 }}>{error}</div>}

      {/* ---------- Секционная inline-форма (без модала) ---------- */}
      {showForm && (
        <div className="card form-card">
          <div className="form-head">
            <div className="form-title">
              {editingId ? 'Редактирование вакансии' : 'Новая вакансия'}
            </div>
            <button className="form-close" aria-label="Закрыть форму" onClick={closeForm}>
              ✕
            </button>
          </div>

          <form onSubmit={handleSave}>
            <div className="form-body">
              {/* Секция: Основное */}
              <div className="form-section-title">Основное</div>
              <div className="form-grid">
                <div className="full">
                  <Field label="Название">
                    <input
                      className="input"
                      value={form.title}
                      onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))}
                      placeholder="Например: Backend-разработчик в команду платформы"
                      required
                    />
                  </Field>
                </div>
                <div className="full">
                  <Field label="Чем занимается команда">
                    <textarea
                      className="input"
                      rows={3}
                      value={form.description}
                      onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
                      placeholder="Пара предложений о продукте и команде — этого достаточно для первого контакта"
                      required
                    />
                  </Field>
                </div>
              </div>

              {/* Секция: Условия */}
              <div className="form-section-title">Условия</div>
              <div className="form-grid">
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
                <Field label="Грейд от – до">
                  <div className="form-pair">
                    <select
                      className="input"
                      value={form.gradeFromId}
                      onChange={(e) =>
                        setForm((f) => ({ ...f, gradeFromId: e.target.value }))
                      }
                    >
                      {grades.map((g) => (
                        <option key={g.id} value={g.id}>
                          {g.name}
                        </option>
                      ))}
                    </select>
                    <select
                      className="input"
                      value={form.gradeToId}
                      onChange={(e) =>
                        setForm((f) => ({ ...f, gradeToId: e.target.value }))
                      }
                    >
                      {grades.map((g) => (
                        <option key={g.id} value={g.id}>
                          {g.name}
                        </option>
                      ))}
                    </select>
                  </div>
                </Field>
                <Field label="Зарплатная вилка, ₽ · обязательно (ТЗ 2.2)">
                  <div className="form-pair">
                    <input
                      className="input"
                      type="number"
                      min="1"
                      value={form.salaryFrom}
                      onChange={(e) => setForm((f) => ({ ...f, salaryFrom: e.target.value }))}
                      placeholder="от"
                      required
                    />
                    <input
                      className="input"
                      type="number"
                      min="1"
                      value={form.salaryTo}
                      onChange={(e) => setForm((f) => ({ ...f, salaryTo: e.target.value }))}
                      placeholder="до"
                      required
                    />
                  </div>
                </Field>
                <div className="form-grid" style={{ gap: 12 }}>
                  <Field label="Формат">
                    <select
                      className="input"
                      value={form.employmentType}
                      onChange={(e) =>
                        setForm((f) => ({ ...f, employmentType: e.target.value }))
                      }
                    >
                      <option value="remote">Удалённо</option>
                      <option value="office">Офис</option>
                      <option value="hybrid">Гибрид</option>
                    </select>
                  </Field>
                </div>
                <div className="full">
                  <Field label="Локация">
                    <input
                      className="input"
                      value={form.location}
                      onChange={(e) => setForm((f) => ({ ...f, location: e.target.value }))}
                      placeholder="Москва · необязательно для удалёнки"
                    />
                  </Field>
                </div>
              </div>

              {/* Секция: Требования */}
              <div className="form-section-title">Требования · навыки</div>
              <div className="vac-tags" style={{ marginTop: 0 }}>
                {dictionaries.skills.map((s) => (
                  <span
                    key={s.id}
                    className={`tag ${form.skillIds.includes(s.id) ? 'sel' : ''}`}
                    style={{ cursor: 'pointer' }}
                    onClick={() => toggleSkill(s.id)}
                  >
                    {s.name}
                  </span>
                ))}
              </div>
            </div>

            {/* Липкая панель действий */}
            <div className="form-sticky">
              <button className="btn" type="submit" disabled={saving}>
                {saving ? 'Сохранение…' : editingId ? 'Сохранить изменения' : 'Опубликовать'}
              </button>
              <button className="btn ghost" type="button" onClick={closeForm}>
                Отмена
              </button>
            </div>
          </form>
        </div>
      )}

      {vacancies.length === 0 ? (
        <div className="card">
          <p className="hint">
            У вашей компании нет опубликованных вакансий. Создайте первую — кандидаты
            смогут откликнуться самостоятельно.
          </p>
        </div>
      ) : (
        vacancies.map((v) => (
          <div key={v.id} className="card" style={{ marginBottom: 12 }}>
            <div className="row" style={{ justifyContent: 'space-between' }}>
              <b>{v.title}</b>
              <div className="row" style={{ gap: 8 }}>
                <span className="tag sel">{formatSalaryRange(v.salaryFrom, v.salaryTo)}</span>
                <Badge color={v.status === 'активен' ? 'green' : ''}>{v.status}</Badge>
              </div>
            </div>
            <div className="hint" style={{ margin: '6px 0' }}>
              {v.specializationName} · {v.gradeRange} · {v.location ?? 'локация не указана'}
              {v.publishedAt ? ` · опубликована ${formatDate(v.publishedAt)}` : ''}
            </div>
            <div className="row" style={{ justifyContent: 'space-between' }}>
              <div className="row" style={{ gap: 8 }}>
                <button className="btn ghost sm" onClick={() => showApplications(v)}>
                  Отклики
                </button>
                <button className="btn ghost sm" onClick={() => startEdit(v)}>
                  Редактировать
                </button>
              </div>
              {v.status === 'активен' && (
                <button className="btn red sm" onClick={() => closeVacancy(v)}>
                  В архив
                </button>
              )}
            </div>

            {appsFor?.vacancyId === v.id && (
              <div style={{ marginTop: 12 }}>
                {appsFor.items.length === 0 ? (
                  <p className="hint">Откликов нет.</p>
                ) : (
                  <table className="tbl">
                    <thead>
                      <tr>
                        <th>Кандидат</th>
                        <th>Отклик</th>
                        <th>Статус</th>
                        <th></th>
                      </tr>
                    </thead>
                    <tbody>
                      {appsFor.items.map((a) => (
                        <tr key={a.id}>
                          <ApplicationCandidateCell app={a} />
                          <td data-label="Отклик">{formatDate(a.submittedAt)}</td>
                          <td data-label="Статус">{a.status}</td>
                          <td data-label="">
                            {['отправлен', 'просмотрен'].includes(a.status) && (
                              <div className="row" style={{ gap: 6 }}>
                                <button
                                  className="btn sm"
                                  onClick={() => setAppStatus(v.id, a, 'принят')}
                                >
                                  Принять
                                </button>
                                <button
                                  className="btn red sm"
                                  onClick={() => setAppStatus(v.id, a, 'отклонен')}
                                >
                                  Отклонить
                                </button>
                              </div>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>
            )}
          </div>
        ))
      )}
    </div>
  )
}