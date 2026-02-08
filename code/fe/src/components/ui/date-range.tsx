interface DateRangeProps {
  dateFrom: string;
  dateTo: string;
}

export function DateRange({ dateFrom, dateTo }: DateRangeProps) {
  const from = new Date(dateFrom);
  const to = new Date(dateTo);

  const formatDate = (date: Date): string => {
    return date.toLocaleDateString("en-US", {
      month: "short",
      day: "numeric",
      year: "numeric",
    });
  };

  // If same day, show single date
  if (from.toDateString() === to.toDateString()) {
    return <span>{formatDate(from)}</span>;
  }

  // If same year, don't repeat the year
  if (from.getFullYear() === to.getFullYear()) {
    const fromFormatted = from.toLocaleDateString("en-US", {
      month: "short",
      day: "numeric",
    });
    const toFormatted = formatDate(to);
    return <span>{fromFormatted} - {toFormatted}</span>;
  }

  // Different years, show full dates
  return <span>{formatDate(from)} - {formatDate(to)}</span>;
}
