import { api } from './client'

export const atsApi = {
  list: () => api.get('/ats-integrations').then((r) => r.data),
  upsert: (dto) => api.post('/ats-integrations', dto).then((r) => r.data),
  sync: (id) => api.post(`/ats-integrations/${id}/sync`),
}