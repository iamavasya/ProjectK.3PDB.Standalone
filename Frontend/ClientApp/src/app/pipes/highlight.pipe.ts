import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'highlight',
  standalone: true
})
export class HighlightPipe implements PipeTransform {

  transform(text: string | number | null, search: string): string {
    if (!text) return '';
    
    const textStr = String(text);

    if (!search || search.length < 1) {
      return textStr;
    }

    const pattern = search.replace(/[\-\[\]\/\{\}\(\)\*\+\?\.\\\^\$\|]/g, "\\$&");
    
    const regex = new RegExp(`(${pattern})`, 'gi');

    return textStr.replace(regex, '<span class="bg-yellow-200 text-slate-900 font-bold px-0.5 rounded">$1</span>');
  }
}