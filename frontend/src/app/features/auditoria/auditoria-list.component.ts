import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuditoriaService } from '../../services/auditoria.service';
import { RegistroAuditoria } from '../../core/models/auditoria.model';

@Component({
  selector: 'app-auditoria-list',
  standalone: true,
  imports: [CommonModule, FormsModule, MatFormFieldModule, MatSelectModule, MatInputModule, MatPaginatorModule, MatProgressSpinnerModule],
  templateUrl: './auditoria-list.component.html',
  styleUrl: './auditoria-list.component.scss'
})
export class AuditoriaListComponent implements OnInit {
  readonly registros = signal<RegistroAuditoria[]>([]);
  readonly loading = signal(true);
  readonly totalCount = signal(0);

  page = 1;
  pageSize = 25;
  filtroModulo = '';
  filtroAccion = '';
  filtroDesde = '';
  filtroHasta = '';

  readonly modulos = ['Dashboard', 'Productos', 'Categorias', 'Compras', 'Ventas', 'Facturacion', 'Finanzas', 'Inventario', 'Proveedores', 'Clientes', 'Usuarios', 'Auditoria', 'Configuracion'];
  readonly acciones = ['Ver', 'Crear', 'Editar', 'Eliminar', 'Confirmar', 'Anular'];

  constructor(private auditoriaService: AuditoriaService) {}

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.auditoriaService.getFiltered({
      page: this.page,
      pageSize: this.pageSize,
      modulo: this.filtroModulo || undefined,
      accion: this.filtroAccion || undefined,
      desde: this.filtroDesde || undefined,
      hasta: this.filtroHasta || undefined
    }).subscribe({
      next: (res) => {
        this.registros.set(res.data.items);
        this.totalCount.set(res.data.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  aplicarFiltros(): void {
    this.page = 1;
    this.cargar();
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.cargar();
  }
}
