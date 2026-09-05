import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { EmpresaIdentidadService } from '../../services/empresa-identidad.service';
import { ModeloCatalogoPublico, ProductoCatalogoPublico, VaristorehnService } from './varistorehn.service';

interface ProductoTienda {
  id: number;
  nombre: string;
  precio: number;
  descripcion: string;
  categoria: string;
  imagenes: string[];
  modelos: ModeloTienda[];
  disponible: boolean;
}

interface ModeloTienda {
  clave: string;
  nombre: string;
  marca: string;
  precio: number;
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
  readonly productoDetalle = signal<ProductoTienda | null>(null);
  readonly imagenActiva = signal<Record<number, number>>({});
  readonly modeloActivo = signal<Record<number, string>>({});
  readonly categoriaActiva = signal('Todas');
  readonly categorias = computed(() => ['Todas', ...new Set(this.productos().map((producto) => producto.categoria).filter(Boolean))]);
  readonly productosVisibles = computed(() => this.categoriaActiva() === 'Todas'
    ? this.productos()
    : this.productos().filter((producto) => producto.categoria === this.categoriaActiva()));

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

  seleccionarCategoria(categoria: string): void {
    this.categoriaActiva.set(categoria);
  }

  abrirDetalle(producto: ProductoTienda): void {
    this.productoDetalle.set(producto);
  }

  cerrarDetalle(): void {
    this.productoDetalle.set(null);
  }

  imagenCategoria(categoria: string): string | null {
    return this.productos().find((producto) => producto.categoria === categoria)?.imagenes[0] ?? null;
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

  claveModelo(producto: ProductoTienda): string {
    return this.modeloActivo()[producto.id] || producto.modelos[0]?.clave || 'base';
  }

  modeloSeleccionado(producto: ProductoTienda): ModeloTienda | null {
    const clave = this.claveModelo(producto);
    return producto.modelos.find(modelo => modelo.clave === clave) ?? null;
  }

  seleccionarModelo(producto: ProductoTienda, clave: string): void {
    this.modeloActivo.update((estado) => ({ ...estado, [producto.id]: clave }));
    this.mostrarImagen(producto.id, 0);
  }

  imagenesVisibles(producto: ProductoTienda): string[] {
    return this.modeloSeleccionado(producto)?.imagenes ?? producto.imagenes;
  }

  productoSeleccionado(producto: ProductoTienda): ProductoTienda {
    const modelo = this.modeloSeleccionado(producto);
    if (!modelo) return producto;
    return {
      ...producto,
      nombre: modelo.nombre ? `${producto.nombre} · ${modelo.nombre}` : producto.nombre,
      precio: modelo.precio || producto.precio,
      imagenes: modelo.imagenes,
      disponible: modelo.disponible
    };
  }

  indiceImagen(productoId: number): number {
    return this.imagenActiva()[productoId] || 0;
  }

  moverImagen(producto: ProductoTienda, cambio: number): void {
    const imagenes = this.imagenesVisibles(producto);
    const actual = this.indiceImagen(producto.id);
    const siguiente = (actual + cambio + imagenes.length) % imagenes.length;
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

    const modelos = (producto.modelos ?? []).map((modelo) => {
      const imagenesModelo = modelo.imagenes.map((imagen) => imagen.url).filter(Boolean);
      return {
        clave: modelo.modeloId == null ? 'base' : String(modelo.modeloId),
        nombre: modelo.modeloNombre || 'Modelo general',
        marca: modelo.marcaNombre || producto.marcaNombre || '',
        precio: modelo.precio,
        imagenes: imagenesModelo.length > 0 ? imagenesModelo : imagenes,
        disponible: !modelo.estaAgotado && modelo.cantidadDisponible > 0
      };
    });

    return {
      id: producto.id,
      nombre: producto.nombre,
      precio: producto.precio,
      descripcion: producto.descripcion || `${producto.marcaNombre || ''} ${producto.modeloNombre || ''}`.trim(),
      categoria: producto.categoriaNombre || 'Colección',
      imagenes,
      modelos,
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
