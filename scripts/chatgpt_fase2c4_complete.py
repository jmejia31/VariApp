from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path.cwd()


def write(path: str, content: str) -> None:
    target = ROOT / path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(content.rstrip() + "\n", encoding="utf-8")


def replace_once(path: str, old: str, new: str) -> None:
    target = ROOT / path
    text = target.read_text(encoding="utf-8")
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{path}: se esperaba una coincidencia y se encontraron {count}")
    target.write_text(text.replace(old, new, 1), encoding="utf-8")


def replace_method(path: str, method_name: str, next_method: str, replacement: str) -> None:
    target = ROOT / path
    text = target.read_text(encoding="utf-8")
    pattern = re.compile(
        rf"  private {re.escape(method_name)}\(.*?\n(?=  private {re.escape(next_method)}\()",
        re.DOTALL,
    )
    updated, count = pattern.subn(replacement.rstrip() + "\n\n", text, count=1)
    if count != 1:
        raise RuntimeError(f"{path}: no se pudo reemplazar {method_name}")
    target.write_text(updated, encoding="utf-8")


replace_once(
    "frontend/src/app/core/models/producto.model.ts",
    """export interface ProductoVariante {
  id: number;
  productoId: number;
  productoNombre: string;
  colorId?: number;""",
    """export interface ProductoVariante {
  id: number;
  productoId: number;
  productoNombre: string;
  colorId: number;""",
)

for form_path in (
    "frontend/src/app/features/ventas/venta-form.component.ts",
    "frontend/src/app/features/compras/compra-form.component.ts",
):
    replace_once(form_path, "colorId: item.colorId ?? undefined,", "colorId: item.colorId ?? 0,")

replace_method(
    "frontend/src/app/features/ventas/venta-form.component.ts",
    "agregarProductoEscaneado",
    "incorporarProductoEscaneado",
    """  private agregarProductoEscaneado(item: ProductoEscaneadoVenta): void {
    const coincidencias = this.detalles.controls
      .map((grupo, index) => ({ grupo, index }))
      .filter(({ grupo }) =>
        Number(grupo.value.productoId) === item.productoId
        && Number(grupo.value.productoVarianteId) === item.productoVarianteId
      );

    if (coincidencias.length > 0) {
      const cantidadActual = coincidencias.reduce(
        (total, { grupo }) => total + Number(grupo.value.cantidad || 0),
        0
      );
      const nuevaCantidad = cantidadActual + 1;
      if (nuevaCantidad > item.cantidadDisponible) {
        this.errorEscaneo.set(true);
        this.mensajeEscaneo.set(
          `Stock insuficiente para ${item.productoNombre}. Disponible: ${item.cantidadDisponible}.`
        );
        return;
      }

      coincidencias[0].grupo.patchValue({
        cantidad: nuevaCantidad,
        precioUnitario: item.precio
      });
      coincidencias
        .slice(1)
        .map(({ index }) => index)
        .sort((a, b) => b - a)
        .forEach((index) => this.detalles.removeAt(index));

      this.mensajeEscaneo.set(`${item.productoNombre}: cantidad consolidada en ${nuevaCantidad}.`);
      return;
    }

    this.incorporarProductoEscaneado(item);
    const filaVacia = this.detalles.controls.find((grupo) => !grupo.value.productoId);
    const valores = {
      productoId: item.productoId,
      productoVarianteId: item.productoVarianteId,
      cantidad: 1,
      precioUnitario: item.precio
    };
    if (filaVacia) filaVacia.patchValue(valores);
    else this.agregarDetalle(item.productoId, item.productoVarianteId, 1, item.precio);

    this.errorMessage.set(null);
    this.mensajeEscaneo.set(
      `${item.productoNombre}${item.colorNombre ? ` · ${item.colorNombre}` : ''} agregado a la venta.`
    );
  }""",
)

