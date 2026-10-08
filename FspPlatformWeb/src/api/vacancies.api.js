import { api } from './client'

export const vacanciesApi = {
  published: (params) => api.get('/vacancies', { params }).then((r) => r.data),
  get: (id) => api.get(`/vacancies/${id}`).then((r) => r.data),
  apply: (vacancyId, coverLetter) =>
    api.post(`/applications/vacancies/${vacancyId}`, { coverLetter }),
  myApplications: () => api.get('/applications/me').then((r) => r.data),
  withdraw: (applicationId) =>
    api.post(`/applications/${applicationId}/withdraw`),
}