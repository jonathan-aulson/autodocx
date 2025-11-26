import { cn } from '@/lib/utils';

interface SkeletonProps extends React.HTMLAttributes<HTMLDivElement> {
  pulsing?: boolean;
}

function Skeleton({ className, pulsing = true, ...props }: SkeletonProps) {
  return (
    <div
      className={cn(
        pulsing ? 'animate-pulse' : '',
        'rounded-md bg-primary/10',
        className
      )}
      {...props}
    />
  );
}

export { Skeleton };
