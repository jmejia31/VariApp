/** Pure catalog/cart rules: deliberately independent of Angular and the backend. */
import type {
  FiltrosCatalogo,
  ImagenCatalogo,
  ItemCarrito,
  ModeloTienda,
  ProductoCatalogoPublico,
  ProductoTienda,
  ReferenciaCarrito
} from './varistorehn.models';

export type {
  CategoriaCatalogoPublico,
  CategoriaTienda,
  EstadoConsultaPublica,
  EstadoDisponibilidad,
  EstadoPromocion,
  FiltrosCatalogo,
  ImagenCatalogo,
  ItemCarrito,
  ModeloCatalogoPublico,
  ModeloTienda,
  OrdenCatalogo,
  ProductoCatalogoPublico,
  ProductoTienda,
  ReferenciaCarrito
} from './varistorehn.models';

export function normalizarTexto(valor: string): string {
  return valor.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase('es').trim();
}
const stockSeguro = (valor: number): number => Number.isFinite(valor) ? Math.max(0, Math.floor(valor)) : 0;
const precioValido = (valor: number): boolean => Number.isFinite(valor) && valor >= 0;
const listaImagenes = (imagenes: ImagenCatalogo[] | undefined): string[] =>
  [...(imagenes ?? [])].sort((a, b) => a.orden - b.orden).map(i => i.url).filter(Boolean);

export function mapearProducto(producto: ProductoCatalogoPublico): ProductoTienda {
  const imagenes = listaImagenes(producto.imagenes);
  if (!imagenes.length && producto.imagenPrincipalUrl) imagenes.push(producto.imagenPrincipalUrl);
  const activo = producto.activo !== false;
  const stockProducto = stockSeguro(producto.cantidadDisponible);
  const modelos: ModeloTienda[] = (producto.modelos ?? []).map(modelo => {
    const fotos = listaImagenes(modelo.imagenes);
    const stock = stockSeguro(modelo.cantidadDisponible);
    return {
      // The public API groups by model AND brand, including null model IDs.
      clave: JSON.stringify([modelo.modeloId ?? null, modelo.modeloNombre ?? '', modelo.marcaNombre ?? '']),
      modeloId: modelo.modeloId ?? null,
      nombre: modelo.modeloNombre || 'Modelo general',
      marca: modelo.marcaNombre || producto.marcaNombre || '',
      sku: modelo.sku?.trim() || '',
      precio: precioValido(modelo.precio) ? modelo.precio : 0,
      stock,
      disponible: activo && stock > 0 && !modelo.estaAgotado && precioValido(modelo.precio),
      imagenes: fotos.length ? fotos : imagenes
    };
  });
  if (!modelos.length) modelos.push({
    clave: 'base', modeloId: null, nombre: producto.modeloNombre || 'Modelo general',
    marca: producto.marcaNombre || '', sku: producto.sku?.trim() || '',
    precio: precioValido(producto.precio) ? producto.precio : 0,
    stock: stockProducto,
    disponible: activo && !producto.estaAgotado && stockProducto > 0 && precioValido(producto.precio),
    imagenes
  });
  const disponibles = modelos.filter(m => m.disponible);
  const precio = Math.min(...(disponibles.length ? disponibles : modelos).map(m => m.precio));
  const precioOferta = typeof producto.precioOferta === 'number' && precioValido(producto.precioOferta)
    ? producto.precioOferta : null;
  return {
    id: producto.id,
    slug: producto.slug?.trim() || '',
    nombre: producto.nombre,
    descripcion: producto.descripcion || '',
    categoriaId: producto.categoriaId ?? null,
    categoria: producto.categoriaNombre || 'Otros productos',
    marca: producto.marcaNombre || '',
    sku: producto.sku?.trim() || '',
    precio,
    precioOferta,
    stock: stockProducto,
    disponible: activo && disponibles.length > 0,
    activo,
    destacado: Boolean(producto.esDestacado),
    fechaCreacion: producto.fechaCreacion || null,
    imagenes,
    modelos
  };
}

