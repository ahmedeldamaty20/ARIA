'use client'

import { IndexingStatus, ToastState } from '@/types';
import { GitFork, LoaderCircle, Sparkles } from 'lucide-react';
import { KeyboardEvent, useEffect, useRef } from 'react'
import { useRouter } from "next/navigation";

type Props = {
  repoUrl: string;
  onChange: (val: string) => void;
  onIndex: () => Promise<boolean> | boolean;
  status: IndexingStatus;
  onToast?: (toast: ToastState) => void;
};

export default function RepoInput({ repoUrl, onChange, onIndex, status, onToast }: Props) {
  const isIndexing = status.state === "indexing";
  const previousState = useRef(status.state);
  const router = useRouter()

  useEffect(() => {
    if (!onToast || previousState.current === status.state) {
      previousState.current = status.state;
      return;
    }

    previousState.current = status.state;

    if (status.state === "error") {
      onToast({
        title: "Indexing failed",
        description: status.message,
        variant: "error",
      });
    }

    if (status.state === "ready") {
      onToast({
        title: "Repository indexed",
        description: "You can start asking grounded questions now.",
        variant: "success",
      });
    }
  }, [onToast, status.state, status]);

  async function onIndexClick() {
    if (!isIndexing && repoUrl.trim()) {
      // check if the repo url is a valid GitHub repository
      const githubRepoPattern = /^https:\/\/github\.com\/[\w.-]+\/[\w.-]+\/?$/;
      if (!githubRepoPattern.test(repoUrl)) {
        onToast?.({
          title: "Invalid URL",
          description: "Please enter a valid GitHub repository URL.",
          variant: "error",
        });
        return;
      }

      // check if repo is already indexed
      if (status.state === "ready") {
        router.push("/chat");
        return;
      }

      const didIndex = await onIndex();

      if (didIndex) {
        router.push("/chat");
      }
    }
  }

  const handleKey = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter") {
      e.preventDefault();
      if (!isIndexing && repoUrl.trim()) {
        onIndexClick();
      }
    }
  };

  return (
    <div className="repo-shell">
      <div className="repo-header">
        <div className="repo-header-content">
          <div className="repo-header-icon">
            <GitFork className="h-5 w-5" />
          </div>
          <div>
            <p className="repo-header-title">Repository URL</p>
            <p className="repo-header-subtitle">Paste a public GitHub repo and index it in one click.</p>
          </div>
        </div>
        <div className="repo-badge">
          <Sparkles className="h-3.5 w-3.5" />
          AI-ready
        </div>
      </div>

      <div className="repo-content">
        <div className="repo-controls">
          <div className="repo-input-wrap">
            <div className="repo-input-icon">
              <GitFork className="h-4 w-4" />
            </div>
            <input
              type="text"
              value={repoUrl}
              onChange={(e) => onChange(e.target.value)}
              onKeyDown={handleKey}
              placeholder="https://github.com/owner/repo"
              disabled={isIndexing}
              className="repo-input"
            />
          </div>

          <button onClick={onIndexClick} disabled={isIndexing || !repoUrl.trim()} className="repo-button">
            {isIndexing ? <LoaderCircle className="h-4 w-4 animate-spin" /> : <Sparkles className="h-4 w-4" />}
            {isIndexing ? "Indexing..." : "Index Repo"}
          </button>
        </div>

        {isIndexing && (
          <div className="repo-progress">
            <div className="repo-progress-track">
              <div className="repo-progress-fill" />
            </div>
            <span className="repo-progress-label">{status.progress}</span>
          </div>
        )}

        <div className="repo-chips">
          <span className="repo-chip">Private repos not supported</span>
          <span className="repo-chip">Index once, ask many</span>
          <span className="repo-chip">File-linked answers</span>
        </div>
      </div>


    </div>
  )
}
