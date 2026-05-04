"use client"

import { useTheme } from "next-themes"
import { Toaster as Sonner, type ToasterProps } from "sonner"
import { CircleCheckIcon, InfoIcon, TriangleAlertIcon, AlertOctagon, Loader2Icon } from "lucide-react"

const Toaster = ({ ...props }: ToasterProps) => {
  const { theme = "system" } = useTheme()

  return (
    <Sonner
      theme={theme as ToasterProps["theme"]}
      className="toaster"
      icons={{
        success: (
          <div className="flex items-center justify-center w-5 h-5 rounded-full bg-linear-to-br from-green-400/30 to-green-500/20">
            <CircleCheckIcon className="w-4 h-4 text-green-400" strokeWidth={3} />
          </div>
        ),
        info: (
          <div className="flex items-center justify-center w-5 h-5 rounded-full bg-linear-to-br from-cyan-400/30 to-cyan-500/20">
            <InfoIcon className="w-4 h-4 text-cyan-400" strokeWidth={3} />
          </div>
        ),
        warning: (
          <div className="flex items-center justify-center w-5 h-5 rounded-full bg-linear-to-br from-amber-400/30 to-amber-500/20">
            <TriangleAlertIcon className="w-4 h-4 text-amber-400" strokeWidth={3} />
          </div>
        ),
        error: (
          <div className="flex items-center justify-center w-5 h-5 rounded-full bg-linear-to-br from-red-400/30 to-red-500/20">
            <AlertOctagon className="w-4 h-4 text-red-400" strokeWidth={3} />
          </div>
        ),
        loading: (
          <div className="flex items-center justify-center w-5 h-5 rounded-full bg-linear-to-br from-cyan-400/30 to-cyan-500/20">
            <Loader2Icon className="w-4 h-4 text-cyan-400 animate-spin" strokeWidth={3} />
          </div>
        ),
      }}
      style={
        {
          "--normal-bg": "var(--toast-bg)",
          "--normal-text": "var(--toast-text)",
          "--normal-border": "var(--toast-border)",
          "--border-radius": "12px",
        } as React.CSSProperties
      }
      position="bottom-right"
      gap={12}
      richColors
      {...props}
    />
  )
}

export { Toaster }
