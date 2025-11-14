'use client'
import { useState } from 'react'
import Link from 'next/link'

export default function Navigation() {
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)

  return (
    <nav className="bg-white shadow-md sticky top-0 z-50">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-20">
          <div className="flex items-center">
            <Link href="/" className="text-3xl font-bold text-blue-600">Mediconyx</Link>
          </div>
          <div className="hidden md:flex items-center space-x-8">
            <a href="#home" className="text-gray-700 hover:text-blue-600 font-medium transition">Home</a>
            <div className="relative group">
              <button className="text-gray-700 hover:text-blue-600 font-medium transition flex items-center gap-1">
                About Us
                <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                </svg>
              </button>
              <div className="absolute left-0 mt-2 w-48 bg-white shadow-lg rounded-md opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all">
                <a href="#about" className="block px-4 py-2 text-gray-700 hover:bg-blue-50">Our Story</a>
                <a href="#team" className="block px-4 py-2 text-gray-700 hover:bg-blue-50">Our Team</a>
              </div>
            </div>
            <div className="relative group">
              <button className="text-gray-700 hover:text-blue-600 font-medium transition flex items-center gap-1">
                Services
                <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                </svg>
              </button>
              <div className="absolute left-0 mt-2 w-48 bg-white shadow-lg rounded-md opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all">
                <a href="#services" className="block px-4 py-2 text-gray-700 hover:bg-blue-50">All Services</a>
                <a href="#features" className="block px-4 py-2 text-gray-700 hover:bg-blue-50">Key Features</a>
              </div>
            </div>
            <a href="#testimonials" className="text-gray-700 hover:text-blue-600 font-medium transition">Testimonials</a>
            <a href="#news" className="text-gray-700 hover:text-blue-600 font-medium transition">News</a>
            <a href="#contact" className="text-gray-700 hover:text-blue-600 font-medium transition">Contact</a>
            <a href="/login" className="bg-blue-600 text-white px-6 py-2 rounded-md hover:bg-blue-700 transition font-medium">
              SIGN IN
            </a>
          </div>
          <button onClick={() => setMobileMenuOpen(!mobileMenuOpen)} className="md:hidden text-gray-700">
            <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </button>
        </div>
      </div>
      {mobileMenuOpen && (
        <div className="md:hidden bg-white border-t">
          <a href="#home" className="block px-4 py-2 text-gray-700 hover:bg-blue-50">Home</a>
          <a href="#about" className="block px-4 py-2 text-gray-700 hover:bg-blue-50">About Us</a>
          <a href="#services" className="block px-4 py-2 text-gray-700 hover:bg-blue-50">Services</a>
          <a href="#testimonials" className="block px-4 py-2 text-gray-700 hover:bg-blue-50">Testimonials</a>
          <a href="#news" className="block px-4 py-2 text-gray-700 hover:bg-blue-50">News</a>
          <a href="#contact" className="block px-4 py-2 text-gray-700 hover:bg-blue-50">Contact</a>
          <a href="/login" className="block px-4 py-2 text-blue-600 font-semibold">SIGN IN</a>
        </div>
      )}
    </nav>
  )
}
