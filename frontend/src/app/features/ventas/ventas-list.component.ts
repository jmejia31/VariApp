import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { debounceTime, Subject } from 'rxjs';
import { VentaService } from '../../services/venta.service';
import { Venta } from '../../core/models/venta.model';

@Component({
  selector: 'app-ventas-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, MatIconModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatPaginatorModule, MatProgressSpinnerModule],
  templateUrl: './ventas-list.component.html',
  styleUrl: './ventas-list.component.scss'
})
export class VentasListComponent implements OnInit {
  readonly ventas = signal<Venta[]>([]);
  readonly loading = signal(true);
  readonly totalCount = signal(0);

  page = 1;
  pageSize = 10;
  search = '';
  private searchSubject = new Subject<string>();

  constructor(private ventaService: VentaService) {
    this.searchSubject.pipe(debounceTime(350)).subscribe(() => { this.page = 1; this.cargar(); });
  }

  ngOnInit(): void { this.cargar(); }

  onSearchChange(value: string): void { this.search = value; this.searchSubject.next(value); }

  cargar(): void {
    this.loading.set(true);
    this.ventaService.getPaged({ page: this.page, pageSize: this.pageSize, search: this.search, sortBy: 'Fecha', sortDirection: 'desc' })
      .subscribe({
        next: (res) => { this.ventas.set(res.data.items); this.totalCount.set(res.data.totalCount); this.loading.set(false); },
        error: () => this.loading.set(false)
      });
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.cargar();
  }
}
