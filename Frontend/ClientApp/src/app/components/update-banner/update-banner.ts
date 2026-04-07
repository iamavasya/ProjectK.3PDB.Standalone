import { Component } from '@angular/core';
import { UpdateService } from '../../services/update.service';
import { MessageModule } from 'primeng/message';
import { AsyncPipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-update-banner',
  imports: [MessageModule, AsyncPipe, ButtonModule],
  templateUrl: './update-banner.html',
  styleUrl: './update-banner.css',
})
export class UpdateBanner {
  isDismissed = false;

  updateState$;

  constructor(public updateService: UpdateService) {
    this.updateState$ = this.updateService.updateState$;
  }

  startDownload() {
    this.updateService.download().subscribe();
  }

  restart() {
    const expectedVersion = this.updateService.newVersion$.value;
    this.updateService.setPlannedRestart(true);

    this.updateService.apply().subscribe({
      next: () => this.waitForBackendAndReload(expectedVersion),
      error: () => {
        this.waitForBackendAndReload(expectedVersion);
      }
    });
  }

  private waitForBackendAndReload(expectedVersion: string | null) {
    const maxAttempts = 20;
    let attempts = 0;
    let delayMs = 500;

    const runPoll = () => {
      attempts += 1;

      this.updateService.getRestartReadiness().subscribe({
        next: status => {
          this.updateService.sendAliveHeartbeat().pipe(
            catchError(() => of(null))
          ).subscribe();

          const isVersionReady = !expectedVersion || status.version === expectedVersion;
          if (status.ready && isVersionReady) {
            this.reloadWithCacheBust();
            return;
          }

          scheduleNext();
        },
        error: () => {
          scheduleNext();
        }
      });
    };

    const scheduleNext = () => {
      if (attempts >= maxAttempts) {
        this.updateService.setPlannedRestart(false);
        return;
      }

      delayMs = Math.min(Math.floor(delayMs * 1.5), 4000);
      window.setTimeout(runPoll, delayMs);
    };

    window.setTimeout(runPoll, delayMs);
  }

  private reloadWithCacheBust() {
    const url = new URL(window.location.href);
    url.searchParams.set('v', Date.now().toString());
    window.history.replaceState({}, '', url.toString());
    window.location.reload();
  }

  dismiss() {
    this.isDismissed = true;
  }
}
