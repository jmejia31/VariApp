import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { FormsModule } from '@angular/forms';
import { ProveedorService } from '../../services/proveedor.service';
import { Proveedor } from '../../core/models/proveedor.model';

@Component({
  selector: 'app-proveedores-list',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule, MatSlideToggleModule, FormsModule],
  templateUrl: './proveedores-list.component.html',
  styleUrl: './proveedores-list.component.scss'
})
export class ProveedoresListComponent implements OnInit {
  readonly proveedores = signal<Proveedor[]>([]);
  readonly loading = signal(true);

  constructor(private proveedorService: ProveedorService) {}

  ngOnInit(): void { this.cargar(); }

  cargar(): void {
    this.loading.set(true);
    this.proveedorService.getAll().subscribe({
      next: (res) => { this.proveedores.set(res.data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  toggleActivo(p: Proveedor): void {
    this.proveedorService.update(p.id, {
      nombre: p.nombre, telefono: p.telefono, documento: p.documento,
      correo: p.correo, direccion: p.direccion, activo: !p.activo
    }).subscribe(() => this.cargar());
  }
}