replace_method(
    "frontend/src/app/features/compras/compra-form.component.ts",
    "agregarProductoEscaneado",
    "incorporarProductoEscaneado",
    """  private agregarProductoEscaneado(item: ProductoEscaneadoCompra): void {
    const coincidencias = this.detalles.controls
      .map((grupo, index) => ({ grupo, index }))
      .filter(({ grupo }) =>
        Number(grupo.value.productoId) === item.productoId
        && Number(grupo.value.productoVarianteId) === item.productoVarianteId
      );

    if (coincidencias.length > 0) {
      const cantidadActual = coincidencias.reduce(
        (total, { grupo }) => total + Number(grupo.value.cantidad || 0),
        0
      );
      const nuevaCantidad = cantidadActual + 1;
      coincidencias[0].grupo.patchValue({
        cantidad: nuevaCantidad,
        costoUnitario: item.costo
      });
      coincidencias
        .slice(1)
        .map(({ index }) => index)
        .sort((a, b) => b - a)
        .forEach((index) => this.detalles.removeAt(index));

      this.mensajeEscaneo.set(`${item.productoNombre}: cantidad consolidada en ${nuevaCantidad}.`);
      return;
    }

    this.incorporarProductoEscaneado(item);
    const filaVacia = this.detalles.controls.find((grupo) => !grupo.value.productoId);
    const valores = {
      productoId: item.productoId,
      productoVarianteId: item.productoVarianteId,
      cantidad: 1,
      costoUnitario: item.costo
    };
    if (filaVacia) filaVacia.patchValue(valores);
    else this.agregarDetalle(item.productoId, item.productoVarianteId, 1, item.costo);

    this.errorMessage.set(null);
    this.mensajeEscaneo.set(
      `${item.productoNombre}${item.colorNombre ? ` · ${item.colorNombre}` : ''} agregado a la compra.`
    );
  }""",
)

