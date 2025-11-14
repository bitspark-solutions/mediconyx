"use client"
import { useRouter } from "next/navigation"

export default function Header() {
  const router = useRouter()
  const logout = async () => {
    const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:9765"
    const token = typeof window !== "undefined" ? localStorage.getItem("accessToken") : null
    if (token) {
      await fetch(`${baseUrl}/api/auth/logout`, { headers: { Authorization: `Bearer ${token}` }, method: "POST" })
    }
    localStorage.removeItem("accessToken")
    localStorage.removeItem("refreshToken")
    router.push("/login")
  }
  return (
    <header className="flex items-center justify-between border-b bg-white px-4 py-3">
      <div className="font-semibold">Mediconyx</div>
      <button onClick={logout} className="rounded-md bg-gray-100 px-3 py-1 hover:bg-gray-200">Logout</button>
    </header>
  )
}
