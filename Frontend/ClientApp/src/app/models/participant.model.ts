export interface ParticipantHistory {
  id: number;
  participantId: number;
  propertyName: string;
  oldValue: string | null;
  newValue: string | null;
  changedAt: string; 
}

export interface Participant {
  id: number;
  fullName: string;
  kurin: number | null;
  email: string;
  phone: string;
  
  
  isProbeOpen: boolean;
  isMotivationLetterWritten: boolean;
  isFormFilled: boolean;
  isProbeContinued: boolean;
  isProbeFrozen: boolean;
  
  
  probeOpenDate: string | null;
  birthDate: string | null;
  
  notes: string;

  daysToProbeEnd?: number;
  
  
  history?: ParticipantHistory[];
}