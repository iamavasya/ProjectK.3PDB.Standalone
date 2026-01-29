/**
 * Returns a date string in YYYY-MM-DD format based on local time.
 * This avoids UTC timezone shifts during serialization.
 */
export function formatDateToISO(date: Date | string | null | undefined): string {
  if (!date) return '';

  const d = new Date(date);
  
  // Check for "Invalid Date"
  if (isNaN(d.getTime())) {
    console.error('formatDateToISO: Invalid date provided', date);
    return '';
  }

  // Quick fix: Using 'sv-SE' locale to force YYYY-MM-DD format.
  // This prevents the date from shifting to the previous day due to UTC/timezone 
  // conversion when sending the payload to the .NET backend.
  return d.toLocaleDateString('sv-SE');
}