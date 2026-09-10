import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface RentabilidadResultado {
  AgrupacionId: string | number;
  Agrupacion: string;
  Nombre: string;
  Venta: number;
  Costo: number;
  UtilidadBruta: number;
  IncluyeDescuentoEncabezadoEnUtilidad: boolean;
  Semantica: string;
}

@Component({
  selector: 'app-rentabilidad-resultados-tabla',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './rentabilidad-resultados-tabla.component.html',
  styleUrls: ['./rentabilidad-resultados-tabla.component.css'],
})
export class RentabilidadResultadosTablaComponent {
  @Input() resultados: RentabilidadResultado[] | null = null;
  @Input() isLoading: boolean = false;
  @Input() error: string | null = null;
}
