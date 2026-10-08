import axios from 'axios'
import { useAuthStore } from '../stores/authStore'

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '/api',
  headers: { 'Content-Type': 'application/json' },
})

// Подстановка JWT-токена в каждый запрос
api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().token
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Единая обработка ошибок: backend возвращает { error: "текст" }
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      useAuthStore.getState().logout()
      if (!window.location.pathname.startsWith('/login')) {
        window.location.href = '/login'
      }
    }
    const backendMessage = error.response?.data?.error
    const status = error.response?.status
    const message =
      (backendMessage && status ? `[${status}] ${backendMessage}` : backendMessage) ??
      'Ошибка сервера. Попробуйте позже.'
    return Promise.reject(new Error(message))
  }
)