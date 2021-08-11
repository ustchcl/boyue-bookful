using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;
using GeneralUtils.Extensions;
using GeneralUtils.Helpers;
using InceptionPurchaseFailureReason = InceptionPlugins.Pluggables.Purchase.PurchaseFailureReason;
using UnityPurchaseFailureReason = UnityEngine.Purchasing.PurchaseFailureReason;
using ProductType = UnityEngine.Purchasing.ProductType;
using InceptionPlugins.Pluggables.Purchase;
using PhoenixServices.Models;
using InAppPurchase;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace BoyuePurchase
{
    /// Source: https://unity3d.com/learn/tutorials/topics/ads-analytics/integrating-unity-iap-your-game
    /// Mods: removed unnecessary code + customized 
    // ReSharper disable once InconsistentNaming
    [CreateAssetMenu(menuName = "BoyuePlugins/StoreHandlerPlugin", fileName = "StoreHandlerPlugin")]
    public class BoyuePurchaseManager : ScriptableObject, IStoreListener, IStoreHandlerPlugin
    {
        public Action<(IProduct, InceptionPurchaseFailureReason?)> OnTransactionComplete;

        private IStoreController _storeController; // The Unity Purchasing system.
        private IExtensionProvider _storeExtensionProvider; // The store-specific Purchasing subsystems.
        private bool? _isInitialized;
        private (IProduct, InceptionPurchaseFailureReason?) _transaction;

        #region ScriptableObject initialization helper methods
        public void Init(IEnumerable<string> inAppProdIds)
        {
            // If we haven't set up the Unity Purchasing reference
            if (_isInitialized == true) return;

            // Set the default culture for all threads, for converting prices from US dollar to local price.
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("EN-US");

            // Begin to configure our connection to Purchasing
            InitializePurchasing(inAppProdIds);
        }

        public static BoyuePurchaseManager CreateInstance(IEnumerable<string> inAppProdIds)
        {
            var data = CreateInstance<BoyuePurchaseManager>();
            data.Init(inAppProdIds);
            return data;
        }
        #endregion

        #region IStoreListener interface
        // Cause we don't use UntiyEngine.Purchasing, these methods will never be called

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            Log("Initialize: PASS");
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Log($"Initialize: FAILED. Reason: {error}");
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product unityProduct, UnityPurchaseFailureReason failureReason)
        {
            Log($"OnPurchaseFailed: FAIL. Product: '{unityProduct.definition.storeSpecificId}', PurchaseFailureReason: {failureReason}");
        }

        #endregion

        #region IStoreHandlerPlugin interface
        public Task<(IProduct, InceptionPurchaseFailureReason?)> Purchase(string inAppProdId)
        {
            return InternalPurchase(inAppProdId);
        }

        public Task<bool> RestorePurchases(IEnumerable<string> inAppProductIds, Action<(IProduct, InceptionPurchaseFailureReason?)> callback)
        {
            return InternalRestorePurchases(inAppProductIds, callback);
        }

        public Task<IProductMetadata[]> GetProductsData(IEnumerable<string> inAppProdIds)
        {
            return InternalGetProductsData(inAppProdIds);
        }

        public void Dispose()
        {
            // TODO: anything else we need to dispose here?
            OnTransactionComplete = null;
        }
        #endregion

        public static string alipayResult = null;

        private static async Task<(IProduct, InceptionPurchaseFailureReason?)> InternalPurchase(string inAppProdId)
        {
            var config = BoyuePurchaseConfig.GetInstance();
            Debug.Log("Purchase Boyue");
            InitAndroid2Unity();
            var newInstance = CreateInstance(new[] { inAppProdId });
            // 1. Get BoyueProduct
            newInstance.Log("Get Boyue Production");
            Debug.LogFormat("inAppProdId: {0}", inAppProdId);
            var product = await config.GetProduct(inAppProdId);
            // 2. get signed order info
            newInstance.Log("Get Signed Order");
            var form = new Dictionary<string, string>();
            form.Add("subject", "柠檬阅读");
            form.Add("totalAmount", product.Metadata.LocalizedPrice.ToString());
            form.Add("outTradeNo", ((Int32)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds).ToString());
            var signRequest = UnityWebRequest.Post(config.RemoteServerUri + "/v1/pay/createSignedOrder", form);
            signRequest.SendWebRequest();
            await AwaitHelper.WaitUntil(() => signRequest.isDone, 60000);
            var signedOrderInfo = signRequest.downloadHandler.text;

            // 3. call alipay sdk to make the transaction
            newInstance.Log("Call Alipay");
            Debug.Log("Create Signed Order successfully");
            Debug.Log(signedOrderInfo);

            if (Application.platform == RuntimePlatform.Android)
            {
                try
                {
                    Debug.Log("call aliapy in android");
                    AndroidJavaObject _ajc = new AndroidJavaObject("com.unity3d.player.MainActivity");
                    _ajc.Call<string>("alipay", signedOrderInfo);

                }
                catch (Exception e)
                {
                    Debug.LogWarning("" + signedOrderInfo + " 请求失败");
                    Debug.LogWarning(e.Message);
                    throw;

                }
                finally
                {
                    Debug.Log("aliapy success maybe...");
                }
            }

            //#if UNITY_ANDROID
            //            Debug.Log("call aliapy in android");
            //            AndroidJavaObject currentActivity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
            //            currentActivity.Call("alipay", signedOrderInfo);
            //#endif

            await AwaitHelper.WaitUntil(() => alipayResult != null);

            // 4. check payResult's signature
            var payResult = JsonUtility.FromJson<AlipayResult>(alipayResult);
            // if (payResult.resultStatus != 9000) {
            //     return (null, InceptionPurchaseFailureReason.PaymentDeclined);
            // }
            var checkForm = new Dictionary<string, string>();
            checkForm.Add("key", "alipay_trade_app_pay");
            checkForm.Add("response", payResult.result);
            Debug.Log("Start check pay result Signature");
            var checkSignatureRequest = UnityWebRequest.Post(config.RemoteServerUri + "/v1/pay/checkResponseSign", checkForm);
            checkSignatureRequest.SendWebRequest();
            await AwaitHelper.WaitUntil(() => checkSignatureRequest.isDone, 60000);
            var jsonText = checkSignatureRequest.downloadHandler.text;
            Debug.LogFormat("checkResponse: {0}", jsonText);
	        var result = JsonUtility.FromJson<CheckSignatureResult>(jsonText);
            Debug.LogFormat("Result: {0}", result);

            // 5. pay success
            if (result.code == 200)
            {
                Debug.Log("OK, ontransaction complete");
                (IProduct, InceptionPurchaseFailureReason?) transaction = (product, null);
                newInstance.OnTransactionComplete?.Invoke(transaction);
                return transaction;
            }
            else
            {
                return (null, InceptionPurchaseFailureReason.SignatureInvalid);
            }
        }

        public static void AlipayResult(string result)
        {
            Debug.LogFormat("Message from android: {0}", result);
            BoyuePurchaseManager.alipayResult = result;
        }

        // 暂时不提供
        private static async Task<bool> InternalRestorePurchases(IEnumerable<string> inAppProductIds,
            Action<(IProduct, InceptionPurchaseFailureReason?)> callback)
        {
            LogStatic("RestorePurchases: Trying to restore all purchases. " +
                      $"(InApps: {inAppProductIds.JoinStr(",")})");

            var newInstance = CreateInstance(inAppProductIds);
            await AwaitHelper.WaitUntil(() => newInstance._isInitialized != null);
            newInstance.OnTransactionComplete = callback;
            return await newInstance.Restore();
        }

        private static async Task<IProductMetadata[]> InternalGetProductsData(IEnumerable<string> inAppProdIds)
        {
            // maybe here will be some http request here;
            // but for now, leave it alone
            var products = await BoyuePurchaseConfig.GetInstance().GetProducts(inAppProdIds);
            Debug.Log("ok...");
            var results = products.Select((x) => x.Metadata).ToArray();
            Debug.Log(results);
            return results;
        }

        private void InitializePurchasing(IEnumerable<string> inAppProdIds)
        {
            // Create a builder, first passing in a suite of Unity provided stores.
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            if (inAppProdIds != null)
            {
                foreach (var inAppProdId in inAppProdIds)
                {
                    if (inAppProdId.IsNullOrEmpty())
                    {
                        Debug.LogError("Product id is null!");
                        continue;
                    }

                    builder.AddProduct(inAppProdId, ProductType.NonConsumable);
                }
            }

            // Asynchronous call- expect response via OnInitialized / OnInitializeFailed
            UnityPurchasing.Initialize(this, builder);
        }

        private async Task<(IProduct, InceptionPurchaseFailureReason?)> BuyProductID(string inAppProdId)
        {
            // If Purchasing has been initialized ...
            if (_isInitialized == false)
            {
                // ... report the fact Purchasing has not succeeded initializing yet. Consider waiting longer or 
                // retrying initialization.
                Log($"BuyProductID FAIL (StoreId: {inAppProdId}). Not initialized.");
                return (null, InceptionPurchaseFailureReason.PurchasingUnavailable);
            }

            var product = _storeController.products.WithID(inAppProdId);

            // found product for this device's store and product can be sold 
            if (product != null && product.availableToPurchase)
            {
                Log($"BuyProductID: Purchasing product (async): '{product.definition.id}'");

                // Expect async response thru ProcessPurchase / OnPurchaseFailed 
                _storeController.InitiatePurchase(product);
            }
            else
            {
                Log($"BuyProductID: FAIL (StoreId: {inAppProdId}). " +
                    "Not purchasing product, either is not found or is not available for purchase");
                return (null, InceptionPurchaseFailureReason.ProductUnavailable);
            }

            await AwaitHelper.WaitUntil(() => _transaction.Item1 != null);
            return _transaction;
        }

        // Restore purchases previously made by this customer. Some platforms automatically restore purchases, like Google. 
        // Apple currently requires explicit purchase restoration for IAP, conditionally displaying a password prompt.
        private async Task<bool> Restore()
        {
            // If Purchasing has not yet been set up ...
            if (_isInitialized == false)
            {
                // ... report the situation and stop restoring. Consider either waiting longer, or retrying initialization.
                Log("Restore: FAIL. Not initialized.");
                return false;
            }

            // running on Apple device 
            if (Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                // restore purchases
                Log("RestorePurchases started ...");
                var taskCompletionSource = new TaskCompletionSource<bool>();
                _storeExtensionProvider.GetExtension<IAppleExtensions>()
                    .RestoreTransactions(taskCompletionSource.SetResult);
                return await taskCompletionSource.Task;
            }

            // We are not running on an Apple device
            // We need to do the restore ourselves because Android does not support manual restore
            var didFindPurchases = false;
            foreach (var unityProduct in _storeController.products.all)
            {
                if (unityProduct.IsPurchased())
                {
                    Log($"Restoring android purchase: {unityProduct.definition.storeSpecificId}. receipt: {unityProduct.receipt}");

                    var convertedProduct = ConvertUnityProduct(unityProduct);
                    OnTransactionComplete?.Invoke((convertedProduct, null));
                    didFindPurchases = true;
                }
            }

            return didFindPurchases;
        }

        private static void LogStatic(string message)
        {
            Debug.Log($"IAP: {message} [STATIC]");
        }

        private void Log(string message, LogType logType = LogType.Log)
        {
            var logMessage = $"IAP: {message} [instance {GetHashCode()}]";
            switch (logType)
            {
                case LogType.Log:
                    Debug.Log(logMessage);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(logMessage);
                    break;
                case LogType.Error:
                    Debug.LogError(logMessage);
                    break;
                case LogType.Exception:
                    Debug.LogException(new Exception(logMessage));
                    break;
                case LogType.Assert:
                    Debug.LogAssertion(logMessage);
                    break;
            }
        }

        private static IProduct ConvertUnityProduct(Product unityProduct)
        {
            var metadata = ConvertUnityMetadata(unityProduct.metadata);
            var product = new TransactionLM(
                unityProduct.definition.storeSpecificId,
                unityProduct.transactionID,
                unityProduct.receipt,
                metadata,
                null);

            return product;
        }

        private static IProductMetadata ConvertUnityMetadata(ProductMetadata unityMetadata)
        {
            var metadata = new TransactionMetadataLM(
                unityMetadata.localizedPrice,
                unityMetadata.isoCurrencyCode,
                unityMetadata.localizedPriceString);

            return metadata;
        }

        private static void InitAndroid2Unity()
        {
            if (GetSceneNode("Android2Unity") == null)
            {
                GameObject Prefab = (GameObject)Resources.Load("Android2Unity");
                Prefab = Instantiate(Prefab);
                Prefab.name = "Android2Unity";
                Prefab.transform.parent = null;
            }
        }
        private static GameObject GetSceneNode(string nodeName)
        {
            GameObject[] nodeList = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < nodeList.Length; i++)
            {
                if (nodeList[i].name == nodeName)
                {
                    return nodeList[i];
                }
            }
            return null;
        }
    }
}