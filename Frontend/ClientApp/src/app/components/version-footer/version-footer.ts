import { Component } from '@angular/core';
import { UpdateService } from '../../services/update.service';
import { AsyncPipe } from '@angular/common';

@Component({
  selector: 'app-version-footer',
  imports: [AsyncPipe],
  template: `
    <div class="fixed bottom-3 right-4 z-40 px-3 py-1.5 rounded-lg 
                bg-white/90 dark:bg-gray-800/90 backdrop-blur-sm 
                border border-gray-200 dark:border-gray-700 shadow-sm 
                text-xs text-gray-500 dark:text-gray-400 select-none 
                flex items-center gap-2 transition-opacity hover:opacity-100 opacity-60">
      <i class="pi pi-tag text-[10px]"></i>
      <span class="font-mono">v{{ updateService.currentVersion$ | async }}</span>
    </div>
  `
})
export class VersionFooter {
  constructor(public updateService: UpdateService) {}
}