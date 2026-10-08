import { useCallback, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { matchingApi } from '../../api/matching.api'
import Spinner from '../../shared/components/Spinner'
import Badge from '../../shared/components/Badge'
import { formatDate } from '../../shared/utils/format'

export default function CandidateCardPage() {
  const { candidateId } = useParams()
  const navigate = useNavigate()

  const [card, setCard] = useState(null)
  const [contacts, setContacts] = useState(null)
  const [contactsError, setContactsError] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const load = useCallback(() => {
    matchingApi
      .candidateCard(candidateId)
      .then(setCard)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false))
  }, [candidateId])

  useEffect(load, [load])

  async function openContacts() {
    setContactsError(null)
    try {
      setContacts(await matchingApi.candidateContacts(candidateId))
    } catch (e) {
      setContactsError(e.message)
    }
  }

  if (loading) return <Spinner />
  if (error)
    return (
      <div>
        <div className="notice red">{error}</div>
        <button className="btn ghost" style={{ marginTop: 14 }} onClick={() => navigate(-1)}>
          Назад
        </button>
      </div>
    )

  return (
    <div>
      <div className="card" style={{ marginBottom: 14 }}>
        <div className="row" style={{ justifyContent: 'space-between' }}>
          <div className="row">
            <div className="avatar">
              {(card.lastName?.[0] ?? '') + (card.firstName?.[0] ?? '')}
            </div>
            <div>
              <div className="h1" style={{ fontSize: 18 }}>
                {card.lastName} {card.firstName} {card.middleName ?? ''}
              </div>
              <div className="sub" style={{ margin: 0 }}>
                {card.categoryName ?? 'категория не подтверждена'}
                {card.city ? ` · г. ${card.city}` : ''}
              </div>
              {/* Q&A менторов: неподтверждённый грейд не скрываем — показываем статусом */}
              {card.confirmed === false && (
                <div style={{ marginTop: 6 }}>
                  <Badge color="blue">грейд не подтверждён тестом</Badge>
                </div>
              )}
            </div>
          </div>
          <div className="row">
            <button className="btn" onClick={() => navigate(`/employer/invite/${candidateId}`)}>
              Пригласить
            </button>
          </div>
        </div>
      </div>

      <div className="grid3">
        <div className="card">
          <div className="mono" style={{ marginBottom: 8 }}>ПОДТВЕРЖДЁННЫЙ ПРОФИЛЬ</div>
          <div className="kv">
            <span className="k">Тест на грейд</span>
            <span className="score">
              {card.bestAttemptPercent != null
                ? `${card.bestAttemptPercent}% · ${card.confirmed ? 'пройден' : 'не пройден'}`
                : '—'}
            </span>
          </div>
          <div className="kv">
            <span className="k">Опыт</span>
            <span>{card.experienceYears} лет</span>
          </div>
          <div className="kv">
            <span className="k">Рейтинг профиля</span>
            {/* Рейтинг считается внутри категории: пока грейд не подтверждён,
                записи о рейтинге нет — показываем «—», а не вводящий ноль. */}
            <span className="score">
              {card.rankScore > 0 ? Math.round(card.rankScore) : '—'}
            </span>
          </div>
          <div className="kv">
            <span className="k">История ФСП</span>
            {card.hasFspHistory ? <Badge color="green">есть</Badge> : <Badge>нет</Badge>}
          </div>
          <p className="hint" style={{ marginTop: 10 }}>
            Рейтинг (0–100) = 0.64 × результат теста + до 27 баллов за достижения ФСП
            + до 9 за полноту профиля. Чем выше — тем выше позиция в подборке.
          </p>
        </div>

        <div className="card">
          <div className="mono" style={{ marginBottom: 8 }}>СТЕК И НАВЫКИ</div>
          <div>
            {(card.skills ?? []).map((s) => (
              <span key={s} className="tag">
                {s}
              </span>
            ))}
            {(card.skills ?? []).length === 0 && <span className="hint">не указан</span>}
          </div>
          {(card.softSkills ?? []).length > 0 && (
            <>
              <div className="mono" style={{ margin: '10px 0 6px' }}>СОФТ-СКИЛЛЫ</div>
              <div>
                {card.softSkills.map((s) => (
                  <span key={s} className="tag">
                    {s}
                  </span>
                ))}
              </div>
            </>
          )}
        </div>

        <div className="card">
          <div className="mono" style={{ marginBottom: 8 }}>ДОСТИЖЕНИЯ ФСП</div>
          {(card.fspAchievements ?? []).length === 0 ? (
            <p className="hint">
              Истории ФСП нет — профиль работает полностью, достижения просто не
              влияют на позицию.
            </p>
          ) : (
            card.fspAchievements.map((a) => (
              <div key={a.id} className="kv">
                <span>
                  {a.eventName}
                  <div className="hint">{formatDate(a.eventDate)}</div>
                </span>
                <span className="score">
                  {a.resultPlace ? `${a.resultPlace} место` : (a.role ?? 'участник')}
                </span>
              </div>
            ))
          )}
        </div>
      </div>

      {card.contactsVisible ? (
        <div className="card" style={{ marginTop: 14 }}>
          <div className="mono" style={{ marginBottom: 8 }}>КОНТАКТЫ</div>
          {contacts ? (
            // Отдельная строка на каждый канал связи — читается лучше,
            // чем склейка "email · телефон · telegram" одной строкой.
            // Каждый канал кликабелен: mailto / tel / t.me.
            <div>
              <div className="kv">
                <span className="k">Email</span>
                <span>
                  {contacts.email ? (
                    <a href={`mailto:${contacts.email}`}>{contacts.email}</a>
                  ) : (
                    '—'
                  )}
                </span>
              </div>
              <div className="kv">
                <span className="k">Телефон</span>
                <span>
                  {contacts.phone ? (
                    <a href={`tel:${contacts.phone.replace(/[\s()\-]/g, '')}`}>
                      {contacts.phone}
                    </a>
                  ) : (
                    '—'
                  )}
                </span>
              </div>
              <div className="kv">
                <span className="k">Telegram</span>
                <span>
                  {contacts.telegram ? (
                    <a
                      href={`https://t.me/${contacts.telegram.replace(/^@/, '')}`}
                      target="_blank"
                      rel="noreferrer"
                    >
                      {contacts.telegram}
                    </a>
                  ) : (
                    '—'
                  )}
                </span>
              </div>
            </div>
          ) : (
            <div className="row">
              <button className="btn sm" onClick={openContacts}>
                Открыть контакты
              </button>
              {contactsError && <span className="hint">{contactsError}</span>}
            </div>
          )}
        </div>
      ) : (
        <div className="notice red" style={{ marginTop: 14 }}>
          Контакты скрыты до принятия приглашения или отклика кандидата. Передача
          условий — только через приглашение с вилкой зарплаты.
        </div>
      )}
    </div>
  )
}