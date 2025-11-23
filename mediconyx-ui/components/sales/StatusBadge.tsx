interface StatusBadgeProps {
  status: string;
}

const statusConfig: Record<string, { bg: string; text: string }> = {
  Pending: { bg: 'bg-gray-600', text: 'text-white' },
  Qualified: { bg: 'bg-blue-600', text: 'text-white' },
  InProgress: { bg: 'bg-yellow-600', text: 'text-white' },
  AwaitingPayment: { bg: 'bg-purple-600', text: 'text-white' },
  Provisioning: { bg: 'bg-cyan-600', text: 'text-white' },
  Active: { bg: 'bg-green-600', text: 'text-white' },
  Rejected: { bg: 'bg-red-600', text: 'text-white' },
  Suspended: { bg: 'bg-orange-600', text: 'text-white' },
  Deactivated: { bg: 'bg-slate-600', text: 'text-white' },
};

export default function StatusBadge({ status }: StatusBadgeProps) {
  const config = statusConfig[status] || { bg: 'bg-gray-500', text: 'text-white' };
  
  return (
    <span className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-medium ${config.bg} ${config.text}`}>
      {status}
    </span>
  );
}
