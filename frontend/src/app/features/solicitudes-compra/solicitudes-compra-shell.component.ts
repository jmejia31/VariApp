import { Component } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-solicitudes-compra-shell',
  standalone: true,
  imports: [MatIconModule],
  template: `
    <section class="solicitudes-shell" aria-labelledby="solicitudes-title">
      <div class="solicitudes-shell__icon" aria-hidden="true"><mat-icon>request_quote</mat-icon></div>
      <div>
        <h1 id="solicitudes-title">Solicitudes de compra</h1>
        <p>Flujo documental para preparar, enviar y decidir necesidades de compra antes de generar una compra transaccional.</p>
      </div>
    </section>
  `,
  styles: [`
    .solicitudes-shell {
      display: flex;
      align-items: flex-start;
      gap: 1rem;
      padding: 1.5rem;
      border-radius: 1rem;
      background: var(--app-surface, #fff);
      box-shadow: 0 1px 3px rgb(0 0 0 / 10%);
    }
    .solicitudes-shell__icon {
      display: grid;
      place-items: center;
      width: 3rem;
      height: 3rem;
      border-radius: .75rem;
      background: color-mix(in srgb, currentColor 10%, transparent);
      flex: 0 0 auto;
    }
    h1 { margin: 0 0 .5rem; font-size: clamp(1.5rem, 3vw, 2rem); }
    p { margin: 0; max-width: 70ch; line-height: 1.5; }
    @media (max-width: 600px) {
      .solicitudes-shell { padding: 1rem; }
    }
  `]
})
export class SolicitudesCompraShellComponent {}
