"use client";
import { useState, useEffect } from "react";
import { apiClient } from "../../lib/apiClient";
import { Search, Calendar, Clock, User, Plus, ChevronLeft, ChevronRight } from "lucide-react";

interface Patient {
  id: number;
  firstName: string;
  lastName: string;
  phone: string;
}

interface Doctor {
  id: number;
  firstName: string;
  lastName: string;
  specialty: string;
}

interface TimeSlot {
  startTime: string;
  endTime: string;
  available: boolean;
}

interface AppointmentSlot {
  date: string;
  dayOfWeek: string;
  slots: TimeSlot[];
}

export default function AppointmentBooking() {
  const [step, setStep] = useState(1);
  const [selectedPatient, setSelectedPatient] = useState<Patient | null>(null);
  const [patientSearch, setPatientSearch] = useState("");
  const [patientResults, setPatientResults] = useState<Patient[]>([]);
  const [selectedDoctor, setSelectedDoctor] = useState<Doctor | null>(null);
  const [doctorSearch, setDoctorSearch] = useState("");
  const [doctorResults, setDoctorResults] = useState<Doctor[]>([]);
  const [selectedDate, setSelectedDate] = useState("");
  const [availableSlots, setAvailableSlots] = useState<AppointmentSlot[]>([]);
  const [selectedSlot, setSelectedSlot] = useState<TimeSlot | null>(null);
  const [reason, setReason] = useState("");
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState(false);

  // Search patients
  useEffect(() => {
    if (patientSearch.length < 2) {
      setPatientResults([]);
      return;
    }
    const timer = setTimeout(async () => {
      try {
        const res = await apiClient.get(`/api/patient?search=${encodeURIComponent(patientSearch)}&pageSize=10`);
        if (res.ok) {
          const data = await res.json();
          setPatientResults(data.items || []);
        }
      } catch (err) {
        console.error("Patient search failed:", err);
      }
    }, 300);
    return () => clearTimeout(timer);
  }, [patientSearch]);

  // Search doctors (in a real app, there would be a doctors endpoint)
  useEffect(() => {
    if (doctorSearch.length < 2) {
      setDoctorResults([]);
      return;
    }
    // For now, we'll use UserManagement/doctors when that endpoint exists
    // For now we'll show a placeholder - in production this would call /api/doctor
  }, [doctorSearch]);

  // Load available slots when doctor and date are selected
  useEffect(() => {
    if (!selectedDoctor || !selectedDate) {
      setAvailableSlots([]);
      return;
    }
    loadAvailableSlots();
  }, [selectedDoctor, selectedDate]);

  const loadAvailableSlots = async () => {
    setLoading(true);
    try {
      const res = await apiClient.get(`/api/appointment/slots?doctorId=${selectedDoctor?.id}&date=${selectedDate}`);
      if (res.ok) {
        const data = await res.json();
        setAvailableSlots(Array.isArray(data) ? data : []);
      } else {
        // If no slots endpoint, generate mock slots for demo
        const mockSlots = generateMockSlots(selectedDate);
        setAvailableSlots(mockSlots);
      }
    } catch (err) {
      // Generate mock slots on error
      const mockSlots = generateMockSlots(selectedDate);
      setAvailableSlots(mockSlots);
    } finally {
      setLoading(false);
    }
  };

  const generateMockSlots = (date: string): AppointmentSlot[] => {
    if (!date) return [];
    const dayOfWeek = new Date(date).toLocaleDateString("en-US", { weekday: "long" });
    const slots: TimeSlot[] = [];
    for (let hour = 9; hour < 17; hour++) {
      for (let min = 0; min < 60; min += 30) {
        const startHour = hour.toString().padStart(2, "0");
        const startMin = min.toString().padStart(2, "0");
        const endHour = min === 30 ? (hour + 1).toString().padStart(2, "0") : hour.toString().padStart(2, "0");
        const endMin = min === 30 ? "00" : "30";
        slots.push({
          startTime: `${startHour}:${startMin}`,
          endTime: `${endHour}:${endMin}`,
          available: Math.random() > 0.3,
        });
      }
    }
    return [{ date, dayOfWeek, slots }];
  };

  const handleBookAppointment = async () => {
    if (!selectedPatient || !selectedDoctor || !selectedSlot || !selectedDate) {
      setError("Please complete all required fields");
      return;
    }
    setSaving(true);
    setError("");
    try {
      const appointmentDateTime = `${selectedDate}T${selectedSlot.startTime}:00`;
      const payload = {
        patientId: selectedPatient.id,
        doctorId: selectedDoctor.id,
        appointmentTime: appointmentDateTime,
        reason: reason || undefined,
      };
      const res = await apiClient.post("/api/appointment", payload);
      if (!res.ok) {
        const err = await res.json();
        throw new Error(err.message || "Failed to book appointment");
      }
      setSuccess(true);
      setTimeout(() => {
        resetForm();
      }, 2000);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const resetForm = () => {
    setStep(1);
    setSelectedPatient(null);
    setSelectedDoctor(null);
    setPatientSearch("");
    setDoctorSearch("");
    setSelectedDate("");
    setSelectedSlot(null);
    setAvailableSlots([]);
    setReason("");
    setSuccess(false);
  };

  const minDate = new Date().toISOString().split("T")[0];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold text-white">Book Appointment</h2>
          <p className="text-sm text-gray-500 mt-1">Schedule a new appointment for a patient</p>
        </div>
        {step > 1 && (
          <button
            onClick={() => setStep(Math.max(1, step - 1))}
            className="flex items-center gap-2 px-3 py-1.5 bg-gray-800 border border-gray-700 rounded-lg text-gray-300 text-sm hover:bg-gray-700 transition-colors"
          >
            <ChevronLeft className="w-4 h-4" /> Back
          </button>
        )}
      </div>

      {success && (
        <div className="bg-green-500/10 border border-green-500/30 rounded-xl p-4 flex items-center gap-3">
          <div className="w-10 h-10 rounded-full bg-green-500/20 flex items-center justify-center">
            <svg className="w-5 h-5 text-green-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
            </svg>
          </div>
          <div>
            <p className="text-green-100 font-medium">Appointment Booked Successfully!</p>
            <p className="text-green-300/70 text-sm">
              {selectedPatient?.firstName} {selectedPatient?.lastName} — {selectedDate} at {selectedSlot?.startTime}
            </p>
          </div>
        </div>
      )}

      {error && (
        <div className="bg-red-500/10 border border-red-500/30 text-red-400 text-sm p-3 rounded-xl">
          {error}
        </div>
      )}

      {/* Progress Steps */}
      <div className="flex items-center gap-2">
        {["Patient", "Doctor & Date", "Time & Confirm"].map((label, idx) => (
          <div key={label} className="flex items-center gap-2">
            <div className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-medium ${
              step > idx + 1 ? "bg-purple-600 text-white" : step === idx + 1 ? "bg-purple-600/20 text-purple-400 border border-purple-500" : "bg-gray-800 text-gray-500"
            }`}>
              {step > idx + 1 ? "✓" : idx + 1}
            </div>
            <span className={`text-sm ${step === idx + 1 ? "text-white" : "text-gray-500"}`}>{label}</span>
            {idx < 2 && <div className={`w-12 h-0.5 ${step > idx + 1 ? "bg-purple-600" : "bg-gray-700"}`} />}
          </div>
        ))}
      </div>

      {/* Step 1: Select Patient */}
      {step === 1 && (
        <div className="bg-gray-900 border border-gray-800 rounded-xl p-6 space-y-5">
          <div>
            <label className="block text-sm font-medium text-gray-300 mb-2">Search Patient *</label>
            <div className="relative">
              <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-gray-500" />
              <input
                type="text"
                value={patientSearch}
                onChange={(e) => setPatientSearch(e.target.value)}
                placeholder="Search by name, phone, or email..."
                className="w-full pl-10 pr-4 py-2.5 bg-gray-800 border border-gray-700 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-purple-500"
              />
            </div>
          </div>

          {patientResults.length > 0 && !selectedPatient && (
            <div className="bg-gray-800 rounded-xl divide-y divide-gray-700 max-h-64 overflow-y-auto">
              {patientResults.map((patient) => (
                <button
                  key={patient.id}
                  onClick={() => {
                    setSelectedPatient(patient);
                    setPatientSearch(`${patient.firstName} ${patient.lastName}`);
                    setPatientResults([]);
                  }}
                  className="w-full flex items-center gap-3 p-3 hover:bg-gray-700 transition-colors text-left"
                >
                  <div className="w-10 h-10 rounded-full bg-purple-600/20 flex items-center justify-center text-purple-400">
                    <User className="w-5 h-5" />
                  </div>
                  <div>
                    <p className="text-white font-medium">{patient.firstName} {patient.lastName}</p>
                    <p className="text-gray-500 text-sm">{patient.phone}</p>
                  </div>
                </button>
              ))}
            </div>
          )}

          {selectedPatient && (
            <div className="bg-green-500/10 border border-green-500/30 rounded-xl p-4 flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-full bg-green-500/20 flex items-center justify-center text-green-400">
                  <User className="w-5 h-5" />
                </div>
                <div>
                  <p className="text-green-100 font-medium">{selectedPatient.firstName} {selectedPatient.lastName}</p>
                  <p className="text-green-300/70 text-sm">ID: {selectedPatient.id}</p>
                </div>
              </div>
              <button
                onClick={() => { setSelectedPatient(null); setPatientSearch(""); }}
                className="text-green-400 hover:text-green-300 text-sm"
              >
                Change
              </button>
            </div>
          )}

          <button
            onClick={() => selectedPatient && setStep(2)}
            disabled={!selectedPatient}
            className="w-full py-3 bg-purple-600 hover:bg-purple-500 disabled:bg-gray-700 disabled:text-gray-500 text-white rounded-xl font-medium transition-colors"
          >
            Continue
          </button>
        </div>
      )}

      {/* Step 2: Select Doctor & Date */}
      {step === 2 && (
        <div className="bg-gray-900 border border-gray-800 rounded-xl p-6 space-y-5">
          <div>
            <label className="block text-sm font-medium text-gray-300 mb-2">Doctor Name *</label>
            <input
              type="text"
              value={doctorSearch}
              onChange={(e) => setDoctorSearch(e.target.value)}
              placeholder="Enter doctor name or specialty..."
              className="w-full px-4 py-2.5 bg-gray-800 border border-gray-700 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-purple-500"
            />
            <p className="text-xs text-gray-500 mt-1.5">
              For demo: enter any doctor name and select from results
            </p>
            {doctorSearch.length >= 2 && doctorResults.length === 0 && (
              <div className="mt-3 space-y-2">
                {["Dr. Sarah Johnson - Cardiologist", "Dr. Michael Chen - General Physician", "Dr. Emily Brown - Pediatrician"].map((doc) => (
                  <button
                    key={doc}
                    onClick={() => {
                      const name = doc.replace("Dr. ", "").split(" - ")[0];
                      const specialty = doc.split(" - ")[1];
                      setSelectedDoctor({ id: Math.floor(Math.random() * 100), firstName: name.split(" ")[0], lastName: name.split(" ")[1] || "", specialty });
                      setDoctorSearch(doc);
                    }}
                    className="w-full flex items-center gap-3 p-3 bg-gray-800 rounded-xl hover:bg-gray-700 transition-colors text-left"
                  >
                    <div className="w-10 h-10 rounded-full bg-blue-500/20 flex items-center justify-center text-blue-400">
                      <User className="w-5 h-5" />
                    </div>
                    <div>
                      <p className="text-white font-medium">{doc.split(" - ")[0]}</p>
                      <p className="text-gray-500 text-sm">{doc.split(" - ")[1]}</p>
                    </div>
                  </button>
                ))}
              </div>
            )}
            {selectedDoctor && (
              <div className="bg-blue-500/10 border border-blue-500/30 rounded-xl p-3 mt-3 flex items-center justify-between">
                <span className="text-blue-100">Selected: Dr. {selectedDoctor.firstName} {selectedDoctor.lastName} ({selectedDoctor.specialty})</span>
                <button onClick={() => { setSelectedDoctor(null); setDoctorSearch(""); }} className="text-blue-400 hover:text-blue-300 text-sm">Change</button>
              </div>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-300 mb-2">Appointment Date *</label>
            <input
              type="date"
              value={selectedDate}
              onChange={(e) => { setSelectedDate(e.target.value); setSelectedSlot(null); }}
              min={minDate}
              className="w-full px-4 py-2.5 bg-gray-800 border border-gray-700 rounded-xl text-white focus:outline-none focus:border-purple-500"
            />
          </div>

          <button
            onClick={() => selectedDoctor && selectedDate && setStep(3)}
            disabled={!selectedDoctor || !selectedDate}
            className="w-full py-3 bg-purple-600 hover:bg-purple-500 disabled:bg-gray-700 disabled:text-gray-500 text-white rounded-xl font-medium transition-colors"
          >
            Continue
          </button>
        </div>
      )}

      {/* Step 3: Select Time & Confirm */}
      {step === 3 && (
        <div className="space-y-5">
          {/* Time Slots */}
          <div className="bg-gray-900 border border-gray-800 rounded-xl p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-medium text-white">Available Time Slots</h3>
              <div className="flex items-center gap-2 text-sm text-gray-400">
                <Calendar className="w-4 h-4" />
                {new Date(selectedDate).toLocaleDateString("en-US", { weekday: "long", month: "long", day: "numeric" })}
              </div>
            </div>

            {loading ? (
              <div className="flex justify-center py-8">
                <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-purple-500"></div>
              </div>
            ) : availableSlots.length === 0 ? (
              <p className="text-gray-500 text-center py-8">No slots available for this date</p>
            ) : (
              <div className="grid grid-cols-4 sm:grid-cols-6 gap-2">
                {availableSlots[0]?.slots.map((slot, idx) => (
                  <button
                    key={idx}
                    onClick={() => slot.available && setSelectedSlot(slot)}
                    disabled={!slot.available}
                    className={`py-2 px-3 rounded-lg text-sm font-medium transition-colors ${
                      !slot.available
                        ? "bg-gray-800 text-gray-600 cursor-not-allowed"
                        : selectedSlot?.startTime === slot.startTime
                        ? "bg-purple-600 text-white"
                        : "bg-gray-800 text-gray-300 hover:bg-gray-700 hover:text-white"
                    }`}
                  >
                    {slot.startTime}
                  </button>
                ))}
              </div>
            )}
          </div>

          {/* Reason */}
          <div className="bg-gray-900 border border-gray-800 rounded-xl p-6">
            <label className="block text-sm font-medium text-gray-300 mb-2">Reason for Visit (Optional)</label>
            <textarea
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              rows={3}
              placeholder="Brief description of the reason for this appointment..."
              className="w-full px-4 py-2.5 bg-gray-800 border border-gray-700 rounded-xl text-white placeholder-gray-500 focus:outline-none focus:border-purple-500 resize-none"
            />
          </div>

          {/* Summary & Confirm */}
          <div className="bg-gray-900 border border-gray-800 rounded-xl p-6">
            <h3 className="text-lg font-medium text-white mb-4">Appointment Summary</h3>
            <div className="space-y-3 text-sm">
              <div className="flex justify-between">
                <span className="text-gray-500">Patient</span>
                <span className="text-white">{selectedPatient?.firstName} {selectedPatient?.lastName}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-500">Doctor</span>
                <span className="text-white">Dr. {selectedDoctor?.firstName} {selectedDoctor?.lastName}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-500">Date</span>
                <span className="text-white">{new Date(selectedDate).toLocaleDateString("en-US", { weekday: "long", month: "long", day: "numeric", year: "numeric" })}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-500">Time</span>
                <span className="text-white">{selectedSlot?.startTime} - {selectedSlot?.endTime}</span>
              </div>
              {reason && (
                <div className="flex justify-between">
                  <span className="text-gray-500">Reason</span>
                  <span className="text-white">{reason}</span>
                </div>
              )}
            </div>

            <button
              onClick={handleBookAppointment}
              disabled={!selectedSlot || saving}
              className="w-full mt-6 py-3 bg-purple-600 hover:bg-purple-500 disabled:bg-gray-700 disabled:text-gray-500 text-white rounded-xl font-medium transition-colors flex items-center justify-center gap-2"
            >
              {saving ? (
                <>
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                  Booking...
                </>
              ) : (
                <>
                  <Plus className="w-4 h-4" /> Confirm Booking
                </>
              )}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}