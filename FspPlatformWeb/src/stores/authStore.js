import { create } from 'zustand'
import { persist } from 'zustand/middleware'

// Роли приходят с backend на русском — фиксируем константы
export const ROLES = {
  CANDIDATE: 'кандидат',
  EMPLOYER: 'работодатель',
}

export const useAuthStore = create(
  persist(
    (set) => ({
      token: null,
      userId: null,
      email: null,
      role: null,
      fspId: null,
      emailConfirmed: false,

      login: (data) =>
        set({
          token: data.token,
          userId: data.userId,
          email: data.email,
          role: data.role,
          fspId: data.fspId ?? null,
          emailConfirmed: data.emailConfirmed,
        }),

      logout: () =>
        set({
          token: null,
          userId: null,
          email: null,
          role: null,
          fspId: null,
          emailConfirmed: false,
        }),

      // Локальная пометка «почта подтверждена» (без перелогина):
      // вызывается со страницы /confirm-email после успешного подтверждения.
      setEmailConfirmed: (value) => set({ emailConfirmed: value }),
    }),
    { name: 'fsp-auth' } // localStorage
  )
)