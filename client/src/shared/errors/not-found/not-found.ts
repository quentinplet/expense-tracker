import { Location } from '@angular/common';
import { Component, inject } from '@angular/core';
import { CardModule } from 'primeng/card';
import { Button } from 'primeng/button';

@Component({
  selector: 'app-not-found',
  imports: [CardModule, Button],
  templateUrl: './not-found.html',
  styleUrl: './not-found.scss',
})
export class NotFound {
  private location = inject(Location);

  goBack() {
    this.location.back();
  }
}
