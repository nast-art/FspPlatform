import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { candidateApi } from '../../api/candidate.api'
import { testsApi } from '../../api/tests.api'
import { useDictionaries } from '../../shared/hooks/useDictionaries'
import Spinner from '../../shared/components/Spinner'
import Badge from '../../shared/components/Badge'
import { formatDate } from '../../shared/utils/format'

function isConfirmed(spec) {
  return spec.status === 'Грейд подтверждён' || spec.status === 'грейд_подтвержден'
}

export default function TestPage() {
  const navigate = useNavigate()
  const { dictionaries, loading: dictLoading } = useDictionaries()
  const [specializations, setSpecializations] = useState(null)
  const [history, setHistory] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [startingSpecId, setStartingSpecId] = useState(null)

  const load = useCallback(() => {
    Promise.all([candidateApi.getSpecializations(), testsApi.history()])
      .then(([specs, h]) => {
        setSpecializations(specs)
        setHistory(h)
      })
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false))
  }, [])

  useEffect(load, [load])

  // Грейды по рангу: Стажёр(1) → Архитектор(6)
  const sortedGrades = dictionaries
    ? [...dictionaries.grades].sort((a, b) => a.rank - b.rank)
    : []

  // Грейд на один уровень ниже. null — если ниже некуда.
  function lowerGradeOf(gradeId) {
    const idx = sortedGrades.findIndex((g) => g.id === gradeId)
    return idx > 0 ? sortedGrades[idx - 1] : null
  }

  // Последняя завершённая попытка на текущий грейд специализации
  // (history отсортирован по дате desc — первое совпадение и есть последняя).
  function lastAttemptOnCurrentGrade(spec) {
    const rows = history.filter(
      (h) =>
        h.specializationName === spec.specializationName &&
        h.targetGradeName === spec.currentGradeName &&
        h.status === 'завершен'
    )
    return rows.length > 0 ? rows[0] : null
  }

  async function startTest(spec, gradeId) {
    setStartingSpecId(spec.specializationId)
    setError(null)
    try {
      const test = await testsApi.start({
        specializationId: spec.specializationId,
        targetGradeId: gradeId,
      })
      navigate(`/candidate/test/${test.sessionId}`)
    } catch (e) {
      setError(e.message)
      setStartingSpecId(null)
    }
  }

  if (loading || dictLoading) return <Spinner />
  if (error && !specializations) return <div className="notice red">{error}</div>

  const unconfirmed = (specializations ?? []).filter((s) => !isConfirmed(s))
  const confirmed = (specializations ?? []).filter(isConfirmed)

  return (
    <div>
      <div className="h1">Тестирование</div>
      <div className="sub">
        Грейд подтверждается тестом: система собирает уникальный набор заданий
        из банка. У каждого кандидата — свои вопросы сопоставимой сложности.
      </div>

      {error && (
        <div className="notice red" style={{ marginBottom: 14 }}>
          {error}
        </div>
      )}

      <div className="card" style={{ marginBottom: 16 }}>
        <div className="mono" style={{ marginBottom: 12 }}>
          ДОСТУПНЫЕ ТЕСТЫ
        </div>
        {unconfirmed.length === 0 ? (
          <p className="hint">
            Все специализации подтверждены. Можно пересдать тест для повышения
            грейда — не чаще раза в 3 месяца.
          </p>
        ) : (
          unconfirmed.map((spec) => {
            // ТЗ: не пройден тест на уровень N — следующим доступен ТОЛЬКО тест на N−1.
            // Кнопка на текущий грейд после провала скрыта (бэкенд отклонил бы запрос).
            const lower = lowerGradeOf(spec.currentGradeId)
            const last = lastAttemptOnCurrentGrade(spec)
            const failedHere = last && last.passed === false
            return (
              <div
                key={spec.specializationId}
                className="kv"
                style={{ alignItems: 'center' }}
              >
                <div>
                  <b>{spec.specializationName}</b>
                  <div className="hint" style={{ marginTop: 4 }}>
                    заявленный грейд «{spec.declaredGradeName}» · тест на «
                    {spec.currentGradeName}»
                    {failedHere &&
                      ` · последний результат: ${last.percent}% — не пройден`}
                  </div>
                </div>
                <div className="row" style={{ gap: 8, flexShrink: 0 }}>
                  {!failedHere && (
                    <button
                      className="btn sm"
                      disabled={startingSpecId === spec.specializationId}
                      onClick={() => startTest(spec, spec.currentGradeId)}
                    >
                      {startingSpecId === spec.specializationId
                        ? 'Готовим…'
                        : `Начать тест на «${spec.currentGradeName}»`}
                    </button>
                  )}
                  {lower && (
                    <button
                      className={failedHere ? 'btn sm' : 'btn ghost sm'}
                      disabled={startingSpecId === spec.specializationId}
                      onClick={() => startTest(spec, lower.id)}
                      title={
                        failedHere
                          ? 'ТЗ: не пройденный тест — следующим доступен уровень ниже'
                          : 'Пройти тест на уровень ниже заявленного'
                      }
                    >
                      {failedHere
                        ? `Не пройден — пройти на «${lower.name}»`
                        : `на «${lower.name}»`}
                    </button>
                  )}
                </div>
              </div>
            )
          })
        )}
      </div>

      <div className="grid2">
        <div className="card">
          <div className="mono" style={{ marginBottom: 12 }}>
            ПОДТВЕРЖДЁННЫЕ ГРЕЙДЫ
          </div>
          {confirmed.length === 0 ? (
            <p className="hint">Подтверждённых грейдов пока нет.</p>
          ) : (
            confirmed.map((spec) => (
              <div
                key={spec.specializationId}
                className="kv"
                style={{ alignItems: 'center' }}
              >
                <div>
                  <b>{spec.specializationName}</b>
                  <Badge color="green">«{spec.currentGradeName}»</Badge>
                  {!spec.canChangeGrade && spec.nextChangeAvailableAt && (
                    <div className="hint" style={{ marginTop: 4 }}>
                      повышение доступно с {formatDate(spec.nextChangeAvailableAt)}
                    </div>
                  )}
                </div>
                <button
                  className="btn ghost sm"
                  disabled={
                    startingSpecId === spec.specializationId ||
                    !spec.canChangeGrade
                  }
                  onClick={() => startTest(spec, spec.currentGradeId)}
                >
                  {startingSpecId === spec.specializationId
                    ? 'Готовим…'
                    : 'Пересдать'}
                </button>
              </div>
            ))
          )}
          {/* Исправлено: автоповышения нет (как в документации 3.4 и Q&A) —
              повышение — только отдельным тестом */}
          <div className="notice" style={{ marginTop: 14 }}>
            Не пройденный тест не понижает грейд — следующим доступен тест на
            уровень ниже (повторная попытка на том же уровне открывается после
            подтверждения грейда ниже — это сохраняет стабильность результата).
            Повышение — только отдельным тестом, не чаще раза в 3 месяца.
          </div>
        </div>

        <div className="card">
          <div className="mono" style={{ marginBottom: 10 }}>
            ИСТОРИЯ ТЕСТИРОВАНИЯ
          </div>
          {history.length === 0 ? (
            <p className="hint">Тесты ещё не пройдены.</p>
          ) : (
            <table className="tbl">
              <thead>
                <tr>
                  <th>Дата</th>
                  <th>Специализация</th>
                  <th>Грейд</th>
                  <th>Результат</th>
                  <th>Итог</th>
                </tr>
              </thead>
              <tbody>
                {history.map((h) => (
                  <tr key={h.sessionId}>
                    <td>{formatDate(h.finishedAt)}</td>
                    <td>{h.specializationName}</td>
                    <td>{h.targetGradeName}</td>
                    <td className="score">
                      {h.percent != null ? `${h.percent}%` : '—'}
                    </td>
                    <td>
                      {h.passed == null ? (
                        <Badge>на проверке</Badge>
                      ) : h.passed ? (
                        <Badge color="green">подтверждён</Badge>
                      ) : (
                        <Badge>не пройден</Badge>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  )
}