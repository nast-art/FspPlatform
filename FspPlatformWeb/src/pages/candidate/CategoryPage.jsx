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

export default function CategoryPage() {
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

  // Грейды отсортированы по рангу: Стажёр(1) → Архитектор(6)
  const sortedGrades = dictionaries
    ? [...dictionaries.grades].sort((a, b) => a.rank - b.rank)
    : []

  // Грейд на один уровень НИЖЕ указанного (по рангу). null — если ниже некуда.
  function lowerGradeOf(gradeId) {
    const idx = sortedGrades.findIndex((g) => g.id === gradeId)
    return idx > 0 ? sortedGrades[idx - 1] : null
  }

  // Повторная попытка на минимальном грейде (уровня ниже нет): через 90 дней после провала.
  const SAME_GRADE_RETRY_DAYS = 90
  function retryDateOf(last) {
    if (!last?.finishedAt) return null
    const d = new Date(last.finishedAt)
    d.setDate(d.getDate() + SAME_GRADE_RETRY_DAYS)
    return d
  }

  // Последняя завершённая попытка на ТЕКУЩИЙ грейд этой специализации.
  // history отсортирован по дате desc — первое совпадение и есть последняя.
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

  const confirmed = (specializations ?? []).filter(isConfirmed)
  const passedList = history.filter((h) => h.passed === true)

  return (
    <div style={{ maxWidth: 1400, margin: '0 auto' }}>
      <div className="grid3" style={{ marginBottom: 18 }}>
        <div className="card">
          <div className="stat">
            <span className="v" style={{ color: 'var(--blue)' }}>
              {specializations?.length ?? 0}
            </span>
          </div>
          <div className="l">специализаций выбрано</div>
        </div>
        <div className="card">
          <div className="stat">
            <span className="v" style={{ color: 'var(--green)' }}>
              {confirmed.length}
            </span>
          </div>
          <div className="l">грейдов подтверждено</div>
        </div>
        <div className="card">
          <div className="stat">
            <span className="v">{passedList.length}</span>
          </div>
          <div className="l">тестов пройдено</div>
        </div>
      </div>

      {error && (
        <div className="notice red" style={{ marginBottom: 14 }}>
          {error}
        </div>
      )}

      <div className="card" style={{ marginBottom: 16 }}>
        <div className="mono" style={{ marginBottom: 12 }}>
          МОИ СПЕЦИАЛИЗАЦИИ
        </div>
        {(specializations ?? []).map((spec) => {
          // ТЗ: не прошли тест на уровень N — следующим доступен ТОЛЬКО тест на N−1.
          // Кнопка на текущий грейд скрывается после провала; понижение доступно всегда.
          const lower = lowerGradeOf(spec.currentGradeId)
          const last = lastAttemptOnCurrentGrade(spec)
          const failedHere = !isConfirmed(spec) && last && last.passed === false
          // Провал на минимальном грейде: повтор открывается после кулдауна (90 дней).
          const retryAt = failedHere && !lower ? retryDateOf(last) : null
          const retryOpen = retryAt != null && retryAt <= new Date()
          return (
            <div
              key={spec.specializationId}
              className="kv"
              style={{ alignItems: 'center' }}
            >
              <div>
                <b>{spec.specializationName}</b>
                {spec.isPrimary && <Badge color="blue">основная</Badge>}
                <div className="hint" style={{ marginTop: 4 }}>
                  заявлен «{spec.declaredGradeName}» · фактический «
                  {spec.currentGradeName}»
                  {spec.lastTestAt
                    ? ` · тест: ${formatDate(spec.lastTestAt)}`
                    : ' · тест не пройден'}
                  {!spec.canChangeGrade && spec.nextChangeAvailableAt && (
                    <>
                      {' '}
                      · смена грейда с {formatDate(spec.nextChangeAvailableAt)}
                    </>
                  )}
                </div>
              </div>
              <div className="row" style={{ gap: 8, flexShrink: 0 }}>
                {isConfirmed(spec) ? (
                  <>
                    <Badge color="green">подтверждён</Badge>
                    <button
                      className="btn ghost sm"
                      disabled={startingSpecId === spec.specializationId}
                      onClick={() => startTest(spec, spec.currentGradeId)}
                    >
                      {startingSpecId === spec.specializationId
                        ? 'Готовим…'
                        : 'Пересдать'}
                    </button>
                    {lower && (
                      <button
                        className="btn ghost sm"
                        disabled={startingSpecId === spec.specializationId}
                        onClick={() => startTest(spec, lower.id)}
                        title="Подтвердить грейд ниже текущего"
                      >
                        Тест на «{lower.name}»
                      </button>
                    )}
                  </>
                ) : (
                  <>
                    {failedHere ? (
                      <Badge color="red">не пройден</Badge>
                    ) : (
                      <Badge>ожидает тестирования</Badge>
                    )}
                    {/* После провала кнопка на текущий грейд скрыта, если есть уровень
                        ниже (бэкенд всё равно отклонит, согласованность результата).
                        На минимальном грейде повтор открывается после кулдауна 90 дней —
                        тогда кнопка возвращается. */}
                    {!failedHere && (
                      <button
                        className="btn sm"
                        disabled={startingSpecId === spec.specializationId}
                        onClick={() => startTest(spec, spec.currentGradeId)}
                      >
                        {startingSpecId === spec.specializationId
                          ? 'Готовим…'
                          : `Пройти тест на «${spec.currentGradeName}»`}
                      </button>
                    )}
                    {failedHere && !lower && !retryOpen && retryAt && (
                      <Badge>повтор с {formatDate(retryAt)}</Badge>
                    )}
                    {failedHere && !lower && retryOpen && (
                      <button
                        className="btn sm"
                        disabled={startingSpecId === spec.specializationId}
                        onClick={() => startTest(spec, spec.currentGradeId)}
                      >
                        {startingSpecId === spec.specializationId
                          ? 'Готовим…'
                          : `Повторить тест на «${spec.currentGradeName}»`}
                      </button>
                    )}
                    {lower && (
                      <button
                        className={failedHere ? 'btn sm' : 'btn ghost sm'}
                        disabled={startingSpecId === spec.specializationId}
                        onClick={() => startTest(spec, lower.id)}
                        title="ТЗ: не прошли заявленный уровень — следующим доступен тест на уровень ниже"
                      >
                        {failedHere
                          ? `Не пройден — пройти на «${lower.name}»`
                          : `на «${lower.name}»`}
                      </button>
                    )}
                  </>
                )}
              </div>
            </div>
          )
        })}
        <div className="notice" style={{ marginTop: 14 }}>
          Провал на уровне N — следующим доступен тест на N−1. Повышение — не
          чаще раза в 3 месяца. На минимальном грейде повтор откроется через
          90 дней после провала — дата указана в списке выше.
        </div>
      </div>

      <div className="grid2">
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
                        <Badge color="blue">на проверке</Badge>
                      ) : h.passed ? (
                        <Badge color="green">подтверждён</Badge>
                      ) : (
                        <Badge color="red">не пройден</Badge>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        <div className="card">
          <div className="mono" style={{ marginBottom: 10 }}>
            ДОСТИЖЕНИЯ ФСП
          </div>
          <p className="hint">
            Достижения участника ФСП повышают позицию во всех ваших категориях.
            Привяжите ID участника на странице настроек — или продолжайте без
            истории ФСП: профиль работает полностью.
          </p>
          <div style={{ marginTop: 12 }}>
            <button
              className="btn ghost sm"
              onClick={() => navigate('/candidate/settings')}
            >
              Настройки: связь с ФСП
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}