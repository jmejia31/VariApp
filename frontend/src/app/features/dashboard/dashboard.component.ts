import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DashboardService } from '../../services/dashboard.service';
import { DashboardResumen } from '../../core/models/dashboard.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  readonly resumen = signal<DashboardResumen | null>(null);
  readonly loading = signal(true);

  constructor(private dashboardService: DashboardService) {}

  ngOnInit(): void {
    this.dashboardService.getResumen().subscribe({
      next: (res) => {
        this.resumen.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
