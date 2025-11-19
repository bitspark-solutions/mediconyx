"use client";
import { useRouter } from "next/navigation";
import { useAuthStore } from "../../store/useAuthStore";

export default function UnauthorizedPage() {
  const router = useRouter();
  const { user, logout } = useAuthStore();

  const handleGoBack = () => {
    // Redirect to appropriate dashboard based on role
    const role = (user?.role || "").toLowerCase();
    switch (role) {
      case "master":
        router.push("/portal/master");
        break;
      case "sales":
        router.push("/portal/sales");
        break;
      case "doctor":
        router.push("/portal/doctor");
        break;
      case "nurse":
        router.push("/portal/nurse");
        break;
      case "admin":
        router.push("/portal/admin");
        break;
      case "patient":
        router.push("/portal/patient");
        break;
      case "accountant":
        router.push("/portal/accountant");
        break;
      case "receptionist":
        router.push("/portal/receptionist");
        break;
      default:
        router.push("/login");
    }
  };

  const handleLogout = () => {
    logout();
    router.push("/login");
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-900">
      <div className="max-w-md w-full mx-4">
        <div className="bg-gray-800 rounded-xl shadow-2xl p-8 border border-red-500/20">
          <div className="text-center">
            {/* Icon */}
            <div className="mx-auto flex items-center justify-center h-16 w-16 rounded-full bg-red-500/20 mb-4">
              <svg
                className="h-8 w-8 text-red-500"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                />
              </svg>
            </div>

            {/* Title */}
            <h1 className="text-2xl font-bold text-white mb-2">Access Denied</h1>

            {/* Message */}
            <p className="text-gray-400 mb-6">
              You don&apos;t have permission to access this page.
              {user && (
                <span className="block mt-2 text-sm">
                  Your role: <span className="text-purple-400 font-medium capitalize">{user.role}</span>
                </span>
              )}
            </p>

            {/* Actions */}
            <div className="flex flex-col gap-3">
              <button
                onClick={handleGoBack}
                className="w-full px-6 py-3 bg-purple-600 hover:bg-purple-700 text-white rounded-lg font-medium transition-colors"
              >
                Go to My Dashboard
              </button>
              <button
                onClick={handleLogout}
                className="w-full px-6 py-3 bg-gray-700 hover:bg-gray-600 text-gray-300 rounded-lg font-medium transition-colors"
              >
                Logout
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
