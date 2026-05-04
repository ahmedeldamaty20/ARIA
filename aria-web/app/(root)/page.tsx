'use client'

import RepoInput from '@/components/RepoInput'
import { useChat } from '@/hooks/useChat';
import { heroPills, onboardingSteps } from '@/lib/constants';
import { ToastState } from '@/types';
import { toast } from 'sonner'

export default function Home() {

  const {
    repoUrl,
    setRepoUrl,
    status,
    indexRepo,
  } = useChat();

  const showToast = (props: ToastState) => {
    if (props.variant === 'success') {
      toast.success(props.title, {
        description: props.description
      });
    } else {
      toast.error(props.title, {
        description: props.description
      });
    }
  }

  return (
    <div className="py-20 home-shell">
      <main className="wrapper home-main">
        <div className="home-layout">
            <section className="space-y-5">
              <span className="home-kicker">Repository intelligence</span>
              <div className="space-y-4">
                <h1 className="home-title">
                  Index a GitHub repo, then ask questions that stay grounded in the code.
                </h1>
                <p className="home-copy">
                  Paste a public repository URL, let ARIA build the index, and keep the workflow focused on answers instead of setup noise.
                </p>
              </div>

              <div className="flex flex-wrap gap-3">
                {heroPills.map((pill) => (
                  <span key={pill} className="home-pill">
                    {pill}
                  </span>
                ))}
              </div>
            </section>

            <aside className="grid gap-3 sm:grid-cols-3 lg:grid-cols-1">
              {onboardingSteps.map((step) => (
                <div key={step.number} className="home-step-card">
                  <p className="text-xs uppercase tracking-[0.24em] text-slate-400">{step.number}</p>
                  <p className="mt-2 text-sm font-medium text-white">{step.title}</p>
                  <p className="mt-1 text-sm text-slate-400">{step.description}</p>
                </div>
              ))}
            </aside>

          <section className="home-panel">
            <RepoInput
              repoUrl={repoUrl}
              onChange={setRepoUrl}
              onIndex={indexRepo}
              status={status}
              onToast={showToast}
            />
          </section>
        </div>
      </main>
    </div>
  )
}
