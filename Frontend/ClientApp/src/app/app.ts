import { Component, HostListener, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ParticipantsListComponent } from './components/participants-list/participants-list';
import { HttpClient } from '@angular/common/http';
import { VersionFooter } from './components/version-footer/version-footer';
import { UpdateService } from './services/update.service';
import { UpdateBanner } from './components/update-banner/update-banner';

@Component({
  selector: 'app-root',
  imports: [ButtonModule, ParticipantsListComponent, UpdateBanner, VersionFooter],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  protected readonly title = signal('3pdbApp');
  private updateService = inject(UpdateService);
  private http = inject(HttpClient);

  @HostListener('window:beforeunload') 
  onBeforeUnload() {
    navigator.sendBeacon('http://localhost:5220/api/kill');
  }

  ngOnInit() {
    this.http.post('http://localhost:5220/api/alive', {}).subscribe();
  }
}
