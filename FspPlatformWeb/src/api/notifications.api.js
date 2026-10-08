import { api } from './client'

export const notificationsApi = {
  // Последние 50 уведомлений текущего пользователя
  list: () => api.get('/notifications').then((r) => r.data),
  // Пометить одно уведомление прочитанным
  markRead: (id) => api.post(`/notifications/${id}/read`),
}