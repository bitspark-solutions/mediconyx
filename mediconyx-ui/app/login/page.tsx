"use client"
import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import { useAuthStore } from "../../store/useAuthStore"

export default function LoginPage() {
  const [email, setEmail] = useState("")
  const [password, setPassword] = useState("")
  const [rememberMe, setRememberMe] = useState(false)
  const [message, setMessage] = useState("")
  const [isLoading, setIsLoading] = useState(false)
  const [hasHydrated, setHasHydrated] = useState(false)
  const router = useRouter()
  const { login, isAuthenticated, user } = useAuthStore()
  
  // Check hydration status
  useEffect(() => {
    // Only access persist on client side
    if (typeof window !== 'undefined' && useAuthStore.persist) {
      setHasHydrated(useAuthStore.persist.hasHydrated())
      
      // Subscribe to hydration changes
      const unsubscribe = useAuthStore.persist.onHydrate(() => {
        setHasHydrated(false)
      })
      
      const unsubscribeFinish = useAuthStore.persist.onFinishHydration(() => {
        setHasHydrated(true)
      })
      
      return () => {
        unsubscribe()
        unsubscribeFinish()
      }
    }
  }, [])
  
  // Redirect if already logged in
  useEffect(() => {
    if (hasHydrated && isAuthenticated && user) {
      const roleRouteMap: Record<string, string> = {
        'Master': '/portal/master',
        'Admin': '/portal/admin',
        'Doctor': '/portal/doctor',
        'Nurse': '/portal/nurse',
        'Receptionist': '/portal/receptionist',
        'Accountant': '/portal/accountant',
        'Patient': '/portal/patient',
      }
      router.push(roleRouteMap[user.role] || '/portal/patient')
    }
  }, [hasHydrated, isAuthenticated, user, router])
  
  // Don't render login form until store is hydrated
  if (!hasHydrated) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-100">
        <div className="text-center">
          <div className="animate-spin rounded-full h-16 w-16 border-t-2 border-b-2 border-blue-500 mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading...</p>
        </div>
      </div>
    )
  }
  
  // Don't render login form if user is authenticated
  if (isAuthenticated && user) {
    return null
  }
  
  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setMessage("")
    setIsLoading(true)

    try {
      await login(email, password)
      
      // Get the updated user from store
      const user = useAuthStore.getState().user
      
      // Redirect based on role
      const roleRouteMap: Record<string, string> = {
        'Master': '/portal/master',
        'Admin': '/portal/admin',
        'Doctor': '/portal/doctor',
        'Nurse': '/portal/nurse',
        'Receptionist': '/portal/receptionist',
        'Accountant': '/portal/accountant',
        'Patient': '/portal/patient',
      }

      router.push(roleRouteMap[user?.role || ''] || '/portal/patient')
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Invalid credentials')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-100">
      <div className="container mx-auto px-4">
        <div className="max-w-6xl mx-auto">
          <div className="grid lg:grid-cols-2 gap-0 bg-white shadow-2xl rounded-lg overflow-hidden">
            {/* Left Panel - Welcome Section */}
            <div className="bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-800 p-12 lg:p-16 text-white flex flex-col justify-center">
              <div className="space-y-6">
                <h2 className="text-3xl lg:text-4xl font-bold leading-tight">
                  To keep connected with Largest Medical Platform in the World
                </h2>
                <p className="text-lg text-blue-100">
                  We are glad to see you again! Get access to your Orders, Wishlist and Recommendations.
                </p>
              </div>
            </div>

            {/* Right Panel - Login Form */}
            <div className="p-12 lg:p-16">
              <div className="mb-10">
                <h3 className="text-3xl font-bold text-gray-800 mb-2">Log In</h3>
                <p className="text-gray-600">
                  New to Mediconyx?{" "}
                  <a href="#" className="text-blue-600 hover:text-blue-700 font-medium hover:underline">
                    Sign Up
                  </a>
                </p>
              </div>

              <form onSubmit={onSubmit} className="space-y-6">
                <div>
                  <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-2">
                    Email Address
                  </label>
                  <input
                    id="email"
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none transition"
                    placeholder="Enter Your Email"
                    required
                  />
                </div>

                <div>
                  <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-2">
                    Password
                  </label>
                  <input
                    id="password"
                    type="password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    className="w-full px-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none transition"
                    placeholder="Enter Password"
                    required
                  />
                </div>

                <div className="flex items-center justify-between">
                  <div className="flex items-center">
                    <input
                      id="remember-me"
                      type="checkbox"
                      checked={rememberMe}
                      onChange={(e) => setRememberMe(e.target.checked)}
                      className="h-4 w-4 text-blue-600 border-gray-300 rounded focus:ring-blue-500"
                    />
                    <label htmlFor="remember-me" className="ml-2 block text-sm text-gray-700">
                      Remember Me
                    </label>
                  </div>
                  <a href="#" className="text-sm text-blue-600 hover:text-blue-700 hover:underline">
                    Forgot your username or password?
                  </a>
                </div>

                <button
                  type="submit"
                  disabled={isLoading}
                  className="w-full bg-blue-600 hover:bg-blue-700 text-white font-semibold py-3 px-6 rounded-lg transition duration-200 shadow-md hover:shadow-lg disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {isLoading ? 'Logging in...' : 'Log In'}
                </button>
              </form>

              {message && (
                <div className="mt-4 p-3 bg-red-50 border border-red-200 rounded-lg">
                  <p className="text-sm text-red-600 text-center">{message}</p>
                </div>
              )}
            </div>
          </div>

          {/* Footer */}
          <div className="text-center mt-8 text-gray-600 text-sm">
            <p>Copyright © {new Date().getFullYear()} Mediconyx. All Rights Reserved.</p>
          </div>
        </div>
      </div>
    </div>
  )
}
