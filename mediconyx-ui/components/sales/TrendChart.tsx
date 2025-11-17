"use client";
import { useEffect, useState } from "react";
import {
  Chart,
  LineController,
  LineElement,
  PointElement,
  CategoryScale,
  LinearScale,
  Tooltip,
  Legend,
} from "chart.js";

Chart.register(LineController, LineElement, PointElement, CategoryScale, LinearScale, Tooltip, Legend);

type Period = "week" | "month" | "year";

export default function TrendChart({
  data,
  height = "h-[360px]",
}: {
  data: { period: string; count: number; deltaPercent: number }[];
  height?: string;
}) {
  const [period, setPeriod] = useState<Period>("month");
  const [chartData, setChartData] = useState(data);

  useEffect(() => {
    // In a real implementation, this would fetch data based on period
    setChartData(data);
  }, [period, data]);

  useEffect(() => {
    const ctx = document.getElementById("trend-chart") as HTMLCanvasElement | null;
    if (!ctx) return;
    const existing = Chart.getChart(ctx);
    if (existing) existing.destroy();

    new Chart(ctx, {
      type: "line",
      data: {
        labels: chartData.map((d) => d.period),
        datasets: [
          {
            label: "Requests",
            data: chartData.map((d) => d.count),
            borderColor: "#8b5cf6",
            backgroundColor: "rgba(139, 92, 246, 0.1)",
            tension: 0.4,
            fill: true,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            labels: {
              color: getComputedStyle(document.documentElement).getPropertyValue("--text-secondary") || "#cbd5e1",
            },
          },
          tooltip: {
            enabled: true,
            callbacks: {
              label: (ctx: any) => {
                const index = ctx.dataIndex;
                const value = ctx.parsed.y;
                const delta = chartData[index]?.deltaPercent || 0;
                return [
                  `Requests: ${value}`,
                  delta !== 0 ? `Change: ${delta > 0 ? "+" : ""}${delta.toFixed(1)}%` : "",
                ].filter(Boolean);
              },
            },
          },
        },
        scales: {
          x: {
            ticks: {
              color: getComputedStyle(document.documentElement).getPropertyValue("--text-muted") || "#94a3b8",
            },
            grid: { color: "rgba(255,255,255,0.08)" },
          },
          y: {
            ticks: {
              color: getComputedStyle(document.documentElement).getPropertyValue("--text-muted") || "#94a3b8",
            },
            grid: { color: "rgba(255,255,255,0.08)" },
          },
        },
      },
    });
  }, [chartData]);

  return (
    <div className={`rounded-xl surface p-6 ${height}`}>
      <div className="flex items-center justify-between mb-3">
        <h2 className="text-lg font-semibold text-white">Request Trends</h2>
        <div className="flex gap-1 rounded-lg bg-white/5 p-1">
          <button
            onClick={() => setPeriod("week")}
            className={`px-3 py-1 rounded text-sm transition-colors ${
              period === "week" ? "bg-purple-600 text-white" : "text-gray-400 hover:text-white"
            }`}
          >
            Week
          </button>
          <button
            onClick={() => setPeriod("month")}
            className={`px-3 py-1 rounded text-sm transition-colors ${
              period === "month" ? "bg-purple-600 text-white" : "text-gray-400 hover:text-white"
            }`}
          >
            Month
          </button>
          <button
            onClick={() => setPeriod("year")}
            className={`px-3 py-1 rounded text-sm transition-colors ${
              period === "year" ? "bg-purple-600 text-white" : "text-gray-400 hover:text-white"
            }`}
          >
            Year
          </button>
        </div>
      </div>
      <div className="h-full">
        <canvas id="trend-chart" style={{ width: "100%", height: "100%" }} />
      </div>
    </div>
  );
}
