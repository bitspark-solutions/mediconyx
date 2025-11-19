"use client"
import { useRouter } from "next/navigation"
import { useAuthStore } from "../store/useAuthStore"

export default function Header() {
  const router = useRouter()
  const handleLogout = async () => {
    const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:9765"
    const { accessToken, logout } = useAuthStore.getState()
    if (accessToken) {
      await fetch(`${baseUrl}/api/auth/logout`, { headers: { Authorization: `Bearer ${accessToken}` }, method: "POST" })
    }
    logout()
    router.push("/login")
  }
  return (
    <header className="flex items-center justify-between border-b bg-white px-4 py-3">
      <div className="font-semibold">Mediconyx</div>
      <button onClick={handleLogout} className="rounded-md bg-gray-100 px-3 py-1 hover:bg-gray-200">Logout</button>
    </header>
  )
}
