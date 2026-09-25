import * as React from 'react';
import { Check, Minus } from 'lucide-react';
import { cn } from '@/lib/utils';

export interface CheckboxProps
  extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type' | 'onChange'> {
  checked?: boolean;
  indeterminate?: boolean;
  onCheckedChange?: (checked: boolean) => void;
}

const Checkbox = React.forwardRef<HTMLInputElement, CheckboxProps>(
  (
    {
      className,
      checked = false,
      indeterminate = false,
      onCheckedChange,
      disabled,
      ...props
    },
    ref
  ) => {
    const inputRef = React.useRef<HTMLInputElement>(null);

    React.useImperativeHandle(ref, () => inputRef.current as HTMLInputElement);

    React.useEffect(() => {
      if (inputRef.current) {
        inputRef.current.indeterminate = indeterminate;
      }
    }, [indeterminate]);

    return (
      <label
        className={cn(
          'relative inline-flex items-center justify-center h-4 w-4 shrink-0 rounded border transition-colors cursor-pointer select-none ring-offset-background focus-within:ring-2 focus-within:ring-ring focus-within:ring-offset-1',
          checked || indeterminate
            ? 'bg-blue-600 border-blue-600 text-white dark:bg-blue-500 dark:border-blue-500'
            : 'border-input bg-background hover:border-foreground/50',
          disabled && 'cursor-not-allowed opacity-50',
          className
        )}
      >
        <input
          type="checkbox"
          ref={inputRef}
          checked={checked}
          disabled={disabled}
          onChange={(e) => onCheckedChange?.(e.target.checked)}
          className="sr-only"
          {...props}
        />
        {checked && <Check className="h-3 w-3 stroke-[3]" />}
        {!checked && indeterminate && <Minus className="h-3 w-3 stroke-[3]" />}
      </label>
    );
  }
);
Checkbox.displayName = 'Checkbox';

export { Checkbox };
