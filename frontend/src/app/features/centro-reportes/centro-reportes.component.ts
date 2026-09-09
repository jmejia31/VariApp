import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

@Component({
  selector: 'app-centro-reportes',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './centro-reportes.component.html',
  styleUrl: './centro-reportes.component.scss'
})
export class CentroReportesComponent {
  readonly permisosRuntime = inject(PermisosRuntimeService);

  puedeVerFinancieros(): boolean {
    return this.permisosRuntime.puede('Finanzas', 'Ver');
  }

  puedeVerAdministrativos(): boolean {
    return this.permisosRuntime.esAdministrador()
      && this.permisosRuntime.puede('ReportesAdministrativos', 'Ver');
  }

  tieneReportesDisponibles(): boolean {
    return this.puedeVerFinancieros() || this.puedeVerAdministrativos();
  }
}
