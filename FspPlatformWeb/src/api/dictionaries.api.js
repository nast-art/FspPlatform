import { api } from './client'

// Справочники платформы: специализации, грейды, навыки, роли, категории
export const dictionariesApi = {
  getAll: () => api.get('/dictionaries').then((r) => r.data),
}