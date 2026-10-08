// Цвет статуса: 'blue' | 'red' | 'green' | '' (нейтральный)
export default function Badge({ color = '', children }) {
  return (
    <span className={`badge ${color}`}>
      <span className="d" />
      {children}
    </span>
  )
}