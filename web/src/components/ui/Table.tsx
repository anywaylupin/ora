import type { ComponentProps } from 'react'

/**
 * Scrolls sideways inside its own box on narrow screens, so the page itself never scrolls horizontally.
 */
export function Table({ className = '', ...props }: ComponentProps<'table'>) {
  return (
    <div className="overflow-x-auto rounded-lg border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-900">
      <table className={`w-full border-collapse text-left text-sm ${className}`} {...props} />
    </div>
  )
}

export function TableHead(props: ComponentProps<'thead'>) {
  return (
    <thead
      className="border-b border-zinc-200 bg-zinc-50 text-xs font-medium tracking-wide text-zinc-500 uppercase dark:border-zinc-800 dark:bg-zinc-900/60 dark:text-zinc-400"
      {...props}
    />
  )
}

export function TableBody(props: ComponentProps<'tbody'>) {
  return <tbody className="divide-y divide-zinc-200 dark:divide-zinc-800" {...props} />
}

export function TableRow({ className = '', ...props }: ComponentProps<'tr'>) {
  return <tr className={className} {...props} />
}

export function TableHeader({ className = '', ...props }: ComponentProps<'th'>) {
  return <th scope="col" className={`px-3 py-2 font-medium ${className}`} {...props} />
}

export function TableCell({ className = '', ...props }: ComponentProps<'td'>) {
  return <td className={`px-3 py-2 align-middle ${className}`} {...props} />
}
