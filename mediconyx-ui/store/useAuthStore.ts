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

interface Branch {
  id: number
  tenantId: number
  name: string
  code: string
  address?: string
  city?: string
  phone?: string
  email?: string
  isMain: boolean
  isActive: boolean
  createdAt: string
}

interface AuthState {
  user: User | null
  accessToken: string | null
  refreshToken: string | null
  isAuthenticated: boolean

  // Branch (multi-branch Stage 4)
  branchId: number | null
  branchName: string | null
  branches: Branch[]

  // Actions
  login: (email: string, password: string) => Promise<void>
  logout: () => void
  setUser: (user: User) => void
  setTokens: (accessToken: string, refreshToken: string) => void
  setBranch: (branchId: number | null, branchName: string | null) => void
  setBranches: (branches: Branch[]) => void
  fetchBranches: () => Promise<void>
  switchBranch: (branchId: number) => Promise<void>
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,

      // Branch
      branchId: null,
      branchName: null,
      branches: [],

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
          branchId: data.user?.primaryBranchId ?? null,
          branchName: data.user?.primaryBranchName ?? null,
        })

        try {
          const meRes = await fetch(`${apiBase}/api/UserManagement/me`, {
            headers: { Authorization: `Bearer ${data.accessToken}` },
            credentials: 'include',
          })
          if (meRes.ok) {
            const me = await meRes.json()
            const meRole = String(me.role || role || '').toLowerCase()
            set({
              user: { ...me, role: meRole },
              branchId: me.primaryBranchId ?? null,
              branchName: me.primaryBranchName ?? null,
            })
          }
        } catch {}

        // Fetch available branches for the tenant
        try {
          await get().fetchBranches()
        } catch {}
      },

      logout: () => {
        set({
          user: null,
          accessToken: null,
          refreshToken: null,
          isAuthenticated: false,
          branchId: null,
          branchName: null,
          branches: [],
        })
      },

      setUser: (user: User) => {
        set({ user, isAuthenticated: true })
      },

      setTokens: (accessToken: string, refreshToken: string) => {
        set({ accessToken, refreshToken })
      },

      setBranch: (branchId: number | null, branchName: string | null) => {
        set({ branchId, branchName })
      },

      setBranches: (branches: Branch[]) => {
        set({ branches })
      },

      fetchBranches: async () => {
        const { accessToken, user } = get()
        if (!accessToken || !user) return
        try {
          const apiBase = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:9765'
          const res = await fetch(`${apiBase}/api/branch`, {
            headers: { Authorization: `Bearer ${accessToken}` },
          })
          if (res.ok) {
            const data = await res.json()
            set({ branches: Array.isArray(data) ? data : [] })
          }
        } catch {}
      },

      switchBranch: async (branchId: number) => {
        const { accessToken } = get()
        if (!accessToken) return
        try {
          const apiBase = process.env.NEXT_PUBLIC_API_BASE_URL || 'http://localhost:9765'
          const res = await fetch(`${apiBase}/api/auth/switch-branch`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              Authorization: `Bearer ${accessToken}`,
            },
            body: JSON.stringify({ branchId }),
          })
          if (!res.ok) {
            const error = await res.json()
            throw new Error(error.message || 'Branch switch failed')
          }
          const data = await res.json()
          const branch = get().branches.find(b => b.id === branchId)
          set({
            accessToken: data.accessToken,
            refreshToken: data.refreshToken,
            branchId,
            branchName: branch?.name ?? null,
            user: data.user ? { ...get().user!, ...data.user } : get().user,
          })
        } catch (err) {
          console.error('Branch switch failed:', err)
          throw err
        }
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        user: state.user,
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        isAuthenticated: state.isAuthenticated,
        branchId: state.branchId,
        branchName: state.branchName,
      }),
    }
  )
)