write(
    "frontend/src/app/shared/codigo-scanner-dialog/codigo-scanner-dialog.component.ts",
    r"""import { CommonModule } from '@angular/common';
import { Component, OnDestroy, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

const TAMANO_MAXIMO_BYTES = 10 * 1024 * 1024;
const DIMENSION_MAXIMA = 4096;
const PIXELES_MAXIMOS = 16_000_000;
const EXTENSIONES_PERMITIDAS = new Set(['jpg', 'jpeg', 'png', 'webp']);
const MIME_PERMITIDOS = new Set(['image/jpeg', 'image/png', 'image/webp']);

@Component({
  selector: 'app-codigo-scanner-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './codigo-scanner-dialog.component.html',
  styleUrl: './codigo-scanner-dialog.component.scss'
})
export class CodigoScannerDialogComponent implements OnDestroy {
  private static secuencia = 0;
  readonly readerId = `codigo-scanner-reader-${++CodigoScannerDialogComponent.secuencia}`;
  readonly iniciando = signal(false);
  readonly camaraActiva = signal(false);
  readonly procesandoArchivo = signal(false);
  readonly error = signal<string | null>(null);

  private lector?: import('html5-qrcode').Html5Qrcode;
  private resultadoEntregado = false;

  constructor(private readonly dialogRef: MatDialogRef<CodigoScannerDialogComponent>) {}

  async alternarCamara(): Promise<void> {
    if (this.camaraActiva()) {
      await this.detenerCamara();
      return;
    }

    this.error.set(null);
    this.iniciando.set(true);
    try {
      const modulo = await import('html5-qrcode');
      await this.liberarLector();
      this.lector = new modulo.Html5Qrcode(this.readerId, {
        formatsToSupport: [
          modulo.Html5QrcodeSupportedFormats.QR_CODE,
          modulo.Html5QrcodeSupportedFormats.EAN_13,
          modulo.Html5QrcodeSupportedFormats.EAN_8,
          modulo.Html5QrcodeSupportedFormats.UPC_A,
          modulo.Html5QrcodeSupportedFormats.UPC_E,
          modulo.Html5QrcodeSupportedFormats.CODE_128,
          modulo.Html5QrcodeSupportedFormats.CODE_39
        ]
      });

      await this.lector.start(
        { facingMode: 'environment' },
        {
          fps: 10,
          qrbox: (ancho, alto) => {
            const lado = Math.max(160, Math.floor(Math.min(ancho, alto) * 0.72));
            return { width: lado, height: Math.max(120, Math.floor(lado * 0.62)) };
          }
        },
        (codigo) => void this.entregarResultado(codigo),
        () => undefined
      );
      this.camaraActiva.set(true);
    } catch {
      await this.liberarLector();
      this.error.set(
        'No se pudo activar la cámara. Verifica el permiso del navegador, usa HTTPS o selecciona una imagen.'
      );
    } finally {
      this.iniciando.set(false);
    }
  }

  async seleccionarArchivo(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0];
    input.value = '';
    if (!archivo || this.procesandoArchivo()) return;

    this.error.set(null);
    this.procesandoArchivo.set(true);
    try {
      await this.validarArchivo(archivo);
      const modulo = await import('html5-qrcode');
      await this.detenerCamara();
      this.lector = new modulo.Html5Qrcode(this.readerId, {
        formatsToSupport: [
          modulo.Html5QrcodeSupportedFormats.QR_CODE,
          modulo.Html5QrcodeSupportedFormats.EAN_13,
          modulo.Html5QrcodeSupportedFormats.EAN_8,
          modulo.Html5QrcodeSupportedFormats.UPC_A,
          modulo.Html5QrcodeSupportedFormats.UPC_E,
          modulo.Html5QrcodeSupportedFormats.CODE_128,
          modulo.Html5QrcodeSupportedFormats.CODE_39
        ]
      });
      const codigo = await this.lector.scanFile(archivo, true);
      await this.entregarResultado(codigo);
    } catch (error) {
      await this.liberarLector();
      this.error.set(
        error instanceof Error
          ? error.message
          : 'No se encontró un código compatible en la imagen seleccionada.'
      );
    } finally {
      this.procesandoArchivo.set(false);
    }
  }

  async cerrar(): Promise<void> {
    await this.liberarLector();
    this.dialogRef.close();
  }

  ngOnDestroy(): void {
    void this.liberarLector();
  }

  private async entregarResultado(codigo: string): Promise<void> {
    const normalizado = codigo.trim();
    if (!normalizado || this.resultadoEntregado) return;
    this.resultadoEntregado = true;
    await this.liberarLector();
    this.dialogRef.close(normalizado);
  }

  private async detenerCamara(): Promise<void> {
    if (!this.lector || !this.camaraActiva()) return;
    try {
      await this.lector.stop();
    } finally {
      this.camaraActiva.set(false);
      try {
        this.lector.clear();
      } catch {
        // El contenedor puede haberse liberado al cerrar el diálogo.
      }
    }
  }

  private async liberarLector(): Promise<void> {
    if (!this.lector) {
      this.camaraActiva.set(false);
      return;
    }
    try {
      if (this.camaraActiva()) await this.lector.stop();
    } catch {
      // El stream ya pudo haberse detenido por el navegador.
    }
    try {
      this.lector.clear();
    } catch {
      // El elemento puede haber sido destruido.
    }
    this.lector = undefined;
    this.camaraActiva.set(false);
  }

  private async validarArchivo(archivo: File): Promise<void> {
    const extension = archivo.name.split('.').pop()?.toLowerCase() ?? '';
    if (!EXTENSIONES_PERMITIDAS.has(extension) || (archivo.type && !MIME_PERMITIDOS.has(archivo.type))) {
      throw new Error('Selecciona una imagen JPG, JPEG, PNG o WEBP válida.');
    }
    if (archivo.size <= 0 || archivo.size > TAMANO_MAXIMO_BYTES) {
      throw new Error('La imagen debe pesar más de 0 bytes y como máximo 10 MB.');
    }

    const dimensiones = await this.leerDimensiones(archivo);
    if (
      dimensiones.ancho > DIMENSION_MAXIMA
      || dimensiones.alto > DIMENSION_MAXIMA
      || dimensiones.ancho * dimensiones.alto > PIXELES_MAXIMOS
    ) {
      throw new Error('La imagen no puede superar 4096 px por lado ni 16 megapíxeles.');
    }
  }

  private leerDimensiones(archivo: File): Promise<{ ancho: number; alto: number }> {
    return new Promise((resolve, reject) => {
      const url = URL.createObjectURL(archivo);
      const imagen = new Image();
      imagen.onload = () => {
        URL.revokeObjectURL(url);
        resolve({ ancho: imagen.naturalWidth, alto: imagen.naturalHeight });
      };
      imagen.onerror = () => {
        URL.revokeObjectURL(url);
        reject(new Error('La imagen está dañada o no puede leerse.'));
      };
      imagen.src = url;
    });
  }
}
""",
)

