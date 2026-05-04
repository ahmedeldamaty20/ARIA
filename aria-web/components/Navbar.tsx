import Image from 'next/image'
import Link from 'next/link'
import { GitFork } from 'lucide-react'

export default function Navbar() {
  return (
    <header className="w-full fixed z-50 bg-primary backdrop-blur-md border-b border-black/10">
      <div className="wrapper navbar-height py-4 flex justify-between items-center">
				<Link href="/" className="flex gap-0.5 items-center">
					<Image src="/assets/logo.png" alt="ARIA" width={42} height={26} />
					<span className="logo-text ml-2">ARIA - AI Repository Inspector & Assistant</span>
				</Link>
        <nav className="w-fit flex gap-7 items-center">
          <Link href="https://github.com/ahmedeldamaty20/ARIA" className="nav-link-base flex content-center items-center gap-1 text-black hover:opacity-70">
            <GitFork className="w-8 h-8 text-blue-500 relative z-10" />
            GitHub Repo Explorer
          </Link>
				</nav>
			</div>
    </header>
  )
}
