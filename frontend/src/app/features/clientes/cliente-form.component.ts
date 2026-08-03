import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, FormGroup } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { ClienteService } from '../../services/cliente.service';
import { TipoClienteService } from '../../services/tipo-cliente.service';
import { TipoCliente } from '../../core/models/tipo-cliente.model';

@Component({
  selector: 'app-cliente-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatSelectModule],
  templateUrl: './cliente-form.component.html',
  styleUrl: './cliente-form.component.scss'
})
export class ClienteFormComponent implements OnInit {
  readonly isEdit = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly tiposClientes = signal<TipoCliente[]>([]);
  private clienteId: number | null = null;

  form!: FormGroup;

  constructor(
    private fb: FormBuilder,
    private clienteService: ClienteService,
    private tipoClienteService: TipoClienteService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      nombre: ['', Validators.required],
      telefono: [''],
      identidadORTN: [''],
      correo: [''],
      direccion: [''],
      tipoClienteId: [null]
    });

    this.tipoClienteService.getActivos().subscribe({
      next: (res) => {
        this.tiposClientes.set(res.data);
      }
    });

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEdit.set(true);
      this.clienteId = Number(idParam);
      this.clienteService.getById(this.clienteId).subscribe((res) => {
        this.form.patchValue(res.data);
      });
    }
  }

  submit(): void {
    if (this.form.invalid) return;

    this.saving.set(true);
    this.errorMessage.set(null);

    const value = this.form.getRawValue() as any;
    const request$ = this.isEdit()
      ? this.clienteService.update(this.clienteId!, { ...value, activo: true })
      : this.clienteService.create(value);

    request$.subscribe({
      next: () => { this.saving.set(false); this.router.navigate(['/clientes']); },
      error: (err) => { this.saving.set(false); this.errorMessage.set(err.error?.message ?? 'No se pudo guardar el cliente.'); }
    });
  }
}