export function filtrarProductos(productos: ProductoTienda[], filtros: FiltrosCatalogo): ProductoTienda[] {
  const palabras = normalizarTexto(filtros.busqueda).split(/\s+/).filter(Boolean);
  const resultado = productos.filter(p => {
    const texto = normalizarTexto([p.nombre, p.descripcion, p.categoria, p.marca, p.sku,
      ...p.modelos.flatMap(m => [m.nombre, m.marca, m.sku])].join(' '));
    return p.activo
      && (!filtros.categoria || p.categoria === filtros.categoria)
      && (!filtros.soloDisponibles || p.disponible)
      && (filtros.precioMaximo === null || p.precio <= filtros.precioMaximo)
      && palabras.every(palabra => texto.includes(palabra));
  });
  switch (filtros.orden) {
    case 'precio-asc': return resultado.sort((a, b) => a.precio - b.precio);
    case 'precio-desc': return resultado.sort((a, b) => b.precio - a.precio);
    case 'nombre': return resultado.sort((a, b) => a.nombre.localeCompare(b.nombre, 'es'));
    default: return resultado.sort((a, b) => Number(b.disponible) - Number(a.disponible));
  }
}

/**
 * Precio efectivo único para catálogo, detalle y carrito.
 * Una oferta solo se considera válida si es finita, no negativa y menor al precio de la variante.
 * La vigencia temporal de promociones pertenece a Fase 9 y debe venir resuelta por la fuente pública.
 */
export function precioVenta(producto: ProductoTienda, modelo: ModeloTienda): number {
  const oferta = producto.precioOferta;
  return typeof oferta === 'number' && precioValido(oferta) && oferta < modelo.precio
    ? oferta
    : modelo.precio;
}

export function crearItem(producto: ProductoTienda, modelo: ModeloTienda, unidades = 1): ItemCarrito {
  return {
    clave: `${producto.id}:${modelo.clave}`, productoId: producto.id, modeloClave: modelo.clave,
    modeloId: modelo.modeloId, nombre: producto.nombre, modelo: modelo.nombre,
    precio: precioVenta(producto, modelo), stock: modelo.stock,
    unidades: Math.max(1, Math.min(stockSeguro(unidades), modelo.stock)),
    imagen: modelo.imagenes[0] || '', ilustracion: producto.ilustracion || 'paquete'
  };
}

export function agregarItem(items: ItemCarrito[], producto: ProductoTienda, modelo: ModeloTienda): ItemCarrito[] {
  if (!modelo.disponible || modelo.stock < 1) return items;
  const item = crearItem(producto, modelo);
  const anterior = items.find(i => i.clave === item.clave);
  if (!anterior) return [...items, item];
  if (anterior.unidades >= modelo.stock) return items;
  return items.map(i => i.clave === item.clave ? { ...item, unidades: i.unidades + 1 } : i);
}

export function cambiarCantidad(items: ItemCarrito[], clave: string, cambio: number): ItemCarrito[] {
  if (!Number.isInteger(cambio)) return items;
  return items.map(i => i.clave === clave ? { ...i, unidades: Math.min(i.stock, i.unidades + cambio) } : i)
    .filter(i => i.unidades > 0);
}

export function referenciasCarrito(items: ItemCarrito[]): ReferenciaCarrito[] {
  // Never persist prices, stock, image URLs or customer/payment information.
  return items.map(({ productoId, modeloClave, unidades }) => ({ productoId, modeloClave, unidades }));
}

export function restaurarCarrito(valor: unknown, productos: ProductoTienda[]): ItemCarrito[] {
  if (!Array.isArray(valor)) return [];
  const resultado = new Map<string, ItemCarrito>();
  for (const ref of valor.slice(0, 500)) {
    if (!ref || !Number.isInteger(ref.productoId) || typeof ref.modeloClave !== 'string'
      || !Number.isInteger(ref.unidades) || ref.unidades <= 0) continue;
    const producto = productos.find(p => p.id === ref.productoId);
    const modelo = producto?.modelos.find(m => m.clave === ref.modeloClave && m.disponible);
    if (!producto || !modelo) continue;
    const item = crearItem(producto, modelo, ref.unidades);
    const anterior = resultado.get(item.clave);
    if (anterior) item.unidades = Math.min(modelo.stock, anterior.unidades + item.unidades);
    resultado.set(item.clave, item);
  }
  return [...resultado.values()];
}

export function totalCarrito(items: ItemCarrito[]): number {
  return items.reduce((total, i) => total + Math.round(i.precio * 100) * i.unidades, 0) / 100;
}

