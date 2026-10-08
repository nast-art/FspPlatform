export default function Spinner({ text = 'Загрузка…' }) {
  return (
    <p className="hint" style={{ padding: '24px 0', textAlign: 'center' }}>
      {text}
    </p>
  )
}