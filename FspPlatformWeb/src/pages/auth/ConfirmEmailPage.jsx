import { useEffect, useRef, useState } from 'react'
import { useSearchParams, useNavigate } from 'react-router-dom'
import { authApi } from '../../api/auth.api'
import { useAuthStore } from '../../stores/authStore'
import Spinner from '../../shared/components/Spinner'

/**
 * Страница результата подтверждения email.
 * Ссылка из письма: /confirm-email?token=<одноразовый токен>.
 * Вызывает тот же GET /api/auth/confirm-email, что и раньше, но показывает
 * понятный результат вместо сырого JSON.
 */
export default function ConfirmEmailPage() {
  const [params] = useSearchParams()
  const navigate = useNavigate()
  const token = params.get('token')

  const setEmailConfirmed = useAuthStore((s) => s.setEmailConfirmed)

  const [state, setState] = useState('loading') // loading | ok | error
  const [message, setMessage] = useState('')
  // Защита от двойного запроса: в dev-режиме StrictMode вызывает эффект дважды,
  // а токен одноразовый — без флага второй вызов получил бы 400.
  const requestedRef = useRef(false)

  useEffect(() => {
    if (!token) {
      setState('error')
      setMessage('В ссылке нет токена подтверждения. Запросите письмо повторно.')
      return
    }
    if (requestedRef.current) return
    requestedRef.current = true
    authApi
      .confirmEmail(token)
      .then(() => {
        // Отмечаем почту подтверждённой в текущей сессии —
        // плашка «подтвердите адрес» исчезает без повторного входа.
        setEmailConfirmed(true)
        setState('ok')
        setMessage('Адрес электронной почты подтверждён. Теперь можно войти.')
      })
      .catch((e) => {
        setState('error')
        setMessage(e.message || 'Не удалось подтвердить адрес.')
      })
  }, [token])

  return (
    <div style={{ maxWidth: 460, margin: '60px auto' }}>
      <div className="card" style={{ textAlign: 'center', padding: 32 }}>
        {state === 'loading' && (
          <>
            <Spinner text="Подтверждаем адрес…" />
          </>
        )}
        {state === 'ok' && (
          <>
            <div className="h1" style={{ marginBottom: 10 }}>✓ Готово</div>
            <p style={{ marginBottom: 20 }}>{message}</p>
            <button className="btn" onClick={() => navigate('/login')}>
              Перейти ко входу
            </button>
          </>
        )}
        {state === 'error' && (
          <>
            <div className="h1" style={{ marginBottom: 10 }}>Не получилось</div>
            <p style={{ marginBottom: 20 }}>{message}</p>
            <button className="btn ghost" onClick={() => navigate('/login')}>
              Ко входу
            </button>
          </>
        )}
      </div>
    </div>
  )
}