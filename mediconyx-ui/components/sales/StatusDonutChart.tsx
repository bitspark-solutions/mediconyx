"use client";
import { useEffect } from "react";
import { Chart, ArcElement, Tooltip, Legend, DoughnutController } from "chart.js";

Chart.register(DoughnutController, ArcElement, Tooltip, Legend);

export default function StatusDonutChart({ counts, height = "h-[360px]" }: { counts: { pending: number; inProgress: number; active: number; deactivated: number }; height?: string }) {
  useEffect(() => {
    const el = document.getElementById("status-donut") as HTMLCanvasElement | null;
    if (!el) return;
    const existing = Chart.getChart(el);
    if (existing) existing.destroy();
    new Chart(el, {
      type: "doughnut",
      data: {
        labels: ["Pending", "In Progress", "Active", "Deactivated"],
        datasets: [
          {
            data: [counts.pending, counts.inProgress, counts.active, counts.deactivated],
            backgroundColor: ["#f59e0b", "#3b82f6", "#22c55e", "#64748b"],
            borderColor: "#0f1535",
          },
        ],
      },
      options: {
        maintainAspectRatio: false,
        plugins: {
          legend: { labels: { color: getComputedStyle(document.documentElement).getPropertyValue('--text-secondary') || "#cbd5e1" } },
          tooltip: {
            enabled: true,
            callbacks: {
              label: (ctx: any) => `${ctx.label}: ${ctx.parsed}`,
            }
          }
        },
      },
    });
  }, [counts]);

  return (
    <div className={`rounded-xl surface p-6 ${height} flex flex-col`}>
      <h2 className="mb-3 text-lg font-semibold text-white flex-shrink-0">Requests Status</h2>
      <div className="flex-1 relative w-full min-h-0">
        <canvas id="status-donut" className="absolute inset-0" />
      </div>
    </div>
  );
}
