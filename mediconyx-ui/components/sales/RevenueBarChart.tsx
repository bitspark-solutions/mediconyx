"use client";
import { useEffect } from "react";
import {
  Chart,
  BarController,
  BarElement,
  CategoryScale,
  LinearScale,
  Tooltip,
  Legend,
} from "chart.js";

Chart.register(BarController, BarElement, CategoryScale, LinearScale, Tooltip, Legend);

export default function RevenueBarChart({ data, height = "h-[360px]" }: { data: { period: string; realizedRevenue: number; quotedRevenue: number }[]; height?: string }) {
  useEffect(() => {
    const ctx = document.getElementById("revenue-bar") as HTMLCanvasElement | null;
    if (!ctx) return;
    const existing = Chart.getChart(ctx);
    if (existing) existing.destroy();
    new Chart(ctx, {
      type: "bar",
      data: {
        labels: data.map((d) => d.period),
        datasets: [
          {
            label: "Realized",
            data: data.map((d) => d.realizedRevenue || 0),
            backgroundColor: "#22c55e",
          },
          {
            label: "Quoted",
            data: data.map((d) => d.quotedRevenue || 0),
            backgroundColor: "#60a5fa",
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { labels: { color: getComputedStyle(document.documentElement).getPropertyValue('--text-secondary') || "#cbd5e1" } },
          tooltip: {
            enabled: true,
            callbacks: {
              label: (ctx: any) => {
                const v = ctx.parsed.y ?? 0;
                return `${ctx.dataset.label}: $${Number(v).toFixed(2)}`;
              }
            }
          },
        },
        scales: {
          x: { ticks: { color: getComputedStyle(document.documentElement).getPropertyValue('--text-muted') || "#94a3b8" }, grid: { color: "rgba(255,255,255,0.08)" } },
          y: { ticks: { color: getComputedStyle(document.documentElement).getPropertyValue('--text-muted') || "#94a3b8" }, grid: { color: "rgba(255,255,255,0.08)" } },
        },
      },
    });
  }, [data]);

  return (
    <div className={`rounded-xl surface p-6 ${height} flex flex-col`}>
      <h2 className="mb-3 text-lg font-semibold text-white flex-shrink-0">Revenue Trend</h2>
      <div className="flex-1 relative w-full min-h-0">
        <canvas id="revenue-bar" className="absolute inset-0" />
      </div>
    </div>
  );
}
