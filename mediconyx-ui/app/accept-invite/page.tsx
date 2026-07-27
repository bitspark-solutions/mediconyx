"use client";

import { useEffect, useState, Suspense } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import Link from "next/link";
import { CheckCircle2, XCircle, Loader2, Heart, AlertCircle, Mail } from "lucide-react";

function AcceptInviteInner() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const token = searchParams.get("token") || "";

  const [loading, setLoading] = useState(true);
  const [validating, setValidating] = useState(true);
  const [valid, setValid] = useState(false);
  const [email, setEmail] = useState("");
  const [hospitalName, setHospitalName] = useState("");
  const [accountActivatedOnAccept, setAccountActivatedOnAccept] = useState(false);
  const [error, setError] = useState("");

  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState("");
  const [success, setSuccess] = useState<{ message: string; accountActivated: boolean } | null>(null);

  useEffect(() => {
    if (!token) {
      setError("Missing invitation token.");
      setValidating(false);
      setLoading(false);
      return;
    }

    (async () => {
      try {
        const r = await fetch(
          `${process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:9765"}/api/invitations/validate?token=${encodeURIComponent(token)}`
        );
        const data = await r.json().catch(() => ({}));
        if (r.ok && data.valid) {
          setValid(true);
          setEmail(data.email || "");
          setHospitalName(data.hospitalName || "");
          setAccountActivatedOnAccept(data.accountActivatedOnAccept || false);
        } else {
          setError("This invitation link is invalid or has expired.");
        }
      } catch (e: any) {
        setError(e.message || "Network error. Please try again.");
      } finally {
        setValidating(false);
        setLoading(false);
      }
    })();
  }, [token]);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitError("");

    if (password !== confirmPassword) {
      setSubmitError("Passwords do not match.");
      return;
    }
    if (password.length < 8) {
      setSubmitError("Password must be at least 8 characters.");
      return;
    }

    setSubmitting(true);
    try {
      const r = await fetch(
        `${process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:9765"}/api/invitations/accept`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ token, password, confirmPassword }),
        }
      );
      const data = await r.json().catch(() => ({}));
      if (r.ok) {
        setSuccess({ message: data.message, accountActivated: data.accountActivated });
      } else {
        setSubmitError(data.message || "Failed to accept invitation. Please try again.");
      }
    } catch (e: any) {
      setSubmitError(e.message || "Network error. Please try again.");
    } finally {
      setSubmitting(false);
    }
  };

  if (loading || validating) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-emerald-50 via-white to-blue-50 flex items-center justify-center p-4">
        <div className="max-w-md w-full bg-white rounded-2xl shadow-lg p-8 text-center">
          <Loader2 className="w-10 h-10 text-emerald-600 mx-auto mb-3 animate-spin" />
          <p className="text-slate-600">Validating your invitation…</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-emerald-50 via-white to-blue-50 flex items-center justify-center p-4">
        <div className="max-w-md w-full bg-white rounded-2xl shadow-lg p-8 text-center">
          <XCircle className="w-12 h-12 text-red-500 mx-auto mb-3" />
          <h1 className="text-2xl font-bold text-slate-900 mb-2">Invalid Invitation</h1>
          <p className="text-slate-700 mb-4">{error}</p>
          <Link
            href="/login"
            className="block px-4 py-2 bg-slate-100 hover:bg-slate-200 rounded-lg text-slate-700"
          >
            Back to login
          </Link>
        </div>
      </div>
    );
  }

  if (success) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-emerald-50 via-white to-blue-50 flex items-center justify-center p-4">
        <div className="max-w-md w-full bg-white rounded-2xl shadow-lg p-8 text-center">
          <div className="w-16 h-16 bg-emerald-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <CheckCircle2 className="w-9 h-9 text-emerald-600" />
          </div>
          <h1 className="text-2xl font-bold text-slate-900 mb-2">
            {success.accountActivated ? "Account Ready!" : "Password Set!"}
          </h1>
          <p className="text-slate-600 mb-4">{success.message}</p>
          {success.accountActivated ? (
            <Link
              href="/login"
              className="block px-4 py-2.5 bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 font-medium"
            >
              Log in now
            </Link>
          ) : (
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 text-left text-sm text-blue-900">
              <div className="font-semibold flex items-center gap-2 mb-1">
                <Mail className="w-4 h-4" /> What's next?
              </div>
              Your hospital account is being provisioned. You'll receive another email when it's ready to use.
            </div>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-emerald-50 via-white to-blue-50 flex items-center justify-center p-4 py-8">
      <div className="max-w-md w-full bg-white rounded-2xl shadow-lg p-8">
        <div className="text-center mb-6">
          <div className="w-12 h-12 bg-emerald-100 rounded-full flex items-center justify-center mx-auto mb-3">
            <Heart className="w-6 h-6 text-emerald-600" />
          </div>
          <h1 className="text-2xl font-bold text-slate-900">Set up your account</h1>
          <p className="text-sm text-slate-500 mt-1">
            You've been invited to set up the administrator account for{" "}
            <span className="font-semibold">{hospitalName}</span>
          </p>
          <p className="text-sm text-slate-500 mt-1">
            Email: <span className="font-semibold">{email}</span>
          </p>
        </div>

        <form onSubmit={submit} className="space-y-3">
          {submitError && (
            <div className="p-3 bg-red-50 border border-red-200 text-red-700 rounded-lg text-sm flex items-start gap-2">
              <AlertCircle className="w-4 h-4 mt-0.5 flex-shrink-0" />
              <span>{submitError}</span>
            </div>
          )}

          <div>
            <label htmlFor="password" className="block text-sm font-medium text-slate-700 mb-1 after:content-['*'] after:text-red-500">
              Password
            </label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              placeholder="At least 8 characters"
              className="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:outline-none focus:border-emerald-500"
            />
          </div>

          <div>
            <label htmlFor="confirm-password" className="block text-sm font-medium text-slate-700 mb-1 after:content-['*'] after:text-red-500">
              Confirm password
            </label>
            <input
              id="confirm-password"
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              required
              className="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:outline-none focus:border-emerald-500"
            />
          </div>

          <button
            type="submit"
            disabled={submitting}
            className="w-full py-2.5 bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 disabled:opacity-50 font-medium flex items-center justify-center gap-2 mt-4"
          >
            {submitting ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin" /> Setting password…
              </>
            ) : (
              "Set password"
            )}
          </button>

          <p className="text-center text-sm text-slate-500 mt-3">
            Already have an account?{" "}
            <Link href="/login" className="text-emerald-600 hover:underline font-medium">
              Log in
            </Link>
          </p>
        </form>
      </div>
    </div>
  );
}

export default function AcceptInvitePage() {
  return (
    <Suspense
      fallback={
        <div className="min-h-screen flex items-center justify-center">
          <Loader2 className="w-8 h-8 animate-spin text-emerald-600" />
        </div>
      }
    >
      <AcceptInviteInner />
    </Suspense>
  );
}
