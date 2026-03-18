import { Component, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-copy-text',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './copy-text.html',
  styleUrl: './copy-text.css',
})
export class CopyText {
  text = input<string | number | null | undefined>();
  copyValue = input<string | number | null | undefined>();
  
  isCopied = signal(false);

  copy() {
    // If copyValue is provided, use it. Otherwise use text input as backup copy source.
    // The consumer might project content so text input might not be what's displayed.
    const valueToCopy = this.copyValue() ?? this.text();
    
    if (valueToCopy === null || valueToCopy === undefined || valueToCopy === '') return;

    navigator.clipboard.writeText(valueToCopy.toString()).then(() => {
      this.isCopied.set(true);
      setTimeout(() => {
        this.isCopied.set(false);
      }, 1500);
    });
  }
}
