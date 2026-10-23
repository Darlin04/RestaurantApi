## Descripción

Closes #

ID del plan:

## Checklist DoD

- [ ] Todos los criterios de aceptación (Gherkin) del PBI están cubiertos por tests automatizados.
- [ ] Hay tests de dominio por cada regla nueva y tests de integración según la sección 6.
- [ ] `dotnet build -warnaserror` pasa sin errores ni warnings.
- [ ] `dotnet format --verify-no-changes` pasa sin cambios.
- [ ] `dotnet test` pasa completo: el suite entero, no solo los tests del PBI.
- [ ] Solo se tocaron los archivos del PBI. Cero archivos congelados.
- [ ] Commits con el formato de la sección 11.5, una Task por commit.
- [ ] El PR enlaza su Issue y tiene la DoD marcada.

## Decisiones

Ninguna.
