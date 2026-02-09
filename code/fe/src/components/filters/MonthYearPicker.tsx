'use client';

import { useState, useMemo } from 'react';
import { Calendar } from 'lucide-react';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';
import { Button } from '@/components/ui/button';

export type DateRangePreset = 'this_month' | 'last_month' | 'this_year' | 'last_year' | 'custom' | 'all';

export interface DateRange {
  preset: DateRangePreset;
  month?: number; // 1-12
  year?: number;
}

interface MonthYearPickerProps {
  value: DateRange;
  onChange: (value: DateRange) => void;
}

const MONTHS = [
  { value: 1, label: 'January' },
  { value: 2, label: 'February' },
  { value: 3, label: 'March' },
  { value: 4, label: 'April' },
  { value: 5, label: 'May' },
  { value: 6, label: 'June' },
  { value: 7, label: 'July' },
  { value: 8, label: 'August' },
  { value: 9, label: 'September' },
  { value: 10, label: 'October' },
  { value: 11, label: 'November' },
  { value: 12, label: 'December' },
];

const getPresetLabel = (preset: DateRangePreset, month?: number, year?: number): string => {
  switch (preset) {
    case 'this_month':
      return 'This Month';
    case 'last_month':
      return 'Last Month';
    case 'this_year':
      return 'This Year';
    case 'last_year':
      return 'Last Year';
    case 'all':
      return 'All Time';
    case 'custom':
      if (month && year) {
        const monthName = MONTHS.find(m => m.value === month)?.label || '';
        return `${monthName} ${year}`;
      }
      return 'Custom';
    default:
      return 'Select Period';
  }
};

export function MonthYearPicker({ value, onChange }: MonthYearPickerProps) {
  const [isOpen, setIsOpen] = useState(false);

  // Generate years list dynamically on client side
  const YEARS = useMemo(() => {
    const currentYear = new Date().getFullYear();
    return Array.from({ length: 10 }, (_, i) => currentYear - i);
  }, []);

  // Use lazy initialization to avoid SSR issues
  const [customMonth, setCustomMonth] = useState<number>(() =>
    value.month || (typeof window !== 'undefined' ? new Date().getMonth() + 1 : 1)
  );
  const [customYear, setCustomYear] = useState<number>(() =>
    value.year || (typeof window !== 'undefined' ? new Date().getFullYear() : 2024)
  );

  const handlePresetChange = (preset: DateRangePreset) => {
    if (preset === 'custom') {
      // Don't close the popover for custom selection
      return;
    }

    onChange({ preset });
    setIsOpen(false);
  };

  const handleCustomApply = () => {
    onChange({
      preset: 'custom',
      month: customMonth,
      year: customYear,
    });
    setIsOpen(false);
  };

  return (
    <Popover open={isOpen} onOpenChange={setIsOpen}>
      <PopoverTrigger asChild>
        <Button variant="outline" className="w-[200px] justify-start gap-2">
          <Calendar className="h-4 w-4" />
          <span>{getPresetLabel(value.preset, value.month, value.year)}</span>
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-[240px] p-3" align="start">
        <div className="space-y-2">
          <div className="text-sm font-medium mb-3">Select Period</div>

          {/* Preset options */}
          <button
            onClick={() => handlePresetChange('this_month')}
            className={`w-full text-left px-3 py-2 text-sm rounded-md hover:bg-accent transition-colors ${
              value.preset === 'this_month' ? 'bg-accent' : ''
            }`}
          >
            This Month
          </button>

          <button
            onClick={() => handlePresetChange('last_month')}
            className={`w-full text-left px-3 py-2 text-sm rounded-md hover:bg-accent transition-colors ${
              value.preset === 'last_month' ? 'bg-accent' : ''
            }`}
          >
            Last Month
          </button>

          <button
            onClick={() => handlePresetChange('this_year')}
            className={`w-full text-left px-3 py-2 text-sm rounded-md hover:bg-accent transition-colors ${
              value.preset === 'this_year' ? 'bg-accent' : ''
            }`}
          >
            This Year
          </button>

          <button
            onClick={() => handlePresetChange('last_year')}
            className={`w-full text-left px-3 py-2 text-sm rounded-md hover:bg-accent transition-colors ${
              value.preset === 'last_year' ? 'bg-accent' : ''
            }`}
          >
            Last Year
          </button>

          <button
            onClick={() => handlePresetChange('all')}
            className={`w-full text-left px-3 py-2 text-sm rounded-md hover:bg-accent transition-colors ${
              value.preset === 'all' ? 'bg-accent' : ''
            }`}
          >
            All Time
          </button>

          <div className="border-t pt-3 mt-3">
            <div className="text-sm font-medium mb-2">Custom</div>
            <div className="space-y-2">
              <Select
                value={customMonth.toString()}
                onValueChange={(val) => setCustomMonth(parseInt(val))}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Month" />
                </SelectTrigger>
                <SelectContent>
                  {MONTHS.map((month) => (
                    <SelectItem key={month.value} value={month.value.toString()}>
                      {month.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>

              <Select
                value={customYear.toString()}
                onValueChange={(val) => setCustomYear(parseInt(val))}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Year" />
                </SelectTrigger>
                <SelectContent>
                  {YEARS.map((year) => (
                    <SelectItem key={year} value={year.toString()}>
                      {year}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>

              <Button
                size="sm"
                className="w-full"
                onClick={handleCustomApply}
              >
                Apply
              </Button>
            </div>
          </div>
        </div>
      </PopoverContent>
    </Popover>
  );
}