write(
    "frontend/src/app/shared/codigo-scanner-dialog/codigo-scanner-dialog.component.html",
    r"""<h2 mat-dialog-title>
  <mat-icon>qr_code_scanner</mat-icon>
  Escanear código
</h2>

<mat-dialog-content>
  <p class="scanner-description">
    Usa la cámara trasera o selecciona una imagen local. La imagen se procesa únicamente en este navegador.
  </p>

  <div class="reader-shell" [class.reader-active]="camaraActiva()">
    <div [id]="readerId" class="reader" aria-label="Vista de cámara para escanear códigos"></div>
    @if (!camaraActiva() && !iniciando()) {
      <div class="reader-placeholder">
        <mat-icon>photo_camera</mat-icon>
        <span>La cámara está detenida</span>
      </div>
    }
    @if (iniciando()) {
      <div class="reader-placeholder">
        <mat-spinner diameter="38"></mat-spinner>
        <span>Solicitando permiso de cámara…</span>
      </div>
    }
  </div>

  @if (error()) {
    <p class="scanner-dialog-error" role="alert">
      <mat-icon>error</mat-icon>
      {{ error() }}
    </p>
  }

  <input
    #archivoInput
    type="file"
    hidden
    accept=".jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp"
    (change)="seleccionarArchivo($event)">
</mat-dialog-content>

<mat-dialog-actions align="end">
  <button
    mat-stroked-button
    type="button"
    (click)="alternarCamara()"
    [disabled]="iniciando() || procesandoArchivo()">
    <mat-icon>{{ camaraActiva() ? 'videocam_off' : 'photo_camera' }}</mat-icon>
    {{ camaraActiva() ? 'Detener cámara' : 'Activar cámara' }}
  </button>

  <button
    mat-stroked-button
    type="button"
    (click)="archivoInput.click()"
    [disabled]="iniciando() || procesandoArchivo()">
    @if (procesandoArchivo()) { <mat-spinner diameter="18"></mat-spinner> }
    @else { <mat-icon>image_search</mat-icon> }
    Leer imagen
  </button>

  <button mat-button type="button" (click)="cerrar()">Cerrar</button>
</mat-dialog-actions>
""",
)

