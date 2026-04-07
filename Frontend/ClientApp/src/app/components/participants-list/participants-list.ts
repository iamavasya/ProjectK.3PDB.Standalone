import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';


import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Badge } from 'primeng/badge';
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
import { ChartModule } from 'primeng/chart';
import { forkJoin } from 'rxjs';

import { ParticipantService } from '../../services/participant.service';
import { Participant, QuarterlyProbeReportItem, QuarterlyProbeTotalsItem } from '../../models/participant.model';
import { AgePipe } from '../../pipes/age.pipe';
import { MessageService, ConfirmationService } from 'primeng/api';
import { TooltipModule } from 'primeng/tooltip';
import { HighlightPipe } from '../../pipes/highlight.pipe';
import { formatDateToISO } from '../../functions/formatDateToISO.function';
import { Header } from '../header/header';
import { CopyText } from '../copy-text/copy-text';

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
    Badge,
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
    ChartModule,
    TooltipModule,
    HighlightPipe,
    Header,
    CopyText
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
  activeParticipants = computed(() => this.participants().filter((participant) => !participant.isArchived));
  activeParticipantsCount = computed(() => this.activeParticipants().length);
  activeOpenedProbesCount = computed(() => this.activeParticipants().filter((participant) => participant.isProbeOpen).length);
  activeNotOpenedProbesCount = computed(() => this.activeParticipants().filter((participant) => !participant.isProbeOpen).length);
  activeParticipantsBadgeValue = computed(() => `${this.activeOpenedProbesCount()}+${this.activeNotOpenedProbesCount()}`);
  archivedParticipantsCount = computed(() => this.participants().filter((participant) => participant.isArchived).length);
  activeTab = signal<'participants' | 'archive' | 'quarterly-report'>('participants');
  visibleParticipants = computed(() =>
    this.activeTab() === 'quarterly-report'
      ? []
      : this.participants().filter((participant) =>
          this.activeTab() === 'archive' ? participant.isArchived : !participant.isArchived
        )
  );
  filteredParticipants = computed(() => {
    const search = this.searchValue().trim().toLowerCase();

    if (!search) {
      return this.visibleParticipants();
    }

    return this.visibleParticipants().filter((participant) => {
      const fieldsToSearch = [
        participant.fullName,
        participant.email,
        participant.phone,
        participant.kurin?.toString() ?? ''
      ].join(' ').toLowerCase();

      return fieldsToSearch.includes(search);
    });
  });
  reportYear = signal<number>(new Date().getFullYear());
  reportYearDate = signal<Date>(new Date(this.reportYear(), 0, 1));
  reportQuarter = signal<1 | 2 | 3 | 4>(this.getCurrentQuarter());
  quarterlyReport = signal<QuarterlyProbeReportItem[]>([]);
  reportLoading = signal<boolean>(false);
  yearlyChartLoading = signal<boolean>(false);
  yearlyQuarterChartData = signal<any | null>(null);
  yearlyQuarterChartOptions = signal<any>({});
  openedInQuarter = computed(() => this.quarterlyReport().filter((row) => row.action === 'opened').length);
  archivedInQuarter = computed(() => this.quarterlyReport().filter((row) => row.action === 'archived').length);
  unarchivedInQuarter = computed(() => this.quarterlyReport().filter((row) => row.action === 'unarchived').length);
  quarterLabel = computed(() => `Q${this.reportQuarter()} ${this.reportYear()}`);
  periodLabel = computed(() => {
    const period = this.getQuarterPeriod(this.reportYear(), this.reportQuarter());
    const start = this.formatDate(period.start);
    const end = this.formatDate(period.end);
    return `${start} - ${end}`;
  });
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
    this.yearlyQuarterChartOptions.set(this.buildYearlyQuarterChartOptions());
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

  setActiveTab(tab: 'participants' | 'archive' | 'quarterly-report') {
    this.activeTab.set(tab);

    if (tab === 'quarterly-report' && this.quarterlyReport().length === 0) {
      this.loadQuarterlyReport();
    }
  }

  onSearchInput(event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.searchValue.set(value);
  }

  setReportYear(value: Date | Date[] | null) {
    const selectedYearDate = Array.isArray(value) ? value[0] : value;

    if (!selectedYearDate) {
      return;
    }

    const year = selectedYearDate.getFullYear();
    this.reportYear.set(year);
    this.reportYearDate.set(new Date(year, 0, 1));
  }

  setReportQuarter(quarter: 1 | 2 | 3 | 4) {
    this.reportQuarter.set(quarter);
  }

  loadQuarterlyReport() {
    this.reportLoading.set(true);
    this.yearlyChartLoading.set(true);

    forkJoin({
      reportRows: this.service.getQuarterlyProbeReport(this.reportYear(), this.reportQuarter()),
      yearlyTotals: this.service.getQuarterlyProbeTotals(this.reportYear())
    }).subscribe({
      next: ({ reportRows, yearlyTotals }) => {
        this.quarterlyReport.set(reportRows);
        this.updateYearlyQuarterChart(yearlyTotals);
        this.reportLoading.set(false);
        this.yearlyChartLoading.set(false);
      },
      error: (err) => {
        this.showError('Помилка звіту', err.error?.message || err.message);
        this.reportLoading.set(false);
        this.yearlyChartLoading.set(false);
      }
    });
  }

  deleteQuarterlyHistory(event: Event, row: QuarterlyProbeReportItem) {
    const historyKey = row.participantHistoryKey;

    if (!historyKey) {
      this.messageService.add({ severity: 'warn', summary: 'Недоступно', detail: 'Запис відкриття проби видаляється через зміну поля дати відкриття проби' });
      return;
    }

    this.confirmationService.confirm({
      target: event.target as EventTarget,
      message: `Видалити запис історії для ${row.fullName}?`,
      header: 'Підтвердження',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonProps: { severity: 'danger' },
      accept: () => {
        this.service.softDeleteHistory(historyKey).subscribe({
          next: () => {
            this.messageService.add({ severity: 'info', summary: 'Видалено', detail: 'Запис видалено' });
            this.loadQuarterlyReport();
          },
          error: (err) => this.showError('Помилка видалення', err.error?.message || err.message)
        });
      }
    });
  }

  getReportActionLabel(action: 'opened' | 'archived' | 'unarchived') {
    if (action === 'opened') {
      return 'Проба відкрита';
    }

    if (action === 'archived') {
      return 'Перенесено в архів';
    }

    return 'Винесено з архіву';
  }

  getReportActionSeverity(action: 'opened' | 'archived' | 'unarchived'): 'success' | 'danger' | 'info' {
    if (action === 'opened') {
      return 'success';
    }

    if (action === 'archived') {
      return 'danger';
    }

    return 'info';
  }

  copyQuarterlyReport() {
    const reportMarkdown = this.buildQuarterlyReportMarkdown();

    navigator.clipboard.writeText(reportMarkdown)
      .then(() => {
        this.messageService.add({ severity: 'success', summary: 'Скопійовано', detail: 'Звіт скопійовано у буфер обміну' });
      })
      .catch(() => {
        this.showError('Помилка копіювання', 'Не вдалося скопіювати звіт у буфер обміну');
      });
  }

  exportQuarterlyReport() {
    const reportMarkdown = this.buildQuarterlyReportMarkdown();
    const blob = new Blob([reportMarkdown], { type: 'text/markdown;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');

    anchor.href = url;
    anchor.download = `quarterly_probe_report_${this.reportYear()}_Q${this.reportQuarter()}.md`;

    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);
    URL.revokeObjectURL(url);
  }

  private buildQuarterlyReportMarkdown(): string {
    const rows = this.quarterlyReport();
    const lines = rows.map((row, index) => {
      const actionLabel = row.action === 'opened'
        ? 'ПРОБА ВІДКРИТА'
        : row.action === 'archived'
          ? 'ПЕРЕНЕСЕНО В АРХІВ'
          : 'ВИНЕСЕНО З АРХІВУ';
      const kurin = row.kurin ?? '-';
      const probeOpenDate = row.probeOpenDate ? this.formatDateOnly(row.probeOpenDate) : '-';

      return `${index + 1}. ${this.formatDateOnly(row.changedAt)} | ${actionLabel} | День відкриття: ${probeOpenDate} | Курінь ${kurin} | ${row.fullName}`;
    });

    return [
      '# Квартальний звіт по 3-ій пробі',
      '',
      `Період: ${this.quarterLabel()} (${this.periodLabel()})`,
      `Всього подій: ${rows.length}`,
      `Відкрито проб: ${this.openedInQuarter()}`,
      `Перенесено в архів: ${this.archivedInQuarter()}`,
      `Винесено з архіву: ${this.unarchivedInQuarter()}`,
      '',
      '## Таймлайн',
      ...(lines.length ? lines : ['Немає подій для вибраного кварталу.'])
    ].join('\n');
  }

  private getCurrentQuarter(): 1 | 2 | 3 | 4 {
    const month = new Date().getMonth() + 1;
    return Math.ceil(month / 3) as 1 | 2 | 3 | 4;
  }

  private updateYearlyQuarterChart(totals: QuarterlyProbeTotalsItem[]) {
    const sortedTotals = [...totals].sort((left, right) => left.quarter - right.quarter);
    const openedTotal = sortedTotals.map((item) => item.openedTotal);
    const openedInQuarter = sortedTotals.map((item) => item.openedInQuarter);
    const archivedInQuarterNegative = sortedTotals.map((item) => item.archivedInQuarter * -1);

    this.yearlyQuarterChartOptions.set(this.buildYearlyQuarterChartOptions());
    this.yearlyQuarterChartData.set({
      labels: ['Q1', 'Q2', 'Q3', 'Q4'],
      datasets: [
        {
          type: 'line',
          label: 'Загалом відкритих проб',
          data: openedTotal,
          borderColor: this.getCssVar('--p-primary-color', '#3b82f6'),
          backgroundColor: this.getCssVar('--p-primary-color', '#3b82f6'),
          pointBackgroundColor: this.getCssVar('--p-primary-color', '#3b82f6'),
          pointRadius: 4,
          tension: 0.3,
          fill: false,
          yAxisID: 'y'
        },
        {
          type: 'bar',
          label: 'Відкрито (за датою відкриття)',
          data: openedInQuarter,
          borderRadius: 8,
          backgroundColor: this.getCssVar('--p-green-500', '#22c55e'),
          yAxisID: 'y'
        },
        {
          type: 'bar',
          label: 'Архівовано (за датою архівації)',
          data: archivedInQuarterNegative,
          borderRadius: 8,
          backgroundColor: this.getCssVar('--p-red-500', '#ef4444'),
          yAxisID: 'y'
        }
      ]
    });
  }

  private buildYearlyQuarterChartOptions(): any {
    const textColor = this.getCssVar('--p-text-color', '#334155');
    const mutedTextColor = this.getCssVar('--p-text-muted-color', '#64748b');
    const borderColor = this.getCssVar('--p-content-border-color', '#e2e8f0');

    return {
      maintainAspectRatio: false,
      plugins: {
        legend: {
          labels: {
            color: textColor
          }
        },
        tooltip: {
          callbacks: {
            label: (context: any) => {
              const value = Number(context.raw ?? 0);
              const absoluteValue = Math.abs(value);
              return `${context.dataset.label}: ${absoluteValue}`;
            }
          }
        }
      },
      scales: {
        x: {
          ticks: {
            color: mutedTextColor
          },
          grid: {
            color: borderColor
          }
        },
        y: {
          ticks: {
            color: mutedTextColor
          },
          grid: {
            color: borderColor
          },
          title: {
            display: true,
            text: 'Кількість учасників',
            color: textColor
          }
        }
      }
    };
  }

  private getCssVar(variableName: string, fallback: string): string {
    const value = getComputedStyle(document.documentElement).getPropertyValue(variableName).trim();
    return value || fallback;
  }

  private getQuarterPeriod(year: number, quarter: 1 | 2 | 3 | 4): { start: Date; end: Date } {
    const startMonth = ((quarter - 1) * 3);
    const startDate = new Date(year, startMonth, 1);
    const endDate = new Date(year, startMonth + 3, 0);

    return {
      start: startDate,
      end: endDate
    };
  }

  private formatDate(value: Date): string {
    return value.toLocaleDateString('uk-UA');
  }

  private formatDateOnly(value: string): string {
    return new Date(value).toLocaleDateString('uk-UA');
  }

}