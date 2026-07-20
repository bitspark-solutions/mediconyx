"use client";
import { useEffect, useState } from "react";
import Link from "next/link";
import { Stethoscope, Building2, Search, Filter, Globe, Award, Calendar } from "lucide-react";

interface PublicDoctor {
  id: number;
  fullName: string;
  title: string;
  specialization: string;
  departmentName?: string;
  yearsOfExperience: number;
  languages: string[];
  qualifications: string[];
  consultationFee?: number;
  feeCurrency: string;
  isAcceptingPatients: boolean;
  photoUrl?: string;
  tenantId: number;
  hospitalName: string;
  hospitalLogoUrl?: string;
  hospitalSubdomain?: string;
}

interface ListResponse {
  totalCount: number;
  doctors: PublicDoctor[];
  availableSpecializations: string[];
}

export default function PublicDoctorsPage() {
  const [data, setData] = useState<ListResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [q, setQ] = useState("");
  const [specialization, setSpecialization] = useState("");
  const [hospital, setHospital] = useState("");

  const load = async () => {
    setLoading(true);
    try {
      const url = new URL(`${process.env.NEXT_PUBLIC_API_URL || "http://localhost:9765"}/api/public/doctors`);
      if (q) url.searchParams.set("q", q);
      if (specialization) url.searchParams.set("specialization", specialization);
      if (hospital) url.searchParams.set("tenantSubdomain", hospital);
      const r = await fetch(url.toString());
      if (r.ok) setData(await r.json());
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [specialization, hospital]);

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-emerald-50">
      {/* Top bar */}
      <div className="bg-white border-b border-slate-200">
        <div className="max-w-6xl mx-auto px-6 py-4 flex items-center justify-between">
          <Link href="/" className="flex items-center gap-2">
            <Stethoscope className="w-6 h-6 text-emerald-600" />
            <span className="text-lg font-bold text-slate-900">Mediconyx</span>
          </Link>
          <div className="flex items-center gap-3">
            <Link href="/login" className="px-3 py-1.5 text-sm text-slate-700 hover:text-slate-900">Log in</Link>
            <Link href="/register" className="px-4 py-1.5 text-sm bg-emerald-600 text-white rounded-lg hover:bg-emerald-700">Register</Link>
          </div>
        </div>
      </div>

      <div className="max-w-6xl mx-auto px-6 py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-slate-900 mb-2">Find a doctor</h1>
          <p className="text-slate-600">Browse doctors across the Mediconyx network. No account needed to explore.</p>
        </div>

        {/* Filters */}
        <div className="bg-white rounded-xl border border-slate-200 p-4 mb-6">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
            <div className="md:col-span-2 relative">
              <Search className="w-4 h-4 absolute left-3 top-2.5 text-slate-400" />
              <input
                value={q}
                onChange={e => setQ(e.target.value)}
                onKeyDown={e => { if (e.key === "Enter") load(); }}
                placeholder="Search by name, specialty, hospital..."
                className="w-full pl-9 pr-3 py-2 border border-slate-300 rounded-lg text-sm focus:outline-none focus:border-emerald-500"
              />
            </div>
            <select value={specialization} onChange={e => setSpecialization(e.target.value)}
              className="px-3 py-2 border border-slate-300 rounded-lg text-sm focus:outline-none focus:border-emerald-500">
              <option value="">All specialties</option>
              {data?.availableSpecializations.map(s => <option key={s} value={s}>{s}</option>)}
            </select>
            <input value={hospital} onChange={e => setHospital(e.target.value)}
              placeholder="Hospital subdomain (e.g. citygeneral)"
              className="px-3 py-2 border border-slate-300 rounded-lg text-sm focus:outline-none focus:border-emerald-500" />
          </div>
          <button onClick={load} className="mt-3 px-4 py-1.5 text-sm bg-slate-100 hover:bg-slate-200 rounded-lg flex items-center gap-1">
            <Filter className="w-3.5 h-3.5" /> Apply
          </button>
        </div>

        {loading ? (
          <div className="text-center py-12 text-slate-500">Loading doctors…</div>
        ) : !data || data.doctors.length === 0 ? (
          <div className="text-center py-12 bg-white rounded-xl border border-slate-200">
            <Stethoscope className="w-12 h-12 text-slate-300 mx-auto mb-3" />
            <p className="text-slate-500">No doctors match your filters.</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {data.doctors.map(d => <DoctorCard key={d.id} d={d} />)}
          </div>
        )}
      </div>
    </div>
  );
}

function DoctorCard({ d }: { d: PublicDoctor }) {
  return (
    <Link href={`/doctors/${d.id}`} className="block bg-white rounded-xl border border-slate-200 hover:border-emerald-400 hover:shadow-md transition p-4">
      <div className="flex items-start gap-3">
        {d.photoUrl ? (
          <img src={d.photoUrl} alt={d.fullName} className="w-16 h-16 rounded-full object-cover" />
        ) : (
          <div className="w-16 h-16 rounded-full bg-emerald-100 text-emerald-700 flex items-center justify-center text-xl font-bold">
            {d.fullName.split(" ").map(p => p[0]).join("").slice(0, 2)}
          </div>
        )}
        <div className="flex-1 min-w-0">
          <h3 className="font-bold text-slate-900 truncate">{d.fullName}</h3>
          <p className="text-sm text-emerald-700 font-medium">{d.specialization}</p>
          {d.title && <p className="text-xs text-slate-500 truncate">{d.title}</p>}
          <p className="text-xs text-slate-500 flex items-center gap-1 mt-1">
            <Building2 className="w-3 h-3" /> {d.hospitalName}
          </p>
        </div>
      </div>
      <div className="mt-3 flex flex-wrap gap-1.5 text-xs">
        {d.yearsOfExperience > 0 && <span className="px-2 py-0.5 bg-slate-100 text-slate-700 rounded-full">{d.yearsOfExperience} yr exp</span>}
        {d.languages.slice(0, 3).map(l => <span key={l} className="px-2 py-0.5 bg-blue-50 text-blue-700 rounded-full flex items-center gap-1"><Globe className="w-3 h-3" />{l}</span>)}
        {d.consultationFee != null && <span className="px-2 py-0.5 bg-emerald-50 text-emerald-700 rounded-full font-semibold">{d.consultationFee} {d.feeCurrency}</span>}
      </div>
      <div className="mt-3 pt-3 border-t border-slate-100 flex items-center justify-between text-xs">
        {d.isAcceptingPatients ? (
          <span className="text-emerald-700 font-medium flex items-center gap-1">
            <span className="w-2 h-2 rounded-full bg-emerald-500"></span> Accepting patients
          </span>
        ) : (
          <span className="text-slate-500">Not accepting new patients</span>
        )}
        <span className="text-blue-600 font-medium">View profile →</span>
      </div>
    </Link>
  );
}
