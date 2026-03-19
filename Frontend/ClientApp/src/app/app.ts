import { Component, HostListener, inject, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ParticipantsListComponent } from './components/participants-list/participants-list';
import { HttpClient } from '@angular/common/http';
import { VersionFooter } from './components/version-footer/version-footer';
import { UpdateService } from './services/update.service';
import { UpdateBanner } from './components/update-banner/update-banner';
import { ChangelogDialog } from './components/changelog-dialog/changelog-dialog';
import { environment } from './environments/environment';

@Component({
  selector: 'app-root',
  imports: [ButtonModule, ParticipantsListComponent, UpdateBanner, VersionFooter, ChangelogDialog],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  protected readonly title = signal('3pdbApp');
  private updateService = inject(UpdateService);
  private http = inject(HttpClient);

  @HostListener('window:beforeunload') 
  onBeforeUnload() {
    if (this.updateService.isPlannedRestart()) {
      return;
    }

    navigator.sendBeacon(`${environment.apiUrl}/kill`);
  }
  
  // Globally intercept all clicks on external links and open them in a new window/browser
  @HostListener('document:click', ['$event'])
  onGlobalClick(event: MouseEvent) {
    const target = event.target as HTMLElement;
    const anchor = target.closest('a');
    
    // If it's a link and has an href
    if (anchor && anchor.href) {
        // If the click was already handled (e.g. by Angular Router), ignore it
        if (event.defaultPrevented) return;

        try {
            const url = new URL(anchor.href, window.location.href);
            
            // Check if it's an external link (different origin)
            if (url.origin !== window.location.origin) {
                event.preventDefault();
                // To force a new WINDOW (not tab), you must provide dimensions or other features
                window.open(anchor.href, '_blank', 'width=1024,height=768,noopener,noreferrer,resizable,scrollbars');
            }
        } catch (e) {
            // Ignore invalid URLs
        }
    }
  }

  ngOnInit() {
    this.http.post(`${environment.apiUrl}/alive`, {}).subscribe();
  }
}
