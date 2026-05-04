export type IndexingStatus =
  | { state: "idle" }
  | { state: "indexing"; progress: string }
  | { state: "ready"; }
  | { state: "error"; message: string };

  export type Message = {
    role: "user" | "assistant";
    content: string;
  };

  export type ToastState = {
    title: string;
    description: string;
    variant: "success" | "error";
  };

