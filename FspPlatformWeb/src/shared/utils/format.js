// 180000 → "180 000 ₽"
export function formatSalary(value) {
  if (value == null) return '—'
  return `${Number(value).toLocaleString('ru-RU')} ₽`
}

// Диапазон зарплаты
export function formatSalaryRange(from, to) {
  if (from == null && to == null) return '—'
  if (to == null || from === to) return `от ${formatSalary(from)}`
  return `${formatSalary(from)} – ${formatSalary(to)}`
}

// ISO → "02.10.2026"
export function formatDate(iso) {
  if (!iso) return '—'
  return new Date(iso).toLocaleDateString('ru-RU')
}

export function formatDateTime(iso) {
  if (!iso) return '—'
  const d = new Date(iso)
  return `${d.toLocaleDateString('ru-RU')} ${d.toLocaleTimeString('ru-RU', {
    hour: '2-digit',
    minute: '2-digit',
  })}`
}

export function isFresh(dateStr, days = 3) {
  if (!dateStr) return false
  return Date.now() - new Date(dateStr).getTime() < days * 86400_000
}

// ISO → "только что" / "5 минут назад" / "вчера" / "3 дня назад" / "08.10.2026".
// Для дат старше недели возвращает обычную дату — в панели уведомлений
// точность до секунды старше 7 дней не нужна.
export function formatRelativeTime(iso) {
  if (!iso) return '—'
  const then = new Date(iso).getTime()
  if (Number.isNaN(then)) return '—'
  const diffMs = Date.now() - then
  if (diffMs < 0) return formatDate(iso) // часы на клиенте отстают — не показываем "через 5 минут"
  const min = Math.floor(diffMs / 60000)
  if (min < 1) return 'только что'
  if (min < 60) return `${min} мин. назад`
  const hours = Math.floor(min / 60)
  if (hours < 24) {
    const tail = min % 60
    return tail >= 30 ? `${hours} ч. ${tail} мин. назад` : `${hours} ч. назад`
  }
  const days = Math.floor(hours / 24)
  if (days === 1) return 'вчера'
  if (days < 7) return `${days} дн. назад`
  return formatDate(iso)
}

// Компактная зарплата для карточек: «140–200 тыс. ₽», «от 150 тыс. ₽», «150 000 ₽».
// Для диапазонов от 100 тыс. сокращает до тысяч — как на hh/Яндекс.Вакансиях.
export function formatSalaryShort(from, to) {
  if (!from && !to) return 'з/п не указана'
  const useK = (n) => n >= 1000
  if (to && to > from && useK(from) && useK(to)) {
    return `${Math.round(from / 1000)}–${Math.round(to / 1000)} тыс. ₽`
  }
  if (from && useK(from) && (!to || to <= from)) {
    return `от ${Math.round(from / 1000)} тыс. ₽`
  }
  const f = new Intl.NumberFormat('ru-RU').format(from)
  return to && to > from
    ? `${f} – ${new Intl.NumberFormat('ru-RU').format(to)} ₽`
    : `от ${f} ₽`
}