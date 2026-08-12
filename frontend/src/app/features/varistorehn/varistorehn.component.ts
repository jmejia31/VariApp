import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { EmpresaIdentidadService } from '../../services/empresa-identidad.service';
import { ProductoCatalogoPublico, VaristorehnService } from './varistorehn.service';

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
  private readonly varistorehnService = inject(VaristorehnService);
  readonly identidad = inject(EmpresaIdentidadService);

  readonly productos = signal<ProductoTienda[]>([]);
  readonly cargando = signal(true);
  readonly errorCatalogo = signal('');
  readonly carrito = signal<ItemCarrito[]>(this.leerCarrito());
  readonly carritoAbierto = signal(false);
  readonly imagenActiva = signal<Record<number, number>>({});

  ngOnInit(): void {
    this.varistorehnService.obtenerProductos().subscribe({
      next: (response) => {
        this.productos.set(response.data?.items
          .map((producto) => this.mapearProducto(producto)) ?? []);
        this.cargando.set(false);
      },
      error: () => {
        this.errorCatalogo.set('No fue posible cargar el catálogo en este momento.');
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
    const telefono = (this.identidad.config().whatsApp || '').replace(/\D/g, '');
    const destino = telefono ? `https://wa.me/${telefono}` : 'https://wa.me/';
    window.open(`${destino}?text=${encodeURIComponent(mensaje)}`, '_blank', 'noopener,noreferrer');
  }

  enlaceWhatsapp(): string {
    const telefono = (this.identidad.config().whatsApp || '').replace(/\D/g, '');
    return telefono ? `https://wa.me/${telefono}` : 'https://wa.me/';
  }

  private mapearProducto(producto: ProductoCatalogoPublico): ProductoTienda {
    const imagenes = producto.imagenes
      .slice()
      .sort((a, b) => a.orden - b.orden)
      .map((imagen) => imagen.url);
    if (imagenes.length === 0 && producto.imagenPrincipalUrl) imagenes.push(producto.imagenPrincipalUrl);

    return {
      id: producto.id,
      nombre: producto.nombre,
      precio: producto.precio,
      descripcion: producto.descripcion || `${producto.marcaNombre || ''} ${producto.modeloNombre || ''}`.trim(),
      categoria: producto.categoriaNombre || 'Colección',
      imagenes,
      disponible: !producto.estaAgotado && producto.cantidadDisponible > 0
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
