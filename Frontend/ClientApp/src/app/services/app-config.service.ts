import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AppConfigService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/config`;

  getTitleSuffix(): Observable<{ suffix: string }> {
    return this.http.get<{ suffix: string }>(`${this.apiUrl}/title-suffix`);
  }

  updateTitleSuffix(suffix: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/title-suffix`, { suffix });
  }
}
