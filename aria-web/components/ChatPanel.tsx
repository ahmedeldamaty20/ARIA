"use client";

import { Bot, LoaderCircle, Send, Sparkles, UserRound } from "lucide-react";
import { Fragment, useEffect, useMemo, useRef, useState } from "react";

import type { Message } from "@/types";

type Props = {
  messages: Message[];
  isLoading: boolean;
  onSend: (question: string) => void | Promise<void>;
  disabled?: boolean;
};

const starterPrompts = [
  "What does this repository do?",
  "Show me the main API flow.",
  "Where is authentication handled?",
];

type MarkdownBlock =
  | { type: "heading"; level: 1 | 2 | 3 | 4 | 5 | 6; text: string }
  | { type: "paragraph"; text: string }
  | { type: "bulletList"; items: string[] }
  | { type: "numberedList"; items: string[] }
  | { type: "quote"; text: string }
  | { type: "code"; language: string; code: string }
  | { type: "divider" };

function renderInlineText(text: string) {
  const parts = text.split(/(`[^`]+`)/g).filter(Boolean);

  return parts.map((part, index) => {
    if (part.startsWith("`") && part.endsWith("`")) {
      return (
        <code
          key={`${part}-${index}`}
          className="markdown-code-inline"
        >
          {part.slice(1, -1)}
        </code>
      );
    }

    const boldParts = part.split(/(\*\*[^*]+\*\*)/g).filter(Boolean);

    return (
      <Fragment key={`${part}-${index}`}>
        {boldParts.map((boldPart, boldIndex) => {
          if (boldPart.startsWith("**") && boldPart.endsWith("**")) {
            return (
              <strong key={`${boldPart}-${boldIndex}`} className="font-semibold text-white">
                {boldPart.slice(2, -2)}
              </strong>
            );
          }

          return <Fragment key={`${boldPart}-${boldIndex}`}>{boldPart}</Fragment>;
        })}
      </Fragment>
    );
  });
}

function parseMarkdownBlocks(content: string): MarkdownBlock[] {
  const lines = content.replace(/\r\n/g, "\n").split("\n");
  const blocks: MarkdownBlock[] = [];
  let index = 0;

  while (index < lines.length) {
    const line = lines[index];
    const trimmed = line.trim();

    if (!trimmed) {
      index += 1;
      continue;
    }

    const codeFence = trimmed.match(/^```([^`]*)$/);
    if (codeFence) {
      const language = codeFence[1].trim();
      const codeLines: string[] = [];
      index += 1;

      while (index < lines.length && lines[index].trim() !== "```") {
        codeLines.push(lines[index]);
        index += 1;
      }

      if (index < lines.length && lines[index].trim() === "```") {
        index += 1;
      }

      blocks.push({ type: "code", language, code: codeLines.join("\n") });
      continue;
    }

    const heading = trimmed.match(/^(#{1,6})\s+(.*)$/);
    if (heading) {
      blocks.push({
        type: "heading",
        level: heading[1].length as 1 | 2 | 3 | 4 | 5 | 6,
        text: heading[2],
      });
      index += 1;
      continue;
    }

    if (/^([-*_])\1\1+$/.test(trimmed)) {
      blocks.push({ type: "divider" });
      index += 1;
      continue;
    }

    if (trimmed.startsWith(">")) {
      const quoteLines: string[] = [];

      while (index < lines.length && lines[index].trim().startsWith(">")) {
        quoteLines.push(lines[index].trim().replace(/^>\s?/, ""));
        index += 1;
      }

      blocks.push({ type: "quote", text: quoteLines.join(" ") });
      continue;
    }

    const bulletMatch = trimmed.match(/^[-*•]\s+(.*)$/);
    if (bulletMatch) {
      const items: string[] = [];

      while (index < lines.length) {
        const current = lines[index].trim();
        const match = current.match(/^[-*•]\s+(.*)$/);
        if (!match) break;
        items.push(match[1]);
        index += 1;
      }

      blocks.push({ type: "bulletList", items });
      continue;
    }

    const numberedMatch = trimmed.match(/^\d+[.)]\s+(.*)$/);
    if (numberedMatch) {
      const items: string[] = [];

      while (index < lines.length) {
        const current = lines[index].trim();
        const match = current.match(/^\d+[.)]\s+(.*)$/);
        if (!match) break;
        items.push(match[1]);
        index += 1;
      }

      blocks.push({ type: "numberedList", items });
      continue;
    }

    const paragraphLines: string[] = [];

    while (index < lines.length) {
      const current = lines[index];
      const currentTrimmed = current.trim();
      if (!currentTrimmed) break;

      const startsBlock =
        currentTrimmed.startsWith("#") ||
        currentTrimmed.startsWith(">") ||
        currentTrimmed.startsWith("```") ||
        /^([-*_])\1\1+$/.test(currentTrimmed) ||
        /^[-*•]\s+/.test(currentTrimmed) ||
        /^\d+[.)]\s+/.test(currentTrimmed);

      if (startsBlock && paragraphLines.length > 0) break;
      if (startsBlock && paragraphLines.length === 0) break;

      paragraphLines.push(current);
      index += 1;
    }

    if (paragraphLines.length > 0) {
      blocks.push({ type: "paragraph", text: paragraphLines.join(" ").replace(/\s+/g, " ").trim() });
      continue;
    }

    index += 1;
  }

  return blocks;
}

