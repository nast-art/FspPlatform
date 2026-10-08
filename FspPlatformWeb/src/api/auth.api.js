import { api } from './client'

export const authApi = {
  login: (dto) =>
    api.post('/auth/login', dto).then((r) => r.data),

  register: (dto) =>
    api.post('/auth/register', dto).then((r) => r.data),

  // Заглушка подтверждения email: API есть, но в MVP-демо не используется.
  // Реальная ссылка приходит на почту (в dev-режиме бэкенд пишет письмо в консоль).
  confirmEmail: (token) =>
    api.get('/auth/confirm-email', { params: { token } }).then((r) => r.data),

  changePassword: (dto) =>
    api.post('/auth/change-password', dto),
    confirmEmail: (token) => api.get('/auth/confirm-email', { params: { token } }),
}