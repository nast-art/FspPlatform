// Переключатель-тумблер на базе общесистемного .toggle (theme.css).
// checked — состояние; onChange — колбэк нового значения.
export default function Toggle({ checked, onChange, label, hint }) {
  return (
    <div
      className="vis-row"
      role="switch"
      aria-checked={checked}
      onClick={() => onChange(!checked)}
    >
      <span className={`toggle ${checked ? '' : 'off'}`}>
        <i />
      </span>
      <span className="vis-text">
        <span className="vis-label">{label}</span>
        {hint && <span className="vis-hint">{hint}</span>}
      </span>
    </div>
  )
}