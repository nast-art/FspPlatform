import { Link } from 'react-router-dom'

/**
 * Ячейка «Кандидат» в таблицах откликов работодателя.
 * Имя — ссылка на карточку кандидата; ниже контакты и сопроводительное письмо.
 * Контакты API отдаёт только после отклика (ТЗ 2.2 п.2), поэтому их отсутствие
 * — штатное состояние «контактов нет в профиле», а не ошибка рендера.
 * data-label нужен мобильному режиму: на телефоне td превращается в строку
 * «подпись — значение».
 */
export default function ApplicationCandidateCell({ app }) {
  const name = app.candidateName?.trim() || 'Кандидат'
  const contacts = [app.email, app.phone, app.telegram].filter(Boolean)

  return (
    <td data-label="Кандидат">
      {app.candidateId ? (
        <Link to={`/employer/candidates/${app.candidateId}`}>
          <b>{name}</b>
        </Link>
      ) : (
        <b>{name}</b>
      )}
      {contacts.length > 0 && <div className="hint">{contacts.join(' · ')}</div>}
      {app.coverLetter && (
        <div className="hint" style={{ marginTop: 4, fontStyle: 'italic' }}>
          «{app.coverLetter}»
        </div>
      )}
    </td>
  )
}