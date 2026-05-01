"use client";
import { useState, useRef } from "react";
import Link from "next/link";

const apiBase = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:9765";

const initialForm = {
  hospitalName: "",
  contactPerson: "",
  email: "",
  phone: "",
  region: "",
  notes: "",
  captchaToken: "ok",
};

export default function ContactSalesPage() {
  const [form, setForm] = useState(initialForm);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [submitted, setSubmitted] = useState(false);
  const [files, setFiles] = useState<File[]>([]);
  const [uploadProgress, setUploadProgress] = useState<{[key: string]: number}>({});
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = Array.from(e.target.files || []);
    const allowedTypes = ['application/pdf', 'application/msword', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
                          'application/vnd.ms-excel', 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
                          'image/png', 'image/jpeg'];
    const maxSize = 5 * 1024 * 1024;

    const validFiles = selectedFiles.filter(file => {
      if (!allowedTypes.includes(file.type)) {
        setError(`File "${file.name}" has an invalid type. Allowed: PDF, DOC, DOCX, XLS, XLSX, PNG, JPG`);
        return false;
      }
      if (file.size > maxSize) {
        setError(`File "${file.name}" exceeds the 5 MB limit.`);
        return false;
      }
      return true;
    });

    const newFiles = [...files, ...validFiles].slice(0, 3);
    setFiles(newFiles);
    if (newFiles.length >= 3) setError("Maximum 3 files allowed.");
    if (fileInputRef.current) fileInputRef.current.value = '';
  };

  const removeFile = (index: number) => {
    setFiles(files.filter((_, i) => i !== index));
    setError("");
  };

  const uploadAttachments = async (requestId: number) => {
    for (const file of files) {
      const formData = new FormData();
      formData.append('file', file);
      try {
        setUploadProgress(prev => ({ ...prev, [file.name]: 0 }));
        const res = await fetch(`${apiBase}/api/sales/requests/${requestId}/attachments`, {
          method: 'POST',
          body: formData,
        });
        if (!res.ok) {
          const err = await res.json().catch(() => ({ message: 'Upload failed' }));
          setError(`Failed to upload ${file.name}: ${err.message}`);
        } else {
          setUploadProgress(prev => ({ ...prev, [file.name]: 100 }));
        }
      } catch {
        setError(`Failed to upload ${file.name}`);
      }
    }
  };

  const submit = async () => {
    setError("");
    if (!form.hospitalName.trim() || !form.contactPerson.trim() || !form.email.trim() || !form.phone.trim()) {
      setError("Please fill in all required fields.");
      return;
    }
    setLoading(true);
    setUploadProgress({});

    try {
      const res = await fetch(`${apiBase}/api/sales/requests/public`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(form),
      });

      if (res.ok) {
        const data = await res.json();
        if (files.length > 0) await uploadAttachments(data.id);
        setForm(initialForm);
        setFiles([]);
        setSubmitted(true);
      } else {
        const data = await res.json().catch(() => ({}));
        setError(data?.message || "Submission failed. Please try again.");
      }
    } catch {
      setError("Network error. Please check your connection and try again.");
    }
    setLoading(false);
  };

  const inputClass = "w-full rounded-lg border border-gray-300 bg-white px-4 py-2.5 text-sm text-gray-900 transition focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/20";
  const labelClass = "mb-1.5 block text-sm font-medium text-gray-700";
  const requiredDot = <span className="text-red-500 ml-0.5">*</span>;

  const input = (name: string, label: string, required = false, props?: any) => (
    <div>
      <label className={labelClass}>{label}{required && requiredDot}</label>
      <input
        className={inputClass}
        value={(form as any)[name] as any}
        onChange={(e) => setForm({ ...form, [name]: e.target.value })}
        {...props}
      />
    </div>
  );

  // Success state
  if (submitted) {
    return (
      <div className="min-h-screen bg-gradient-to-b from-blue-50 to-white">
        <nav className="bg-white shadow-md sticky top-0 z-50">
          <div className="mx-auto flex h-20 max-w-7xl items-center justify-between px-6">
            <Link href="/" className="text-3xl font-bold text-blue-600">Mediconyx</Link>
          </div>
        </nav>
        <div className="flex items-center justify-center px-6 py-24">
          <div className="w-full max-w-lg rounded-2xl bg-white p-10 text-center shadow-lg">
            <div className="mx-auto mb-6 flex h-16 w-16 items-center justify-center rounded-full bg-green-100">
              <svg className="h-8 w-8 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <h2 className="mb-2 text-2xl font-semibold text-gray-900">Request Submitted</h2>
            <p className="mb-8 text-gray-600">
              Thank you for your interest in Mediconyx. Our sales team will review your request and
              get back to you within 1-2 business days.
            </p>
            <div className="flex items-center justify-center gap-4">
              <Link href="/" className="rounded-lg border border-gray-300 px-5 py-2.5 text-sm font-medium text-gray-700 transition hover:bg-gray-50">
                Back to Home
              </Link>
              <button
                onClick={() => setSubmitted(false)}
                className="rounded-lg bg-blue-600 px-5 py-2.5 text-sm font-medium text-white transition hover:bg-blue-700"
              >
                Submit Another
              </button>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-b from-blue-50 to-white">
      {/* Navbar — matches landing page */}
      <nav className="bg-white shadow-md sticky top-0 z-50">
        <div className="mx-auto flex h-20 max-w-7xl items-center justify-between px-6">
          <Link href="/" className="text-3xl font-bold text-blue-600">Mediconyx</Link>
          <div className="flex items-center gap-4">
            <Link href="/" className="text-gray-700 hover:text-blue-600 font-medium transition hidden sm:inline">Home</Link>
            <Link href="/login" className="bg-blue-600 text-white px-6 py-2.5 rounded-md hover:bg-blue-700 transition font-medium">
              SIGN IN
            </Link>
          </div>
        </div>
      </nav>

      <div className="mx-auto max-w-4xl px-6 py-12">
        {/* Hero header */}
        <div className="mb-10 text-center">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full bg-blue-100 px-4 py-1.5 text-xs font-semibold tracking-wide text-blue-700 uppercase">
            <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
            </svg>
            Hospital Onboarding
          </div>
          <h1 className="mb-3 text-3xl font-bold tracking-tight text-gray-900 sm:text-4xl">
            Get Started with Mediconyx
          </h1>
          <p className="mx-auto max-w-2xl text-base text-gray-600">
            Fill out the form below and our sales team will reach out to discuss how Mediconyx
            can streamline your hospital operations.
          </p>
        </div>

        {/* Form card */}
        <div className="rounded-2xl bg-white shadow-lg ring-1 ring-gray-200/60">
          {/* Section: Hospital Info */}
          <div className="border-b border-gray-100 px-8 py-6">
            <h2 className="mb-1 text-lg font-semibold text-gray-900">Hospital Information</h2>
            <p className="text-sm text-gray-500">Tell us about your hospital and how to reach you.</p>
          </div>
          <div className="grid grid-cols-1 gap-5 px-8 py-6 sm:grid-cols-2">
            {input("hospitalName", "Hospital Name", true)}
            {input("contactPerson", "Contact Person", true)}
            {input("email", "Email Address", true, { type: "email" })}
            {input("phone", "Phone Number", true)}
            {input("region", "Region / City")}
          </div>

          {/* Section: Additional details */}
          <div className="border-t border-gray-100 px-8 py-6">
            <h2 className="mb-1 text-lg font-semibold text-gray-900">Additional Details</h2>
            <p className="text-sm text-gray-500">Any extra context to help us prepare a tailored offer.</p>
          </div>
          <div className="space-y-5 px-8 py-6">
            <div>
              <label className={labelClass}>Notes / Requirements</label>
              <textarea
                className={`${inputClass} resize-none`}
                rows={4}
                value={form.notes}
                onChange={(e) => setForm({ ...form, notes: e.target.value })}
              />
            </div>

            {/* Attachments */}
            <div>
              <label className={labelClass}>Attachments</label>
              <p className="mb-3 text-xs text-gray-500">Optional. Up to 3 files, 5 MB each. PDF, DOC, XLS, PNG, JPG accepted.</p>
              <div className="space-y-2">
                {files.map((file, idx) => (
                  <div key={idx} className="flex items-center justify-between rounded-lg border border-gray-200 bg-gray-50 px-4 py-2.5">
                    <div className="flex items-center gap-3">
                      <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-blue-100">
                        <svg className="h-4 w-4 text-blue-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                          <path strokeLinecap="round" strokeLinejoin="round" d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
                        </svg>
                      </div>
                      <div>
                        <p className="text-sm font-medium text-gray-800">{file.name}</p>
                        <p className="text-xs text-gray-500">{(file.size / 1024).toFixed(0)} KB</p>
                      </div>
                    </div>
                    <button type="button" onClick={() => removeFile(idx)} className="rounded-md p-1.5 text-gray-400 transition hover:bg-red-50 hover:text-red-600">
                      <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                        <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    </button>
                  </div>
                ))}
                {files.length < 3 && (
                  <>
                    <input ref={fileInputRef} type="file" onChange={handleFileSelect} accept=".pdf,.doc,.docx,.xls,.xlsx,.png,.jpg,.jpeg" className="hidden" id="file-upload" />
                    <label htmlFor="file-upload" className="flex cursor-pointer items-center justify-center gap-2 rounded-lg border-2 border-dashed border-gray-300 bg-gray-50 px-4 py-4 text-sm text-gray-500 transition hover:border-blue-400 hover:bg-blue-50 hover:text-blue-600">
                      <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
                        <path strokeLinecap="round" strokeLinejoin="round" d="M12 16.5V9.75m0 0l3 3m-3-3l-3 3M6.75 19.5a4.5 4.5 0 01-1.41-8.775 5.25 5.25 0 0110.233-2.33 3 3 0 013.758 3.848A3.752 3.752 0 0118 19.5H6.75z" />
                      </svg>
                      Click to upload or drag files here
                    </label>
                  </>
                )}
              </div>
            </div>
          </div>

          {/* Footer / Submit */}
          <div className="rounded-b-2xl border-t border-gray-100 bg-gray-50 px-8 py-5">
            {Object.keys(uploadProgress).length > 0 && (
              <div className="mb-4 space-y-2">
                <p className="text-sm font-medium text-gray-700">Uploading attachments...</p>
                {Object.entries(uploadProgress).map(([name, progress]) => (
                  <div key={name} className="flex items-center gap-3">
                    <span className="w-32 truncate text-xs text-gray-500">{name}</span>
                    <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-gray-200">
                      <div className="h-full rounded-full bg-blue-600 transition-all duration-300" style={{ width: `${progress}%` }} />
                    </div>
                    <span className="text-xs font-medium text-gray-600">{progress}%</span>
                  </div>
                ))}
              </div>
            )}

            {error && (
              <div className="mb-4 flex items-center gap-2 rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
                <svg className="h-4 w-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m9-.75a9 9 0 11-18 0 9 9 0 0118 0zm-9 3.75h.008v.008H12v-.008z" />
                </svg>
                {error}
              </div>
            )}

            <div className="flex items-center justify-between">
              <p className="text-xs text-gray-500">Fields marked with <span className="text-red-500">*</span> are required</p>
              <button
                onClick={submit}
                disabled={loading}
                className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-6 py-2.5 text-sm font-semibold text-white shadow-sm transition hover:bg-blue-700 hover:shadow-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {loading ? (
                  <>
                    <svg className="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                    </svg>
                    Submitting...
                  </>
                ) : (
                  <>
                    Submit Request
                    <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                      <path strokeLinecap="round" strokeLinejoin="round" d="M13 7l5 5m0 0l-5 5m5-5H6" />
                    </svg>
                  </>
                )}
              </button>
            </div>
          </div>
        </div>

        {/* Trust badges */}
        <div className="mt-10 flex flex-wrap items-center justify-center gap-8 text-xs text-gray-500">
          <div className="flex items-center gap-2">
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" />
            </svg>
            HIPAA Compliant
          </div>
          <div className="flex items-center gap-2">
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M16.5 10.5V6.75a4.5 4.5 0 10-9 0v3.75m-.75 11.25h10.5a2.25 2.25 0 002.25-2.25v-6.75a2.25 2.25 0 00-2.25-2.25H6.75a2.25 2.25 0 00-2.25 2.25v6.75a2.25 2.25 0 002.25 2.25z" />
            </svg>
            256-bit SSL Encryption
          </div>
          <div className="flex items-center gap-2">
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6h4.5m4.5 0a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            Response within 24 hours
          </div>
        </div>
      </div>
    </div>
  );
}
