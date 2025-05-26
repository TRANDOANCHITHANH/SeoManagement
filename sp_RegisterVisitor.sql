USE [SeoManagementDB]
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_RegisterVisit]
AS
BEGIN
    SET NOCOUNT ON;

    -- Ghi nhận lượt truy cập
    DECLARE @VisitorCounterGanNhat BIGINT
    DECLARE @Count INT
    SELECT @Count = COUNT(*) FROM AccessStatistics
    IF @Count IS NULL SET @Count = 0
    IF @Count = 0
        INSERT INTO AccessStatistics(Time, VisitorCounter)
        VALUES (GETDATE(), 1)
    ELSE
    BEGIN
        DECLARE @TimeGanNhat DATETIME
        SELECT @VisitorCounterGanNhat = tk.VisitorCounter, @TimeGanNhat = tk.Time 
        FROM AccessStatistics tk
        WHERE tk.Id = (SELECT MAX(tk1.Id) FROM AccessStatistics tk1)
        IF @VisitorCounterGanNhat IS NULL SET @VisitorCounterGanNhat = 0
        IF @TimeGanNhat IS NULL SET @TimeGanNhat = GETDATE()

        -- Nếu cùng ngày, tăng VisitorCounter
        IF DAY(@TimeGanNhat) = DAY(GETDATE()) AND MONTH(@TimeGanNhat) = MONTH(GETDATE()) AND YEAR(@TimeGanNhat) = YEAR(GETDATE())
        BEGIN
            UPDATE AccessStatistics
            SET VisitorCounter = @VisitorCounterGanNhat + 1,
                Time = GETDATE()
            WHERE Id = (SELECT MAX(tk1.Id) FROM AccessStatistics tk1)
        END
        -- Nếu sang ngày mới, thêm bản ghi mới
        ELSE
            INSERT INTO AccessStatistics(Time, VisitorCounter)
            VALUES (GETDATE(), 1)
    END
END