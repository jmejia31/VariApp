import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MovimientoInventarioService } from '../../services/movimiento-inventario.service';
import { MovimientoInventario } from '../../core/models/movimiento-inventario.model';

@Component({
  selector: 'app-movimientos-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatFormFieldModule, MatSelectModule,
    MatProgressSpinnerModule, MatIconModule
  ],
  templateUrl: './movimientos-list.component.html',
  styleUrl: './movimientos-list.component.scss'
})
export class MovimientosListComponent implements OnInit {
  readonly movimientos = signal<MovimientoInventario[]>([]);
  readonly loading = signal(true);
  filtroTipo = '';

  constructor(private movimientoService: MovimientoInventarioService) {}

  ngOnInit(): void { this.cargar(); }

  cargar(): void {
    this.loading.set(true);
    this.movimientoService.getFiltered(undefined, this.filtroTipo || undefined).subscribe({
      next: (res) => { this.movimientos.set(res.data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }
}
