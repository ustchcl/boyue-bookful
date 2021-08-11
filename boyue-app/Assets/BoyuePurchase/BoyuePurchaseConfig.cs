using InceptionPlugins.Pluggables.Purchase;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Threading.Tasks;
using GeneralUtils.Helpers;
using UnityEngine;

namespace BoyuePurchase {

  public class BoyuePurchaseConfig {
    static BoyuePurchaseConfig _instance = null;
    BoyueProduct[] products;
    private bool fetched = false;

    public readonly string RemoteServerUri = "http://47.106.97.210:3056";

    private BoyuePurchaseConfig() {
      products = new BoyueProduct[]{};
    }

    public async Task<BoyueProduct[]> GetAllProducts() {
      if (!this.fetched) {
        products = await FetchAllProducts();
      }
      return products;
    }

    public async Task<BoyueProduct[]> GetProducts(IEnumerable<string> ids) {
      var allProducts = await GetAllProducts();
      return allProducts.Where(x => ids.Contains(x.InAppStoreId)).ToArray();
    }

    public static BoyuePurchaseConfig GetInstance() {
      if (_instance == null) {
        _instance = new BoyuePurchaseConfig();
      }      
      return _instance;
    }

    public async Task<BoyueProduct> GetProduct(string inAppProId) {
      var allProducts = await GetAllProducts();
      return allProducts.First(x => x.InAppStoreId == inAppProId);
    }

    public Task<BoyueProduct[]> FetchAllProducts() {
      // var productRequest = UnityWebRequest.Get(RemoteServerUri + "/v1/product/");
      // productRequest.SendWebRequest();
      // await AwaitHelper.WaitUntil(() => productRequest.isDone);
      // var jsonText = productRequest.downloadHandler.text;
      // var result = JsonUtility.FromJson<ProductsResponse>(jsonText);

      return Task.Run(() => {
        return new BoyueProduct[] {
          new BoyueProduct("boyue_store_android_30d", 30, "", "receipt", new BoyueProductMetadata("30元", "CNY", 30)),
          new BoyueProduct("boyue_store_android_360d", 300, "", "receipt", new BoyueProductMetadata("300元", "CNY", 300)),
          new BoyueProduct("boyue_store_android_90d", 80, "", "receipt", new BoyueProductMetadata("80元", "CNY", 80)),
        };
      });
    }
  }
}