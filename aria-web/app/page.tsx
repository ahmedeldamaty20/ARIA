import React from 'react'

export default function Home() {
  return (
    <div className="flex flex-col h-screen bg-primary">

      {/* Navbar */}
      <nav className="flex items-center gap-2 px-4 py-3 border-b border-border shrink-0 bg-primary">
        <div className="w-2 h-2 rounded-full" />
        <span className="text-xl font-bold text-on-primary">ARIA - AI Repository Inspector & Assistant</span>
      </nav>
    </div>
  )
}
