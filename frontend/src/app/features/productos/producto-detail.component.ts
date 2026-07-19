import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ProductoService } from '../../services/producto.service';
import { Producto, ProductoImagen } from '../../core/models/producto.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

@Component({
  selector: 'app-producto-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, MatIconModule, MatButtonModule, MatProgressSpinnerModule],
  templateUrl: './producto-detail.component.html',
  styleUrl: './producto-detail.component.scss'
})
export class ProductoDetailComponent implements OnInit {
  readonly producto = signal<Producto | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly imagenAmpliada = signal<ProductoImagen | null>(null);
  readonly descargando = signal<number | null>(null); // id de imagen en descarga, o -1 para "todas"
  readonly puedeExportar = signal(false);
  readonly puedeEditar = signal(false);

  constructor(
    private productoService: ProductoService,
    private route: ActivatedRoute,
    private permisosRuntime: PermisosRuntimeService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.puedeExportar.set(this.permisosRuntime.puede('Productos', 'Exportar'));
    this.puedeEditar.set(this.permisosRuntime.puede('Productos', 'Editar'));

    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.productoService.getById(id).subscribe({
      next: (res) => { this.producto.set(res.data); this.loading.set(false); },
      error: () => { this.notFound.set(true); this.loading.set(false); }
    });
  }

  ampliar(imagen: ProductoImagen): void {
    this.imagenAmpliada.set(imagen);
  }

  cerrarAmpliada(): void {
    this.imagenAmpliada.set(null);
  }

  private guardarBlob(blob: Blob, nombreSugerido: string): void {
    const url = window.URL.createObjectURL(blob);
    const enlace = document.createElement('a');
    enlace.href = url;
    enlace.download = nombreSugerido;
    enlace.click();
    window.URL.revokeObjectURL(url);
  }

  descargarImagen(imagen: ProductoImagen): void {
    const producto = this.producto();
    if (!producto) return;

    this.descargando.set(imagen.id);
    this.productoService.descargarImagen(producto.id, imagen.id).subscribe({
      next: (blob) => {
        this.descargando.set(null);
        this.guardarBlob(blob, `${producto.nombre}-${imagen.orden + 1}.jpg`);
      },
      error: () => {
        this.descargando.set(null);
        this.snackBar.open('No se pudo descargar la imagen. El archivo podría ya no estar disponible.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  descargarTodas(): void {
    const producto = this.producto();
    if (!producto) return;

    this.descargando.set(-1);
    this.productoService.descargarTodasLasImagenes(producto.id).subscribe({
      next: (blob) => {
        this.descargando.set(null);
        this.guardarBlob(blob, `${producto.nombre}-imagenes.zip`);
      },
      error: () => {
        this.descargando.set(null);
        this.snackBar.open('No se pudieron descargar las imágenes.', 'Cerrar', { duration: 5000 });
      }
    });
  }
}
