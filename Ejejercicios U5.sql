--5.2.1.Eliminar todos los desencadenadores, excepto TR_CUENTAS1 y TR_MOVIMIENTOS2.
DISABLE TRIGGER ALL ON DATABASE;
 
-- Elimina todos los triggers excepto TR_CUENTAS1 y TR_MOVIMIENTOS2
DECLARE @trigger_name NVARCHAR(128);
 
DECLARE trigger_cursor CURSOR FOR
SELECT name 
FROM sys.triggers 
WHERE name NOT IN ('TR_CUENTAS1', 'TR_MOVIMIENTOS2');
 
OPEN trigger_cursor;
 
FETCH NEXT FROM trigger_cursor INTO @trigger_name;
 
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC('DROP TRIGGER ' + @trigger_name);
    FETCH NEXT FROM trigger_cursor INTO @trigger_name;
END;
 
CLOSE trigger_cursor;
DEALLOCATE trigger_cursor;
 
-- Habilita los triggers que no fueron eliminados
ENABLE TRIGGER ALL ON DATABASE;

/*5.2.2.Crear procedimientos almacenados para:
5.2.2.1. Agregar y/o modificar CLIENTES, tomando en cuenta:
5.2.2.1.1. No se aceptan clientes menores de 18 años de edad
5.2.2.1.2. El género del cliente debe ser masculino o femenino*/
CREATE PROCEDURE sp_AgregarModificarCliente
@idClave INT = NULL,
@nombre VARCHAR(50),
@appaterno VARCHAR(50),
@apmaterno VARCHAR(50),
@fechanac DATE,
@genero CHAR(1)
AS
BEGIN
IF @fechanac > DATEADD(year, -18, GETDATE())
BEGIN
RAISERROR('No se aceptan clientes menores de 18 años de edad', 16, 1)
RETURN
END

IF @genero NOT IN ('M', 'F')
BEGIN
    RAISERROR('El género del cliente debe ser masculino (M) o femenino (F)', 16, 1)
    RETURN
END

IF @idClave IS NULL
BEGIN
    INSERT INTO Clientes (nombre, appaterno, apmaterno, fechanac, genero)
    VALUES (@nombre, @appaterno, @apmaterno, @fechanac, @genero)
END
ELSE
BEGIN
    UPDATE Clientes
    SET nombre = @nombre,
        appaterno = @appaterno,
        apmaterno = @apmaterno,
        fechanac = @fechanac,
        genero = @genero
    WHERE clave = @idClave
END

END
GO

/*5.2.2.2. Abrir cuentas bancarias, tomando en cuenta:
5.2.2.2.1. Cada cliente solo puede tener una cuenta
5.2.2.2.2. Al abrir la cuenta debe crear su respectivo saldo en cero*/
CREATE PROCEDURE sp_AbrirCuenta
@numcta INT,
@cliente INT,
@numsuc INT,
@fechaap DATETIME
AS
BEGIN
-- Verificar si el cliente ya tiene una cuenta
IF EXISTS (SELECT 1 FROM Cuentas WHERE cliente = @cliente)
BEGIN
RAISERROR('El cliente ya tiene una cuenta', 16, 1)
RETURN
END

-- Insertar la nueva cuenta
INSERT INTO Cuentas (numcta, cliente, numsuc, fechaap)
VALUES (@numcta, @cliente, @numsuc, @fechaap)

END
GO

/*5.2.2.3. Realizar depósitos, retiros y/o transferencias entre cuentas.
Debe dejar en todos los casos, registro de cada operación en la tabla MOVIMIENTOS, 
así como también considerar las validaciones necesarias.*/
CREATE TRIGGER TR_EliminarCuentaVieja
ON cuentas
AFTER DELETE
AS
BEGIN
    DECLARE @numcta CHAR(12);
    DECLARE @fechaap SMALLDATETIME;
    DECLARE @saldo MONEY;
    DECLARE @rowcount INT;
 
    -- Obtener los datos de la cuenta eliminada
    SELECT @numcta = numcta, @fechaap = fechaap
    FROM deleted;
 
    -- Verificar si la cuenta tiene al menos 90 días de antigüedad
    IF DATEDIFF(DAY, @fechaap, GETDATE()) < 90
    BEGIN
        RAISERROR ('La cuenta debe tener al menos 90 días de antigüedad.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
 
    -- Verificar si el saldo de la cuenta es cero
    SELECT @saldo = saldo FROM cuentasaldo WHERE numcta = @numcta;
    IF @saldo != 0
    BEGIN
        RAISERROR ('El saldo de la cuenta debe estar en cero.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
 
    -- Verificar si no tiene movimientos registrados
    IF EXISTS (SELECT 1 FROM movimientos WHERE numcta = @numcta)
    BEGIN
        RAISERROR ('La cuenta no debe tener movimientos registrados.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
 
    -- Registrar la fecha de baja en una variable
    DECLARE @fecha_baja SMALLDATETIME = GETDATE();
 
    -- Eliminar registros de cuentasaldo y movimientos
    DELETE FROM cuentasaldo WHERE numcta = @numcta;
    DELETE FROM movimientos WHERE numcta = @numcta;
END;