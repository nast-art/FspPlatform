import { useEffect } from 'react'

/**
 * Модальное окно в стиле Linear/Notion:
 * - закрытие по клику на оверлей, по Esc и по крестику;
 * - блокирует прокрутку страницы, пока открыто.
 */
export default function Modal({ title, onClose, children, width = 620 }) {
  useEffect(() => {
    const onKey = (e) => {
      if (e.key === 'Escape') onClose?.()
    }
    document.addEventListener('keydown', onKey)
    document.body.style.overflow = 'hidden'
    return () => {
      document.removeEventListener('keydown', onKey)
      document.body.style.overflow = ''
    }
  }, [onClose])

  return (
    <div
      className="modal-overlay"
      onMouseDown={(e) => {
        // клик именно по оверлею, а не по содержимому окна
        if (e.target === e.currentTarget) onClose?.()
      }}
    >
      <div className="modal" style={{ maxWidth: width }}>
        <div className="modal-head">
          <div className="modal-title">{title}</div>
          <button className="modal-close" onClick={onClose} aria-label="Закрыть">
            ✕
          </button>
        </div>
        <div className="modal-body">{children}</div>
      </div>
    </div>
  )
}