// update.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, catchError, filter, interval, Observable, of, switchMap, tap } from 'rxjs';
import { environment } from '../environments/environment';

export interface CheckResult {
  available: boolean;
  version?: string;
}

export type UpdateState = 'idle' | 'available' | 'downloading' | 'ready';

@Injectable({ providedIn: 'root' })
export class UpdateService {
  private apiUrl = `${environment.apiUrl}/update`;

  public updateState$ = new BehaviorSubject<UpdateState>('idle');
  public newVersion$ = new BehaviorSubject<string | null>(null);
  public currentVersion$ = new BehaviorSubject<string>(' Loading...');

  constructor(private http: HttpClient) {
    this.init();
  }

  private init() {
    this.http.get<{ version: string }>(`${this.apiUrl}/current-version`)
      .subscribe(res => {
        this.currentVersion$.next(res.version)
      });

      interval(3600000)
      .pipe(
        filter(() => 
          this.updateState$.value == 'idle'
        ), 
        switchMap(() => 
          this.check()
            .pipe(
              catchError(err => 
                of({ available: false, version: 'error' })
              )
            )
          )
        )
      .subscribe(res => { 
          if (res.available && res.version) {
            this.newVersion$.next(res.version);
            this.updateState$.next('available');
          }
        }
      );
  }

  check(): Observable<CheckResult> {
    return this.http.get<CheckResult>(`${this.apiUrl}/check`);
  }

  download(): Observable<any> {
    this.updateState$.next('downloading');
    return this.http.post(`${this.apiUrl}/download`, {})
      .pipe(
        tap(() => 
          this.updateState$.next('ready')
        )
      );
  }

  apply(): Observable<any> {
    return this.http.post(`${this.apiUrl}/apply`, {});
  }
}