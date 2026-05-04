"use client";

import { IndexingStatus, Message } from "@/types";
import { useCallback, useEffect, useState } from "react";

const CHAT_SESSION_KEY = "aria:chat-session";

type StoredChatSession = {
  repoUrl: string;
  isIndexed: boolean;
  messages: Message[];
};

function readStoredChatSession(): StoredChatSession | null {
  if (typeof window === "undefined") return null;

  try {
    const raw = window.sessionStorage.getItem(CHAT_SESSION_KEY);
    if (!raw) return null;

    const parsed = JSON.parse(raw) as Partial<StoredChatSession>;
    if (
      typeof parsed.repoUrl !== "string" ||
      typeof parsed.isIndexed !== "boolean" ||
      !Array.isArray(parsed.messages)
    ) {
      return null;
    }

    return {
      repoUrl: parsed.repoUrl,
      isIndexed: parsed.isIndexed,
      messages: parsed.messages as Message[],
    };
  } catch {
    return null;
  }
}

function persistStoredChatSession(session: StoredChatSession | null) {
  if (typeof window === "undefined") return;

  if (!session) {
    window.sessionStorage.removeItem(CHAT_SESSION_KEY);
    return;
  }

  window.sessionStorage.setItem(CHAT_SESSION_KEY, JSON.stringify(session));
}

export function useChat() {
  const storedSession = readStoredChatSession();

  const [repoUrl, setRepoUrl] = useState(storedSession?.repoUrl ?? "");
  const [messages, setMessages] = useState<Message[]>(storedSession?.messages ?? []);
  const [status, setStatus] = useState<IndexingStatus>(
    storedSession?.isIndexed ? { state: "ready" } : { state: "idle" }
  );
  const [isLoading, setIsLoading] = useState(false);
  const [isIndexed, setIsIndexed] = useState(storedSession?.isIndexed ?? false);

  useEffect(() => {
    if (repoUrl.trim() || messages.length > 0 || isIndexed) {
      persistStoredChatSession({ repoUrl, isIndexed, messages });
      return;
    }

    persistStoredChatSession(null);
  }, [repoUrl, messages, isIndexed]);

  // indexRepo — It sends the initial question to start the indexing process
  const indexRepo = useCallback(async () => {
    if (!repoUrl.trim()) return false;

    setStatus({ state: "indexing", progress: "Starting indexing..." });
    setMessages([]);
    setIsIndexed(false);

    try {
      const res = await fetch("/api/chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          repo_url: repoUrl,
          question: "Index this repository",
          is_indexed: false,
        }),
      });

      const data = await res.json();

      if (!res.ok) throw new Error(data.error);

      setIsIndexed(data.is_indexed);
      const assistantMessage: Message = {
        role: "assistant",
        content: data.answer
      };

      setStatus({ state: "ready" });
      setMessages([assistantMessage]);
      persistStoredChatSession({
        repoUrl,
        isIndexed: true,
        messages: [assistantMessage],
      });
      return true;
    } catch (err) {
      setStatus({
        state: "error",
        message: err instanceof Error ? err.message : "Unknown error",
      });
      return false;
    }
  }, [repoUrl]);

  // sendMessage — It sends a message and adds the response to the messages
  const sendMessage = useCallback(
    async (question: string) => {
      if (!question.trim() || isLoading) return;

      const userMessage: Message = { role: "user", content: question };
      setMessages((prev) => [...prev, userMessage]);
      setIsLoading(true);

      try {
        const res = await fetch("/api/chat", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            repo_url: repoUrl,
            question,
            is_indexed: isIndexed,
          }),
        });

        const data = await res.json();
        if (!res.ok) throw new Error(data.error);

        setIsIndexed(data.is_indexed);

        const assistantMessage: Message = {
          role: "assistant",
          content: data.answer
        };

        setMessages((prev) => [...prev, assistantMessage]);
      } catch (err) {
        setMessages((prev) => [
          ...prev,
          {
            role: "assistant",
            content:
              err instanceof Error ? err.message : "Something went wrong.",
          },
        ]);
      } finally {
        setIsLoading(false);
      }
    },
    [repoUrl, isIndexed, isLoading]
  );

  return {
    repoUrl,
    setRepoUrl,
    messages,
    status,
    isLoading,
    indexRepo,
    isIndexed,
    sendMessage,
  };
}

export { readStoredChatSession };