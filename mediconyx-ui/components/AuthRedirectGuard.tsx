"use client";
import { useEffect, useState, ReactNode } from "react";
import { useRouter } from "next/navigation";
import { useAuthStore } from "../store/useAuthStore";

interface AuthRedirectGuardProps {
    children: ReactNode;
}

/**
 * Guard component for auth pages (login, register, etc.)
 * Prevents authenticated users from accessing these pages
 * and redirects them to their appropriate portal.
 */
export default function AuthRedirectGuard({ children }: AuthRedirectGuardProps) {
    const [status, setStatus] = useState<"loading" | "authenticated" | "unauthenticated">("loading");
    const router = useRouter();
    const { isAuthenticated, user } = useAuthStore();

    useEffect(() => {
        // Wait for store hydration on client side
        if (typeof window === "undefined") return;

        const checkAuth = () => {
            if (!useAuthStore.persist) {
                setStatus("unauthenticated");
                return;
            }

            const hasHydrated = useAuthStore.persist.hasHydrated();

            if (!hasHydrated) {
                // Wait for hydration
                const unsubscribe = useAuthStore.persist.onFinishHydration(() => {
                    checkAuth();
                    unsubscribe();
                });
                return;
            }

            // Store is hydrated, check auth state
            const state = useAuthStore.getState();
            if (state.isAuthenticated && state.user) {
                setStatus("authenticated");

                // Redirect to appropriate portal
                const roleRouteMap: Record<string, string> = {
                    master: "/portal/master",
                    admin: "/portal/admin",
                    doctor: "/portal/doctor",
                    nurse: "/portal/nurse",
                    receptionist: "/portal/receptionist",
                    accountant: "/portal/accountant",
                    patient: "/portal/patient",
                    sales: "/portal/sales",
                };
                const portalRoute = roleRouteMap[(state.user.role || "").toLowerCase()] || "/portal/patient";
                router.replace(portalRoute);
            } else {
                setStatus("unauthenticated");
            }
        };

        checkAuth();
    }, [router]);

    // Show loading spinner while checking auth
    if (status === "loading") {
        return (
            <div className="min-h-screen flex items-center justify-center bg-gray-100">
                <div className="text-center">
                    <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-blue-500 mx-auto"></div>
                    <p className="mt-4 text-gray-600">Loading...</p>
                </div>
            </div>
        );
    }

    // Already authenticated - don't render children (redirect in progress)
    if (status === "authenticated") {
        return (
            <div className="min-h-screen flex items-center justify-center bg-gray-100">
                <div className="text-center">
                    <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-blue-500 mx-auto"></div>
                    <p className="mt-4 text-gray-600">Redirecting to portal...</p>
                </div>
            </div>
        );
    }

    // Not authenticated - render the login/register form
    return <>{children}</>;
}
