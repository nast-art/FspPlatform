import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { matchingApi } from '../../api/matching.api'
import { employerApi } from '../../api/employer.api'
import Field from '../../shared/components/Field'
import Spinner from '../../shared/components/Spinner'
import Badge from '../../shared/components/Badge'
import { formatSalary, formatSalaryRange } from '../../shared/utils/format'

const CONTACT_METHODS = [
  { code: 'почта', label: 'Почта' },
  { code: 'телефон', label: 'Телефон' },
  { code: 'телеграм', label: 'Telegram' },
  { code: 'через_приложение', label: 'Через приложение' },
]

export default function InvitePage() {
  const { candidateId } = useParams()
  const navigate = useNavigate()

  const [card, setCard] = useState(null)
  const [myCompany, setMyCompany] = useState(null)
  const [vacancies, setVacancies] = useState([])
  const [form, setForm] = useState({
    title: '',
    message: '',
    salaryFrom: '',
    salaryTo: '',
    vacancyId: '',
    contactMethod: 'почта',
  })
  const [sending, setSending] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    Promise.all([
      matchingApi.candidateCard(candidateId).catch((e) => {
        setError(e.message)
        return null
      }),
      employerApi.publishedVacancies({ page: 1, pageSize: 50 }).catch(() => []),
      employerApi.myCompany().catch(() => null),
    ]).then(([c, v, company]) => {
      setCard(c)
      setVacancies(v)
      setMyCompany(company)
      setLoading(false)
    })
  }, [candidateId])

  // Заполненность границ считаем по самим полям (а не по Number('') → 0),
  // чтобы предпросмотр показывал то, что реально введено.
  const fromFilled = form.salaryFrom !== '' && Number(form.salaryFrom) > 0
  const toFilled = form.salaryTo !== '' && Number(form.salaryTo) > 0
  const salaryFrom = Number(form.salaryFrom) || 0
  const salaryTo = Number(form.salaryTo) || 0
  // Отправка по-прежнему требует полный диапазон «от–до» (ТЗ 2.2 п.1)
  const salaryValid = fromFilled && toFilled && salaryTo >= salaryFrom

  // Предпросмотр: показываем любую введённую границу, даже неполную
  const salaryPreview =
    !fromFilled && !toFilled
      ? '— вилка не задана —'
      : fromFilled && toFilled
      ? salaryFrom === salaryTo
        ? formatSalary(salaryFrom) // 33–33 → «33 ₽»
        : formatSalaryRange(salaryFrom, salaryTo) // 33–55 → «33 ₽ – 55 ₽»
      : fromFilled
      ? `от ${formatSalary(salaryFrom)}` // 33–пусто → «от 33 ₽»
      : `до ${formatSalary(salaryTo)}` // пусто–55 → «до 55 ₽»

  const formReady =
    form.title.trim().length > 0 &&
    form.message.trim().length > 0 &&
    salaryValid

  // Что именно не заполнено — конкретика вместо абстрактного «заполните поля»
  const missing = []
  if (!form.title.trim()) missing.push('название')
  if (!form.message.trim()) missing.push('описание')
  if (!salaryValid) missing.push('вилка зарплаты (нужны обе границы)')

  async function handleSend(e) {
    e.preventDefault()
    if (!formReady) {
      setError(
        !salaryValid
          ? 'Проверьте зарплатную вилку: нужны обе границы, «от» > 0 и «до» ≥ «от».'
          : 'Заполните название и описание предложения.'
      )
      return
    }
    setSending(true)
    setError(null)
    try {
      await matchingApi.sendInvitation({
        candidateId,
        title: form.title.trim(),
        message: form.message.trim(),
        salaryFrom,
        salaryTo,
        vacancyId: form.vacancyId || undefined,
        contactMethod: form.contactMethod,
      })
      navigate('/employer/invitations')
    } catch (e2) {
      setError(e2.message)
      setSending(false)
    }
  }

  if (loading) return <Spinner text="Готовим форму приглашения…" />
  if (error && !card)
    return (
      <div style={{ maxWidth: 560, margin: '40px auto' }}>
        <div className="notice red">{error}</div>
        <button className="btn ghost" style={{ marginTop: 14 }} onClick={() => navigate(-1)}>
          Назад
        </button>
      </div>
    )

  return (
    <div>
      {/* Шапка: кандидат */}
      <div className="card" style={{ marginBottom: 14 }}>
        <div className="row" style={{ justifyContent: 'space-between' }}>
          <div className="row">
            <div className="avatar">
              {(card.lastName?.[0] ?? '') + (card.firstName?.[0] ?? '')}
            </div>
            <div>
              <div className="h1" style={{ fontSize: 18 }}>
                Приглашение: {card.lastName} {card.firstName}
              </div>
              <div className="sub" style={{ margin: 0 }}>
                {card.categoryName ?? 'категория не подтверждена'}
                {card.city ? ` · ${card.city}` : ''}
                {card.bestAttemptPercent != null ? ` · тест ${card.bestAttemptPercent}%${card.confirmed ? '' : ' (не пройден)'}` : ''}
              </div>
            </div>
          </div>
          <div className="row" style={{ gap: 8 }}>
            {card.hasFspHistory && <Badge color="blue">участник ФСП</Badge>}
            <Badge color="green">видит условия до общения</Badge>
          </div>
        </div>
      </div>

      {error && <div className="notice red" style={{ marginBottom: 14 }}>{error}</div>}

      <form onSubmit={handleSend}>
        <div className="grid2" style={{ gap: 14, alignItems: 'start' }}>
          {/* Левая колонка — содержание предложения */}
          <div className="card">
            <Field label="Название предложения">
              <input
                className="input"
                value={form.title}
                onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))}
                placeholder="Backend-разработчик в команду платёжных сервисов"
                required
              />
            </Field>
            <Field
              label={`Описание предложения${form.message ? ` · ${form.message.length} символов` : ''}`}
              hint="Стек, задачи, формат работы, рост. Можно кратко — вилка и название несут основную информацию."
            >
              <textarea
                className="input"
                rows={7}
                value={form.message}
                onChange={(e) => setForm((f) => ({ ...f, message: e.target.value }))}
                placeholder="Ищем разработчика в команду платёжных сервисов. Стек: C#, PostgreSQL, Docker. Гибрид 2/5, рост до Миддл за год."
                required
              />
            </Field>
            <Field label="Привязка к вакансии (необязательно)">
              <select
                className="input"
                value={form.vacancyId}
                onChange={(e) => setForm((f) => ({ ...f, vacancyId: e.target.value }))}
              >
                <option value="">— без вакансии, прямое предложение —</option>
                {vacancies.map((v) => (
                  <option key={v.id} value={v.id}>
                    {v.title} · {formatSalaryRange(v.salaryFrom, v.salaryTo)}
                  </option>
                ))}
              </select>
              {form.vacancyId && (
                <p className="hint" style={{ marginTop: 5 }}>
                  Вилка вакансии подставлена ниже — проверьте, что она совпадает с предложением.
                </p>
              )}
            </Field>
            <Field label="Способ связи после принятия">
              <select
                className="input"
                value={form.contactMethod}
                onChange={(e) => setForm((f) => ({ ...f, contactMethod: e.target.value }))}
              >
                {CONTACT_METHODS.map((m) => (
                  <option key={m.code} value={m.code}>
                    {m.label}
                  </option>
                ))}
              </select>
            </Field>
          </div>

          {/* Правая колонка — условия и отправка (липкая) */}
          <div className="card" style={{ position: 'sticky', top: 16 }}>
            <div className="mono" style={{ marginBottom: 10 }}>УСЛОВИЯ</div>
            <Field
              label="Зарплатная вилка, ₽ · обязательно"
              hint="Диапазон «от–до» попадёт в приглашение автоматически. Для отправки нужны обе границы."
            >
              <div className="grid2" style={{ gap: 10 }}>
                <input
                  className="input"
                  type="number"
                  min="1"
                  placeholder="от"
                  value={form.salaryFrom}
                  onChange={(e) => setForm((f) => ({ ...f, salaryFrom: e.target.value }))}
                  required
                />
                <input
                  className="input"
                  type="number"
                  min="1"
                  placeholder="до"
                  value={form.salaryTo}
                  onChange={(e) => setForm((f) => ({ ...f, salaryTo: e.target.value }))}
                  required
                />
              </div>
            </Field>

            {/* Живой предпросмотр вилки */}
            <div
              className="notice"
              style={{
                margin: '4px 0 14px',
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'baseline',
              }}
            >
              <span className="mono">Кандидат увидит:</span>
              <b style={{ fontSize: 15 }}>{salaryPreview}</b>
            </div>

            <div className="kv" style={{ marginBottom: 12 }}>
              <span className="k">Компания</span>
              <span style={{ textAlign: 'right' }}>
                <b>{myCompany?.name ?? 'сначала создайте профиль компании'}</b>
                {myCompany?.industry && (
                  <div className="hint">{myCompany.industry}</div>
                )}
              </span>
            </div>
            <div className="kv" style={{ marginBottom: 12 }}>
              <span className="k">Контакты кандидата</span>
              <span>раскроются после принятия</span>
            </div>
            <div className="kv" style={{ marginBottom: 4 }}>
              <span className="k">Готовность формы</span>
              <Badge color={formReady ? 'green' : ''}>
                {formReady ? 'можно отправлять' : 'заполните поля'}
              </Badge>
            </div>
            {!formReady && missing.length > 0 && (
              <p className="hint" style={{ margin: '0 0 8px', textAlign: 'right' }}>
                Не хватает: {missing.join(' · ')}
              </p>
            )}

            <button
              className="btn"
              type="submit"
              disabled={sending}
              style={{ width: '100%', marginTop: 12 }}
            >
              {sending ? 'Отправка…' : 'Отправить приглашение'}
            </button>
            <p className="hint" style={{ marginTop: 10, textAlign: 'center' }}>
              Отправить можно и без готовой вакансии — это прямое предложение
              конкретному человеку.
            </p>
          </div>
        </div>
      </form>
    </div>
  )
}