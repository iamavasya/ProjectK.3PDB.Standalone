import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';


import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { FileUploadModule } from 'primeng/fileupload';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { DialogModule } from 'primeng/dialog';

import { ParticipantService } from '../../services/participant.service';
import { Participant } from '../../models/participant.model';
import { AgePipe } from '../../pipes/age.pipe';

@Component({
  selector: 'app-participants-list',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule,
    TableModule, 
    ButtonModule, 
    TagModule,
    IconFieldModule, 
    InputIconModule, 
    InputTextModule,
    FileUploadModule,
    ToastModule,
    DialogModule,
    AgePipe,
  ],
  providers: [MessageService], 
  templateUrl: './participants-list.html',
  styleUrl: './participants-list.css' 
})
export class ParticipantsListComponent implements OnInit {
  private service = inject(ParticipantService);
  private messageService = inject(MessageService);

  
  participants = signal<Participant[]>([]);
  loading = signal<boolean>(true);
  
  
  searchValue = signal<string>('');

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.loading.set(true);
    this.service.getAll().subscribe({
      next: (data) => {

        const enrichedData = data.map(participant => ({
          ...participant,
          daysToProbeEnd: this.calculateDays(participant.birthDate)
        }));

        this.participants.set(enrichedData);
        this.loading.set(false);
      },
      error: (err) => {
        this.showError('Помилка завантаження', err.message);
        this.loading.set(false);
      }
    });
  }

  
  onFileSelect(event: any) {
    const file = event.files[0];
    if (file) {
      this.service.importCsv(file).subscribe({
        next: () => {
          this.messageService.add({ severity: 'success', summary: 'Успіх', detail: 'CSV імпортовано' });
          this.loadData(); 
        },
        error: (err) => {
          this.showError('Помилка імпорту', err.error?.message || err.message);
        }
      });
    }
  }

  
  getSeverity(status: boolean): "success" | "danger" {
    return status ? 'success' : 'danger'; 
  }
  
  showError(summary: string, detail: string) {
    this.messageService.add({ severity: 'error', summary, detail });
  }

  private calculateDays(birthDateString: string | null): number {
    if (!birthDateString) return -9999; 

    const birth = new Date(birthDateString);
    const deadline = new Date(birth);
    deadline.setFullYear(birth.getFullYear() + 18);

    const today = new Date();
    const diffTime = deadline.getTime() - today.getTime();
    
    
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  }
}