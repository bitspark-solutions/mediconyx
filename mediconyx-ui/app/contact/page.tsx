import OnboardingForm from "../../components/public/OnboardingForm";

export default function OnboardingPage() {
    return (
        <div className="min-h-screen bg-black text-white relative overflow-hidden">
            {/* Background Gradients */}
            <div className="absolute top-0 left-0 w-full h-full overflow-hidden pointer-events-none z-0">
                <div className="absolute top-[-10%] left-[-10%] w-[50%] h-[50%] bg-purple-900/30 rounded-full blur-[120px]"></div>
                <div className="absolute bottom-[-10%] right-[-10%] w-[50%] h-[50%] bg-blue-900/30 rounded-full blur-[120px]"></div>
                <div className="absolute top-[30%] left-[30%] w-[40%] h-[40%] bg-indigo-900/20 rounded-full blur-[100px]"></div>
            </div>

            {/* Navbar */}
            <nav className="relative z-10 border-b border-white/5 bg-black/50 backdrop-blur-md">
                <div className="container mx-auto px-6 h-20 flex items-center justify-between">
                    <div className="flex items-center gap-3">
                        <div className="h-10 w-10 rounded-xl bg-gradient-to-br from-purple-600 to-blue-600 flex items-center justify-center shadow-lg shadow-purple-500/20">
                            <span className="text-xl font-bold text-white">M</span>
                        </div>
                        <span className="text-xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-white to-gray-400">
                            Mediconyx
                        </span>
                    </div>
                    <div className="flex items-center gap-6">
                        <a href="/" className="text-gray-400 hover:text-white transition-colors text-sm">Home</a>
                        <a href="/login" className="text-gray-400 hover:text-white transition-colors text-sm">Login</a>
                        <a
                            href="/contact"
                            className="px-4 py-2 rounded-lg bg-white/10 text-white text-sm font-medium hover:bg-white/20 transition-all border border-white/5"
                        >
                            Get Started
                        </a>
                    </div>
                </div>
            </nav>

            {/* Hero Content */}
            <main className="relative z-10 container mx-auto px-6 py-16 lg:py-24">
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-16 items-center">

                    {/* Left Column: Text */}
                    <div className="space-y-8">
                        <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-purple-500/10 border border-purple-500/20">
                            <span className="flex h-2 w-2 rounded-full bg-purple-500 animate-pulse"></span>
                            <span className="text-sm font-medium text-purple-300">Accepting New Partners</span>
                        </div>

                        <h1 className="text-5xl lg:text-7xl font-bold leading-tight">
                            Transform Your <br />
                            <span className="bg-clip-text text-transparent bg-gradient-to-r from-purple-400 via-blue-400 to-emerald-400">
                                Healthcare Facility
                            </span>
                        </h1>

                        <p className="text-xl text-gray-400 max-w-lg leading-relaxed">
                            Join the world's most advanced medical network. Streamline operations, enhance patient care, and secure your data with next-gen blockchain technology.
                        </p>

                        <div className="space-y-4 pt-4">
                            <div className="flex items-center gap-4">
                                <div className="h-10 w-10 rounded-full bg-emerald-500/10 flex items-center justify-center">
                                    <svg className="h-6 w-6 text-emerald-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                                    </svg>
                                </div>
                                <div>
                                    <p className="font-semibold text-white">Instant Deployment</p>
                                    <p className="text-sm text-gray-400">Get up and running in days, not months.</p>
                                </div>
                            </div>
                            <div className="flex items-center gap-4">
                                <div className="h-10 w-10 rounded-full bg-blue-500/10 flex items-center justify-center">
                                    <svg className="h-6 w-6 text-blue-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
                                    </svg>
                                </div>
                                <div>
                                    <p className="font-semibold text-white">AI-Powered Insights</p>
                                    <p className="text-sm text-gray-400">Analytics that drive decision making.</p>
                                </div>
                            </div>
                            <div className="flex items-center gap-4">
                                <div className="h-10 w-10 rounded-full bg-purple-500/10 flex items-center justify-center">
                                    <svg className="h-6 w-6 text-purple-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                                    </svg>
                                </div>
                                <div>
                                    <p className="font-semibold text-white">Blockchain Secured</p>
                                    <p className="text-sm text-gray-400">Immutable records and transparent auditing.</p>
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* Right Column: Form */}
                    <div className="lg:pl-10">
                        <OnboardingForm />
                    </div>
                </div>
            </main>

            {/* Footer */}
            <footer className="relative z-10 border-t border-white/5 bg-black/50 py-12 mt-20">
                <div className="container mx-auto px-6 text-center">
                    <p className="text-gray-500 text-sm">
                        &copy; {new Date().getFullYear()} Mediconyx Inc. All rights reserved.
                    </p>
                </div>
            </footer>
        </div>
    );
}
