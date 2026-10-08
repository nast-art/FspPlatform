import { api } from './client'

export const testsApi = {
  start: (dto) => api.post('/tests/start', dto).then((r) => r.data),
  getSession: (sessionId) => api.get(`/tests/${sessionId}`).then((r) => r.data),
  submitAnswers: (sessionId, dto) =>
    api.post(`/tests/${sessionId}/answers`, dto),
  finish: (sessionId) =>
    api.post(`/tests/${sessionId}/finish`).then((r) => r.data),
  history: () => api.get('/tests/history').then((r) => r.data),
}