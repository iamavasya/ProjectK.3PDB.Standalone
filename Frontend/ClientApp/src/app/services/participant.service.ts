import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Participant, ParticipantHistory } from '../models/participant.model';

@Injectable({
  providedIn: 'root'
})
export class ParticipantService {
  private http = inject(HttpClient);
  private apiUrl = 'https://localhost:7001/api/participant';

  getAll(): Observable<Participant[]> {
    return this.http.get<Participant[]>(this.apiUrl);
  }

  getById(id: number): Observable<Participant> {
    return this.http.get<Participant>(`${this.apiUrl}/${id}`);
  }
  
  getHistory(id: number): Observable<ParticipantHistory[]> {
    return this.http.get<ParticipantHistory[]>(`${this.apiUrl}/${id}/history`);
  }

  create(participant: Participant): Observable<Participant> {
    return this.http.post<Participant>(this.apiUrl, participant);
  }

  update(id: number, participant: Participant): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, participant);
  }

  
  importCsv(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${this.apiUrl}/import`, formData);
  }
  
  
  downloadDb(): void {
    window.location.href = '/api/backup/download-db'; 
  }
}