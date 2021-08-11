package com.unity3d.player;

import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.util.Map;
import com.alipay.sdk.app.AuthTask;
import com.alipay.sdk.app.EnvUtils;
import com.alipay.sdk.app.PayTask;

import android.annotation.SuppressLint;
import android.app.Activity;
import android.app.AlertDialog;
import android.content.Context;
import android.content.DialogInterface;
import android.os.Bundle;
import android.os.Handler;
import android.os.Message;
import android.text.TextUtils;
import android.util.Log;
import android.view.View;
import android.widget.Toast;

import org.json.JSONObject;

import  com.unity3d.player.OrderInfoUtil2_0;

/**
 *  重要说明：
 *
 *  本 Demo 只是为了方便直接向商户展示支付宝的整个支付流程，所以将加签过程直接放在客户端完成
 *  （包括 OrderInfoUtil2_0_HK 和 OrderInfoUtil2_0）。
 *
 *  在真实 App 中，私钥（如 RSA_PRIVATE 等）数据严禁放在客户端，同时加签过程务必要放在服务端完成，
 *  否则可能造成商户私密数据泄露或被盗用，造成不必要的资金损失，面临各种安全风险。
 *
 *  Warning:
 *
 *  For demonstration purpose, the assembling and signing of the request parameters are done on
 *  the client side in this demo application.
 *
 *  However, in practice, both assembling and signing must be carried out on the server side.
 */
public class MainActivity extends UnityPlayerActivity {

    /**
     * 用于支付宝支付业务的入参 app_id。
     */
    public static final String APPID = "2021000117678405";

    /**
     * 用于支付宝账户登录授权业务的入参 pid。
     */
    public static final String PID = "";

    /**
     * 用于支付宝账户登录授权业务的入参 target_id。
     */
    public static final String TARGET_ID = "";

