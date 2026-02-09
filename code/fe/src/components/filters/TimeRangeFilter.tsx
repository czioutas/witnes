'use client';

import { useState } from 'react';
import { Calendar, Clock } from 'lucide-react';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

export type TimeRangePreset = 'last_24h' | 'last_7d' | 'last_30d' | 'custom' | 'around' | 'all';

export interface TimeRange {
  preset: TimeRangePreset;
  startDate?: Date;
  endDate?: Date;
  aroundTime?: Date; // For "around" feature
}

interface TimeRangeFilterProps {
  value: TimeRange;
  onChange: (value: TimeRange) => void;
}

const getPresetLabel = (preset: TimeRangePreset, range?: TimeRange): string => {
  switch (preset) {
    case 'last_24h':
      return 'Last 24 Hours';
    case 'last_7d':
      return 'Last 7 Days';
    case 'last_30d':
      return 'Last 30 Days';
    case 'all':
      return 'All Time';
    case 'around':
      if (range?.aroundTime) {
        return `Around ${range.aroundTime.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })}`;
      }
      return 'Around Time';
    case 'custom':
      if (range?.startDate || range?.endDate) {
        const start = range.startDate?.toLocaleDateString() || 'Start';
        const end = range.endDate?.toLocaleDateString() || 'End';
        return `${start} - ${end}`;
      }
      return 'Custom Range';
    default:
      return 'Select Time Range';
  }
};

export function TimeRangeFilter({ value, onChange }: TimeRangeFilterProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [customStart, setCustomStart] = useState(
    value.startDate?.toISOString().slice(0, 16) || ''
  );
  const [customEnd, setCustomEnd] = useState(
    value.endDate?.toISOString().slice(0, 16) || ''
  );
  const [aroundTime, setAroundTime] = useState(
    value.aroundTime?.toISOString().slice(0, 16) || ''
  );

  const handlePresetChange = (preset: TimeRangePreset) => {
    const now = new Date();
    let newValue: TimeRange = { preset };

    switch (preset) {
      case 'last_24h':
        newValue.startDate = new Date(now.getTime() - 24 * 60 * 60 * 1000);
        newValue.endDate = now;
        break;
      case 'last_7d':
        newValue.startDate = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
        newValue.endDate = now;
        break;
      case 'last_30d':
        newValue.startDate = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
        newValue.endDate = now;
        break;
      case 'all':
        // No date range for "all time"
        break;
      default:
        return; // For custom and around, don't close popover
    }

    onChange(newValue);
    setIsOpen(false);
  };

  const handleCustomApply = () => {
    onChange({
      preset: 'custom',
      startDate: customStart ? new Date(customStart) : undefined,
      endDate: customEnd ? new Date(customEnd) : undefined,
    });
    setIsOpen(false);
  };

  const handleAroundApply = () => {
    if (!aroundTime) return;

    const centerTime = new Date(aroundTime);
    // Create ±15 minute window
    const startDate = new Date(centerTime.getTime() - 15 * 60 * 1000);
    const endDate = new Date(centerTime.getTime() + 15 * 60 * 1000);

    onChange({
      preset: 'around',
      aroundTime: centerTime,
      startDate,
      endDate,
    });
    setIsOpen(false);
  };

  return (
    <Popover open={isOpen} onOpenChange={setIsOpen}>
      <PopoverTrigger asChild>
        <Button variant="outline" className="justify-start gap-2">
          <Calendar className="h-4 w-4" />
          <span>{getPresetLabel(value.preset, value)}</span>
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-[320px] p-3" align="end">
        <div className="space-y-2">
          <div className="text-sm font-medium mb-3">Select Time Range</div>

          {/* Preset options */}
          <button
            onClick={() => handlePresetChange('last_24h')}
            className={`w-full text-left px-3 py-2 text-sm rounded-md hover:bg-accent transition-colors ${
              value.preset === 'last_24h' ? 'bg-accent' : ''
            }`}
          >
            Last 24 Hours
          </button>

          <button
            onClick={() => handlePresetChange('last_7d')}
            className={`w-full text-left px-3 py-2 text-sm rounded-md hover:bg-accent transition-colors ${
              value.preset === 'last_7d' ? 'bg-accent' : ''
            }`}
          >
            Last 7 Days
          </button>

          <button
            onClick={() => handlePresetChange('last_30d')}
            className={`w-full text-left px-3 py-2 text-sm rounded-md hover:bg-accent transition-colors ${
              value.preset === 'last_30d' ? 'bg-accent' : ''
            }`}
          >
            Last 30 Days
          </button>

          <button
            onClick={() => handlePresetChange('all')}
            className={`w-full text-left px-3 py-2 text-sm rounded-md hover:bg-accent transition-colors ${
              value.preset === 'all' ? 'bg-accent' : ''
            }`}
          >
            All Time
          </button>

          {/* Around Time Section */}
          <div className="border-t pt-3 mt-3">
            <div className="text-sm font-medium mb-2 flex items-center gap-2">
              <Clock className="h-4 w-4" />
              Around Time (±15 min)
            </div>
            <div className="space-y-2">
              <Input
                type="datetime-local"
                value={aroundTime}
                onChange={(e) => setAroundTime(e.target.value)}
                className="w-full"
              />
              <Button
                size="sm"
                className="w-full"
                onClick={handleAroundApply}
                disabled={!aroundTime}
              >
                Apply
              </Button>
            </div>
          </div>

          {/* Custom Range Section */}
          <div className="border-t pt-3 mt-3">
            <div className="text-sm font-medium mb-2">Custom Range</div>
            <div className="space-y-2">
              <div>
                <Label htmlFor="start-date" className="text-xs">Start Date</Label>
                <Input
                  id="start-date"
                  type="datetime-local"
                  value={customStart}
                  onChange={(e) => setCustomStart(e.target.value)}
                  className="w-full mt-1"
                />
              </div>
              <div>
                <Label htmlFor="end-date" className="text-xs">End Date</Label>
                <Input
                  id="end-date"
                  type="datetime-local"
                  value={customEnd}
                  onChange={(e) => setCustomEnd(e.target.value)}
                  className="w-full mt-1"
                />
              </div>
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
