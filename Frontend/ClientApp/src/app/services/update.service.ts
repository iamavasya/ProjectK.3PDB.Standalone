import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, catchError, filter, interval, Observable, of, switchMap, tap, timer } from 'rxjs';
import { environment } from '../environments/environment';

export interface CheckResult {
  available: boolean;
  version?: string;
}

export interface RestartReadiness {
  ready: boolean;
  version: string;
  serverTimeUtc: string;
}

export type UpdateState = 'idle' | 'available' | 'downloading' | 'ready' | 'restarting';

@Injectable({ providedIn: 'root' })
export class UpdateService {
  private apiUrl = `${environment.apiUrl}/update`;

  public updateState$ = new BehaviorSubject<UpdateState>('idle');
  public newVersion$ = new BehaviorSubject<string | null>(null);
  public currentVersion$ = new BehaviorSubject<string>(' Loading...');
  public plannedRestart$ = new BehaviorSubject<boolean>(false);
  
  public showChangelog = signal(false);
  public releaseNotes = signal<string>('');
  
  private versionKey = 'appVersion';

  constructor(private http: HttpClient) {
    this.init();
  }

  private init() {
    this.http.get<{ version: string }>(`${this.apiUrl}/current-version`)
      .subscribe({
        next: res => {
            const serverVersion = res.version;
            this.currentVersion$.next(serverVersion);
            this.checkVersionChange(serverVersion);
        },
        error: () => this.currentVersion$.next('Unknown')
      });


    timer(0, 3600000) 
      .pipe(
        filter(() => this.updateState$.value === 'idle'),
        switchMap(() => this.check().pipe(
           catchError(() => of({ available: false }))
        ))
      )
      .subscribe();
  }

  checkVersionChange(serverVersion: string) {
    try {
        const localVersion = localStorage.getItem(this.versionKey);
        // If local version exists and differs from server version -> update happened!
        if (localVersion && localVersion !== serverVersion) {
            this.fetchReleaseNotes(serverVersion).subscribe(notes => {
                this.releaseNotes.set(notes);
                this.showChangelog.set(true);
            });
        }
        // Update local version immediately or wait for user to close dialog? 
        // Usually better to wait, BUT if they refresh page we don't want to lose the prompt if they didn't see it?
        // Let's update it ONLY when they close the dialog (markChangelogSeen).
        
        // For first run (no local version), we might want to just set it without showing huge modal, 
        // or show "Welcome". Let's assume we just set it silently for fresh install.
        if (!localVersion) {
            localStorage.setItem(this.versionKey, serverVersion);
        }
    } catch (e) {
        console.warn('UpdateService: localStorage access failed', e);
    }
  }

  fetchReleaseNotes(version: string): Observable<string> {
    return this.http.get(`${this.apiUrl}/release-notes/${version}`, { responseType: 'text' });
  }

  
  openManualChangelog() {
      const current = this.currentVersion$.value;
      this.fetchReleaseNotes(current).subscribe(notes => {
          this.releaseNotes.set(notes);
          this.showChangelog.set(true);
      });
  }
  
  markChangelogSeen() {
    const currentVer = this.currentVersion$.value;
    if (currentVer && currentVer !== 'Vehicle' && currentVer !== 'Unknown') {
         localStorage.setItem(this.versionKey, currentVer);
    }
    this.showChangelog.set(false);
  }

  check(): Observable<CheckResult> {
    return this.http.get<CheckResult>(`${this.apiUrl}/check`).pipe(
      tap(res => {
        if (res.available && res.version) {
          console.log('UpdateService: New version found!', res.version);
          this.newVersion$.next(res.version);
          this.updateState$.next('available');
        } else {
          this.updateState$.next('idle');
          console.log('UpdateService: No new version available.');
        }
      })
    );
  }

  download(): Observable<any> {
    this.updateState$.next('downloading');
    
    return this.http.post(`${this.apiUrl}/download`, {}).pipe(
      tap(() => {
        this.updateState$.next('ready');
      }),
      catchError(err => {
        console.error('Download failed', err);
        this.updateState$.next('available');
        throw err;
      })
    );
  }

  apply(): Observable<any> {
    this.updateState$.next('restarting');
    return this.http.post(`${this.apiUrl}/apply`, {}).pipe(
      tap(() => {
        this.updateState$.next('idle');
      }),
      catchError(err => {
        console.error('Apply failed', err);
        this.updateState$.next('ready');
        throw err;
      })
    );
  }

  setPlannedRestart(value: boolean) {
    this.plannedRestart$.next(value);
  }

  isPlannedRestart(): boolean {
    return this.plannedRestart$.value;
  }

  getRestartReadiness(): Observable<RestartReadiness> {
    return this.http.get<RestartReadiness>(`${this.apiUrl}/readiness`);
  }

  sendAliveHeartbeat(): Observable<any> {
    return this.http.post(`${environment.apiUrl}/alive`, {});
  }
}