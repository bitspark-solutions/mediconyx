"use client";
import { useState } from "react";

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

  const submit = async () => {
    setLoading(true);
    setMessage("");
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
        </div>
        <div className="mt-6 flex items-center gap-3">
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