write(
    "frontend/src/app/shared/codigo-scanner-dialog/codigo-scanner-dialog.component.scss",
    r""":host {
  display: block;
}

h2 {
  display: flex;
  align-items: center;
  gap: 0.65rem;
}

.scanner-description {
  margin: 0 0 1rem;
  color: var(--text-secondary, #536273);
  line-height: 1.5;
}

.reader-shell {
  position: relative;
  min-height: 300px;
  overflow: hidden;
  border: 1px solid rgba(91, 117, 139, 0.35);
  border-radius: 16px;
  background: #07131d;
}

.reader {
  min-height: 300px;
}

.reader-placeholder {
  position: absolute;
  inset: 0;
  display: grid;
  place-content: center;
  justify-items: center;
  gap: 0.75rem;
  color: #d9e7f2;
  pointer-events: none;
}

.reader-placeholder mat-icon {
  width: 44px;
  height: 44px;
  font-size: 44px;
}

.reader-active .reader-placeholder {
  display: none;
}

.scanner-dialog-error {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  margin: 1rem 0 0;
  padding: 0.8rem 1rem;
  border-radius: 10px;
  color: #9f1239;
  background: #fff1f2;
}

mat-dialog-actions button {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
}

@media (max-width: 640px) {
  .reader-shell,
  .reader {
    min-height: 240px;
  }

  mat-dialog-actions {
    display: grid;
    grid-template-columns: 1fr;
  }

  mat-dialog-actions button {
    width: 100%;
    justify-content: center;
  }
}
""",
)

write(
    "frontend/src/app/shared/codigo-scanner-input/codigo-scanner-input.component.ts",
    r"""import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  Output,
  ViewChild,
  signal
} from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CodigoScannerDialogComponent } from '../codigo-scanner-dialog/codigo-scanner-dialog.component';

@Component({
  selector: 'app-codigo-scanner-input',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './codigo-scanner-input.component.html',
  styleUrl: './codigo-scanner-input.component.scss'
})
export class CodigoScannerInputComponent implements AfterViewInit {
  @Input() procesando = false;
  @Input() mensaje: string | null = null;
  @Input() error = false;
  @Input() etiqueta = 'Escanear o escribir SKU / código de barras';
  @Output() readonly codigoLeido = new EventEmitter<string>();

  @ViewChild('codigoInput') private codigoInput?: ElementRef<HTMLInputElement>;

  readonly activo = signal(false);
  readonly codigo = new FormControl('', { nonNullable: true });

  constructor(private readonly dialog: MatDialog) {}

  ngAfterViewInit(): void {
    if (this.activo()) this.enfocar();
  }

  alternarModo(): void {
    this.activo.update((valor) => !valor);
    this.codigo.setValue('', { emitEvent: false });
    if (this.activo()) queueMicrotask(() => this.enfocar());
  }

  abrirCamara(): void {
    const referencia = this.dialog.open(CodigoScannerDialogComponent, {
      width: 'min(94vw, 680px)',
      maxWidth: '680px',
      disableClose: true,
      autoFocus: false,
      restoreFocus: true
    });

    referencia.afterClosed().subscribe((codigo: string | undefined) => {
      const normalizado = codigo?.trim();
      if (normalizado) this.codigoLeido.emit(normalizado);
    });
  }

  procesar(): void {
    if (!this.activo() || this.procesando) return;
    const valor = this.codigo.value.trim();
    if (!valor) {
      this.enfocar();
      return;
    }

    this.codigoLeido.emit(valor);
    this.codigo.setValue('', { emitEvent: false });
  }

  reenfocar(): void {
    if (this.activo()) queueMicrotask(() => this.enfocar());
  }

  private enfocar(): void {
    this.codigoInput?.nativeElement.focus({ preventScroll: true });
  }
}
""",
)

