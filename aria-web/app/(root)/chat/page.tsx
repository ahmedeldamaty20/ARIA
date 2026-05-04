'use client';

import { ChatPanel } from '@/components/ChatPanel';
import { readStoredChatSession, useChat } from '@/hooks/useChat';
import { useRouter } from "next/navigation";
import { useEffect, useState } from 'react';

export default function Chat() {
  const router = useRouter();
  const [hasAccess, setHasAccess] = useState(false);

  const {
    messages,
    status,
    isLoading,
    isIndexed,
    sendMessage,
  } = useChat();

  useEffect(() => {
    const session = readStoredChatSession();

    if (!session?.isIndexed || !session.repoUrl.trim()) {
      router.replace("/");
      
      return;
    }

    setHasAccess(true);
  }, [router]);

  const isReady = status.state === "ready";

  if(!hasAccess) {
    return (
      <div className="chat-loading">
        <p className="chat-loading-text">Checking for existing chat session...</p>
      </div>
    );
  }

  return (
    <main className="chat-shell">
      <div className="chat-grid-overlay" />

      <div className="chat-container">
        <div className="min-h-0">
          <ChatPanel
            messages={messages}
            isLoading={isLoading}
            onSend={sendMessage}
            disabled={!isReady}
          />
        </div>
      </div>
    </main>
  );
}
