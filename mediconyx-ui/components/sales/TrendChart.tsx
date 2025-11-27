"use client";
import { useEffect, useState } from "react";
import { apiClient } from "../../lib/apiClient";
import { useAuthStore } from "../../store/useAuthStore";
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
  const [loading, setLoading] = useState(false);
  const { accessToken } = useAuthStore();

  const getPeriodLabel = () => {
    switch (period) {
      case "week":
        return "Past 4 Weeks";
      case "year":
        return "Past Year";
      case "month":
      default:
        return "Past 6 Months";
    }
  };

  const getMonthsParam = () => {
    switch (period) {
      case "week":
        return 1;
      case "year":
        return 12;
      case "month":
      default:
        return 6;
    }
  };

  const loadTrendData = async () => {
    if (!accessToken) return;
    
    setLoading(true);
    try {
      const bucketParam = period === "week" ? "week" : period === "year" ? "year" : "month";
      const monthsParam = getMonthsParam();
      const res = await apiClient.get(
        `/api/sales/metrics/trends?bucket=${bucketParam}&months=${monthsParam}`
      );
      if (res.ok) {
        const trendData = await res.json();
        setChartData(Array.isArray(trendData) ? trendData : []);
      }
    } catch (error) {
      console.error("Failed to load trend data", error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (data && data.length > 0) {
      setChartData(data);
    } else if (accessToken) {
      loadTrendData();
    }
  }, [period, accessToken]);

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
      <div className="flex items-center justify-between mb-4">
        <div>
          <h2 className="text-lg font-semibold text-white">Request Trends</h2>
          <p className="text-sm text-gray-400 mt-1">{getPeriodLabel()}</p>
        </div>
        <div className="flex gap-1 rounded-lg bg-white/5 p-1">
          <button
            onClick={() => setPeriod("week")}
            disabled={loading}
            className={`px-3 py-1 rounded text-sm transition-colors disabled:opacity-50 ${
              period === "week" ? "bg-purple-600 text-white" : "text-gray-400 hover:text-white"
            }`}
          >
            Week
          </button>
          <button
            onClick={() => setPeriod("month")}
            disabled={loading}
            className={`px-3 py-1 rounded text-sm transition-colors disabled:opacity-50 ${
              period === "month" ? "bg-purple-600 text-white" : "text-gray-400 hover:text-white"
            }`}
          >
            Month
          </button>
          <button
            onClick={() => setPeriod("year")}
            disabled={loading}
            className={`px-3 py-1 rounded text-sm transition-colors disabled:opacity-50 ${
              period === "year" ? "bg-purple-600 text-white" : "text-gray-400 hover:text-white"
            }`}
          >
            Year
          </button>
        </div>
      </div>
      {loading ? (
        <div className="h-full flex items-center justify-center">
          <div className="text-gray-400">Loading...</div>
        </div>
      ) : chartData.length === 0 ? (
        <div className="h-full flex items-center justify-center">
          <div className="text-gray-400">No data available</div>
        </div>
      ) : (
        <div className="h-full">
          <canvas id="trend-chart" style={{ width: "100%", height: "100%" }} />
        </div>
      )}
    </div>
  );
}
