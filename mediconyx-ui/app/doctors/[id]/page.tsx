"use client";
import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { Stethoscope, Building2, Globe, Award, Clock, ArrowLeft, Calendar, MessageSquare } from "lucide-react";

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
  consultationDurationMinutes: number;
  isAcceptingPatients: boolean;
  bio?: string;
  photoUrl?: string;
  hospitalName: string;
  hospitalLogoUrl?: string;
  hospitalSubdomain?: string;
}

export default function DoctorProfilePage() {
  const params = useParams<{ id: string }>();
  const [doctor, setDoctor] = useState<PublicDoctor | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    (async () => {
      try {
        const r = await fetch(`${process.env.NEXT_PUBLIC_API_URL || "http://localhost:9765"}/api/public/doctors/${params.id}`);
        if (r.ok) setDoctor(await r.json());
        else setError("Doctor not found.");
      } catch (e: any) { setError(e.message); }
      finally { setLoading(false); }
    })();
  }, [params.id]);

  if (loading) return <Centered>Loading…</Centered>;
  if (error || !doctor) return (
    <Centered>
      <p className="text-slate-600 mb-4">{error || "Not found."}</p>
      <Link href="/doctors" className="text-emerald-600 hover:underline">← Back to directory</Link>
    </Centered>
  );

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-emerald-50">
      <div className="bg-white border-b border-slate-200">
        <div className="max-w-4xl mx-auto px-6 py-4 flex items-center justify-between">
          <Link href="/doctors" className="flex items-center gap-2 text-slate-700 hover:text-slate-900">
            <ArrowLeft className="w-4 h-4" /> Back to directory
          </Link>
          <Link href="/register" className="px-4 py-1.5 text-sm bg-emerald-600 text-white rounded-lg hover:bg-emerald-700">Register to book</Link>
        </div>
      </div>

      <div className="max-w-4xl mx-auto px-6 py-8">
        <div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-sm">
          <div className="flex flex-col md:flex-row gap-6 items-start">
            {doctor.photoUrl ? (
              <img src={doctor.photoUrl} alt={doctor.fullName} className="w-32 h-32 rounded-2xl object-cover" />
            ) : (
              <div className="w-32 h-32 rounded-2xl bg-emerald-100 text-emerald-700 flex items-center justify-center text-4xl font-bold">
                {doctor.fullName.split(" ").map(p => p[0]).join("").slice(0, 2)}
              </div>
            )}
            <div className="flex-1">
              <h1 className="text-2xl font-bold text-slate-900">{doctor.fullName}</h1>
              {doctor.title && <p className="text-slate-600">{doctor.title}</p>}
              <div className="mt-2 flex flex-wrap gap-2">
                <span className="px-3 py-1 bg-emerald-100 text-emerald-700 rounded-full text-sm font-semibold">{doctor.specialization}</span>
                {doctor.departmentName && <span className="px-3 py-1 bg-blue-100 text-blue-700 rounded-full text-sm">{doctor.departmentName}</span>}
                {doctor.yearsOfExperience > 0 && <span className="px-3 py-1 bg-slate-100 text-slate-700 rounded-full text-sm">{doctor.yearsOfExperience} years experience</span>}
              </div>
              <p className="mt-3 text-slate-700 flex items-center gap-2">
                <Building2 className="w-4 h-4 text-slate-400" />
                {doctor.hospitalName}
              </p>
              <div className="mt-3 flex items-center gap-2">
                {doctor.isAcceptingPatients ? (
                  <span className="text-emerald-700 text-sm font-medium flex items-center gap-1">
                    <span className="w-2 h-2 rounded-full bg-emerald-500"></span> Accepting new patients
                  </span>
                ) : (
                  <span className="text-slate-500 text-sm">Not accepting new patients</span>
                )}
              </div>
            </div>
          </div>

          {doctor.bio && (
            <div className="mt-6 pt-6 border-t border-slate-100">
              <h2 className="text-sm font-semibold text-slate-700 mb-2">About</h2>
              <p className="text-slate-600 leading-relaxed whitespace-pre-line">{doctor.bio}</p>
            </div>
          )}

          {doctor.qualifications.length > 0 && (
            <Section title="Qualifications" icon={Award}>
              <div className="flex flex-wrap gap-2">
                {doctor.qualifications.map((q, i) => (
                  <span key={i} className="px-3 py-1.5 bg-slate-100 text-slate-700 rounded-lg text-sm">{q}</span>
                ))}
              </div>
            </Section>
          )}

          {doctor.languages.length > 0 && (
            <Section title="Languages" icon={Globe}>
              <div className="flex flex-wrap gap-2">
                {doctor.languages.map(l => (
                  <span key={l} className="px-3 py-1.5 bg-blue-50 text-blue-700 rounded-lg text-sm">{l}</span>
                ))}
              </div>
            </Section>
          )}

          <Section title="Consultation" icon={Clock}>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <Info label="Duration" value={`${doctor.consultationDurationMinutes} minutes`} />
              <Info label="Fee" value={doctor.consultationFee != null ? `${doctor.consultationFee} ${doctor.feeCurrency}` : "Contact hospital"} />
            </div>
          </Section>

          <div className="mt-6 pt-6 border-t border-slate-100 flex flex-col sm:flex-row gap-3">
            <Link href="/register" className="flex-1 px-4 py-2.5 bg-emerald-600 text-white rounded-lg hover:bg-emerald-700 font-medium flex items-center justify-center gap-2">
              <Calendar className="w-4 h-4" /> Register to book an appointment
            </Link>
            <Link href="/register" className="flex-1 px-4 py-2.5 bg-blue-600 text-white rounded-lg hover:bg-blue-700 font-medium flex items-center justify-center gap-2">
              <MessageSquare className="w-4 h-4" /> Start telemedicine consultation
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}

function Section({ title, icon: Icon, children }: any) {
  return (
    <div className="mt-6 pt-6 border-t border-slate-100">
      <h2 className="text-sm font-semibold text-slate-700 mb-3 flex items-center gap-2">
        <Icon className="w-4 h-4" /> {title}
      </h2>
      {children}
    </div>
  );
}

function Info({ label, value }: { label: string; value: string }) {
  return (
    <div className="px-3 py-2 bg-slate-50 rounded-lg">
      <div className="text-xs text-slate-500">{label}</div>
      <div className="text-sm font-semibold text-slate-900">{value}</div>
    </div>
  );
}

function Centered({ children }: any) {
  return <div className="min-h-screen flex items-center justify-center text-slate-500">{children}</div>;
}
