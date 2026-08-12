import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Producto } from '../../core/models/producto.model';
import { ProductoService } from '../../services/producto.service';

interface ProductoTienda {
  id: number;
  nombre: string;
  precio: number;
  descripcion: string;
  categoria: string;
  imagenes: string[];
  disponible: boolean;
}

interface ItemCarrito extends ProductoTienda {
  unidades: number;
}

@Component({
  selector: 'app-varistorehn',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './varistorehn.component.html',
  styleUrl: './varistorehn.component.scss'
})
export class VaristorehnComponent implements OnInit {
  private readonly productoService = inject(ProductoService);

  readonly productos = signal<ProductoTienda[]>([]);
  readonly cargando = signal(true);
  readonly errorCatalogo = signal('');
  readonly carrito = signal<ItemCarrito[]>(this.leerCarrito());
  readonly carritoAbierto = signal(false);
  readonly imagenActiva = signal<Record<number, number>>({});

  ngOnInit(): void {
    this.productoService.getPaged({ page: 1, pageSize: 48, activo: true }).subscribe({
      next: (response) => {
        this.productos.set(response.data?.items
          ?.filter((producto) => producto.activo)
          .map((producto) => this.mapearProducto(producto)) ?? []);
        this.cargando.set(false);
      },
      error: () => {
        this.errorCatalogo.set('No fue posible cargar el catálogo. La consulta actual requiere una sesión autorizada.');
        this.cargando.set(false);
      }
    });
  }

  totalUnidades(): number {
    return this.carrito().reduce((total, item) => total + item.unidades, 0);
  }

  totalCarrito(): number {
    return this.carrito().reduce((total, item) => total + item.precio * item.unidades, 0);
  }

  agregar(producto: ProductoTienda): void {
    if (!producto.disponible) return;
    const items = [...this.carrito()];
    const existente = items.find((item) => item.id === producto.id);
    if (existente) existente.unidades += 1;
    else items.push({ ...producto, unidades: 1 });
    this.guardarCarrito(items);
    this.carritoAbierto.set(true);
  }

  cambiarUnidades(id: number, cambio: number): void {
    const items = this.carrito()
      .map((item) => item.id === id ? { ...item, unidades: item.unidades + cambio } : item)
      .filter((item) => item.unidades > 0);
    this.guardarCarrito(items);
  }

  mostrarImagen(productoId: number, indice: number): void {
    this.imagenActiva.update((estado) => ({ ...estado, [productoId]: indice }));
  }

  indiceImagen(productoId: number): number {
    return this.imagenActiva()[productoId] || 0;
  }

  moverImagen(producto: ProductoTienda, cambio: number): void {
    const actual = this.indiceImagen(producto.id);
    const siguiente = (actual + cambio + producto.imagenes.length) % producto.imagenes.length;
    this.mostrarImagen(producto.id, siguiente);
  }

  realizarPedido(): void {
    if (this.carrito().length === 0) return;
    const detalle = this.carrito()
      .map((item) => `• ${item.nombre} x${item.unidades} — ${this.moneda(item.precio * item.unidades)}`)
      .join('\n');
    const mensaje = `Hola VARISTOREHN, deseo realizar este pedido:\n\n${detalle}\n\nTotal: ${this.moneda(this.totalCarrito())}`;
    window.open(`https://wa.me/?text=${encodeURIComponent(mensaje)}`, '_blank', 'noopener,noreferrer');
  }

  private mapearProducto(producto: Producto): ProductoTienda {
    const imagenes = producto.imagenes
      .slice()
      .sort((a, b) => a.orden - b.orden)
      .map((imagen) => imagen.url);
    if (imagenes.length === 0 && producto.imagenPrincipalUrl) imagenes.push(producto.imagenPrincipalUrl);

    return {
      id: producto.id,
      nombre: producto.nombre,
      precio: producto.precioMinimo || producto.precio,
      descripcion: producto.descripcion || `${producto.marcaNombre || producto.marca} ${producto.modeloNombre || producto.modelo}`.trim(),
      categoria: producto.categoriaNombre || 'Colección',
      imagenes,
      disponible: !producto.estaAgotado && producto.cantidad > 0
    };
  }

  private leerCarrito(): ItemCarrito[] {
    try {
      return JSON.parse(localStorage.getItem('varistorehn_carrito') ?? '[]') as ItemCarrito[];
    } catch {
      return [];
    }
  }

  private guardarCarrito(items: ItemCarrito[]): void {
    this.carrito.set(items);
    localStorage.setItem('varistorehn_carrito', JSON.stringify(items));
  }

  private moneda(valor: number): string {
    return new Intl.NumberFormat('es-HN', { style: 'currency', currency: 'HNL' }).format(valor);
  }
}
