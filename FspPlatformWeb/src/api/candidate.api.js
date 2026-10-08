import { api } from './client'

export const candidateApi = {
  // Профиль (null, если ещё не заполнялся)
  getProfile: () => api.get('/candidates/me/profile').then((r) => r.data),

  upsertProfile: (dto) => api.put('/candidates/me/profile', dto),

  // Опрос при регистрации. Вызывается для КАЖДОЙ выбранной специализации —
  // каждая добавляется к кандидату (первая становится основной).
  startSurvey: (dto) =>
    api.post('/candidates/me/survey', dto).then((r) => r.data), // → sessionId

  submitSurvey: (dto) => api.post('/candidates/me/survey/answers', dto),

  // Статус основной специализации (кулдаун и т.д.)
  getStatus: () => api.get('/candidates/me/status').then((r) => r.data),

  // ВСЕ специализации кандидата со статусами грейдов
  getSpecializations: () =>
    api.get('/candidates/me/specializations').then((r) => r.data),

  // Связь с ID участника ФСП
  linkFsp: (dto) => api.post('/candidates/me/fsp-link', dto),

  // Загрузка PDF-резюме (multipart/form-data, поле file).
  // Возвращает { stored, recognizedEmail, recognizedPhone, message }.
  uploadResume: (file) => {
    const fd = new FormData()
    fd.append('file', file)
    return api
      .post('/candidates/me/resume', fd, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      .then((r) => r.data)
  },

  // Автогенерированный стандартизированный PDF-профиль (ТЗ 2.1).
  // Скачиваем blob и сохраняем через временную ссылку.
  downloadProfilePdf: async () => {
    const res = await api.get('/candidates/me/profile.pdf', {
      responseType: 'blob',
    })
    const url = URL.createObjectURL(
      new Blob([res.data], { type: 'application/pdf' })
    )
    const a = document.createElement('a')
    a.href = url
    a.download = 'fsp_profile.pdf'
    document.body.appendChild(a)
    a.click()
    a.remove()
    URL.revokeObjectURL(url)
  },
}