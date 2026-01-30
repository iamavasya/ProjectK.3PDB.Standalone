import { Component, HostListener, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ParticipantsListComponent } from './components/participants-list/participants-list';
import { HttpClient } from '@angular/common/http';
import { UpdateBanner } from './components/update-banner/update-banner';
import { VersionFooter } from './components/version-footer/version-footer';

@Component({
  selector: 'app-root',
  imports: [ButtonModule, ParticipantsListComponent, UpdateBanner, VersionFooter],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('3pdbApp');

  @HostListener('window:beforeunload') 
  onBeforeUnload() {
    navigator.sendBeacon('http://localhost:5220/api/kill');
  }
}
