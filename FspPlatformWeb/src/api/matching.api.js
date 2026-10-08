import { api } from './client'

export const matchingApi = {
  // ---------- Кандидат ----------
  // Входящие приглашения кандидата
  myInvitations: () => api.get('/my-invitations').then((r) => r.data),
  viewInvitation: (id) => api.post(`/my-invitations/${id}/view`),
  respondInvitation: (id, action) =>
    api.post(`/my-invitations/${id}/respond`, { action }),

  // ---------- Работодатель: поиск и подборка ----------
  // Поиск по банку кандидатов (каждый запрос сохраняется на бэкенде)
  searchCandidates: (dto) => api.post('/search/candidates', dto).then((r) => r.data),
  searchHistory: (page = 1, pageSize = 10) =>
    api.get('/search/history', { params: { page, pageSize } }).then((r) => r.data),
  savedResult: (requestId) =>
    api.get(`/search/results/${requestId}`).then((r) => r.data),

  // Карточка кандидата (публичная, без контактов до accept)
  candidateCard: (candidateId) =>
    api.get(`/candidates/${candidateId}`).then((r) => r.data),
  // Контакты — только после принятого приглашения/отклика (иначе 401)
  candidateContacts: (candidateId) =>
    api.get(`/candidates/${candidateId}/contacts`).then((r) => r.data),

  // ---------- Работодатель: приглашения ----------
  sendInvitation: (dto) => api.post('/invitations', dto).then((r) => r.data),
  companyInvitations: () => api.get('/invitations').then((r) => r.data),
  revokeInvitation: (id) => api.post(`/invitations/${id}/revoke`),
}