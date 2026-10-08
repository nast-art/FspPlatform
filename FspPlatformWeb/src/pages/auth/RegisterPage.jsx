import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { authApi } from '../../api/auth.api'
import { ROLES } from '../../stores/authStore'
import Logo from '../../shared/components/Logo'
import Field from '../../shared/components/Field'
import PasswordInput from '../../shared/components/PasswordInput'

const ROLE_CARDS = [
  {
    role: ROLES.CANDIDATE,
    title: 'Кандидат',
    text: 'Пройду опрос и тест — работодатели сами найдут меня.',
  },
  {
    role: ROLES.EMPLOYER,
    title: 'Работодатель',
    text: 'Найду подтверждённых кандидатов и приглашу сам.',
  },
]

function CheckIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3.5" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="20 6 9 17 4 12" />
    </svg>
  )
}

export default function RegisterPage() {
  const navigate = useNavigate()

  const [role, setRole] = useState(null)
  const [form, setForm] = useState({
    email: '',
    password: '',
    passwordRepeat: '',
    fspId: '',
  })
  const [consent, setConsent] = useState(false)
  const [errors, setErrors] = useState({})
  const [error, setError] = useState(null)
  const [loading, setLoading] = useState(false)
  const [registeredEmail, setRegisteredEmail] = useState(null) // заглушка «письмо отправлено»

  function handleChange(e) {
    setForm((f) => ({ ...f, [e.target.name]: e.target.value }))
    setErrors((er) => ({ ...er, [e.target.name]: undefined }))
    setError(null)
  }

  function validate() {
    const er = {}
    if (!role) er.role = 'Выберите роль.'
    if (!form.email) er.email = 'Введите email.'
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email))
      er.email = 'Некорректный email.'
    if (!form.password) er.password = 'Введите пароль.'
    else if (form.password.length < 8) er.password = 'Минимум 8 символов.'
    if (form.passwordRepeat !== form.password)
      er.passwordRepeat = 'Пароли не совпадают.'
    if (!consent)
      er.consent =
        'Необходимо согласие на обработку персональных данных (152-ФЗ).'
    setErrors(er)
    return Object.keys(er).length === 0
  }

  async function handleSubmit(e) {
    e.preventDefault()
    if (!validate()) return
    setLoading(true)
    setError(null)
    try {
      await authApi.register({
        email: form.email.trim(),
        password: form.password,
        role,
        fspId: form.fspId.trim() || undefined,
      })
      // ЗАГЛУШКА: реальное письмо с подтверждением пока не отправляем/не проверяем.
      // На сервере Auth:RequireConfirmedEmail=false — вход доступен сразу.
      setRegisteredEmail(form.email.trim())
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  /* ---------- Экран-заглушка «письмо отправлено» ---------- */
  if (registeredEmail) {
    return (
      <div className="auth-shell">
        <div className="auth-card">
          <Logo />
          <div className="auth-title">Регистрация завершена</div>
          <div className="auth-subtitle">
            На <b style={{ color: 'var(--white)' }}>{registeredEmail}</b>{' '}
            отправлено письмо с подтверждением адреса. Перейдите по ссылке из
            письма в течение 24 часов.
          </div>
               <div className="notice" style={{ marginBottom: 18 }}>
            Письмо отправлено на почтовый сервер стенда — в демо его видно в
            Maildev (http://localhost:1080). Перейдите по ссылке, чтобы подтвердить
            адрес и снять напоминание в кабинете. Вход доступен и без
            подтверждения.
          </div>
          <button
            className="btn auth-submit"
            onClick={() => navigate('/login', { replace: true })}
          >
            Перейти ко входу
          </button>
        </div>
      </div>
    )
  }

  /* ---------- Форма регистрации ---------- */
  return (
    <div className="auth-shell">
      <div className="auth-card auth-card-wide">
        <Logo />

        <div className="auth-title">Создать аккаунт</div>
        <div className="auth-subtitle">
          Одна форма для кандидатов и работодателей — кабинет откроется по
          выбранной роли
        </div>

        {/* Сегментированные табы */}
        <div className="auth-tabs">
          <Link to="/login" className="auth-tabs-item">
            Вход
          </Link>
          <span className="auth-tabs-item active">Регистрация</span>
        </div>

        {error && <div className="auth-error">{error}</div>}

        <form onSubmit={handleSubmit} noValidate>
          {/* Шаг 1: выбор роли */}
          <Field label="Кто вы?" error={errors.role}>
            <div className="role-grid">
              {ROLE_CARDS.map((c) => (
                <div
                  key={c.role}
                  className={`role-card ${role === c.role ? 'sel' : ''}`}
                  onClick={() => {
                    setRole(c.role)
                    setErrors((er) => ({ ...er, role: undefined }))
                  }}
                >
                  <span className="role-check">
                    <CheckIcon />
                  </span>
                  <div className="role-card-title">{c.title}</div>
                  <div className="role-card-text">{c.text}</div>
                </div>
              ))}
            </div>
          </Field>

          {/* Шаг 2: учётные данные — две колонки */}
          <div className="grid2" style={{ gap: '0 14px' }}>
            <Field label="Электронная почта" error={errors.email}>
              <input
                className="input"
                type="email"
                name="email"
                autoComplete="email"
                placeholder="ivanov@example.ru"
                value={form.email}
                onChange={handleChange}
              />
            </Field>

            <Field
              label="ID участника ФСП"
              hint="Необязательно — для участников соревнований ФСП."
              error={errors.fspId}
            >
              <input
                className="input"
                type="text"
                name="fspId"
                placeholder="fsp-xxxxxxxx-…"
                value={form.fspId}
                onChange={handleChange}
              />
            </Field>

            <Field label="Пароль" error={errors.password} hint="Минимум 8 символов.">
              <PasswordInput
                name="password"
                autoComplete="new-password"
                value={form.password}
                onChange={handleChange}
              />
            </Field>

            <Field label="Повторите пароль" error={errors.passwordRepeat}>
              <PasswordInput
                name="passwordRepeat"
                autoComplete="new-password"
                placeholder="••••••••"
                value={form.passwordRepeat}
                onChange={handleChange}
              />
            </Field>
          </div>

          {/* Согласие 152-ФЗ */}
          <label className="auth-consent">
            <input
              type="checkbox"
              checked={consent}
              onChange={(e) => {
                setConsent(e.target.checked)
                setErrors((er) => ({ ...er, consent: undefined }))
              }}
            />
            <span>
              Соглашаюсь с условиями сервиса и обработкой персональных данных
              (152-ФЗ), включая публикацию профиля работодателям.
              {errors.consent && (
                <span className="auth-consent-error">{errors.consent}</span>
              )}
            </span>
          </label>

          <button
            className="btn auth-submit"
            type="submit"
            disabled={loading}
          >
            {loading ? 'Регистрация…' : 'Зарегистрироваться'}
          </button>
        </form>

        <p className="auth-footer">
          Уже есть аккаунт? <Link to="/login">Войти</Link>
        </p>
      </div>
    </div>
  )
}