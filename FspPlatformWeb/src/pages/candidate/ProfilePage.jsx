import { useEffect, useState } from 'react'
import { candidateApi } from '../../api/candidate.api'
import { useDictionaries } from '../../shared/hooks/useDictionaries'
import Field from '../../shared/components/Field'
import Spinner from '../../shared/components/Spinner'
import Badge from '../../shared/components/Badge'
import Toggle from '../../shared/components/Toggle'

const EMPTY = {
  lastName: '',
  firstName: '',
  middleName: '',
  phone: '',
  telegram: '',
  city: '',
  about: '',
  experienceYears: 0,
  softSkills: [],
  skills: [],
  roleIds: [],
  isProfilePublic: true,
}

// Сводка распознанного из PDF — для уведомлений и диалога выбора.
function resumeSummaryParts(res) {
  const parts = []
  if (res.recognizedLastName || res.recognizedFirstName)
    parts.push(`ФИО: ${[res.recognizedLastName, res.recognizedFirstName, res.recognizedMiddleName].filter(Boolean).join(' ')}`)
  if (res.recognizedPhone) parts.push(`телефон: ${res.recognizedPhone}`)
  if (res.recognizedTelegram) parts.push(`telegram: ${res.recognizedTelegram}`)
  if (res.recognizedCity) parts.push(`город: ${res.recognizedCity}`)
  if (res.recognizedAbout)
    parts.push(`о себе: ${res.recognizedAbout.slice(0, 60)}${res.recognizedAbout.length > 60 ? '…' : ''}`)
  if (res.recognizedExperienceYears) parts.push(`стаж: ${res.recognizedExperienceYears} лет`)
  if ((res.recognizedSoftSkills ?? []).length > 0)
    parts.push(`софт-скиллы: ${res.recognizedSoftSkills.join(', ')}`)
  if ((res.recognizedSkills ?? []).length > 0) parts.push(`стек: ${res.recognizedSkills.join(', ')}`)
  // email распознаётся, но в форме его нет (email = логин) — показываем для сверки
  if (res.recognizedEmail) parts.push(`email: ${res.recognizedEmail}`)
  return parts
}

