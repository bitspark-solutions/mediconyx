"use client";
import { useEffect, useState, Suspense } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import Link from "next/link";
import { CheckCircle2, XCircle, Loader2, Heart } from "lucide-react";

function VerifyEmailInner() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const token = searchParams.get("token") || "";
  const [loading, setLoading] = useState(true);
  const [result, setResult] = useState<any>(null);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!token) {
      setError("Missing verification token.");
      setLoading(false);
      return;
    }
    (async () => {
      try {
        const r = await fetch(`${process.env.NEXT_PUBLIC_API_URL || "http://localhost:9765"}/api/auth/verify-email?token=${encodeURIComponent(token)}`);
        const data = await r.json().catch(() => ({}));
        if (r.ok && data.success) {
          setResult(data);
        } else {
          setError(data.message || "Verification failed.");
        }
      } catch (e: any) {
        setError(e.message || "Network error.");
      } finally {
        setLoading(false);
      }
    })();
  }, [token]);

  const goToPortal = () => {
    if (result?.authResponse) {
      // Store tokens + user in localStorage for the SPA auth store
      localStorage.setItem("accessToken", result.authResponse.accessToken);
      localStorage.setItem("refreshToken", result.authResponse.refreshToken);
      localStorage.setItem("user", JSON.stringify(result.authResponse.user));
    }
    router.push("/portal/patient");
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-emerald-50 via-white to-blue-50 flex items-center justify-center p-4">
      <div className="max-w-md w-full bg-white rounded-2xl shadow-lg p-8 text-center">
        <div className="w-12 h-12 bg-emerald-100 rounded-full flex items-center justify-center mx-auto mb-3">
          <Heart className="w-6 h-6 text-emerald-600" />
        </div>
        <h1 className="text-2xl font-bold text-slate-900 mb-2">Email verification</h1>

        {loading ? (
          <div className="py-8">
            <Loader2 className="w-10 h-10 text-emerald-600 mx-auto mb-3 animate-spin" />
            <p className="text-slate-600">Verifying your email…</p>
          </div>
        ) : error ? (
          <div className="py-4">
            <XCircle className="w-12 h-12 text-red-500 mx-auto mb-3" />
            <p className="text-slate-700 mb-4">{error}</p>
            <div className="space-y-2">
              <Link href="/login" className="block px-4 py-2 bg-slate-100 hover:bg-slate-200 rounded-lg text-slate-700">
                Back to login
              </Link>
              <Link href="/register" className="block px-4 py-2 bg-emerald-600 text-white rounded-lg hover:bg-emerald-700">
                Register again
              </Link>
            </div>
          </div>
        ) : result ? (
          <div className="py-4">
            <CheckCircle2 className="w-12 h-12 text-emerald-600 mx-auto mb-3" />
            <p className="text-slate-700 mb-1">{result.message}</p>
            <p className="text-sm text-slate-500 mb-4">
              Verified: <span className="font-semibold">{result.email}</span>
              {result.tenantName && <> · <span className="font-semibold">{result.tenantName}</span></>}
            </p>
            {result.authResponse ? (
              <button onClick={goToPortal}
                className="w-full px-4 py-2.5 bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 font-medium">
                Go to my patient portal
              </button>
            ) : (
              <Link href="/login" className="block px-4 py-2.5 bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 font-medium">
                Log in now
              </Link>
            )}
          </div>
        ) : null}
      </div>
    </div>
  );
}

export default function VerifyEmailPage() {
  return (
    <Suspense fallback={
      <div className="min-h-screen flex items-center justify-center">
        <Loader2 className="w-8 h-8 animate-spin text-emerald-600" />
      </div>
    }>
      <VerifyEmailInner />
    </Suspense>
  );
}
