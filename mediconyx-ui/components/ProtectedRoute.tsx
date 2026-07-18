'use client'
import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import { useAuthStore } from '../store/useAuthStore'

interface ProtectedRouteProps {
  children: React.ReactNode
  allowedRoles?: string[]
}

export default function ProtectedRoute({ children, allowedRoles }: ProtectedRouteProps) {
  const [hasHydrated, setHasHydrated] = useState(false)
  const { user, isAuthenticated } = useAuthStore()
  const router = useRouter()

  // Wait for store hydration before checking auth
  useEffect(() => {
    if (typeof window !== 'undefined' && useAuthStore.persist) {
      if (useAuthStore.persist.hasHydrated()) {
        setHasHydrated(true)
      } else {
        const unsubscribe = useAuthStore.persist.onFinishHydration(() => {
          setHasHydrated(true)
        })
        return () => unsubscribe()
      }
    } else {
      // Fallback for non-persisted stores or server-side
      setHasHydrated(true)
    }
  }, [])

  // Only redirect after hydration is complete
  useEffect(() => {
    if (!hasHydrated) return

    if (!isAuthenticated) {
      router.push('/login')
    } else if (user && allowedRoles) {
      const role = (user.role || '').toLowerCase()
      const allowed = allowedRoles.map(r => r.toLowerCase())
      if (!allowed.includes(role)) {
        router.push('/unauthorized')
      }
    }
  }, [hasHydrated, isAuthenticated, user, allowedRoles, router])

  // Show loading during hydration
  if (!hasHydrated) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-900">
        <div className="text-center">
          <div className="animate-spin rounded-full h-16 w-16 border-t-2 border-b-2 border-blue-500 mx-auto"></div>
          <p className="mt-4 text-gray-400">Loading...</p>
        </div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-900">
        <div className="text-center">
          <div className="animate-spin rounded-full h-16 w-16 border-t-2 border-b-2 border-blue-500 mx-auto"></div>
          <p className="mt-4 text-gray-400">Redirecting to login...</p>
        </div>
      </div>
    )
  }

  if (allowedRoles && user) {
    const role = (user.role || '').toLowerCase()
    const allowed = allowedRoles.map(r => r.toLowerCase())
    if (!allowed.includes(role)) {
      // Redirect to /unauthorized happens in the effect above. If that
      // client-side navigation is ever cancelled (e.g. a racing navigation),
      // rendering null would leave the user on a permanently blank page —
      // render an explicit message instead so the UI fails closed, not blank.
      return (
        <div className="min-h-screen flex items-center justify-center bg-gray-900">
          <div className="text-center">
            <div className="animate-spin rounded-full h-16 w-16 border-t-2 border-b-2 border-blue-500 mx-auto"></div>
            <p className="mt-4 text-gray-400">Redirecting to unauthorized page...</p>
          </div>
        </div>
      )
    }
  }

  return <>{children}</>
}
