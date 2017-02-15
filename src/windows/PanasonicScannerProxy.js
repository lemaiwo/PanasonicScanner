cordova.commandProxy.add("PanasonicScanner",{
    activate:function(successCallback,errorCallback) {
		var ps = new PanasonicScanner.PanasonicScanner()
		var res = ps.activate();
		if (res) {
		    ps.onbarcodeevent = function (bc) {
		        if (bc && bc.detail && bc.detail[0] && bc.detail[0].code) {
		            successCallback(bc.detail[0].code, { keepCallback :true});
                }
		    };
        }else{
			errorCallback(res);
		}
    }
});