export function telefonoWhatsapp(valor?: string): string {
  const limpio = (valor || '').trim();
  // Accept a local Honduran number or an international number; reject prose/URLs.
  if (!/^[+\d\s().-]+$/.test(limpio)) return '';
  let numero = limpio.replace(/\D/g, '');
  if (/^0+$/.test(numero)) return '';
  if (numero.startsWith('00')) numero = numero.slice(2);
  if (numero.length === 8) numero = `504${numero}`;
  return /^[1-9]\d{9,14}$/.test(numero) ? numero : '';
}

/** Illustrative inventory only. Never written to the database or sent to a checkout. */
export function crearCatalogoEjemplo(): ProductoTienda[] {
  const ejemplos: Array<[string, string, number, number, string, string]> = [
    ['Laptop Pro 14', 'Computadoras', 18490, 8, 'laptop', 'Tu espacio de trabajo, donde quieras. Pantalla de 14 pulgadas, SSD y diseño ligero.'],
    ['Audífonos Wireless Studio', 'Audio', 1490, 18, 'audio', 'Desconéctate del ruido y conecta con tu música. Conexión inalámbrica y almohadillas suaves.'],
    ['Smartphone Nova', 'Celulares', 7990, 12, 'telefono', 'Todo tu día en una pantalla. Cámara dual, carga rápida y amplio almacenamiento.'],
    ['Monitor Vision 27', 'Computadoras', 5890, 6, 'monitor', 'Más espacio para tus ideas. Pantalla de 27 pulgadas y soporte de escritorio.'],
    ['Control Game One', 'Gaming', 1190, 15, 'gaming', 'Comodidad en cada partida. Control inalámbrico con agarre ergonómico.'],
    ['Smartwatch Active', 'Accesorios', 1890, 9, 'reloj', 'Un compañero para tu rutina. Pantalla táctil, notificaciones y registro de actividad.'],
    ['Bocina Sound Mini', 'Audio', 890, 24, 'bocina', 'Tu playlist te acompaña. Formato compacto con conexión Bluetooth.'],
    ['Cámara Home Connect', 'Hogar inteligente', 1290, 0, 'camara', 'Conecta con tu espacio desde el celular. Cámara para interiores con base ajustable.'],
    ['Teclado Mechanical TKL', 'Gaming', 1690, 7, 'teclado', 'Un escritorio a tu medida. Teclado mecánico compacto para trabajar y jugar.'],
    ['Tablet Air 10', 'Celulares', 6490, 5, 'telefono', 'Lee, crea y disfruta en una pantalla de 10 pulgadas. Ligera y fácil de llevar.'],
    ['Laptop Everyday 15', 'Computadoras', 12990, 4, 'laptop', 'Tu aliada para estudiar y trabajar. Pantalla amplia y almacenamiento SSD.'],
    ['Hub USB-C Connect', 'Accesorios', 690, 20, 'paquete', 'Dale más posibilidades a tu equipo. Conecta tus accesorios en un solo lugar.'],
    ['Bocina Home Sound', 'Hogar inteligente', 2290, 10, 'bocina', 'Llena tu espacio de sonido. Diseño discreto para tu hogar.'],
    ['Audífonos Travel', 'Audio', 790, 14, 'audio', 'Lleva tu música a todas partes. Diseño plegable y conexión inalámbrica.']
  ];
  return ejemplos.map(([nombre, categoriaNombre, precio, cantidadDisponible, ilustracion, descripcion], indice) => {
    const producto = mapearProducto({
      id: indice + 1,
      slug: `demo-producto-${indice + 1}`,
      nombre,
      categoriaNombre,
      precio,
      cantidadDisponible,
      descripcion,
      activo: true,
      esDestacado: indice < 3,
      marcaNombre: 'Colección demo',
      estaAgotado: cantidadDisponible === 0,
      imagenes: [],
      modelos: indice === 0 ? [
        { modeloId: 101, modeloNombre: '8 GB / 256 GB', marcaNombre: 'Demo', precio, cantidadDisponible: 5, estaAgotado: false, imagenes: [] },
        { modeloId: 102, modeloNombre: '16 GB / 512 GB', marcaNombre: 'Demo', precio: precio + 2500, cantidadDisponible: 3, estaAgotado: false, imagenes: [] }
      ] : []
    });
    return { ...producto, ilustracion };
  });
}
