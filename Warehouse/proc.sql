CREATE OR ALTER PROCEDURE AddProductToWarehouse
    @IdProduct INT,
    @IdWarehouse INT,
    @Amount INT,
    @CreatedAt DATETIME,
    @IdProductWarehouse INT OUTPUT
    AS
BEGIN  
    
    DECLARE @IdProductFromDb INT, @IdOrder INT, @Price DECIMAL(25,2);

SELECT TOP 1 @IdOrder = o.IdOrder FROM "Order" o
                                           LEFT JOIN Product_Warehouse pw ON o.IdOrder = pw.IdOrder
WHERE o.IdProduct = @IdProduct
  AND o.Amount = @Amount
  AND pw.IdProductWarehouse IS NULL
  AND o.CreatedAt < @CreatedAt;

SELECT @IdProductFromDb = Product.IdProduct, @Price = Product.Price
FROM Product
WHERE IdProduct = @IdProduct;

IF @IdProductFromDb IS NULL
BEGIN  
        RAISERROR('Invalid parameter: Provided IdProduct does not exist', 18, 0);  
        RETURN;
END;  

    IF @Amount <= 0
BEGIN
        RAISERROR('Invalid parameter: Amount must be greater than 0', 18, 0);
        RETURN;
END;
    
    IF @IdOrder IS NULL
BEGIN  
        RAISERROR('Invalid parameter: There is no order to fulfill', 18, 0);  
        RETURN;
END;  
       
    IF NOT EXISTS(SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse)
BEGIN  
        RAISERROR('Invalid parameter: Provided IdWarehouse does not exist', 18, 0);  
        RETURN;
END;  
    
SET XACT_ABORT ON;
BEGIN TRAN;

UPDATE "Order"
SET FulfilledAt = @CreatedAt
WHERE IdOrder = @IdOrder;

INSERT INTO Product_Warehouse(
    IdWarehouse,
    IdProduct,
    IdOrder,
    Amount,
    Price,
    CreatedAt
)
VALUES(
          @IdWarehouse,
          @IdProduct,
          @IdOrder,
          @Amount,
          @Amount * @Price,
          @CreatedAt
      );

SET @IdProductWarehouse = SCOPE_IDENTITY();

COMMIT;
END