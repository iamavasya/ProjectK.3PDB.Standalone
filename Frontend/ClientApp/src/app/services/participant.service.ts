import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Participant, ParticipantHistory } from '../models/participant.model';
import { environment } from '../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ParticipantService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/participant`;

  getAll(): Observable<Participant[]> {
    return this.http.get<Participant[]>(this.apiUrl);
  }

  getByKey(participantKey: string): Observable<Participant> {
    return this.http.get<Participant>(`${this.apiUrl}/${participantKey}`);
  }
  
  getHistory(participantKey: string): Observable<ParticipantHistory[]> {
    return this.http.get<ParticipantHistory[]>(`${this.apiUrl}/${participantKey}/history`);
  }

  create(participant: Participant): Observable<Participant> {
    return this.http.post<Participant>(this.apiUrl, participant);
  }

  update(participantKey: string, participant: Participant): Observable<Participant> {
    return this.http.put<Participant>(`${this.apiUrl}/${participantKey}`, participant);
  }

  delete(participantKey: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${participantKey}`);
  }

  
  importCsv(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${this.apiUrl}/import`, formData);
  }

  exportCsv(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export`, { responseType: 'blob' });
  }
  
  
  downloadDb(): void {
    window.location.href = '/api/backup/download-db'; 
  }
}