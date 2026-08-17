import { Component, Input } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';

export type BadgeStatus = 'Aberta' | 'Fechada' | string;

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [MatChipsModule],
  template: `
    <mat-chip-set>
      <mat-chip [class.badge-aberta]="status === 'Aberta'" [class.badge-fechada]="status === 'Fechada'">
        {{ status }}
      </mat-chip>
    </mat-chip-set>
  `,
  styles: [`
    .badge-aberta {
      background-color: #fff3cd !important;
      color: #856404 !important;
      border: 1px solid #ffeeba;
    }
    .badge-fechada {
      background-color: #d4edda !important;
      color: #155724 !important;
      border: 1px solid #c3e6cb;
    }
  `]
})
export class StatusBadgeComponent {
  @Input({ required: true }) status!: BadgeStatus;
}
