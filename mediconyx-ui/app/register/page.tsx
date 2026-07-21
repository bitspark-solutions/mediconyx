"use client";
import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Heart, UserPlus, CheckCircle2, AlertCircle, Mail } from "lucide-react";

export default function RegisterPage() {
  const router = useRouter();
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [phone, setPhone] = useState("");
  const [dateOfBirth, setDateOfBirth] = useState("");
  const [gender, setGender] = useState("Male");
  const [subdomain, setSubdomain] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState<{ email: string; tenantName: string } | null>(null);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    if (password !== confirmPassword) {
      setError("Passwords do not match.");
      return;
    }
    if (password.length < 8) {
      setError("Password must be at least 8 characters.");
      return;
    }
    if (!dateOfBirth) {
      setError("Date of birth is required.");
      return;
    }

    setSubmitting(true);
    try {
      const r = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:9765"}/api/auth/register-patient`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          firstName, lastName, email, password, phone,
          dateOfBirth: new Date(dateOfBirth).toISOString(),
          gender,
          tenantSubdomain: subdomain || null,
        }),
      });
      const data = await r.json().catch(() => ({}));
      if (r.ok) {
        setSuccess({ email: data.email, tenantName: data.tenantName });
      } else {
        setError(data.message || "Registration failed. Please try again.");
      }
    } catch (e: any) {
      setError(e.message || "Network error. Please try again.");
    } finally {
      setSubmitting(false);
    }
  };

  if (success) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-emerald-50 via-white to-blue-50 flex items-center justify-center p-4">
        <div className="max-w-md w-full bg-white rounded-2xl shadow-lg p-8 text-center">
          <div className="w-16 h-16 bg-emerald-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <CheckCircle2 className="w-9 h-9 text-emerald-600" />
          </div>
          <h1 className="text-2xl font-bold text-slate-900 mb-2">Almost there!</h1>
          <p className="text-slate-600 mb-4">
            We've sent a verification email to <span className="font-semibold">{success.email}</span>.
          </p>
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 text-left text-sm text-blue-900 mb-4">
            <div className="font-semibold flex items-center gap-2 mb-1">
              <Mail className="w-4 h-4" /> Check your inbox
            </div>
            Click the link in the email to activate your <span className="font-semibold">{success.tenantName}</span> account.
            The link expires in 48 hours.
          </div>
          <p className="text-xs text-slate-500">
            Didn't get the email? Check your spam folder, or{" "}
            <Link href="/login" className="text-blue-600 hover:underline">return to login</Link>{" "}
            to request a new link.
          </p>
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
          <h1 className="text-2xl font-bold text-slate-900">Create your patient account</h1>
          <p className="text-sm text-slate-500 mt-1">Register to book appointments and access your medical records</p>
        </div>

        <form onSubmit={submit} className="space-y-3">
          {error && (
            <div className="p-3 bg-red-50 border border-red-200 text-red-700 rounded-lg text-sm flex items-start gap-2">
              <AlertCircle className="w-4 h-4 mt-0.5 flex-shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <div className="grid grid-cols-2 gap-3">
            <Field label="First name" value={firstName} onChange={setFirstName} required />
            <Field label="Last name" value={lastName} onChange={setLastName} required />
          </div>

          <Field label="Email" value={email} onChange={setEmail} type="email" required placeholder="you@example.com" />
          <Field label="Phone" value={phone} onChange={setPhone} type="tel" required placeholder="+880 1700-000000" />

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label htmlFor="date-of-birth" className="block text-sm font-medium text-slate-700 mb-1">Date of birth</label>
              <input id="date-of-birth" type="date" value={dateOfBirth} onChange={e => setDateOfBirth(e.target.value)} required
                className="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:outline-none focus:border-emerald-500" />
            </div>
            <div>
              <label htmlFor="gender" className="block text-sm font-medium text-slate-700 mb-1">Gender</label>
              <select id="gender" value={gender} onChange={e => setGender(e.target.value)}
                className="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:outline-none focus:border-emerald-500">
                <option>Male</option>
                <option>Female</option>
                <option>Other</option>
              </select>
            </div>
          </div>

          <Field label="Password" value={password} onChange={setPassword} type="password" required placeholder="At least 8 characters" />
          <Field label="Confirm password" value={confirmPassword} onChange={setConfirmPassword} type="password" required />

          <div>
            <label htmlFor="hospital-subdomain" className="block text-sm font-medium text-slate-700 mb-1">
              Hospital subdomain <span className="text-slate-400 font-normal">(optional)</span>
            </label>
            <input id="hospital-subdomain" type="text" value={subdomain} onChange={e => setSubdomain(e.target.value)}
              placeholder="e.g. citygeneral"
              className="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:outline-none focus:border-emerald-500" />
            <p className="text-xs text-slate-500 mt-1">
              Leave blank to register on the Mediconyx platform. Hospitals give out their subdomain to direct patients.
            </p>
          </div>

          <button type="submit" disabled={submitting}
            className="w-full py-2.5 bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 disabled:opacity-50 font-medium flex items-center justify-center gap-2 mt-4">
            <UserPlus className="w-4 h-4" />
            {submitting ? "Creating account…" : "Create account"}
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

function Field({ label, value, onChange, type = "text", required = false, placeholder = "" }: any) {
  const id = `field-${String(label).toLowerCase().replace(/[^a-z0-9]+/g, "-")}`;
  return (
    <div>
      <label htmlFor={id} className={`block text-sm font-medium text-slate-700 mb-1 ${required ? "after:content-['*'] after:text-red-500" : ""}`}>{label}</label>
      <input id={id} type={type} value={value} onChange={e => onChange(e.target.value)} required={required} placeholder={placeholder}
        className="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:outline-none focus:border-emerald-500" />
    </div>
  );
}
