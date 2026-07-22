import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

export interface RestoreResult {
  requiresRestart: boolean;
  safetyBackupPath?: string;
}

@Injectable({
  providedIn: 'root'
})
export class BackupService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/backup`;

  /** Downloads a consistent full snapshot of the SQLite database (.db). */
  downloadDb(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/download-db`, { responseType: 'blob' });
  }

  /** Downloads a schema-versioned JSON/ZIP archive of all data. */
  exportArchive(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/export-archive`, { responseType: 'blob' });
  }

  /** Uploads a raw .db file to fully replace the current database (applied after restart). */
  restoreDb(file: File): Observable<RestoreResult> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<RestoreResult>(`${this.apiUrl}/restore-db`, formData);
  }

  /** Uploads a JSON/ZIP archive to fully replace all data (no restart needed). */
  importArchive(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post(`${this.apiUrl}/import-archive`, formData);
  }

  /** Asks the backend to relaunch so a staged .db restore is applied. */
  restart(): Observable<any> {
    return this.http.post(`${this.apiUrl}/restart`, {});
  }
}
