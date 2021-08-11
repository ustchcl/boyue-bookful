using InceptionPlugins.Pluggables.Purchase;

namespace BoyuePurchase {
  public class BoyueProductMetadata : IProductMetadata {
    public string LocalizedPriceString { get; }
    public string IsoCurrencyCode { get; }
    public decimal LocalizedPrice { get; }

    public BoyueProductMetadata(
      string localizedPriceString,
      string isoCurrencyCode,
      decimal localizedPrice
    ) {
      LocalizedPrice = localizedPrice;
      IsoCurrencyCode = isoCurrencyCode;
      LocalizedPriceString = localizedPriceString;
    }
  }
}