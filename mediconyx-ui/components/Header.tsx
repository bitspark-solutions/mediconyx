"use client"

import { useRouter } from "next/navigation"
import { useAuthStore } from "../store/useAuthStore"

export default function Header() {
  const router = useRouter()
  const {
    accessToken,
    branches,
    branchId,
    branchName,
    setBranch,
    switchBranch,
    user,
  } = useAuthStore()

  const handleLogout = async () => {
    const baseUrl = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:9765"
    if (accessToken) {
      await fetch(`${baseUrl}/api/auth/logout`, {
        headers: { Authorization: `Bearer ${accessToken}` },
        method: "POST",
      })
    }
    router.push("/login")
  }

  const handleBranchChange = async (nextBranchId: number) => {
    try {
      await switchBranch(nextBranchId)
      // Give the browser a moment to process the new token, then reload
      // so all data-fetching hooks pick up the new branch context.
      router.refresh()
    } catch {
      // Switch failed — stay on current branch
    }
  }

  const currentBranch = branches.find((b) => b.id === branchId)

  return (
    <header className="flex items-center justify-between border-b bg-white px-4 py-3">
      <div className="flex items-center gap-3 font-semibold">
        <span>Mediconyx</span>
        {branches.length > 1 && (
          <div className="flex items-center gap-2 text-sm text-slate-600">
            <span className="font-medium">Branch:</span>
            <select
              value={branchId ?? ""}
              onChange={(e) => handleBranchChange(Number(e.target.value))}
              disabled={branches.length <= 1}
              className="rounded border border-slate-200 bg-slate-50 px-2 py-1 text-slate-700 outline-none focus:border-blue-800 focus:ring-1 focus:ring-blue-800"
            >
              {branches.map((b) => (
                <option key={b.id} value={b.id}>
                  {b.isMain ? `${b.name} (Main)` : b.name}
                </option>
              ))}
            </select>
            <span className="text-xs text-slate-400">
              {currentBranch?.code ? `· ${currentBranch.code}` : ""}
            </span>
          </div>
        )}
      </div>
      <button
        onClick={handleLogout}
        className="rounded-md bg-gray-100 px-3 py-1 hover:bg-gray-200"
      >
        Logout
      </button>
    </header>
  )
}
