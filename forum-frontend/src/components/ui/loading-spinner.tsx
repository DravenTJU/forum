import { cn } from "@/lib/utils"

interface LoadingSpinnerProps {
  size?: "small" | "default" | "large"
  className?: string
}

export function LoadingSpinner({ size = "default", className }: LoadingSpinnerProps) {
  const sizeClasses = {
    small: "h-4 w-4",
    default: "h-8 w-8",
    large: "h-12 w-12",
  }

  return (
    <div className={cn("flex items-center justify-center", className)}>
      <div
        className={cn(
          "animate-spin rounded-full border-2 border-gray-300 border-t-blue-600",
          sizeClasses[size]
        )}
      />
    </div>
  )
}