write(
    "frontend/src/app/shared/codigo-scanner-input/codigo-scanner-input.component.html",
    r"""<section class="scanner-card" [class.scanner-card-active]="activo()" aria-labelledby="scanner-title">
  <div class="scanner-heading">
    <div>
      <h3 id="scanner-title"><mat-icon>barcode_scanner</mat-icon> Escáner de productos</h3>
      <p>Usa un lector USB/Bluetooth, la cámara del dispositivo o una imagen guardada.</p>
    </div>
    <div class="scanner-actions">
      <button mat-stroked-button type="button" (click)="alternarModo()" [attr.aria-pressed]="activo()">
        <mat-icon>{{ activo() ? 'pause_circle' : 'keyboard' }}</mat-icon>
        {{ activo() ? 'Desactivar lector físico' : 'Activar lector físico' }}
      </button>
      <button mat-flat-button color="primary" type="button" (click)="abrirCamara()" [disabled]="procesando">
        <mat-icon>photo_camera</mat-icon>
        Cámara o imagen
      </button>
    </div>
  </div>

  @if (activo()) {
    <div class="scanner-capture">
      <mat-form-field appearance="outline" subscriptSizing="dynamic">
        <mat-label>{{ etiqueta }}</mat-label>
        <input
          #codigoInput
          matInput
          autocomplete="off"
          inputmode="text"
          maxlength="100"
          [formControl]="codigo"
          [disabled]="procesando"
          (keydown.enter)="$event.preventDefault(); procesar()"
          aria-describedby="scanner-help scanner-feedback">
        @if (procesando) { <mat-spinner matSuffix diameter="18"></mat-spinner> }
        @else { <mat-icon matSuffix>keyboard_return</mat-icon> }
      </mat-form-field>
      <button mat-flat-button color="primary" type="button" (click)="procesar()" [disabled]="procesando || !codigo.value.trim()">
        <mat-icon>add_shopping_cart</mat-icon>
        Agregar
      </button>
    </div>
    <p id="scanner-help" class="scanner-help">
      El foco vuelve a este campo únicamente después de una lectura. El sistema no interrumpe otros campos del formulario.
    </p>
  }

  @if (mensaje) {
    <p id="scanner-feedback" class="scanner-feedback" [class.scanner-error]="error" role="status" aria-live="polite">
      <mat-icon>{{ error ? 'error' : 'check_circle' }}</mat-icon>
      {{ mensaje }}
    </p>
  }
</section>
""",
)

write(
    "frontend/src/app/shared/codigo-scanner-input/codigo-scanner-input.component.scss",
    r""".scanner-card {
  margin: 0 0 1.25rem;
  padding: 1rem 1.1rem;
  border: 1px solid rgba(55, 88, 116, 0.25);
  border-radius: 14px;
  background: linear-gradient(135deg, rgba(8, 145, 178, 0.06), rgba(37, 99, 235, 0.04));
  transition: border-color 160ms ease, box-shadow 160ms ease;
}

.scanner-card-active {
  border-color: rgba(2, 132, 199, 0.55);
  box-shadow: 0 8px 28px rgba(2, 132, 199, 0.1);
}

.scanner-heading {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 1rem;
}

.scanner-heading h3 {
  display: flex;
  align-items: center;
  gap: 0.45rem;
  margin: 0;
  font-size: 1rem;
}

.scanner-heading p {
  margin: 0.35rem 0 0;
  color: var(--text-secondary, #5b6876);
  font-size: 0.9rem;
}

.scanner-actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 0.65rem;
}

.scanner-actions button {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
}

.scanner-capture {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  align-items: center;
  gap: 0.75rem;
  margin-top: 1rem;
}

.scanner-capture mat-form-field {
  width: 100%;
}

.scanner-help {
  margin: 0.55rem 0 0;
  color: var(--text-secondary, #5b6876);
  font-size: 0.82rem;
}

.scanner-feedback {
  display: flex;
  align-items: center;
  gap: 0.45rem;
  margin: 0.8rem 0 0;
  padding: 0.65rem 0.8rem;
  border-radius: 9px;
  color: #166534;
  background: #f0fdf4;
}

.scanner-error {
  color: #9f1239;
  background: #fff1f2;
}

@media (max-width: 720px) {
  .scanner-heading,
  .scanner-actions {
    display: grid;
    grid-template-columns: 1fr;
  }

  .scanner-actions {
    width: 100%;
  }

  .scanner-actions button,
  .scanner-capture button {
    width: 100%;
    justify-content: center;
  }

  .scanner-capture {
    grid-template-columns: 1fr;
  }
}
""",
)

