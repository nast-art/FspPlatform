import { useEffect, useState } from 'react'
import { employerApi } from '../../api/employer.api'
import Field from '../../shared/components/Field'
import Spinner from '../../shared/components/Spinner'
import Badge from '../../shared/components/Badge'

const EMPTY = {
  name: '',
  industry: '',
  description: '',
  website: '',
  contactEmail: '',
  contactPhone: '',
}

export default function CompanyPage() {
  const [form, setForm] = useState(EMPTY)
  const [companyId, setCompanyId] = useState(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [notice, setNotice] = useState(null)
  const [error, setError] = useState(null)

  useEffect(() => {
    employerApi
      .myCompany()
      .then((c) => {
        setForm({
          name: c.name ?? '',
          industry: c.industry ?? '',
          description: c.description ?? '',
          website: c.website ?? '',
          contactEmail: c.contactEmail ?? '',
          contactPhone: c.contactPhone ?? '',
        })
        setCompanyId(c.id)
      })
      .catch(() => {
        /* компании ещё нет — останемся с пустой формой создания */
      })
      .finally(() => setLoading(false))
  }, [])

  function setField(name, value) {
    setForm((f) => ({ ...f, [name]: value }))
  }

  async function handleSave(e) {
    e.preventDefault()
    setSaving(true)
    setNotice(null)
    setError(null)
    try {
      const dto = {
        name: form.name.trim(),
        industry: form.industry || undefined,
        description: form.description || undefined,
        website: form.website || undefined,
        contactEmail: form.contactEmail || undefined,
        contactPhone: form.contactPhone || undefined,
      }
      const saved = companyId
        ? await employerApi.updateCompany(companyId, dto)
        : await employerApi.createCompany(dto)
      setCompanyId(saved.id)
      setNotice('Профиль компании сохранён.')
    } catch (e2) {
      setError(e2.message)
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <Spinner />

  return (
    <div>
      <div className="h1">Профиль компании</div>
      <div className="sub">
        Эти данные увидит кандидат в приглашении. Верификация компании — за рамками
        MVP (ТЗ 2.2 п.4).
      </div>

      {notice && <div className="notice" style={{ marginBottom: 14 }}>{notice}</div>}
      {error && <div className="notice red" style={{ marginBottom: 14 }}>{error}</div>}

      <form onSubmit={handleSave}>
        <div className="grid2">
          <div className="card">
            <Field label="Название организации">
              <input
                className="input"
                value={form.name}
                onChange={(e) => setField('name', e.target.value)}
                required
              />
            </Field>
            <Field label="Направление деятельности">
              <input
                className="input"
                value={form.industry}
                onChange={(e) => setField('industry', e.target.value)}
                placeholder="Финансовые технологии"
              />
            </Field>
            <Field label="Сайт">
              <input
                className="input"
                value={form.website}
                onChange={(e) => setField('website', e.target.value)}
                placeholder="company.ru"
              />
            </Field>
            <Field label="Контактный email">
              <input
                className="input"
                type="email"
                value={form.contactEmail}
                onChange={(e) => setField('contactEmail', e.target.value)}
                placeholder="hr@company.ru"
              />
            </Field>
            <Field label="Контактный телефон">
              <input
                className="input"
                value={form.contactPhone}
                onChange={(e) => setField('contactPhone', e.target.value)}
              />
            </Field>
          </div>
          <div className="card">
            <Field label="Описание организации">
              <textarea
                className="input"
                rows={8}
                value={form.description}
                onChange={(e) => setField('description', e.target.value)}
                placeholder="Команда, продукт, стек, культура"
              />
            </Field>
            <div className="kv">
              <span className="k">Статус</span>
              <Badge color="green">активен</Badge>
            </div>
            <button
              className="btn"
              type="submit"
              disabled={saving}
              style={{ width: '100%', marginTop: 14 }}
            >
              {saving ? 'Сохранение…' : 'Сохранить'}
            </button>
          </div>
        </div>
      </form>
    </div>
  )
}