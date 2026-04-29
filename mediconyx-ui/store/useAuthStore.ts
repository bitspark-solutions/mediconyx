import { create } from 'zustand'
import { persist } from 'zustand/middleware'

interface User {
  id: number
  email: string
  firstName: string
  lastName: string
  role: string
  tenantId: number
  patientId?: number
}

interface AuthState {
  user: User | null
  accessToken: string | null
  refreshToken: string | null
  isAuthenticated: boolean
  
  // Actions
  login: (email: string, password: string) => Promise<void>
  logout: () => void
  setUser: (user: User) => void
  setTokens: (accessToken: string, refreshToken: string) => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,

      login: async (email: string, password: string) => {
        const apiBase = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:9765'
        const response = await fetch(`${apiBase}/api/auth/login`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ email, password }),
        })

        if (!response.ok) {
          const error = await response.json()
          throw new Error(error.message || 'Login failed')
        }

        const data = await response.json()
        const role = String(data?.user?.role || '').trim()
        
        set({
          user: { ...data.user, role },
          accessToken: data.accessToken,
          refreshToken: data.refreshToken,
          isAuthenticated: true,
        })

        try {
          const meRes = await fetch(`${apiBase}/api/UserManagement/me`, {
            headers: { Authorization: `Bearer ${data.accessToken}` },
            credentials: 'include',
          })
          if (meRes.ok) {
            const me = await meRes.json()
            const meRole = String(me.role || role || '').toLowerCase()
            set({ user: { ...me, role: meRole } })
          }
        } catch {}
      },

      logout: () => {
        set({
          user: null,
          accessToken: null,
          refreshToken: null,
          isAuthenticated: false,
        })
      },

      setUser: (user: User) => {
        set({ user, isAuthenticated: true })
      },

      setTokens: (accessToken: string, refreshToken: string) => {
        set({ accessToken, refreshToken })
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        user: state.user,
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
)
