import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { AppConfigService } from '../../services/app-config.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, FormsModule, Button, InputText],
  templateUrl: './header.html',
  styleUrl: './header.css',
})
export class Header implements OnInit {
  currentYear = new Date().getFullYear();
  
  titleSuffix = signal('');
  isEditing = signal(false);
  editValue = signal('');
  
  private appConfigService = inject(AppConfigService);

  ngOnInit() {
    this.appConfigService.getTitleSuffix().subscribe({
      next: (data) => {
        this.titleSuffix.set(data.suffix);
      },
      error: (err) => console.error('Failed to load title suffix', err)
    });
  }

  toggleEdit() {
    if (this.isEditing()) {
      this.cancelEdit();
    } else {
      this.editValue.set(this.titleSuffix());
      this.isEditing.set(true);
    }
  }

  cancelEdit() {
    this.isEditing.set(false);
    this.editValue.set('');
  }

  save() {
    const newVal = this.editValue();
    this.appConfigService.updateTitleSuffix(newVal).subscribe({
      next: () => {
        this.titleSuffix.set(newVal);
        this.isEditing.set(false);
      },
      error: (err) => console.error('Failed to save title suffix', err)
    });
  }
}
