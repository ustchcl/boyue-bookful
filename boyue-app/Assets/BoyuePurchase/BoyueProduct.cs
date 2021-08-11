using InceptionPlugins.Pluggables.Purchase;

namespace BoyuePurchase
{
  public class BoyueProduct : IProduct
  {
    public string InAppStoreId { get; }
    public decimal Price { get; }
    public string TransactionID { get; }
    public string Receipt { get; }
    public PurchaseFailureReason? FailureReason { get; }
    public IProductMetadata Metadata { get; }

    public BoyueProduct(
      string storeId,
      decimal price,
      string transactionId,
      string receipt,
      BoyueProductMetadata metadata
    )
    {
      this.InAppStoreId = storeId;
      this.Price = price;
      this.TransactionID = transactionId;
      this.Receipt = receipt;
      this.FailureReason = null;
      this.Metadata = metadata;
    }
  }
}