    /**
     *  pkcs8 格式的商户私钥。
     *
     * 	如下私钥，RSA2_PRIVATE 或者 RSA_PRIVATE 只需要填入一个，如果两个都设置了，本 Demo 将优先
     * 	使用 RSA2_PRIVATE。RSA2_PRIVATE 可以保证商户交易在更加安全的环境下进行，建议商户使用
     * 	RSA2_PRIVATE。
     *
     * 	建议使用支付宝提供的公私钥生成工具生成和获取 RSA2_PRIVATE。
     * 	工具地址：https://doc.open.alipay.com/docs/doc.htm?treeId=291&articleId=106097&docType=1
     */
    public static final String RSA2_PRIVATE = "MIIEogIBAAKCAQEAlOjs7o08jem6WIaNGJgguBhC4QlJ7aKKTqMKAwt6o/8Lh0MrzAk2RDxLIH5yfZ+VGP1fa4wYUPkBewoyKxvpkv5LFxPUu1mB8yB0ZkaMMzsVke+FlrP0Wn2DHAXpPW4NXWPs+O3+63AdZkslhY+deGOefaajcMPwYC6aIhHngdk1Nj6X4bWeIEiLbFbd3mAAAejXi0b5E89lovX/I4oS7mKV2205rGNP/LyQKdoqH2A3QzTcyL7HbXo02TKIV5meFwe3TZ2r51CNWoCRAzoGUc7t6RDKekeXjId8HLLWZ8cmNs1CVXGoHugF+8/8QlniePH6BXfduFfVfJHe+fC4VwIDAQABAoIBAEa2KTTKqtO1BsFc+4mPTBI/qxqTv6Bxy/21nF5/x/gyd7X+psCYK0XR39cJVLLk4fdg8jvu5eklpZdY6yPfeFZOHThTOXTR90yNv9iFbbQyPXb0Z9p9j+6cpXLcN7rtFLmhZvl6gtAoiD14N9Qps5xkqfF+/SPiV7ZEyvqcx/O3hRWxBl5lKWXMZPKEzdz+8Ydk589Q9EVAwLmz4oGauUXLTTJBPiKTkL6ciCZk2g3+s2ik9Du/NkiUIRq6WILu2ESM5Q3yKC414ofMy+igeqMTv1Ea2tJqLly9lXeeZkveb9uCtY4TXpKCF8uKB8BTnNtVd+uYPLJERAZKFgZZH9ECgYEA7+MZ1uqNlwx24fOYUCRIYUKuswYbOgH+hiwP6UIzAFe2TpUbQhMTiPY3mQ82WJTYryt1qwgUmVqd0cnHTnOowgsZChxnN2xKNA5sbfexYVcM4WdUSVw+WI8CGESIOA1QRcO8zS+L0aDZn6OlqcK8r6SRw9emeLAzqXEQDtWWTNkCgYEAnul0oJfFo4Cmba+lfGm1teAm7NY9WFeWmbRf4q+jEr+zBhZu61oW4lNyPrQSCNAGOXdaqnqstpwdpWVAxR7imBa3GNSHdHglKlZ5PH4gWoWVlZXY7xJMMu6EcTwOLIU3g7PF8kRAkJjzHJ5AfSOQBvxVuLEFSjHQdlqKicglsK8CgYAQW6cmbaYsF7B/pfHL/T1mTHrHQHJY2Uv28EhBer8pldGbuDU8ozpgy5FtAYmOLtA72bXTbOCiuHgslxrdMavfV4xkkR1EvUCuHhGsyggxgBMjx70Kp5ykw1O8PeggEmBX8YoNX+Gj8NkGNs2ULvBY9druxOTYNAPB2TDkIhJ2uQKBgCbQMIs05rHrrzqlJw8/JoG4sOkx3qPgKMl789uDOJpQtiIrgoX7aBF5fjLwemMqpAqPK1buRZcIhHksROighYnOKoRnDHqXCcTQgCYVTEOv0vxJMEYcOv5JoZGisHeRRiDl3NgycW6f3OlFgczOzdPW9/z3R6p6hkcSEDpc50/DAoGAR7GL18ZJQ/ZPEao7B3pAvrF+2h4FPrUiSYj/e885vx4fZLoOkd+rbfcw5jUx1J8r2rrf4U80pCK3MR0FocAgUq4CBXqoQNcGEPaWypdDhutyeR/Ir07SKZBElU5esPzBXgYYIh19EAr43JP5p/qLtfBwaKcGgp+x5j+TWQiXKzw=";
    public static final String RSA_PRIVATE = "";

    private static final int SDK_PAY_FLAG = 1;
    private static final int SDK_AUTH_FLAG = 2;

    /**
     * unity项目启动时的的上下文
     */
    public static Activity _unityActivity;
    /**
     * 获取unity项目的上下文
     * @return
     */
    public static Activity getActivity(){
        if(null == _unityActivity) {
            try {
                Class<?> classtype = Class.forName("com.unity3d.player.UnityPlayer");
                Activity activity = (Activity) classtype.getDeclaredField("currentActivity").get(classtype);
                _unityActivity = activity;
            } catch (ClassNotFoundException e) {

            } catch (IllegalAccessException e) {

            } catch (NoSuchFieldException e) {

            }
        }
        return _unityActivity;
    }

