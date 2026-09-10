import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { ReporteRentabilidadDto } from '../../../../core/models/reporte-rentabilidad.models';

@Component({
  selector: 'app-rentabilidad-resultados-tabla',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './rentabilidad-resultados-tabla.component.html',
  styleUrls: ['./rentabilidad-resultados-tabla.component.css'],
})
export class RentabilidadResultadosTablaComponent {
  @Input() resultados: readonly ReporteRentabilidadDto[] | null = null;
  @Input() isLoading = false;
  @Input() error: string | null = null;
}
