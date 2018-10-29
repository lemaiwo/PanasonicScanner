package PanasonicScanner;

import android.os.AsyncTask;

import org.apache.cordova.CordovaPlugin;
import org.apache.cordova.CallbackContext;

import org.apache.cordova.CordovaWebView;
import org.apache.cordova.PluginResult;
import org.apache.cordova.CordovaInterface;

import android.util.Log;
import java.util.ArrayList;
import java.util.List;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import com.panasonic.toughpad.android.api.ToughpadApi;
import com.panasonic.toughpad.android.api.ToughpadApiListener;
import com.panasonic.toughpad.android.api.barcode.BarcodeData;
import com.panasonic.toughpad.android.api.barcode.BarcodeException;
import com.panasonic.toughpad.android.api.barcode.BarcodeListener;
import com.panasonic.toughpad.android.api.barcode.BarcodeReader;
import com.panasonic.toughpad.android.api.barcode.BarcodeReaderManager;
import java.util.concurrent.TimeoutException;

/**
 * This class echoes a string called from JavaScript.
 */
public class PanasonicScanner extends CordovaPlugin  implements BarcodeListener, ToughpadApiListener{

    public static final String ARG_ITEM_ID = "item_id";
    public static final String ACTION_ITEM_ID = ARG_ITEM_ID;
    public static final String TAG = "SCANNER.PANASONIC";
    private CallbackContext callbackContextReference;
    private String message = "";
	
    private class EnableReaderTask extends AsyncTask<BarcodeReader, Void, Boolean> {
                
        @Override
        protected void onPreExecute(){
        }
        @Override
        protected Boolean doInBackground(BarcodeReader... params) {
            try {
                params[0].addBarcodeListener(PanasonicScanner.this);
                return true;
            } catch (Exception ex) {
                Log.e(TAG, ex.toString());
                return false;
            }
        }
        @Override
        protected void onPostExecute(Boolean result) {
        }
    }
	@Override
	public void initialize(CordovaInterface cordova, CordovaWebView webView) {
		try{
			ToughpadApi.initialize(cordova.getActivity(), this);
		}catch(Exception ex){
			Log.e(TAG, ex.toString());
		}
	}
	
    @Override
    public boolean execute(String action, JSONArray args, CallbackContext callbackContext) throws JSONException {
        if (action.equals("coolMethod")) {
            String message = args.getString(0);
            this.coolMethod(message, callbackContext);
            return true;
        }else if(action.equals("getDevices")){
			this.getAllReaders(callbackContext);
			return true;
		}else if(action.equals("activate")){
			//this.activateReader(args.getInt(0),callbackContext);
			this.activateReader(callbackContext);
			return true;
		}
        return false;
    }

    private void coolMethod(String message, CallbackContext callbackContext) {
        //if (message != null && message.length() > 0) {
            //callbackContext.success("hello test2,"+message);
			JSONObject json = new JSONObject();
			try {
				json.put("foo", "bar");
			} catch (JSONException e) {
				Log.e(TAG, e.toString());
			}
			callbackContext.sendPluginResult(new PluginResult(PluginResult.Status.OK, json));
        /*} else {
            callbackContext.error("Expected one non-empty string argument.");
        }*/
    }
	private void getAllReaders(CallbackContext callbackContext) {
        List<BarcodeReader> readers = BarcodeReaderManager.getBarcodeReaders();
        
		try {
			JSONObject parameter = new JSONObject();
			int i = 0;
			for (BarcodeReader reader : readers) {
				i++;
				parameter.put("deviceid"+i, reader.getDeviceName());
			}
			PluginResult result = new PluginResult(PluginResult.Status.OK, parameter);
			result.setKeepCallback(true);
			callbackContext.sendPluginResult(result);

		} catch (JSONException e) {
			Log.e(TAG, e.toString());
		}
    }
	private void activateReader(CallbackContext callbackContext){
		try {
			List<BarcodeReader> readers = BarcodeReaderManager.getBarcodeReaders();
			BarcodeReader selectedReader = readers.get(0);
			//selectedReader.addBarcodeListener(this);
			EnableReaderTask task = new EnableReaderTask();
			task.execute(selectedReader);
			callbackContextReference = callbackContext;
		} catch (Exception e) {
			Log.e(TAG, e.toString());
		}
		
	}
    @Override
    public void onRead(BarcodeReader paramBarcodeReader, BarcodeData paramBarcodeData)
    {
          //String strDeviceId =  paramBarcodeData.getDeviceId();
          String strBarcodeData =  paramBarcodeData.getTextData();
          String strSymbologyId = paramBarcodeData.getSymbology();
          message = strBarcodeData;
          cordova.getActivity().runOnUiThread(new Runnable() {
              public void run() {
                  //Toast toast = Toast.makeText(cordova.getActivity().getApplicationContext(), message, duration);
                  //toast.show();
				PluginResult result = new PluginResult(PluginResult.Status.OK, message);
				result.setKeepCallback(true);
				callbackContextReference.sendPluginResult(result);
              }
          });
     }
	 
    public void onApiConnected(int version) {
        /*List<BarcodeReader> readers = BarcodeReaderManager.getBarcodeReaders();
        
        List<String> readerNames = new ArrayList<String>();
        for (BarcodeReader reader : readers) {
            readerNames.add(reader.getDeviceName());
        }
        */
    }
    
    public void onApiDisconnected() {
    }
}
