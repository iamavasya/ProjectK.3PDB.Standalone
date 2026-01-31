import { Component } from '@angular/core';
import { UpdateService } from '../../services/update.service';
import { MessageModule } from 'primeng/message';
import { AsyncPipe } from '@angular/common';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-update-banner',
  imports: [MessageModule, AsyncPipe, ButtonModule],
  templateUrl: './update-banner.html',
  styleUrl: './update-banner.css',
})
export class UpdateBanner {
  isDismissed = false;

  updateState$;

  constructor(public updateService: UpdateService) {
    this.updateState$ = this.updateService.updateState$;
  }

  startDownload() {
    this.updateService.download().subscribe();
  }

  restart() {
    this.updateService.apply().subscribe();
    
    this.reloadUntilUp();
  }

  private reloadUntilUp() {
    setInterval(() => {
        window.location.reload();
    }, 5000);
  }

  dismiss() {
    this.isDismissed = true;
  }
}