    @SuppressLint("HandlerLeak")
    private Handler mHandler = new Handler() {
        @SuppressWarnings("unused")
        public void handleMessage(Message msg) {
            switch (msg.what) {
                case SDK_PAY_FLAG: {
                    @SuppressWarnings("unchecked")
                    PayResult payResult = new PayResult((Map<String, String>) msg.obj);
                    /**
                     * 对于支付结果，请商户依赖服务端的异步通知结果。同步通知结果，仅作为支付结束的通知。
                     */
                    String resultInfo = payResult.getResult();// 同步返回需要验证的信息
                    String resultStatus = payResult.getResultStatus();
                    // 判断resultStatus 为9000则代表支付成功
                    if (TextUtils.equals(resultStatus, "9000")) {
                        // 该笔订单是否真实支付成功，需要依赖服务端的异步通知。
                        showAlert(MainActivity.this, getString(R.string.pay_success) + payResult);
                    } else {
                        // 该笔订单真实的支付结果，需要依赖服务端的异步通知。
                        showAlert(MainActivity.this, getString(R.string.pay_failed) + payResult);
                    }
                    break;
                }
                default:
                    break;
            }
        };
    };

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        EnvUtils.setEnv(EnvUtils.EnvEnum.SANDBOX);
        super.onCreate(savedInstanceState);
        // setContentView(R.layout.pay_main);
    }

    // method called by unity
    public String alipay( String signedOrderInfo) {
        EnvUtils.setEnv(EnvUtils.EnvEnum.SANDBOX);
        System.out.println("start alipay");
        // 测试发送数据到unity
        callUnity("Android2Unity","TestAndroid2Unity",signedOrderInfo);
        if (TextUtils.isEmpty(APPID) || (TextUtils.isEmpty(RSA2_PRIVATE) && TextUtils.isEmpty(RSA_PRIVATE))) {
            System.out.println(getString(R.string.error_missing_appid_rsa_private));
            return getString(R.string.error_missing_appid_rsa_private);
        }
        final Runnable payRunnable = new Runnable() {

            @Override
            public void run() {
                PayTask alipay = new PayTask(getActivity());
                System.out.println("------- From Alipay Plugin -----");
                System.out.println(signedOrderInfo);

                Map<String, String> result = alipay.payV2(signedOrderInfo, true);
                JSONObject jsonObj = new JSONObject(result);
                Log.i("msp", jsonObj.toString());
                callUnity("Android2Unity","AlipayResult", jsonObj.toString());
//                UnityPlayer.UnitySendMessage("StoreHandlerPlugin", "AlipayResult", result.toString());
            }
        };
        // 必须异步调用
        Thread payThread = new Thread(payRunnable);
        payThread.start();
        return "yes";
    }

    /**
     * 支付宝支付业务示例, 先使用demo
     */
    public void payV2(View v) {
        if (TextUtils.isEmpty(APPID) || (TextUtils.isEmpty(RSA2_PRIVATE) && TextUtils.isEmpty(RSA_PRIVATE))) {
            showAlert(this, getString(R.string.error_missing_appid_rsa_private));
            return;
        }

        /*
         * 这里只是为了方便直接向商户展示支付宝的整个支付流程；所以Demo中加签过程直接放在客户端完成；
         * 真实App里，privateKey等数据严禁放在客户端，加签过程务必要放在服务端完成；
         * 防止商户私密数据泄露，造成不必要的资金损失，及面临各种安全风险；
         *
         * orderInfo 的获取必须来自服务端；
         */
        boolean rsa2 = (RSA2_PRIVATE.length() > 0);
        Map<String, String> params = OrderInfoUtil2_0.buildOrderParamMap(APPID, rsa2);
        String orderParam = OrderInfoUtil2_0.buildOrderParam(params);

        String privateKey = rsa2 ? RSA2_PRIVATE : RSA_PRIVATE;
        String sign = OrderInfoUtil2_0.getSign(params, privateKey, rsa2);


        final String orderInfo = orderParam + "&" + sign;

        System.out.println(orderInfo);

        final Runnable payRunnable = new Runnable() {

            @Override
            public void run() {
                PayTask alipay = new PayTask(MainActivity.this);

                System.out.println("------------");
                System.out.println(orderInfo);


                Map<String, String> result = alipay.payV2(orderInfo, true);
                Log.i("msp", result.toString());

                Message msg = new Message();
                msg.what = SDK_PAY_FLAG;
                msg.obj = result;
                mHandler.sendMessage(msg);
            }
        };

        // 必须异步调用
        Thread payThread = new Thread(payRunnable);
        payThread.start();
    }

    /**
     * 支付宝账户授权业务示例
     */
    public void authV2(View v) {
        if (TextUtils.isEmpty(PID) || TextUtils.isEmpty(APPID)
                || (TextUtils.isEmpty(RSA2_PRIVATE) && TextUtils.isEmpty(RSA_PRIVATE))
                || TextUtils.isEmpty(TARGET_ID)) {
            showAlert(this, getString(R.string.error_auth_missing_partner_appid_rsa_private_target_id));
            return;
        }

        /*
         * 这里只是为了方便直接向商户展示支付宝的整个支付流程；所以Demo中加签过程直接放在客户端完成；
         * 真实App里，privateKey等数据严禁放在客户端，加签过程务必要放在服务端完成；
         * 防止商户私密数据泄露，造成不必要的资金损失，及面临各种安全风险；
         *
         * authInfo 的获取必须来自服务端；
         */
        boolean rsa2 = (RSA2_PRIVATE.length() > 0);
        Map<String, String> authInfoMap = OrderInfoUtil2_0.buildAuthInfoMap(PID, APPID, TARGET_ID, rsa2);
        String info = OrderInfoUtil2_0.buildOrderParam(authInfoMap);

        String privateKey = rsa2 ? RSA2_PRIVATE : RSA_PRIVATE;
        String sign = OrderInfoUtil2_0.getSign(authInfoMap, privateKey, rsa2);
        final String authInfo = info + "&" + sign;
        Runnable authRunnable = new Runnable() {

            @Override
            public void run() {
                // 构造AuthTask 对象
                AuthTask authTask = new AuthTask(MainActivity.this);
                // 调用授权接口，获取授权结果
                Map<String, String> result = authTask.authV2(authInfo, true);

                Message msg = new Message();
                msg.what = SDK_AUTH_FLAG;
                msg.obj = result;
                mHandler.sendMessage(msg);
            }
        };

        // 必须异步调用
        Thread authThread = new Thread(authRunnable);
        authThread.start();
    }

    /**
     * 获取支付宝 SDK 版本号。
     */
    public void showSdkVersion(View v) {
        PayTask payTask = new PayTask(this);
        String version = payTask.getVersion();
        showAlert(this, getString(R.string.alipay_sdk_version_is) + version);
    }

    /**
     * 将 H5 网页版支付转换成支付宝 App 支付的示例
     */
    public void h5Pay(View v) {
    }

    private static void showAlert(Context ctx, String info) {
        showAlert(ctx, info, null);
    }

    private static void showAlert(Context ctx, String info, DialogInterface.OnDismissListener onDismiss) {
        new AlertDialog.Builder(ctx)
                .setMessage(info)
                .setPositiveButton(R.string.confirm, null)
                .setOnDismissListener(onDismiss)
                .show();
    }

    private static void showToast(Context ctx, String msg) {
        Toast.makeText(ctx, msg, Toast.LENGTH_LONG).show();
    }

    private static String bundleToString(Bundle bundle) {
        if (bundle == null) {
            return "null";
        }
        final StringBuilder sb = new StringBuilder();
        for (String key: bundle.keySet()) {
            sb.append(key).append("=>").append(bundle.get(key)).append("\n");
        }
        return sb.toString();
    }

    public static boolean callUnity(String gameObjectName, String functionName, String args) {
        try {
            Class<?> classtype = Class.forName("com.unity3d.player.UnityPlayer");
            Method method = classtype.getMethod("UnitySendMessage", String.class, String.class, String.class);
            method.invoke(classtype, gameObjectName, functionName, args);
            return true;
        } catch (ClassNotFoundException var5) {
        } catch (NoSuchMethodException var6) {
        } catch (IllegalAccessException var7) {
        } catch (InvocationTargetException var8) {
        }

        return false;
    }
}
