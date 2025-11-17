"use client";
import { useState, useRef } from "react";

const apiBase = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:9765";

export default function ContactSalesPage() {
  const [form, setForm] = useState({
    hospitalName: "",
    contactPerson: "",
    email: "",
    phone: "",
    region: "",
    notes: "",
    subscriptionPlan: "",
    billingCycle: "monthly",
    currency: "USD",
    quotedPrice: "",
    seats: "",
    captchaToken: "ok",
  });
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");
  const [files, setFiles] = useState<File[]>([]);
  const [uploadProgress, setUploadProgress] = useState<{[key: string]: number}>({});
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = Array.from(e.target.files || []);
    
    // Validation
    const allowedTypes = ['application/pdf', 'application/msword', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document', 
                          'application/vnd.ms-excel', 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
                          'image/png', 'image/jpeg'];
    const maxSize = 5 * 1024 * 1024; // 5MB
    
    const validFiles = selectedFiles.filter(file => {
      if (!allowedTypes.includes(file.type)) {
        setMessage(`File ${file.name} has invalid type. Allowed: PDF, DOC, DOCX, XLS, XLSX, PNG, JPG`);
        return false;
      }
      if (file.size > maxSize) {
        setMessage(`File ${file.name} is too large. Max size: 5MB`);
        return false;
      }
      return true;
    });
    
    const newFiles = [...files, ...validFiles].slice(0, 3);
    setFiles(newFiles);
    
    if (newFiles.length >= 3 && selectedFiles.length > 0) {
      setMessage("Maximum 3 files allowed");
    }
    
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const removeFile = (index: number) => {
    setFiles(files.filter((_, i) => i !== index));
    setMessage("");
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
          const error = await res.json().catch(() => ({ message: 'Upload failed' }));
          setMessage(`Failed to upload ${file.name}: ${error.message}`);
        } else {
          setUploadProgress(prev => ({ ...prev, [file.name]: 100 }));
        }
      } catch (err) {
        setMessage(`Failed to upload ${file.name}`);
      }
    }
  };

  const submit = async () => {
    setLoading(true);
    setMessage("");
    setUploadProgress({});
    
    const res = await fetch(`${apiBase}/api/sales/requests/public`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        ...form,
        quotedPrice: form.quotedPrice ? Number(form.quotedPrice) : undefined,
        seats: form.seats ? Number(form.seats) : undefined,
        billingCycle: form.billingCycle === "monthly" ? 0 : 1,
      }),
    });
    
    if (res.ok) {
      const data = await res.json();
      const requestId = data.id;
      
      // Upload attachments if any
      if (files.length > 0) {
        await uploadAttachments(requestId);
      }
      
      setMessage("Your request has been submitted. Our sales will contact you.");
      setForm({
        hospitalName: "",
        contactPerson: "",
        email: "",
        phone: "",
        region: "",
        notes: "",
        subscriptionPlan: "",
        billingCycle: "monthly",
        currency: "USD",
        quotedPrice: "",
        seats: "",
        captchaToken: "ok",
      });
      setFiles([]);
    } else {
      const data = await res.json().catch(() => ({}));
      setMessage(data?.message || "Failed to submit. Please try again.");
    }
    setLoading(false);
  };

  const input = (name: string, props?: any) => (
    <input
      className="w-full rounded border border-gray-300 bg-white px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
      value={(form as any)[name] as any}
      onChange={(e) => setForm({ ...form, [name]: e.target.value })}
      {...props}
    />
  );

  return (
    <div className="min-h-screen bg-gray-50 py-10">
      <div className="mx-auto max-w-3xl rounded-xl bg-white p-8 shadow-sm">
        <h1 className="mb-6 text-2xl font-semibold">Contact Sales</h1>
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          <div>
            <label className="mb-1 block text-sm text-gray-600">Hospital Name</label>
            {input("hospitalName")}
          </div>
          <div>
            <label className="mb-1 block text-sm text-gray-600">Contact Person</label>
            {input("contactPerson")}
          </div>
          <div>
            <label className="mb-1 block text-sm text-gray-600">Email</label>
            {input("email", { type: "email" })}
          </div>
          <div>
            <label className="mb-1 block text-sm text-gray-600">Phone</label>
            {input("phone")}
          </div>
          <div>
            <label className="mb-1 block text-sm text-gray-600">Region</label>
            {input("region")}
          </div>
          <div>
            <label className="mb-1 block text-sm text-gray-600">Subscription Plan</label>
            {input("subscriptionPlan")}
          </div>
          <div>
            <label className="mb-1 block text-sm text-gray-600">Billing Cycle</label>
            <select
              className="w-full rounded border border-gray-300 bg-white px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
              value={form.billingCycle}
              onChange={(e) => setForm({ ...form, billingCycle: e.target.value })}
            >
              <option value="monthly">Monthly</option>
              <option value="annual">Annual</option>
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm text-gray-600">Currency</label>
            {input("currency")}
          </div>
          <div>
            <label className="mb-1 block text-sm text-gray-600">Quoted Price</label>
            {input("quotedPrice", { type: "number", step: "0.01" })}
          </div>
          <div>
            <label className="mb-1 block text-sm text-gray-600">Seats</label>
            {input("seats", { type: "number" })}
          </div>
          <div className="md:col-span-2">
            <label className="mb-1 block text-sm text-gray-600">Notes</label>
            <textarea
              className="w-full rounded border border-gray-300 bg-white px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
              rows={4}
              value={form.notes}
              onChange={(e) => setForm({ ...form, notes: e.target.value })}
            />
          </div>
          <div className="md:col-span-2">
            <label className="mb-2 block text-sm text-gray-600">Attachments (Optional, max 3 files, 5MB each)</label>
            <div className="space-y-2">
              {files.map((file, idx) => (
                <div key={idx} className="flex items-center justify-between rounded border border-gray-300 bg-gray-50 px-3 py-2">
                  <div className="flex items-center gap-2">
                    <svg className="h-5 w-5 text-gray-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
                    </svg>
                    <span className="text-sm text-gray-700">{file.name}</span>
                    <span className="text-xs text-gray-500">({(file.size / 1024).toFixed(0)} KB)</span>
                  </div>
                  <button
                    type="button"
                    onClick={() => removeFile(idx)}
                    className="text-red-600 hover:text-red-800"
                  >
                    Remove
                  </button>
                </div>
              ))}
              {files.length < 3 && (
                <div>
                  <input
                    ref={fileInputRef}
                    type="file"
                    onChange={handleFileSelect}
                    accept=".pdf,.doc,.docx,.xls,.xlsx,.png,.jpg,.jpeg"
                    className="hidden"
                    id="file-upload"
                  />
                  <label
                    htmlFor="file-upload"
                    className="inline-flex cursor-pointer items-center gap-2 rounded border border-gray-300 bg-white px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                  >
                    <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                    </svg>
                    Add Attachment
                  </label>
                  <p className="mt-1 text-xs text-gray-500">Accepted: PDF, DOC, DOCX, XLS, XLSX, PNG, JPG</p>
                </div>
              )}
            </div>
          </div>
        </div>
        <div className="mt-6">
          {Object.keys(uploadProgress).length > 0 && (
            <div className="mb-4 space-y-2">
              <p className="text-sm text-gray-600">Uploading attachments...</p>
              {Object.entries(uploadProgress).map(([name, progress]) => (
                <div key={name} className="flex items-center gap-2">
                  <span className="text-xs text-gray-500">{name}</span>
                  <div className="flex-1 h-2 bg-gray-200 rounded-full overflow-hidden">
                    <div className="h-full bg-blue-600 transition-all" style={{width: `${progress}%`}} />
                  </div>
                  <span className="text-xs text-gray-500">{progress}%</span>
                </div>
              ))}
            </div>
          )}
          <button
            onClick={submit}
            disabled={loading}
            className="rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
          >
            {loading ? "Submitting..." : "Submit"}
          </button>
          {message && <span className="text-sm text-gray-700">{message}</span>}
        </div>
      </div>
    </div>
  );
}

