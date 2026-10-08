import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { matchingApi } from '../../api/matching.api'
import { useDictionaries } from '../../shared/hooks/useDictionaries'
import Spinner from '../../shared/components/Spinner'

// Ключ sessionStorage: критерии потребности, привязанные к requestId подборки.
// Нужен, чтобы после перезагрузки страницы подборки критерии не терялись.
const demandKey = (requestId) => `demand:${requestId}`

export default function DemandPage() {
  const navigate = useNavigate()
  const { dictionaries, loading } = useDictionaries()

  const [teamActivity, setTeamActivity] = useState('')
  const [specializationId, setSpecializationId] = useState('')
  const [gradeFromId, setGradeFromId] = useState('')
  const [gradeToId, setGradeToId] = useState('')
  const [selectedSkills, setSelectedSkills] = useState([])
  const [city, setCity] = useState('')
  const [salaryFrom, setSalaryFrom] = useState('')
  const [salaryTo, setSalaryTo] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState(null)

  function toggleSkill(id) {
    setSelectedSkills((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]
    )
  }

  async function submit(e) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    try {
      const res = await matchingApi.searchCandidates({
        specializationId: specializationId ? Number(specializationId) : undefined,
        gradeId: gradeFromId ? Number(gradeFromId) : undefined,
        skillIds: selectedSkills.length ? selectedSkills : undefined,
        city: city || undefined,
        page: 1,
        pageSize: 10,
      })

      // Критерии с DISPLAY-именами сохраняем рядом с подборкой — страница
      // результатов покажет их чипами и предзаполнит фильтры.
      const criteria = {
        teamActivity: teamActivity || null,
        specializationId: specializationId ? Number(specializationId) : null,
        specializationName:
          dictionaries?.specializations.find(
            (s) => String(s.id) === String(specializationId)
          )?.name ?? null,
        gradeFromId: gradeFromId ? Number(gradeFromId) : null,
        gradeFromName:
          dictionaries?.grades.find((g) => String(g.id) === String(gradeFromId))
            ?.name ?? null,
        gradeToId: gradeToId ? Number(gradeToId) : null,
        gradeToName:
          dictionaries?.grades.find((g) => String(g.id) === String(gradeToId))
            ?.name ?? null,
        skillIds: selectedSkills,
        skillNames: (dictionaries?.skills ?? [])
          .filter((s) => selectedSkills.includes(s.id))
          .map((s) => s.name),
        city: city || null,
        salaryFrom: salaryFrom ? Number(salaryFrom) : null,
        salaryTo: salaryTo ? Number(salaryTo) : null,
      }
      sessionStorage.setItem(demandKey(res.requestId), JSON.stringify(criteria))

      navigate(`/employer/shortlist?requestId=${res.requestId}`)
    } catch (e2) {
      setError(e2.message)
    } finally {
      setBusy(false)
    }
  }

  if (loading || !dictionaries) return <Spinner />

  return (
    <div>
      <div className="page-head">
        <div>
          <div className="h1">Кого ищем</div>
          <div className="sub">
            На основе ответов сформируется подборка из подтверждённых кандидатов.
            Выдача строится на категориях (опрос + тест), а не на самоописании резюме.
            Критерии будут показаны на странице подборки — их можно уточнить без потери результата.
          </div>
        </div>
      </div>

      <form onSubmit={submit}>
        <div className="grid2">
          <div className="card">
            <label className="mono" style={{ display: 'block', marginBottom: 8 }}>
              ЧЕМ ЗАНИМАЕТСЯ КОМАНДА
            </label>
            <textarea
              className="input"
              rows={3}
              value={teamActivity}
              onChange={(e) => setTeamActivity(e.target.value)}
              placeholder="Платёжные сервисы: высоконагруженный API, интеграции с банками."
            />
          </div>

          <div className="card">
            <label className="mono" style={{ display: 'block', marginBottom: 8 }}>
              ТРЕБУЕМЫЙ СТЕК
            </label>
            <div>
              {dictionaries.skills.map((s) => (
                <span
                  key={s.id}
                  className={`tag ${selectedSkills.includes(s.id) ? 'sel' : ''}`}
                  style={{ cursor: 'pointer' }}
                  onClick={() => toggleSkill(s.id)}
                >
                  {s.name}
                </span>
              ))}
            </div>
          </div>

          <div className="card">
            <label className="mono" style={{ display: 'block', marginBottom: 8 }}>
              СПЕЦИАЛИЗАЦИЯ
            </label>
            <select
              className="input"
              value={specializationId}
              onChange={(e) => setSpecializationId(e.target.value)}
            >
              <option value="">— выберите —</option>
              {dictionaries.specializations.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name}
                </option>
              ))}
            </select>

            <label className="mono" style={{ display: 'block', margin: '14px 0 8px' }}>
              ГРЕЙДОВЫЙ ДИАПАЗОН
            </label>
            <div className="row" style={{ gap: 8 }}>
              <select
                className="input"
                style={{ flex: 1 }}
                value={gradeFromId}
                onChange={(e) => setGradeFromId(e.target.value)}
              >
                <option value="">от —</option>
                {dictionaries.grades.map((g) => (
                  <option key={g.id} value={g.id}>
                    от {g.name}
                  </option>
                ))}
              </select>
              <select
                className="input"
                style={{ flex: 1 }}
                value={gradeToId}
                onChange={(e) => setGradeToId(e.target.value)}
              >
                <option value="">до —</option>
                {dictionaries.grades.map((g) => (
                  <option key={g.id} value={g.id}>
                    до {g.name}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="card">
            <label className="mono" style={{ display: 'block', marginBottom: 8 }}>
              ГОРОД
            </label>
            <input
              className="input"
              value={city}
              onChange={(e) => setCity(e.target.value)}
              placeholder="Москва"
            />

            <label className="mono" style={{ display: 'block', margin: '14px 0 8px' }}>
              ЗАРПЛАТНАЯ ВИЛКА, ₽
            </label>
            <div className="row" style={{ gap: 8 }}>
              <input
                className="input"
                style={{ flex: 1 }}
                placeholder="от"
                value={salaryFrom}
                onChange={(e) => setSalaryFrom(e.target.value)}
              />
              <input
                className="input"
                style={{ flex: 1 }}
                placeholder="до"
                value={salaryTo}
                onChange={(e) => setSalaryTo(e.target.value)}
              />
            </div>
            <p className="hint" style={{ marginTop: 8 }}>
              Вилка обязательна в вакансии и приглашении — кандидат видит условия до
              общения (ТЗ 2.2). В поиске по банку зарплата не фильтрует: она
              фиксируется в критериях и показывается на странице подборки.
            </p>

            {error && (
              <div className="notice red" style={{ marginTop: 12 }}>
                {error}
              </div>
            )}

            <button
              className="btn"
              style={{ width: '100%', marginTop: 14 }}
              disabled={busy}
              type="submit"
            >
              {busy ? 'Формируем подборку…' : 'Сформировать подборку'}
            </button>
          </div>
        </div>
      </form>
    </div>
  )
}