export default function ProfilePage() {
  const { dictionaries, loading: dictLoading } = useDictionaries()
  const [form, setForm] = useState(EMPTY)
  const [softInput, setSoftInput] = useState('')
  const [skillDraft, setSkillDraft] = useState({ skillId: 0, proficiency: 3 })
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [notice, setNotice] = useState(null)
  const [error, setError] = useState(null)

  // PDF-резюме (ТЗ 2.1)
  const [resumeFile, setResumeFile] = useState(null)
  const [resumeUploading, setResumeUploading] = useState(false)
  const [pdfDownloading, setPdfDownloading] = useState(false)
  // Черновик распознавания, когда распознанные данные конфликтуют с заполненной
  // формой: ждём выбора кандидата — «дополнить пустые» или «перезаписать».
  const [resumeDraft, setResumeDraft] = useState(null)

  useEffect(() => {
    candidateApi
      .getProfile()
      .then((p) => {
        if (p) {
          setForm({ ...EMPTY, ...p })
          setSoftInput((p.softSkills ?? []).join(', '))
        }
      })
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false))
  }, [])

  function setField(name, value) {
    setForm((f) => ({ ...f, [name]: value }))
  }

  function addSkill() {
    const skillId = Number(skillDraft.skillId)
    if (!skillId) return
    setForm((f) => ({
      ...f,
      skills: [
        ...f.skills.filter((s) => s.skillId !== skillId),
        { skillId, proficiency: Number(skillDraft.proficiency) },
      ],
    }))
  }

  function removeSkill(skillId) {
    setForm((f) => ({
      ...f,
      skills: f.skills.filter((s) => s.skillId !== skillId),
    }))
  }

  async function handleSave(e) {
    e.preventDefault()
    setSaving(true)
    setNotice(null)
    setError(null)
    try {
      const dto = {
        ...form,
        experienceYears: Number(form.experienceYears) || 0,
        softSkills: softInput
          .split(',')
          .map((s) => s.trim())
          .filter(Boolean),
      }
      await candidateApi.upsertProfile(dto)
      setNotice('Профиль сохранён.')
    } catch (e2) {
      setError(e2.message)
    } finally {
      setSaving(false)
    }
  }

  // Загрузка PDF-резюме. Подстановка — осознанное действие:
  // конфликтов нет → заполняем пустые поля сразу; конфликты есть → показываем
  // выбор «дополнить пустые / перезаписать распознанным» и ничего не затираем молча.
  async function handleResumeUpload() {
    if (!resumeFile) return
    setResumeUploading(true)
    setNotice(null)
    setError(null)
    try {
      const res = await candidateApi.uploadResume(resumeFile)
      const parts = resumeSummaryParts(res)

      if (parts.length === 0) {
        setNotice('PDF сохранён. Данные не распознались (возможно, скан) — заполните форму вручную.')
        setResumeFile(null)
        return
      }

      if (resumeHasConflicts(res)) {
        setResumeDraft({ res, parts })
        setResumeFile(null)
        return
      }

      applyResume(res, 'fill-empty')
      setNotice(`PDF сохранён. Распознано и подставлено — ${parts.join('; ')}. Проверьте и нажмите «Сохранить профиль».`)
      setResumeFile(null)
    } catch (e2) {
      setError(e2.message)
    } finally {
      setResumeUploading(false)
    }
  }

  // Есть ли конфликт: распознанное значение отличается от уже заполненного поля
  // (либо стек/софт-скиллы уже непустые, а в PDF распознаны свои).
  function resumeHasConflicts(res) {
    const f = form
    const differs = (cur, rec) =>
      rec != null &&
      cur != null &&
      String(cur).trim() !== '' &&
      String(cur).trim() !== String(rec).trim()
    if (differs(f.lastName, res.recognizedLastName)) return true
    if (differs(f.firstName, res.recognizedFirstName)) return true
    if (differs(f.middleName, res.recognizedMiddleName)) return true
    if (differs(f.phone, res.recognizedPhone)) return true
    if (differs(f.telegram, res.recognizedTelegram)) return true
    if (differs(f.city, res.recognizedCity)) return true
    if (differs(f.about, res.recognizedAbout)) return true
    if (
      res.recognizedExperienceYears != null &&
      Number(f.experienceYears) > 0 &&
      Number(f.experienceYears) !== Number(res.recognizedExperienceYears)
    )
      return true
    if ((res.recognizedSoftSkills ?? []).length > 0 && softInput.trim() !== '')
      return true
    if ((res.recognizedSkills ?? []).length > 0 && (f.skills ?? []).length > 0)
      return true
    return false
  }

  // Применение результата распознавания.
  // mode 'fill-empty'   — только пустые поля; стек дополняется недостающими (уровень 3).
  // mode 'overwrite'    — всё распознанное перезаписывает поля; список стека заменяется целиком.
  function applyResume(res, mode) {
    const overwrite = mode === 'overwrite'
    setForm((f) => {
      const next = { ...f }
      const take = (cur, rec) =>
        rec != null && (overwrite || cur == null || String(cur).trim() === '')
      if (take(next.lastName, res.recognizedLastName)) next.lastName = res.recognizedLastName
      if (take(next.firstName, res.recognizedFirstName)) next.firstName = res.recognizedFirstName
      if (take(next.middleName, res.recognizedMiddleName)) next.middleName = res.recognizedMiddleName
      if (take(next.phone, res.recognizedPhone)) next.phone = res.recognizedPhone
      if (take(next.telegram, res.recognizedTelegram)) next.telegram = res.recognizedTelegram
      if (take(next.city, res.recognizedCity)) next.city = res.recognizedCity
      if (take(next.about, res.recognizedAbout)) next.about = res.recognizedAbout
      if (
        res.recognizedExperienceYears != null &&
        (overwrite || !next.experienceYears || Number(next.experienceYears) === 0)
      )
        next.experienceYears = res.recognizedExperienceYears

      // Стек: названия → id из справочника
      const mapped = (res.recognizedSkills ?? [])
        .map((n) => dictionaries.skills.find((s) => s.name === n))
        .filter(Boolean)
      if (overwrite) {
        if (mapped.length > 0)
          next.skills = mapped.map((s) => ({ skillId: s.id, proficiency: 3 }))
      } else {
        const existing = new Set(next.skills.map((s) => s.skillId))
        next.skills = [
          ...next.skills,
          ...mapped
            .filter((s) => !existing.has(s.id))
            .map((s) => ({ skillId: s.id, proficiency: 3 })),
        ]
      }
      return next
    })

    // Софт-скиллы: строка через запятую
    if ((res.recognizedSoftSkills ?? []).length > 0) {
      setSoftInput((cur) =>
        overwrite || cur.trim() === '' ? res.recognizedSoftSkills.join(', ') : cur
      )
    }
  }

  // Выбор в диалоге: применяем выбранный режим и закрываем черновик.
  function applyResumeDraft(mode) {
    if (!resumeDraft) return
    applyResume(resumeDraft.res, mode)
    const verb = mode === 'overwrite' ? 'подставлено (с перезаписью)' : 'подставлено'
    setNotice(`PDF сохранён. Распознано и ${verb} — ${resumeDraft.parts.join('; ')}. Проверьте и нажмите «Сохранить профиль».`)
    setResumeDraft(null)
  }

  function cancelResumeDraft() {
    setResumeDraft(null)
    setNotice('PDF сохранён. Распознанные данные не подставлены.')
  }

  async function handlePdfDownload() {
    setPdfDownloading(true)
    setError(null)
    try {
      await candidateApi.downloadProfilePdf()
    } catch (e2) {
      setError(e2.message)
    } finally {
      setPdfDownloading(false)
    }
  }

  if (loading || dictLoading || !dictionaries) return <Spinner />

  const skillName = (id) =>
    dictionaries.skills.find((s) => s.id === id)?.name ?? `#${id}`

  return (
    <div>
      {/* Шапка страницы: компактная, отступы управляются .page-head */}
      <div className="page-head">
        <div>
          <div className="h1">Профиль кандидата</div>
          <div className="sub">
            Контакты не видны работодателям до принятия приглашения или отклика
          </div>
        </div>
        <div className="head-actions">
          <Badge color="green">PDF-профиль формируется из этих данных</Badge>
        </div>
      </div>

      {notice && (
        <div className="notice" style={{ marginBottom: 14 }}>{notice}</div>
      )}
      {error && (
        <div className="notice red" style={{ marginBottom: 14 }}>{error}</div>
      )}

      {/* Диалог выбора при конфликте распознанных данных с заполненной формой */}
      {resumeDraft && (
        <div className="notice" style={{ marginBottom: 14 }}>
          <div style={{ marginBottom: 10 }}>
            PDF сохранён. Распознано: {resumeDraft.parts.join('; ')}.
            Часть полей уже заполнена — что сделать с распознанными данными?
          </div>
          <div className="row" style={{ gap: 8 }}>
            <button
              type="button"
              className="btn sm"
              onClick={() => applyResumeDraft('fill-empty')}
            >
              Дополнить пустые
            </button>
            <button
              type="button"
              className="btn ghost sm"
              onClick={() => applyResumeDraft('overwrite')}
            >
              Перезаписать распознанным
            </button>
            <button
              type="button"
              className="btn ghost sm"
              onClick={cancelResumeDraft}
            >
              Отмена
            </button>
          </div>
        </div>
      )}

      {/* Резюме (PDF) — ТЗ 2.1 */}
      <div className="card" style={{ marginBottom: 18 }}>
        <div className="resume-grid">
          <div className="resume-info">
            <div className="h2">Резюме (PDF)</div>
            <div className="hint" style={{ marginTop: 4 }}>
              Загрузите PDF — система сохранит его и подставит контакты, ФИО,
              город, «о себе», стаж, софт-скиллы и стек в форму. Пустые поля
              заполняются сразу; при конфликте с уже заполненными — предложит
              выбор «дополнить / перезаписать».
            </div>
          </div>
          <div className="resume-actions">
            <label className="file-pick">
              <input
                type="file"
                accept="application/pdf,.pdf"
                onChange={(e) => setResumeFile(e.target.files?.[0] ?? null)}
              />
              <span className="file-btn">Выбрать файл</span>
              <span className="file-name">
                {resumeFile ? resumeFile.name : 'файл не выбран'}
              </span>
            </label>
            <button
              type="button"
              className="btn ghost"
              disabled={!resumeFile || resumeUploading}
              onClick={handleResumeUpload}
            >
              {resumeUploading ? 'Загрузка…' : 'Загрузить PDF'}
            </button>
            <button
              type="button"
              className="btn"
              disabled={pdfDownloading}
              onClick={handlePdfDownload}
            >
              {pdfDownloading ? 'Формирование…' : 'Скачать PDF-профиль'}
            </button>
          </div>
        </div>
      </div>

      <form onSubmit={handleSave}>
        <div className="grid2">
          <div className="card">
            <Field label="Фамилия">
              <input
                className="input"
                value={form.lastName ?? ''}
                onChange={(e) => setField('lastName', e.target.value)}
                required
              />
            </Field>
            <Field label="Имя">
              <input
                className="input"
                value={form.firstName ?? ''}
                onChange={(e) => setField('firstName', e.target.value)}
                required
              />
            </Field>
            <Field label="Отчество">
              <input
                className="input"
                value={form.middleName ?? ''}
                onChange={(e) => setField('middleName', e.target.value)}
              />
            </Field>
            <Field label="Город">
              <input
                className="input"
                value={form.city ?? ''}
                onChange={(e) => setField('city', e.target.value)}
              />
            </Field>
            <Field
              label="Опыт работы"
              hint="Стаж в годах. Можно дробное число: 0.5 — полгода, 1.5 — полтора года."
            >
              <input
                className="input"
                type="number"
                step="0.5"
                min="0"
                value={form.experienceYears ?? 0}
                onChange={(e) => setField('experienceYears', e.target.value)}
              />
            </Field>
            <Field label="О себе">
              <textarea
                className="input"
                rows={3}
                value={form.about ?? ''}
                onChange={(e) => setField('about', e.target.value)}
              />
            </Field>
            <Field label="Софт-скиллы (через запятую)">
              <input
                className="input"
                value={softInput}
                onChange={(e) => setSoftInput(e.target.value)}
                placeholder="командная работа, коммуникация"
              />
            </Field>
          </div>

          <div className="card">
            <Field
              label="Телефон"
              hint="Скрыт до тех пор, пока вы не примете приглашение или ваш отклик не примут."
            >
              <input
                className="input"
                value={form.phone ?? ''}
                onChange={(e) => setField('phone', e.target.value)}
              />
            </Field>
            <Field
              label="Telegram"
              hint="Тоже скрыт до взаимного согласия — это правило видимости платформы."
            >
              <input
                className="input"
                value={form.telegram ?? ''}
                onChange={(e) => setField('telegram', e.target.value)}
              />
            </Field>

            <Field label="Стек (уровень 1–5)">
              <div className="row" style={{ gap: 8 }}>
                <select
                  className="input"
                  style={{ flex: 2 }}
                  value={skillDraft.skillId}
                  onChange={(e) =>
                    setSkillDraft((d) => ({ ...d, skillId: e.target.value }))
                  }
                >
                  <option value={0}>— технология —</option>
                  {dictionaries.skills.map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.name}
                    </option>
                  ))}
                </select>
                <select
                  className="input"
                  style={{ flex: 1 }}
                  value={skillDraft.proficiency}
                  onChange={(e) =>
                    setSkillDraft((d) => ({ ...d, proficiency: e.target.value }))
                  }
                >
                  {[1, 2, 3, 4, 5].map((n) => (
                    <option key={n} value={n}>
                      {n}
                    </option>
                  ))}
                </select>
                <button type="button" className="btn ghost sm" onClick={addSkill}>
                  +
                </button>
              </div>
              <div style={{ marginTop: 10 }}>
                {form.skills.map((s) => (
                  <span
                    key={s.skillId}
                    className="tag sel"
                    style={{ cursor: 'pointer' }}
                    title="Нажмите, чтобы убрать"
                    onClick={() => removeSkill(s.skillId)}
                  >
                    {skillName(s.skillId)} · {s.proficiency} ✕
                  </span>
                ))}
                {form.skills.length === 0 && (
                  <span className="hint">
                    Стек не указан — добавьте технологии.
                  </span>
                )}
              </div>
            </Field>

            <Field label="Роли в команде">
              <div>
                {dictionaries.roles.map((r) => {
                  const on = form.roleIds.includes(r.id)
                  return (
                    <span
                      key={r.id}
                      className={`tag ${on ? 'sel' : ''}`}
                      style={{ cursor: 'pointer' }}
                      onClick={() =>
                        setForm((f) => ({
                          ...f,
                          roleIds: on
                            ? f.roleIds.filter((id) => id !== r.id)
                            : [...f.roleIds, r.id],
                        }))
                      }
                    >
                      {r.name}
                    </span>
                  )
                })}
              </div>
            </Field>

            {/* Общесистемный тумблер с подписью и пояснением */}
            <Field label="Видимость профиля">
              <Toggle
                checked={form.isProfilePublic}
                onChange={(v) => setField('isProfilePublic', v)}
                label="Профиль виден работодателям в подборке"
                hint="Выключите, чтобы временно скрыть карточку из поиска — грейд и результаты тестов сохранятся."
              />
            </Field>

            <button
              className="btn"
              type="submit"
              disabled={saving}
              style={{ width: '100%' }}
            >
              {saving ? 'Сохранение…' : 'Сохранить профиль'}
            </button>
          </div>
        </div>
      </form>
    </div>
  )
}