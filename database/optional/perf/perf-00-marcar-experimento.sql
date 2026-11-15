/* =============================================================================
   perf-00-marcar-experimento.sql
   Marca o banco MiniGestaoPedidos como ambiente de experimento de performance.

   Obrigatorio antes de perf-01-gerar-volume.sql.
   Uso: apenas em banco descartavel (projeto Compose isolado). Nao rode no
   ambiente de desenvolvimento permanente.
   ============================================================================= */

SET NOCOUNT ON;
GO

USE [MiniGestaoPedidos];
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.extended_properties
    WHERE class = 0
      AND name = N'PerfExperiment'
)
BEGIN
    EXEC sys.sp_addextendedproperty
        @name = N'PerfExperiment',
        @value = N'1';
    PRINT N'perf-00: propriedade PerfExperiment=1 criada no banco.';
END
ELSE
BEGIN
    EXEC sys.sp_updateextendedproperty
        @name = N'PerfExperiment',
        @value = N'1';
    PRINT N'perf-00: propriedade PerfExperiment atualizada para 1.';
END
GO
