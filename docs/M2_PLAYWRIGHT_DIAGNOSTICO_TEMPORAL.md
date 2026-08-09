# Diagnóstico temporal M2

Fallos capturados: 1

## catalogos-mantenimientos.spec.ts > Mantenimientos reutilizables y correcciones críticas > Interfaz muestra mantenimientos, selectores de variantes, Agotado y diálogo accesible

- Archivo: `catalogos-mantenimientos.spec.ts:180`
- Estado: `failed`

```text
Error: expect.toBeVisible: Error: strict mode violation: locator('mat-select[formcontrolname="marcaId"]') resolved to 3 elements:
    1) <mat-select tabindex="0" role="combobox" id="mat-select-1" aria-invalid="false" aria-expanded="false" aria-required="false" aria-disabled="false" aria-haspopup="listbox" formcontrolname="marcaId" _ngcontent-ng-c627256823="" aria-describedby="mat-mdc-hint-3" aria-labelledby="mat-mdc-form-field-label-2" class="mat-mdc-select mat-mdc-select-empty ng-untouched ng-pristine ng-valid">…</mat-select> aka getByLabel('Marca predeterminada (')
    2) <mat-select tabindex="0" role="combobox" id="mat-select-4" aria-invalid="false" aria-expanded="false" aria-required="false" aria-disabled="false" aria-haspopup="listbox" formcontrolname="marcaId" _ngcontent-ng-c330511790="" aria-describedby="mat-mdc-hint-11" aria-labelledby="mat-mdc-form-field-label-6" class="mat-mdc-select mat-mdc-select-empty ng-untouched ng-pristine ng-valid">…</mat-select> aka getByLabel('Marca base (opcional)')
    3) <mat-select tabindex="0" role="combobox" id="mat-select-8" aria-invalid="false" aria-expanded="false" aria-required="false" aria-disabled="false" aria-haspopup="listbox" formcontrolname="marcaId" _ngcontent-ng-c627256823="" aria-labelledby="mat-mdc-form-field-label-14" class="mat-mdc-select mat-mdc-select-empty ng-untouched ng-pristine ng-valid">…</mat-select> aka getByLabel('Marca (opcional)')

Call log:
  - expect.toBeVisible with timeout 10000ms
  - waiting for locator('mat-select[formcontrolname="marcaId"]')

```
