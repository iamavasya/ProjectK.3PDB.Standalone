export interface ParticipantHistory {
  participantHistoryKey: string;
  participantKey: string;
  propertyName: string;
  oldValue: string | null;
  newValue: string | null;
  changedAt: string; 
}

export interface QuarterlyProbeReportItem {
  participantHistoryKey: string | null;
  participantKey: string;
  fullName: string;
  kurin: number | null;
  probeOpenDate: string | null;
  changedAt: string;
  action: 'opened' | 'archived' | 'unarchived';
  oldValue: string | null;
  newValue: string | null;
}

export interface QuarterlyProbeTotalsItem {
  quarter: 1 | 2 | 3 | 4;
  openedTotal: number;
  openedInQuarter: number;
  archivedInQuarter: number;
}

export interface Participant {
  participantKey: string;
  fullName: string;
  kurin: number | null;
  email: string;
  phone: string;
  
  
  isProbeOpen: boolean;
  isMotivationLetterWritten: boolean;
  isFormFilled: boolean;
  isProbeContinued: boolean;
  isProbeFrozen: boolean;
  isSelfReflectionSubmitted: boolean;
  isArchived: boolean;
  
  
  probeOpenDate: string | null;
  birthDate: string | null;
  
  notes: string;

  daysToProbeEnd?: number;
  
  
  history?: ParticipantHistory[];
}