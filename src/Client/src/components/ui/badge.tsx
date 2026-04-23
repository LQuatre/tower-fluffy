import * as React from "react"
import { cn } from "@/lib/utils"

export interface BadgeProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: 'default' | 'secondary' | 'destructive' | 'outline' | 'neon' | 'neon-pink'
}

function Badge({ className, variant = 'default', ...props }: BadgeProps) {
  const variants = {
    default: "border-transparent bg-primary text-background hover:bg-primary/80",
    secondary: "border-transparent bg-muted text-foreground hover:bg-muted/80",
    destructive: "border-transparent bg-red-500 text-white hover:bg-red-600",
    outline: "text-foreground border border-white/20",
    neon: "border-primary text-primary bg-primary/10",
    'neon-pink': "border-secondary text-secondary bg-secondary/10",
  }

  return (
    <div 
      className={cn(
        "inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-primary focus:ring-offset-2 uppercase tracking-tighter", 
        variants[variant],
        className
      )} 
      {...props} 
    />
  )
}

export { Badge }
