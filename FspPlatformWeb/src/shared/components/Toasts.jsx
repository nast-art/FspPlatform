import { useEffect, useState } from 'react'
import { toast } from './toast'

/**
 * Контейнер всплывающих уведомлений в правом нижнем углу.
 * - success/error — с иконкой и цветной обводкой (см. .toast в theme.css);
 * - у каждого тоста есть кнопка закрытия;
 * - ошибки показываются дольше (6 c), успехи — 4 c.
 */
export default function Toasts() {
  const [items, setItems] = useState([])

  useEffect(() => {
    return toast.subscribe((t) => {
      setItems((prev) => [...prev, t])
      const ttl = t.type === 'error' ? 6000 : 4000
      setTimeout(() => {
        setItems((prev) => prev.filter((x) => x.id !== t.id))
      }, ttl)
    })
  }, [])

  function dismiss(id) {
    setItems((prev) => prev.filter((x) => x.id !== id))
  }

  if (!items.length) return null

  return (
    <div className="toast-wrap">
      {items.map((t) => (
        <div key={t.id} className={`toast ${t.type}`} role="status">
          <span className="toast-icon" aria-hidden="true">
            {t.type === 'error' ? '!' : '✓'}
          </span>
          <span className="toast-msg">{t.message}</span>
          <button
            className="toast-close"
            aria-label="Закрыть уведомление"
            onClick={() => dismiss(t.id)}
          >
            ✕
          </button>
        </div>
      ))}
    </div>
  )
}