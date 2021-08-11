namespace BoyuePurchase {
  public class MysqlProduct {
    public string in_app_store_id;
    public decimal price;
    public string receipt;
    public string localized_price_string;
    public string iso_currency_code;
    public decimal localized_price;

    public BoyueProduct ToBoyueProduct() {
      var metadata = new BoyueProductMetadata(localized_price_string, iso_currency_code, localized_price);
      return new BoyueProduct(in_app_store_id, price, "transaction_id", receipt, metadata);
    }
  }

  public class ProductsResponse {
    public int statusCode;
    public string message;
    public MysqlProduct[] products;
  }
}