import logoIcon from '../../assets/logo_small_blue.png'

// variant="stack"  — иконка над текстом, по центру (страницы входа/регистрации)
// variant="inline" — иконка слева от текста (шапка, сайдбар)
export default function Logo({ variant = 'stack', size = 72, withText = true }) {
  if (variant === 'inline') {
    return (
      <div className="logo">
        <img
          src={logoIcon}
          alt="ФСП"
          style={{ height: size * 0.6, width: 'auto', borderRadius: 8 }}
        />
        {withText && <span>ФСП · Платформа</span>}
      </div>
    )
  }

  return (
    <div className="auth-logo">
  <img
  src={logoIcon}
  alt="ФСП"
  style={{ height: size, width: 'auto', borderRadius: 8 }}
/>
      {withText && (
        <span style={{ fontWeight: 800, fontSize: 15, letterSpacing: '.01em' }}>
          ФСП · Платформа ИТ-вакансий
        </span>
      )}
    </div>
  )
}