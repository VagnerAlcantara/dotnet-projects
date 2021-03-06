use TesteEntrevista
GO

IF EXISTS ( SELECT  *
            FROM    sys.objects
            WHERE   object_id = OBJECT_ID(N'PROC_FILME_INSERT')
                    AND type IN ( N'P', N'PC' ) ) 
BEGIN
	DROP PROCEDURE PROC_FILME_INSERT;
END
GO

CREATE PROCEDURE PROC_FILME_INSERT 
	@id int output,
	@titulo varchar(150) ,
	@ano  int
AS
BEGIN
	
	SET NOCOUNT ON;
	SET XACT_ABORT ON;
	SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
	
	BEGIN TRY
		
		BEGIN TRANSACTION;

		-- INSTRU��ES SQL -- IN�CIO

			INSERT INTO [dbo].[Filme]
					   ([Titulo]
					   ,[Ano])
				 VALUES
					   (@titulo
					   ,@ano)

		SELECT @id = SCOPE_IDENTITY();
		-- INSTRU��ES SQL -- FIM
		
		IF @@TRANCOUNT > 0 COMMIT TRANSACTION;
		
	END TRY
	
	BEGIN CATCH
		
		IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
		
		DECLARE @ERRORMESSAGE NVARCHAR(4000);
		DECLARE @ERRORSEVERITY INT;
		DECLARE @ERRORSTATE INT;

		SELECT @ERRORMESSAGE = ERROR_MESSAGE(),
			   @ERRORSEVERITY = ERROR_SEVERITY(),
			   @ERRORSTATE = ERROR_STATE();

		RAISERROR (@ERRORMESSAGE,
				   @ERRORSEVERITY,
				   @ERRORSTATE
				   );
			
	END CATCH
END