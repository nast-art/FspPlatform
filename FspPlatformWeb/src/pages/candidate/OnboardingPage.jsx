import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { candidateApi } from '../../api/candidate.api'
import { testsApi } from '../../api/tests.api'
import { useDictionaries } from '../../shared/hooks/useDictionaries'
import Field from '../../shared/components/Field'
import Spinner from '../../shared/components/Spinner'

export default function OnboardingPage() {
  const navigate = useNavigate()
  const { dictionaries, loading } = useDictionaries()

  const [step, setStep] = useState(1)
  // НЕСКОЛЬКО специализаций: кандидат может иметь несколько направлений,
  // каждое со своим грейдом и тестом (первая выбранная становится основной).
  const [specializationIds, setSpecializationIds] = useState([])
  const [gradeId, setGradeId] = useState(null)
  const [workFormat, setWorkFormat] = useState('офис')
  const [error, setError] = useState(null)
  const [loadingSubmit, setLoadingSubmit] = useState(false)

  if (loading || !dictionaries) return <Spinner text="Загружаем справочники…" />

  // На онбординге — только первые четыре грейда (до Сеньор включительно)
  const grades = dictionaries.grades.filter((g) => g.rank <= 4)

  function toggleSpecialization(id) {
    setSpecializationIds((ids) =>
      ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id]
    )
    setError(null)
  }

  function specName(id) {
    return (
      dictionaries.specializations.find((s) => s.id === id)?.name ??
      String(id)
    )
  }

  function handleNext() {
    setError(null)
    if (step === 1 && specializationIds.length === 0) {
      setError('Выберите хотя бы одну специализацию.')
      return
    }
    if (step === 2 && !gradeId) {
      setError('Выберите предполагаемый грейд.')
      return
    }
    if (step === 1) {
      setStep(2)
      return
    }

    // Шаг 2 → сохраняем опрос ДЛЯ КАЖДОЙ выбранной специализации
    // (последовательно: первая в списке станет основной на бэкенде),
    // затем стартуем тест на первую (основную) специализацию.
    setLoadingSubmit(true)
    const chain = specializationIds.reduce(
      (p, specId) =>
        p.then(() =>
          candidateApi
            .startSurvey({ specializationId: specId, declaredGradeId: gradeId })
            .then((sessionId) =>
              candidateApi.submitSurvey({
                sessionId,
                answers: [
                  { questionKey: 'отрасль', answerValue: { value: 'ИТ' } },
                  {
                    questionKey: 'специализация',
                    answerValue: { value: specName(specId) },
                  },
                  {
                    questionKey: 'формат_работы',
                    answerValue: { value: workFormat },
                  },
                ],
              })
            )
        ),
      Promise.resolve()
    )

    chain
      .then(() =>
        testsApi.start({
          specializationId: specializationIds[0],
          targetGradeId: gradeId,
        })
      )
      .then((test) =>
        navigate(`/candidate/test/${test.sessionId}`, { replace: true })
      )
      .catch((e) => {
        setError(e.message)
        setLoadingSubmit(false)
      })
  }

  return (
    <div style={{ maxWidth: 640, margin: '0 auto', padding: '32px 20px 48px' }}>
      <div className="stepper">
        <i className={step >= 1 ? 'done' : ''} />
        <i className={step >= 2 ? 'done' : ''} />
      </div>

      {step === 1 && (
        <>
          <div className="h1">Кем вы являетесь?</div>
          <p className="sub">
            Шаг 1 из 2 · Отрасль: ИТ · можно выбрать несколько специализаций
          </p>

          <Field
            label={`Специализация${specializationIds.length > 0 ? ` · выбрано: ${specializationIds.length}` : ''}`}
          >
            <div className="grid2" style={{ gap: 10 }}>
              {dictionaries.specializations.map((s) => {
                const selected = specializationIds.includes(s.id)
                return (
                  <button
                    key={s.id}
                    type="button"
                    className={`card q-opt ${selected ? 'sel' : ''}`}
                    style={{ marginBottom: 0 }}
                    onClick={() => toggleSpecialization(s.id)}
                  >
                    <b>{s.name}</b>
                  </button>
                )
              })}
            </div>
          </Field>

          <Field label="Формат работы">
            <select
              className="input"
              value={workFormat}
              onChange={(e) => setWorkFormat(e.target.value)}
            >
              <option value="офис">Офис</option>
              <option value="удаленно">Удалённо</option>
              <option value="гибрид">Гибрид</option>
            </select>
          </Field>
        </>
      )}

      {step === 2 && (
        <>
          <div className="h1">Ваш предполагаемый грейд</div>
          <p className="sub">
            Шаг 2 из 2 · Специализации:{' '}
            <b style={{ color: 'var(--white)' }}>
              {specializationIds.map(specName).join(', ')}
            </b>
          </p>
          <p className="hint" style={{ marginBottom: 14 }}>
            Грейд применится ко всем выбранным специализациям и его нужно будет
            подтвердить тестом по каждой. Сейчас начнётся тест по первой
            специализации — остальные можно будет пройти позже в разделе «Моя
            категория». Тест определит фактический уровень.
          </p>
          <div className="grid2" style={{ gap: 10 }}>
            {grades.map((g) => (
              <button
                key={g.id}
                type="button"
                className={`card q-opt ${gradeId === g.id ? 'sel' : ''}`}
                style={{ marginBottom: 0 }}
                onClick={() => {
                  setGradeId(g.id)
                  setError(null)
                }}
              >
                <b>{g.name}</b>
                <div className="hint">
                  {g.minExpYears === 0
                    ? 'без опыта'
                    : `от ${g.minExpYears} ${
                        g.minExpYears === 1 ? 'года' : 'лет'
                      } опыта`}
                </div>
              </button>
            ))}
          </div>
        </>
      )}

      {error && (
        <div className="notice red" style={{ marginTop: 14 }}>
          {error}
        </div>
      )}

      <div style={{ marginTop: 18, display: 'flex', gap: 10 }}>
        {step === 2 && (
          <button className="btn ghost" onClick={() => setStep(1)}>
            Назад
          </button>
        )}
        <button
          className="btn"
          style={{ flex: 1 }}
          onClick={handleNext}
          disabled={loadingSubmit}
        >
          {loadingSubmit
            ? 'Готовим тест…'
            : step === 1
            ? 'Далее: выбор грейда'
            : `Перейти к тесту на «${
                grades.find((g) => g.id === gradeId)?.name ?? ''
              }»`}
        </button>
      </div>
    </div>
  )
}