import { useEffect, useState } from 'react'
import { candidateApi } from '../../api/candidate.api'
import { useAuthStore } from '../../stores/authStore'
import Field from '../../shared/components/Field'
import Spinner from '../../shared/components/Spinner'
import Badge from '../../shared/components/Badge'

export default function SettingsPage() {
  const { fspId, email, userId } = useAuthStore()
  const [profile, setProfile] = useState(null)
  const [loading, setLoading] = useState(true)
  const [notice, setNotice] = useState(null)
  const [error, setError] = useState(null)
  const [fspInput, setFspInput] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    candidateApi
      .getProfile()
      .then((p) => {
        setProfile(p)
        if (fspId) setFspInput(fspId)
      })
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false))
  }, [fspId])

  async function toggleVisibility(value) {
    if (!profile) return
    setBusy(true)
    setError(null)
    setNotice(null)
    try {
      await candidateApi.upsertProfile({ ...profile, isProfilePublic: value })
      setProfile({ ...profile, isProfilePublic: value })
      setNotice('Настройки приватности сохранены.')
    } catch (e) {
      setError(e.message)
    } finally {
      setBusy(false)
    }
  }

  async function linkFsp(e) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    setNotice(null)
    try {
      await candidateApi.linkFsp({ fspId: fspInput.trim() })
      setNotice('ID участника ФСП привязан. Достижения будут учитываться в ранжировании.')
    } catch (e) {
      setError(e.message)
    } finally {
      setBusy(false)
    }
  }

  if (loading) return <Spinner />

  return (
    <div>
      <div className="h1">Настройки</div>
      <div className="sub">Приватность, согласия, связь с ФСП</div>

      {notice && <div className="notice" style={{ marginBottom: 14 }}>{notice}</div>}
      {error && <div className="notice red" style={{ marginBottom: 14 }}>{error}</div>}

      <div className="grid2">
        <div className="card">
          <div className="mono" style={{ marginBottom: 12 }}>ПРИВАТНОСТЬ</div>
          <div className="kv">
            <span>Профиль виден работодателям</span>
            <span className={`toggle ${profile?.isProfilePublic ? '' : 'off'}`}
              onClick={() => !busy && toggleVisibility(!profile?.isProfilePublic)}>
              <i />
            </span>
          </div>
          <div className="kv">
            <span>Email аккаунта</span>
            <span className="mono">{email}</span>
          </div>
          <div className="divider" />
          <div className="mono" style={{ marginBottom: 12 }}>СОГЛАСИЯ (152-ФЗ)</div>
          <div className="kv">
            <span>Условия сервиса и обработка персональных данных</span>
            <Badge color="green">дано</Badge>
          </div>
          <div className="kv">
            <span>Публикация профиля работодателям</span>
            <Badge color="green">дано</Badge>
          </div>
          <div className="kv">
            <span>Обработка данных ФСП</span>
            <Badge>при привязке ID</Badge>
          </div>
        </div>

        <div className="card">
          <div className="mono" style={{ marginBottom: 12 }}>СВЯЗЬ С ФСП</div>
          {fspId ? (
            <>
              <div className="kv">
                <span>ID участника ФСП</span>
                <span className="mono">{fspId}</span>
              </div>
              <div className="kv">
                <span>Статус</span>
                <Badge color="green">привязан</Badge>
              </div>
              <div className="notice" style={{ margin: '14px 0' }}>
                ID привязан при регистрации. Достижения ФСП учитываются в
                ранжировании внутри категории.
              </div>
            </>
          ) : (
            <>
              <p className="hint" style={{ marginBottom: 12 }}>
                Нет истории ФСП? Это нормально: профиль работает полностью,
                достижения просто не влияют на позицию в подборке.
              </p>
              <form onSubmit={linkFsp}>
                <Field label="ID участника ФСП">
                  <input className="input"
                    value={fspInput}
                    onChange={(e) => setFspInput(e.target.value)}
                    placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" />
                </Field>
                <button className="btn" type="submit" disabled={busy || !fspInput.trim()}>
                  Привязать ID
                </button>
              </form>
            </>
          )}
          <div className="divider" />
          <p className="hint">ID аккаунта: {userId}</p>
        </div>
      </div>
    </div>
  )
}