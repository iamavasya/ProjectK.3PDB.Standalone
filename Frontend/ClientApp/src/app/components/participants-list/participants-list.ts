import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';


import { Table, TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Tag } from 'primeng/tag';
import { IconField } from 'primeng/iconfield';
import { InputIcon } from 'primeng/inputicon';
import { InputText } from 'primeng/inputtext';
import { FileUpload } from 'primeng/fileupload';
import { Toast } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog'; 
import { Drawer } from 'primeng/drawer';
import { DatePicker } from 'primeng/datepicker';
import { Checkbox } from 'primeng/checkbox';
import { Textarea } from 'primeng/textarea';

import { ParticipantService } from '../../services/participant.service';
import { Participant } from '../../models/participant.model';
import { AgePipe } from '../../pipes/age.pipe';
import { MessageService, ConfirmationService } from 'primeng/api';
import { TooltipModule } from 'primeng/tooltip';
import { HighlightPipe } from '../../pipes/highlight.pipe';
import { formatDateToISO } from '../../functions/formatDateToISO.function';

@Component({
  selector: 'app-participants-list',
  standalone: true,
  imports: [
    CommonModule, 
    AgePipe,
    ReactiveFormsModule,
    FormsModule,
    TableModule,
    Button,
    Tag,
    IconField,
    InputIcon,
    InputText,
    FileUpload,
    Toast,
    ConfirmDialogModule,
    Drawer,
    DatePicker,
    Checkbox,
    Textarea,
    TooltipModule,
    HighlightPipe
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './participants-list.html',
  styleUrl: './participants-list.css' 
})
export class ParticipantsListComponent implements OnInit {
  private service = inject(ParticipantService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);
  private fb = inject(FormBuilder);

  
  participants = signal<Participant[]>([]);
  loading = signal<boolean>(true);
  searchValue = signal<string>('');

  
  drawerVisible = signal(false);
  isEditMode = signal(false);

  
  form!: FormGroup;

  constructor() {
    this.initForm();
  }

  ngOnInit() {
    this.loadData();
    this.setupFormValidators();
  }

  
  initForm() {
    this.form = this.fb.group({
      participantKey: [null],
      fullName: ['', Validators.required],
      kurin: [null],
      email: ['', [Validators.required, Validators.email]],
      phone: [''],
      
      
      birthDate: [null],
      probeOpenDate: [null],
      
      
      isProbeOpen: [false],
      isMotivationLetterWritten: [false],
      isFormFilled: [false],
      isProbeContinued: [false],
      isProbeFrozen: [false],
      isSelfReflectionSubmitted: [false],
      isArchived: [false],
      
      notes: ['']
    });
  }

  
  loadData() {
    this.loading.set(true);
    this.service.getAll().subscribe({
      next: (data) => {
        const enrichedData = data.map(participant => ({
          ...participant,
          daysToProbeEnd: this.calculateDays(participant.birthDate, participant.probeOpenDate)
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

  

  
  openCreate() {
    this.isEditMode.set(false);
    this.form.reset({
      participantKey: null 
    });
    this.drawerVisible.set(true);
  }

  
  openEdit(participant: Participant) {
    this.isEditMode.set(true);
    
    
    const patchData = { 
        ...participant,
        birthDate: participant.birthDate ? new Date(participant.birthDate) : null,
        probeOpenDate: participant.probeOpenDate ? new Date(participant.probeOpenDate) : null
    };

    this.form.patchValue(patchData);

    if (participant.isProbeOpen) {
        this.form.get('probeOpenDate')?.setValidators([Validators.required]);
    } else {
        this.form.get('probeOpenDate')?.clearValidators();
    }
    this.form.get('probeOpenDate')?.updateValueAndValidity();

    this.drawerVisible.set(true);
  }

  
  save() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const rawValue = this.form.value;
    
    const payload = {
      ...rawValue,
      isProbeOpen: !!rawValue.isProbeOpen,
      isMotivationLetterWritten: !!rawValue.isMotivationLetterWritten,
      isFormFilled: !!rawValue.isFormFilled,
      isProbeContinued: !!rawValue.isProbeContinued,
      isProbeFrozen: !!rawValue.isProbeFrozen,
      isSelfReflectionSubmitted: !!rawValue.isSelfReflectionSubmitted,
      isArchived: !!rawValue.isArchived,

      probeOpenDate: formatDateToISO(rawValue.probeOpenDate),

      kurin: rawValue.kurin ? Number(rawValue.kurin) : null
    }

    
    const request$ = this.isEditMode() 
        ? this.service.update(payload.participantKey, payload)
        : this.service.create(payload);

    request$.subscribe({
        next: () => {
            this.messageService.add({ severity: 'success', summary: 'Успішно', detail: 'Дані збережено' });
            this.drawerVisible.set(false);
            this.loadData();
        },
        error: (err) => this.showError('Помилка збереження', err.message)
    });
  }

  
  delete(event: Event, participant: Participant) {
    this.confirmationService.confirm({
        target: event.target as EventTarget,
        message: `Видалити учасника ${participant.fullName}?`,
        header: 'Підтвердження',
        icon: 'pi pi-exclamation-triangle',
        acceptButtonProps: { severity: 'danger' },
        accept: () => {
            this.service.delete(participant.participantKey).subscribe({
                next: () => {
                    this.messageService.add({ severity: 'info', summary: 'Видалено' });
                    this.loadData();
                },
                error: (err) => this.showError('Помилка видалення', err.message)
            });
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

  exportCsv() {
    this.service.exportCsv().subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);

        const a = document.createElement('a');
        a.href = url;

        const date = new Date().toISOString().slice(0, 10);
        a.download = `3pdb_export_${date}.csv`;

        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (err) => console.error('Export failed', err)
    })
  }
  
  getSeverity(status: boolean): "success" | "danger" {
    return status ? 'success' : 'danger'; 
  }
  
  showError(summary: string, detail: string) {
    this.messageService.add({ severity: 'error', summary, detail });
  }

  private calculateDays(birthDateString: string | null, probeOpenDateString: string | null): number | undefined {
    
    if (!birthDateString || !probeOpenDateString) return undefined; 

    const birth = new Date(birthDateString);
    const deadline = new Date(birth);
    deadline.setFullYear(birth.getFullYear() + 18);

    const today = new Date();
    const diffTime = deadline.getTime() - today.getTime();
    
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  }

  private setupFormValidators() {
    
    this.form.get('isProbeOpen')?.valueChanges.subscribe(isOpen => {
      const dateControl = this.form.get('probeOpenDate');
      
      if (isOpen) {
        
        dateControl?.setValidators([Validators.required]);
      } else {
        
        dateControl?.clearValidators();
      }
      
      dateControl?.updateValueAndValidity();
    });
  }

  onGlobalFilter(table: Table, event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.searchValue.set(value); // Зберігаємо для хайлайту
    table.filterGlobal(value, 'contains'); // Фільтруємо таблицю
  }

}