function MarkdownContent({ content }: { content: string }) {
  const blocks = parseMarkdownBlocks(content);

  return (
    <div className="markdown-content">
      {blocks.map((block, index) => {
        if (block.type === "heading") {
          const HeadingTag = `h${block.level}` as const;
          const sizeClasses: Record<typeof block.level, string> = {
            1: "text-2xl",
            2: "text-xl",
            3: "text-lg",
            4: "text-base",
            5: "text-sm",
            6: "text-sm",
          };

          return (
            <HeadingTag key={index} className={`markdown-heading ${sizeClasses[block.level]}`}>
              {renderInlineText(block.text)}
            </HeadingTag>
          );
        }

        if (block.type === "paragraph") {
          return <p key={index}>{renderInlineText(block.text)}</p>;
        }

        if (block.type === "quote") {
          return (
            <blockquote
              key={index}
              className="markdown-blockquote"
            >
              {renderInlineText(block.text)}
            </blockquote>
          );
        }

        if (block.type === "divider") {
          return <hr key={index} className="markdown-divider" />;
        }

        if (block.type === "code") {
          return (
            <div key={index} className="markdown-code-block">
              {block.language ? (
                <div className="markdown-code-language">
                  {block.language}
                </div>
              ) : null}
              <pre className="markdown-code">
                <code>{block.code || " "}</code>
              </pre>
            </div>
          );
        }

        if (block.type === "bulletList") {
          return (
            <ul key={index} className="markdown-list">
              {block.items.map((item, itemIndex) => (
                <li key={itemIndex} className="markdown-list-item">
                  {renderInlineText(item)}
                </li>
              ))}
            </ul>
          );
        }

        return (
          <ol key={index} className="markdown-list">
            {block.items.map((item, itemIndex) => (
              <li key={itemIndex} className="list-decimal">
                {renderInlineText(item)}
              </li>
            ))}
          </ol>
        );
      })}
    </div>
  );
}

export function ChatPanel({ messages, isLoading, onSend, disabled = false }: Props) {
  const [draft, setDraft] = useState("");
  const scrollRef = useRef<HTMLDivElement | null>(null);

  const canSend = !disabled && !isLoading && draft.trim().length > 0;
  const messageCount = messages.length;

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight, behavior: "smooth" });
  }, [messages, isLoading]);

  const emptyState = useMemo(() => {
    if (messageCount > 0) return null;

    return (
      <div className="chat-empty-state">
        <div className="chat-empty-icon">
          <Sparkles className="h-6 w-6" />
        </div>
        <h2 className="chat-empty-title">Grounded repo chat</h2>
        <p className="chat-empty-description">
          Index a public GitHub repository, then ask for explanations, entry points, or file-level details. Answers can link directly into the code viewer.
        </p>

        <div className="chat-starter-prompts">
          {starterPrompts.map((prompt) => (
            <button
              key={prompt}
              type="button"
              onClick={() => setDraft(prompt)}
              className="chat-prompt-button"
            >
              {prompt}
            </button>
          ))}
        </div>
      </div>
    );
  }, [messageCount]);

  const submit = async () => {
    const question = draft.trim();
    if (!question || !canSend) return;

    setDraft("");
    await onSend(question);
  };

  return (
    <section className="chat-panel p-2">
      <header className="chat-panel-header">
        <div>
          <p className="chat-panel-title">Chat panel</p>
          <h2 className="chat-panel-subtitle">Ask questions about the indexed repository</h2>
        </div>

        <div className="chat-panel-status">
          {isLoading ? <LoaderCircle className="h-3.5 w-3.5 animate-spin text-cyan-300" /> : <Bot className="h-3.5 w-3.5 text-cyan-300" />}
          {isLoading ? "Thinking" : `${messageCount} messages`}
        </div>
      </header>

      <div ref={scrollRef} className="chat-messages">
        {emptyState}

        {messages.map((message, index) => {
          const isUser = message.role === "user";

          return (
            <article key={`${message.role}-${index}`} className={`chat-message ${isUser ? "chat-message-user" : "chat-message-ai"}`}>
              {!isUser && (
                <div className={`chat-message-avatar ${!isUser ? "chat-message-avatar-ai" : ""}`}>
                  <Bot className="h-4.5 w-4.5" />
                </div>
              )}

              <div className={`chat-message-bubble ${isUser ? "chat-message-bubble-user" : "chat-message-bubble-ai"}`}>
                <div className="chat-message-header">
                  {isUser ? <UserRound className="h-3.5 w-3.5" /> : <Sparkles className="h-3.5 w-3.5 text-cyan-300" />}
                  <span>{isUser ? "You" : "Assistant"}</span>
                </div>

                <div className="chat-message-content">
                  {isUser ? (
                    <p className="whitespace-pre-wrap text-sm leading-7 text-inherit">{message.content}</p>
                  ) : (
                    <MarkdownContent content={message.content} />
                  )}
                </div>

              </div>

              {isUser && (
                <div className={`chat-message-avatar ${isUser ? "chat-message-avatar-user" : ""}`}>
                  <UserRound className="h-4.5 w-4.5" />
                </div>
              )}
            </article>
          );
        })}
      </div>

      <form
        className="chat-form"
        onSubmit={(event) => {
          event.preventDefault();
          void submit();
        }}
      >
        <div className="chat-input-container">
          <textarea
            value={draft}
            onChange={(event) => setDraft(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter" && !event.shiftKey) {
                event.preventDefault();
                void submit();
              }
            }}
            placeholder={disabled ? "Index a repository to start asking questions." : "Ask about functions, files, or architecture..."}
            rows={3}
            disabled={disabled || isLoading}
            className="chat-textarea"
          />

          <div className="chat-form-footer">
            <p className="chat-form-hint">
              Shift+Enter adds a new line. Enter sends the message.
            </p>

            <button type="submit" disabled={!canSend} className="chat-send-button">
              {isLoading ? <LoaderCircle className="h-4 w-4 animate-spin" /> : <Send className="h-4 w-4" />}
              {isLoading ? "Sending" : "Send"}
            </button>
          </div>
        </div>
      </form>
    </section>
  );
}