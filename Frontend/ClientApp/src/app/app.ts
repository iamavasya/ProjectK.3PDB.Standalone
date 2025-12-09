import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ParticipantsListComponent } from './components/participants-list/participants-list';

@Component({
  selector: 'app-root',
  imports: [ButtonModule, ParticipantsListComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('ClientApp');
}
