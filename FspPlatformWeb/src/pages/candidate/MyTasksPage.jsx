import { useCallback, useEffect, useState } from 'react'
import { tasksApi } from '../../api/tasks.api'
import Spinner from '../../shared/components/Spinner'
import Badge from '../../shared/components/Badge'
import { formatDate } from '../../shared/utils/format'

// Статусы назначенного задания (статус_задания в БД)
const TASK_BADGE = {
  назначено: { color: 'blue', text: 'назначено' },
  в_работе: { color: '', text: 'в работе' },
  выполнено: { color: 'green', text: 'выполнено' },
  пропущено: { color: '', text: 'пропущено' },
  срок_истек: { color: 'red', text: 'истёк срок' },
}

// Задание, по которому ещё можно прислать ответ
const OPEN_STATUSES = ['назначено', 'в_работе']

export default function MyTasksPage() {
  const [tasks, setTasks] = useState(null)
  const [error, setError] = useState(null)
  const [busyId, setBusyId] = useState(null)
  // черновики ответов: { [candidateTaskId]: text }
  const [drafts, setDrafts] = useState({})
  // раскрытые просмотры своих решений: { [candidateTaskId]: bool }
  const [openAnswers, setOpenAnswers] = useState({})

  const load = useCallback(() => {
    tasksApi
      .myAssignments() // список заданий КАНДИДАТА: GET /api/my-tasks
      .then(setTasks)
      .catch((e) => setError(e.message))
  }, [])

  useEffect(load, [load])

  async function submit(id) {
    const text = (drafts[id] ?? '').trim()
    if (!text) {
      setError('Напишите решение или подход к решению — пустой ответ не отправить.')
      return
    }
    setBusyId(id)
    setError(null)
    try {
      await tasksApi.answerTask(id, { answerText: text })
      setDrafts((d) => ({ ...d, [id]: '' }))
      load()
    } catch (e) {
      setError(e.message)
    } finally {
      setBusyId(null)
    }
  }

  if (!tasks) return <Spinner />

  const openCount = tasks.filter((t) => OPEN_STATUSES.includes(t.status)).length

  return (
    <div>
      <div className="page-head">
        <div>
          <div className="h1">Задания от работодателей</div>
          <div className="sub">
            Регулярные короткие задачи поддерживают профиль в актуальном состоянии
            и дают работодателю свежий сигнал о вашем уровне. Решите задачу или
            предложите подход к решению.
          </div>
        </div>
        {openCount > 0 && (
          <div className="head-actions">
            <Badge color="blue">{openCount} активных</Badge>
          </div>
        )}
      </div>

      {error && (
        <div className="notice red" style={{ marginBottom: 14 }}>
          {error}
        </div>
      )}

      {tasks.length === 0 ? (
        <div className="card">
          <p className="hint">
            Заданий пока нет. Когда работодатель направит вам короткую задачу,
            она появится здесь — решение или предложенный подход обновят дату
            активности вашего профиля.
          </p>
        </div>
      ) : (
        tasks.map((t) => {
          // «выполнено» до оценки работодателя честнее показывать как «на проверке»:
          // зелёное читается как «принято».
          const sb =
            t.status === 'выполнено' && t.score == null
              ? { color: 'blue', text: 'на проверке' }
              : TASK_BADGE[t.status] ?? { color: '', text: t.status }
          const open = OPEN_STATUSES.includes(t.status)
          return (
            <div key={t.id} className="card" style={{ marginBottom: 12 }}>
              <div className="kv" style={{ alignItems: 'flex-start' }}>
                <div style={{ flex: 1 }}>
                  <b>{t.title}</b>
                  <div className="hint" style={{ marginTop: 4 }}>
                    {t.companyName} · назначено {formatDate(t.assignedAt)}
                    {t.completedAt && ` · выполнено ${formatDate(t.completedAt)}`}
                  </div>
                </div>
                <Badge color={sb.color}>{sb.text}</Badge>
              </div>

              <p style={{ margin: '10px 0', fontSize: 13, lineHeight: 1.55 }}>
                {t.description}
              </p>

              {open && (
                <>
                  <textarea
                    className="input"
                    rows={4}
                    placeholder="Решение или подход к решению: ключевые шаги, компромиссы, что бы вы проверили первым делом…"
                    value={drafts[t.id] ?? ''}
                    onChange={(e) =>
                      setDrafts((d) => ({ ...d, [t.id]: e.target.value }))
                    }
                  />
                  <div className="row" style={{ marginTop: 10, justifyContent: 'flex-end' }}>
                    <button
                      className="btn sm"
                      disabled={busyId === t.id}
                      onClick={() => submit(t.id)}
                    >
                      {busyId === t.id ? 'Отправка…' : 'Отправить решение'}
                    </button>
                  </div>
                </>
              )}

              {!open && t.score != null && (
                <div className="hint" style={{ marginTop: 8 }}>
                  Оценка работодателя: <b>{t.score}</b>
                </div>
              )}

              {!open && t.answerText && (
                <div style={{ marginTop: 8 }}>
                  <button
                    type="button"
                    className="btn ghost sm"
                    onClick={() =>
                      setOpenAnswers((s) => ({ ...s, [t.id]: !s[t.id] }))
                    }
                  >
                    {openAnswers[t.id] ? 'Скрыть моё решение' : 'Показать моё решение'}
                  </button>
                  {openAnswers[t.id] && (
                    <div className="answer-text">{t.answerText}</div>
                  )}
                </div>
              )}
            </div>
          )
        })
      )}
    </div>
  )
}