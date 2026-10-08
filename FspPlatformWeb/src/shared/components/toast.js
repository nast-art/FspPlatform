// Минимальная toast-система без контекста:
// toast.success('...') / toast.error('...') из любого места,
// а рендерит их компонент <Toasts /> (см. Toasts.jsx), смонтированный в корне App.
let listener = null
let seq = 0

function push(type, message) {
  const id = ++seq
  listener?.({ id, type, message })
}

export const toast = {
  success: (message) => push('success', message),
  error: (message) => push('error', message),
  subscribe(fn) {
    listener = fn
    return () => {
      if (listener === fn) listener = null
    }
  },
}