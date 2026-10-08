import { useCallback, useEffect, useState } from 'react'
import { atsApi } from '../../api/ats.api'
import Spinner from '../../shared/components/Spinner'
import Badge from '../../shared/components/Badge'
import Field from '../../shared/components/Field'
import { formatDateTime } from '../../shared/utils/format'

const PRESETS = [
  { provider: 'greenhouse', label: 'Greenhouse' },
  { provider: 'lever', label: 'Lever' },
  { provider: 'custom', label: 'Custom (свой ATS)' },
]

export default function AtsPage() {
  const [items, setItems] = useState([])
  const [loading, setLoading] = useState(true)
  const [provider, setProvider] = useState('greenhouse')
  const [endpoint, setEndpoint] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState(null)
  const [notice, setNotice] = useState(null)

  const load = useCallback(() => {
    setLoading(true)
    setError(null)
    atsApi
      .list()
      .then((list) => {
        setItems(list)
        setLoading(false)
      })
      .catch((e) => {
        setError(e.message)
        setLoading(false)
      })
  }, [])

  useEffect(load, [load])

  async function handleUpsert(e) {
    e.preventDefault()
    setBusy(true)
    setNotice(null)
    setError(null)
    try {
      await atsApi.upsert({
        provider,
        config: {
          endpoint,
          credentials_ref: 'vault:ats/' + provider,
          field_mapping: {
            candidate_id: 'id',
            status: 'stage',
            resume: 'attachments',
          },
        },
      })
      setNotice('Конфигурация сохранена.')
      load()
    } catch (e2) {
      setError(e2.message)
    } finally {
      setBusy(false)
    }
  }

  async function handleSync(id) {
    setBusy(true)
    setError(null)
    try {
      await atsApi.sync(id)
      setNotice(
        'Синхронизация запущена (концепция — адаптер подключается реализацией IAtsProvider).'
      )
      load()
    } catch (e) {
      setError(e.message)
    } finally {
      setBusy(false)
    }
  }

  if (loading) return <Spinner text="Загружаем интеграции…" />

  return (
    <div style={{ maxWidth: 640 }}>
      <div className="h1">Интеграции с ATS</div>
      <div className="sub">
        Концепция за рамками MVP (ТЗ «будет плюсом»): хранение конфигурации и маппинга
        полей готово, адаптер конкретного ATS — точка расширения.
      </div>

      {notice && <div className="notice" style={{ marginBottom: 14 }}>{notice}</div>}
      {error && (
        <div className="notice red" style={{ marginBottom: 14 }}>
          {error}{' '}
          <button className="btn ghost sm" style={{ marginLeft: 10 }} onClick={load}>
            Повторить
          </button>
        </div>
      )}

      <div className="card" style={{ marginBottom: 16 }}>
        <form onSubmit={handleUpsert}>
          <Field label="Провайдер ATS">
            <select
              className="input"
              value={provider}
              onChange={(e) => setProvider(e.target.value)}
            >
              {PRESETS.map((p) => (
                <option key={p.provider} value={p.provider}>
                  {p.label}
                </option>
              ))}
            </select>
          </Field>
          <Field label="Endpoint ATS">
            <input
              className="input"
              value={endpoint}
              onChange={(e) => setEndpoint(e.target.value)}
              placeholder="https://ats.company.ru/api/v1"
            />
          </Field>
          <button className="btn" type="submit" disabled={busy}>
            Сохранить конфигурацию
          </button>
        </form>
      </div>

      <div className="card">
        <div className="mono" style={{ marginBottom: 10 }}>
          НАСТРОЕННЫЕ ИНТЕГРАЦИИ
        </div>
        {items.length === 0 ? (
          <p className="hint">Интеграций пока нет.</p>
        ) : (
          items.map((i) => (
            <div key={i.id} className="kv" style={{ alignItems: 'center' }}>
              <div>
                <b>{i.provider}</b>
                <div className="hint">
                  {i.lastSyncAt
                    ? `последняя синхронизация: ${formatDateTime(i.lastSyncAt)}`
                    : 'не синхронизировалось'}
                </div>
              </div>
              <div className="row" style={{ gap: 8 }}>
                <Badge color={i.isActive ? 'green' : ''}>
                  {i.isActive ? 'активна' : 'черновик'}
                </Badge>
                <button
                  className="btn ghost sm"
                  disabled={busy}
                  onClick={() => handleSync(i.id)}
                >
                  Синхронизировать
                </button>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  )
}