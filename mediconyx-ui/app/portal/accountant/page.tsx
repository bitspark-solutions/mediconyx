"use client";
import { useState, useEffect } from "react";
import Header from "../../../components/Header";
import ProtectedRoute from "../../../components/ProtectedRoute";
import ClinicalPortalLayout from "../../../components/layout/ClinicalPortalLayout";
import { apiClient } from "../../../lib/apiClient";
import { FileText, DollarSign, CheckCircle, Clock, Search, Loader2, Receipt } from "lucide-react";

interface Invoice {
  id: number;
  invoiceNumber?: string;
  patientId: number;
  patientName?: string;
  totalAmount: number;
  paidAmount?: number;
  status: string;
  dueDate?: string;
  createdAt: string;
  items?: any[];
}

export default function AccountantPortal() {
  const [activeTab, setActiveTab] = useState("invoices");
  const menuItems = [
    { id: "invoices", label: "Invoices", icon: FileText },
    { id: "record-payment", label: "Record Payment", icon: DollarSign },
  ];

  return (
    <ProtectedRoute allowedRoles={["Accountant", "Admin", "Master"]}>
      <ClinicalPortalLayout
        portalLabel="Accountant Portal"
        activeItem={activeTab}
        navItems={menuItems}
        onNavigate={setActiveTab}
        primaryActionLabel="Record Payment"
        onPrimaryAction={() => setActiveTab("record-payment")}
        searchPlaceholder="Search invoices..."
      >
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight text-slate-950">
              {menuItems.find(m => m.id === activeTab)?.label}
            </h2>
            <p className="mt-1 text-sm text-slate-500">Manage invoices, track payments, and monitor financial health.</p>
          </div>
        </div>

        {activeTab === "invoices" && <InvoicesTab />}
        {activeTab === "record-payment" && <RecordPaymentTab />}
      </ClinicalPortalLayout>
    </ProtectedRoute>
  );
}

