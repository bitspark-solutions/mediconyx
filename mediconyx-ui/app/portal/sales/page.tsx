"use client";
import ProtectedRoute from "../../../components/ProtectedRoute";
import KpiCard from "../../../components/sales/KpiCard";
import RevenueBarChart from "../../../components/sales/RevenueBarChart";
import StatusDonutChart from "../../../components/sales/StatusDonutChart";
import TrendChart from "../../../components/sales/TrendChart";
import PipelineTable from "../../../components/sales/PipelineTable";
import { useEffect, useState, useMemo } from "react";
import SalesSidebar from "../../../components/sales/SalesSidebar";
import SalesTopbar from "../../../components/sales/SalesTopbar";
import { useAuthStore } from "../../../store/useAuthStore";
import { apiClient } from "../../../lib/apiClient";

const apiBase = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:9765";

export default function SalesDashboardPage() {
  return (
    <ProtectedRoute allowedRoles={["Sales"]}>
      <DashboardContent />
    </ProtectedRoute>
  );
}

function DashboardContent() {
  const [kpis, setKpis] = useState<any>(null);
  const [revenue, setRevenue] = useState<any[]>([]);
  const [trends, setTrends] = useState<any[]>([]);
  const [error, setError] = useState("");
  const { accessToken } = useAuthStore();

  useEffect(() => {
    const load = async () => {
      try {
        const k = await apiClient.get(`/api/sales/metrics/kpis`);
        const r = await apiClient.get(`/api/sales/metrics/revenue?months=6`);
        const t = await apiClient.get(`/api/sales/metrics/trends?bucket=month&months=6`);
        if (!k.ok || !r.ok || !t.ok) throw new Error("Failed to load metrics");
        setKpis(await k.json());
        setRevenue(await r.json());
        setTrends(await t.json());
      } catch (e: any) {
        setError(e.message || "Failed to load");
      }
    };
    if (accessToken) load();
  }, [accessToken]);

  const revenueData = useMemo(() =>
    revenue.map((r: any) => ({
      period: r.period,
      realizedRevenue: r.realizedRevenue || 0,
      quotedRevenue: r.quotedRevenue || 0
    })),
    [revenue]);

  const statusCounts = useMemo(() => ({
    pending: kpis?.pending ?? 0,
    inProgress: kpis?.inProgress ?? 0,
    active: kpis?.active ?? 0,
    deactivated: kpis?.deactivated ?? 0,
  }), [kpis]);

  return (
    <div className="min-h-screen">
      <SalesSidebar />
      <SalesTopbar title="Dashboard" />
      <main className="pt-20 pl-72 pr-6 pb-6">
        <div className="mb-6 flex items-center justify-between">
          <h1 className="text-2xl font-semibold text-white">Sales Dashboard</h1>
          {error && <div className="rounded bg-red-500/20 px-3 py-2 text-sm text-red-200">{error}</div>}
        </div>

        <div className="grid grid-cols-1 gap-4 md:grid-cols-5">
          <KpiCard title="Onboarded" value={kpis?.totalOnboarded ?? 0} icon={<img src="/icons/onboarded.svg" className="h-6 w-6" />} />
          <KpiCard title="In Progress" value={kpis?.inProgress ?? 0} accent="from-blue-500 to-cyan-500" icon={<img src="/icons/progress.svg" className="h-6 w-6" />} />
          <KpiCard title="Active" value={kpis?.active ?? 0} accent="from-emerald-500 to-green-500" icon={<img src="/icons/active.svg" className="h-6 w-6" />} />
          <KpiCard title="Pending" value={kpis?.pending ?? 0} accent="from-amber-500 to-yellow-500" icon={<img src="/icons/pending.svg" className="h-6 w-6" />} />
          <KpiCard title="Deactivated" value={kpis?.deactivated ?? 0} accent="from-slate-500 to-gray-500" icon={<img src="/icons/deactivated.svg" className="h-6 w-6" />} />
        </div>

        <div className="mt-8 grid grid-cols-1 gap-6 lg:grid-cols-2">
          <RevenueBarChart height="h-[360px]" data={revenueData} />
          <StatusDonutChart height="h-[360px]" counts={statusCounts} />
        </div>

        <div className="mt-8">
          <TrendChart height="h-[360px]" data={trends} />
        </div>

        <div className="mt-8 rounded-xl surface p-6">
          <h2 className="mb-4 text-lg font-semibold text-white">Sales Pipeline</h2>
          <PipelineTable />
        </div>
      </main>
    </div>
  );
}
