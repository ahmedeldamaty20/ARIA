'use client'

import RepoInput from '@/components/RepoInput'
import { useChat } from '@/hooks/useChat';
import { ToastState } from '@/types';
import { title } from 'process';
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
    <div className="wrapper py-20 home-shell">
      <RepoInput repoUrl={repoUrl} onChange={setRepoUrl} onIndex={indexRepo} status={status} onToast={showToast} />
    </div>
  )
}