function InvoicesTab() {
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");

  const load = async () => {
    setLoading(true);
    try {
      const res = await apiClient.get("/api/invoice?pageSize=100");
      if (res.ok) {
        const data = await res.json();
        setInvoices(data.invoices || data.items || []);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, []);

  const filtered = invoices.filter(i =>
    `${i.invoiceNumber || ""} ${i.patientName || ""} ${i.patientId} ${i.status}`.toLowerCase().includes(search.toLowerCase())
  );

  const totalDue = invoices.reduce((sum, i) => sum + (i.totalAmount - (i.paidAmount || 0)), 0);
  const totalPaid = invoices.reduce((sum, i) => sum + (i.paidAmount || 0), 0);
  const totalInvoices = invoices.length;

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white border border-slate-200 rounded-xl p-5">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-medium text-slate-500 uppercase">Total Invoices</span>
            <FileText className="w-4 h-4 text-slate-400" />
          </div>
          <p className="text-2xl font-bold text-slate-900">{totalInvoices}</p>
        </div>
        <div className="bg-white border border-slate-200 rounded-xl p-5">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-medium text-slate-500 uppercase">Total Paid</span>
            <CheckCircle className="w-4 h-4 text-emerald-500" />
          </div>
          <p className="text-2xl font-bold text-emerald-600">${totalPaid.toFixed(2)}</p>
        </div>
        <div className="bg-white border border-slate-200 rounded-xl p-5">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-medium text-slate-500 uppercase">Outstanding</span>
            <Clock className="w-4 h-4 text-amber-500" />
          </div>
          <p className="text-2xl font-bold text-amber-600">${totalDue.toFixed(2)}</p>
        </div>
      </div>

      <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
        <div className="p-4 border-b border-slate-200 flex items-center gap-3">
          <div className="relative flex-1">
            <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Search invoices..."
              className="w-full pl-9 pr-4 py-2 border border-slate-200 rounded text-sm focus:outline-none focus:border-blue-500"
            />
          </div>
        </div>
        {loading ? (
          <div className="flex justify-center py-12">
            <Loader2 className="w-6 h-6 text-blue-500 animate-spin" />
          </div>
        ) : filtered.length === 0 ? (
          <div className="text-center py-12 text-slate-500">
            <Receipt className="w-10 h-10 text-slate-300 mx-auto mb-3" />
            <p>No invoices yet</p>
            <p className="text-xs text-slate-400 mt-1">Invoices are created from patient appointments and services.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 text-slate-600 text-xs uppercase">
                <tr>
                  <th className="px-4 py-3 text-left">Invoice #</th>
                  <th className="px-4 py-3 text-left">Patient</th>
                  <th className="px-4 py-3 text-left">Total</th>
                  <th className="px-4 py-3 text-left">Paid</th>
                  <th className="px-4 py-3 text-left">Status</th>
                  <th className="px-4 py-3 text-left">Created</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((inv) => (
                  <tr key={inv.id} className="border-t border-slate-200 hover:bg-slate-50">
                    <td className="px-4 py-3 font-mono text-xs text-slate-900">{inv.invoiceNumber || `INV-${inv.id}`}</td>
                    <td className="px-4 py-3 text-slate-900">#{inv.patientId} {inv.patientName && `(${inv.patientName})`}</td>
                    <td className="px-4 py-3 text-slate-900 font-medium">${inv.totalAmount.toFixed(2)}</td>
                    <td className="px-4 py-3 text-emerald-600">${(inv.paidAmount || 0).toFixed(2)}</td>
                    <td className="px-4 py-3">
                      <span className={`px-2 py-1 rounded-full text-xs font-medium ${
                        inv.status === "Paid" ? "bg-emerald-100 text-emerald-700" :
                        inv.status === "Partial" ? "bg-amber-100 text-amber-700" :
                        inv.status === "Cancelled" ? "bg-red-100 text-red-700" :
                        "bg-slate-100 text-slate-700"
                      }`}>
                        {inv.status}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-slate-500 text-xs">{new Date(inv.createdAt).toLocaleDateString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}

function RecordPaymentTab() {
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [amount, setAmount] = useState("");
  const [method, setMethod] = useState("Cash");
  const [reference, setReference] = useState("");
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState("");

  useEffect(() => {
    apiClient.get("/api/invoice?pageSize=100").then(async (res) => {
      if (res.ok) {
        const data = await res.json();
        const all = data.invoices || data.items || [];
        setInvoices(all.filter((i: Invoice) => i.status !== "Paid" && i.status !== "Cancelled"));
      }
    });
  }, []);

  const handleSubmit = async () => {
    if (!selectedId || !amount) {
      setMessage("Please select an invoice and enter an amount");
      return;
    }
    setSaving(true);
    setMessage("");
    try {
      const res = await apiClient.post(`/api/invoice/${selectedId}/payment`, {
        amount: parseFloat(amount),
        paymentMethod: method,
        reference: reference || undefined,
        paymentDate: new Date().toISOString(),
      });
      if (res.ok) {
        setMessage("Payment recorded successfully");
        setAmount("");
        setReference("");
        setSelectedId(null);
        setTimeout(() => setMessage(""), 3000);
      } else {
        const err = await res.json().catch(() => ({}));
        setMessage(err.message || "Failed to record payment");
      }
    } catch (err: any) {
      console.error(err);
      setMessage(err.message || "Failed to record payment");
    } finally {
      setSaving(false);
    }
  };

  const selected = invoices.find(i => i.id === selectedId);
  const remaining = selected ? selected.totalAmount - (selected.paidAmount || 0) : 0;

  return (
    <div className="bg-white border border-slate-200 rounded-xl p-6 max-w-2xl">
      <div className="flex items-center gap-3 mb-4">
        <DollarSign className="w-5 h-5 text-emerald-600" />
        <h3 className="text-lg font-semibold text-slate-900">Record Payment</h3>
      </div>

      {message && (
        <div className={`mb-4 p-3 rounded text-sm ${message.includes("success") ? "bg-emerald-50 text-emerald-700" : "bg-red-50 text-red-700"}`}>
          {message}
        </div>
      )}

      <div className="space-y-4">
        <div>
          <label htmlFor="payment-invoice" className="block text-sm font-medium text-slate-700 mb-1">Invoice</label>
          <select
            id="payment-invoice"
            value={selectedId || ""}
            onChange={(e) => setSelectedId(e.target.value ? parseInt(e.target.value) : null)}
            className="w-full px-3 py-2 border border-slate-300 rounded text-sm focus:outline-none focus:border-blue-500"
          >
            <option value="">— Select invoice —</option>
            {invoices.map((inv) => (
              <option key={inv.id} value={inv.id}>
                {inv.invoiceNumber || `INV-${inv.id}`} — Patient #{inv.patientId} — ${(inv.totalAmount - (inv.paidAmount || 0)).toFixed(2)} due
              </option>
            ))}
          </select>
        </div>

        {selected && (
          <div className="bg-slate-50 border border-slate-200 rounded p-3 text-sm">
            <p className="text-slate-600">Total: <span className="font-medium text-slate-900">${selected.totalAmount.toFixed(2)}</span></p>
            <p className="text-slate-600">Already paid: <span className="font-medium text-emerald-600">${(selected.paidAmount || 0).toFixed(2)}</span></p>
            <p className="text-slate-600">Remaining: <span className="font-medium text-amber-600">${remaining.toFixed(2)}</span></p>
          </div>
        )}

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label htmlFor="payment-amount" className="block text-sm font-medium text-slate-700 mb-1">Amount</label>
            <input
              id="payment-amount"
              type="number"
              step="0.01"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              max={remaining}
              className="w-full px-3 py-2 border border-slate-300 rounded text-sm focus:outline-none focus:border-blue-500"
              placeholder="0.00"
            />
          </div>
          <div>
            <label htmlFor="payment-method" className="block text-sm font-medium text-slate-700 mb-1">Method</label>
            <select
              id="payment-method"
              value={method}
              onChange={(e) => setMethod(e.target.value)}
              className="w-full px-3 py-2 border border-slate-300 rounded text-sm focus:outline-none focus:border-blue-500"
            >
              <option>Cash</option>
              <option>Card</option>
              <option>Bank Transfer</option>
              <option>Mobile Payment</option>
              <option>Insurance</option>
            </select>
          </div>
        </div>

        <div>
          <label htmlFor="payment-reference" className="block text-sm font-medium text-slate-700 mb-1">Reference (optional)</label>
          <input
            id="payment-reference"
            type="text"
            value={reference}
            onChange={(e) => setReference(e.target.value)}
            className="w-full px-3 py-2 border border-slate-300 rounded text-sm focus:outline-none focus:border-blue-500"
            placeholder="Transaction ID, check number, etc."
          />
        </div>

        <button
          onClick={handleSubmit}
          disabled={saving || !selectedId || !amount}
          className="px-4 py-2 bg-emerald-600 text-white rounded text-sm font-medium hover:bg-emerald-700 disabled:opacity-50 flex items-center gap-2"
        >
          {saving ? <Loader2 className="w-4 h-4 animate-spin" /> : <CheckCircle className="w-4 h-4" />}
          Record Payment
        </button>
      </div>
    </div>
  );
}
