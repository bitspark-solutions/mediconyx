export default function HeroSection() {
  return (
    <section id="home" className="relative bg-gradient-to-br from-blue-600 via-blue-700 to-indigo-800 text-white py-32">
      <div className="absolute inset-0 bg-black opacity-20"></div>
      <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
        <h1 className="text-6xl md:text-7xl font-bold mb-6 leading-tight">
          Healthy Heart,<br />Healthy Family
        </h1>
        <p className="text-2xl md:text-3xl mb-4 text-blue-100 font-light">Inspiring Better Health</p>
        <p className="text-lg md:text-xl mb-12 max-w-4xl mx-auto text-blue-50">
          Globally harness multimedia based collaboration and idea-sharing with backend products. Continually whiteboard superior opportunities via covalent scenarios.
        </p>
        <div className="flex flex-col sm:flex-row gap-4 justify-center">
          <a href="#about" className="bg-white text-blue-600 px-10 py-4 rounded-md text-lg font-semibold hover:bg-blue-50 transition shadow-lg">
            DISCOVER MORE
          </a>
          <a href="#services" className="bg-green-600 text-white px-10 py-4 rounded-md text-lg font-semibold hover:bg-green-700 transition shadow-lg">
            VIEW OUR SERVICES
          </a>
        </div>
      </div>
    </section>
  )
}
