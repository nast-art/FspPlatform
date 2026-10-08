import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { authApi } from '../../api/auth.api'
import { ROLES, useAuthStore } from '../../stores/authStore'
import Logo from '../../shared/components/Logo'
import Field from '../../shared/components/Field'
import PasswordInput from '../../shared/components/PasswordInput'

export default function LoginPage() {
  const navigate = useNavigate()
  const login = useAuthStore((s) => s.login)

  const [form, setForm] = useState({ email: '', password: '' })
  const [error, setError] = useState(null)
  const [loading, setLoading] = useState(false)

  function handleChange(e) {
    setForm((f) => ({ ...f, [e.target.name]: e.target.value }))
    setError(null)
  }

  async function handleSubmit(e) {
    e.preventDefault()
    if (!form.email || !form.password) {
      setError('Введите email и пароль.')
      return
    }
    setLoading(true)
    setError(null)
    try {
      const data = await authApi.login(form)
      login(data)
      navigate(data.role === ROLES.EMPLOYER ? '/employer' : '/candidate', {
        replace: true,
      })
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="auth-shell">
      <div className="auth-card">
        <Logo />

        <div className="auth-title">С возвращением</div>
        <div className="auth-subtitle">
          Войдите, чтобы продолжить — кабинет кандидата или работодателя
          откроется автоматически
        </div>

        {/* Сегментированные табы */}
        <div className="auth-tabs">
          <span className="auth-tabs-item active">Вход</span>
          <Link to="/register" className="auth-tabs-item">
            Регистрация
          </Link>
        </div>

        {error && <div className="auth-error">{error}</div>}

        <form onSubmit={handleSubmit} noValidate>
          <Field label="Электронная почта">
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

          <Field label="Пароль">
            <PasswordInput
              name="password"
              autoComplete="current-password"
              value={form.password}
              onChange={handleChange}
            />
          </Field>

          <div
            style={{
              display: 'flex',
              justifyContent: 'flex-end',
              margin: '-4px 0 20px',
            }}
          >
            <span className="mono" style={{ cursor: 'default' }}>
              Забыли пароль?
            </span>
          </div>

          <button
            className="btn auth-submit"
            type="submit"
            disabled={loading}
          >
            {loading ? 'Вход…' : 'Войти'}
          </button>
        </form>

        <p className="auth-footer">
          Нет аккаунта?{' '}
          <Link to="/register">Зарегистрируйтесь</Link> — опрос, выбор грейда и
          тест займут около 3 минут.
        </p>
      </div>
    </div>
  )
}