vercel_path = ROOT / "frontend/vercel.json"
vercel = json.loads(vercel_path.read_text(encoding="utf-8"))
vercel["headers"] = [
    {
        "source": "/(.*)",
        "headers": [
            {
                "key": "Permissions-Policy",
                "value": "camera=(self), microphone=(), geolocation=()",
            }
        ],
    }
]
vercel_path.write_text(json.dumps(vercel, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

write(
    "frontend/scripts/validate-scanner-2c4.mjs",
    r"""import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

const root = process.cwd();
const read = (path) => readFileSync(resolve(root, path), 'utf8');
const assert = (condition, message) => {
  if (!condition) {
    console.error(`Fase 2C.4: ${message}`);
    process.exit(1);
  }
};

const packageJson = JSON.parse(read('package.json'));
const dialog = read('src/app/shared/codigo-scanner-dialog/codigo-scanner-dialog.component.ts');
const input = read('src/app/shared/codigo-scanner-input/codigo-scanner-input.component.ts');
const venta = read('src/app/features/ventas/venta-form.component.ts');
const compra = read('src/app/features/compras/compra-form.component.ts');
const ventaHtml = read('src/app/features/ventas/venta-form.component.html');
const compraHtml = read('src/app/features/compras/compra-form.component.html');
const ventaService = read('src/app/services/venta.service.ts');
const compraService = read('src/app/services/compra.service.ts');
const vercel = read('vercel.json');

assert(packageJson.dependencies?.['html5-qrcode'] === '2.3.8', 'html5-qrcode debe quedar fijado en 2.3.8.');
assert(dialog.includes("await import('html5-qrcode')"), 'la cámara debe cargarse de forma diferida.');
assert(dialog.includes('.scanFile('), 'debe existir lectura local desde imagen.');
assert(dialog.includes('.stop()') && dialog.includes('.clear()'), 'el stream y el lector deben liberarse.');
assert(dialog.includes('10 * 1024 * 1024'), 'debe existir límite de imagen de 10 MB.');
assert(dialog.includes('16_000_000') && dialog.includes('4096'), 'deben existir límites de dimensiones y megapíxeles.');
assert(input.includes('CodigoScannerDialogComponent'), 'el lector físico debe integrar el diálogo de cámara.');
assert(!input.includes('window.addEventListener'), 'el lector no debe capturar eventos globales ni robar el foco.');
assert(venta.includes('cantidad consolidada') && compra.includes('cantidad consolidada'), 'los escaneos repetidos deben consolidarse.');
assert(ventaHtml.includes('app-codigo-scanner-input') && compraHtml.includes('app-codigo-scanner-input'), 'ventas y compras deben mostrar el escáner.');
assert(ventaService.includes('/productos/por-codigo') && compraService.includes('/productos/por-codigo'), 'ambos formularios deben usar resolución exacta.');
assert(vercel.includes('camera=(self), microphone=(), geolocation=()'), 'Vercel debe restringir cámara al propio origen.');

console.log('Fase 2C.4: validación estática del frontend del escáner aprobada.');
""",
)

package_path = ROOT / "frontend/package.json"
package_json = json.loads(package_path.read_text(encoding="utf-8"))
lint = package_json["scripts"]["lint"]
validator = "node scripts/validate-scanner-2c4.mjs"
if validator not in lint:
    package_json["scripts"]["lint"] = f"{lint} && {validator}"
package_path.write_text(json.dumps(package_json, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

for temporary in (
    ".github/workflows/chatgpt-fase2c4-dependencia.yml",
    ".github/workflows/chatgpt-fase2c4-completar.yml",
    "scripts/chatgpt_fase2c4_complete.py",
):
    target = ROOT / temporary
    if target.exists():
        target.unlink()
