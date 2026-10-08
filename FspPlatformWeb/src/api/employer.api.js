import { api } from './client'

export const employerApi = {
  // Профиль компании
  myCompany: () => api.get('/companies/me').then((r) => r.data),
  createCompany: (dto) => api.post('/companies', dto).then((r) => r.data),
  updateCompany: (companyId, dto) =>
    api.put(`/companies/${companyId}`, dto).then((r) => r.data),

  // Вакансии
  publishedVacancies: (params) =>
    api.get('/vacancies', { params }).then((r) => r.data),
  vacancy: (id) => api.get(`/vacancies/${id}`).then((r) => r.data),
  createVacancy: (dto) => api.post('/vacancies', dto).then((r) => r.data),
  updateVacancy: (id, dto) => api.put(`/vacancies/${id}`, dto).then((r) => r.data),
  publishVacancy: (id) => api.post(`/vacancies/${id}/publish`),
  closeVacancy: (id) => api.post(`/vacancies/${id}/close`),

  // Отклики на вакансии
  // ВСЕ отклики по всем вакансиям компании — одним запросом (было N+1 по чужим вакансиям)
  companyApplications: () =>
    api.get('/applications/company').then((r) => r.data),
  vacancyApplications: (vacancyId) =>
    api.get(`/vacancies/${vacancyId}/applications`).then((r) => r.data),
  updateApplicationStatus: (vacancyId, applicationId, status) =>
    api.patch(`/vacancies/${vacancyId}/applications/${applicationId}`, { status }),
}