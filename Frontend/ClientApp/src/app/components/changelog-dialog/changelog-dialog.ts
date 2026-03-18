import { Component, effect, inject, signal, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Dialog } from 'primeng/dialog';
import { Button } from 'primeng/button';
import { UpdateService } from '../../services/update.service';
import { marked } from 'marked';

@Component({
  selector: 'app-changelog-dialog',
  standalone: true,
  imports: [CommonModule, Dialog, Button],
  templateUrl: './changelog-dialog.html',
  styleUrl: './changelog-dialog.css',
  encapsulation: ViewEncapsulation.None,
})
export class ChangelogDialog {
  updateService = inject(UpdateService);
  
  visible = this.updateService.showChangelog;
  notes = this.updateService.releaseNotes;
  
  htmlContent = signal('');

  constructor() {
    effect(() => {
        const raw = this.notes();
        if (raw) {
             const renderer = new marked.Renderer();
             renderer.link = ({ href, title, text }) => {
                return `<a href="${href}" target="_blank" rel="noopener noreferrer">${text}</a>`;
             };
             
             Promise.resolve(marked.parse(raw, { breaks: true, gfm: true, renderer })).then(html => this.htmlContent.set(html));
        } else {
            this.htmlContent.set('');
        }
    });
  }

  close() {
    this.updateService.markChangelogSeen();
  }
}

