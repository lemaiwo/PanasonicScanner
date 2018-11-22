var exec = require('cordova/exec');

exports.coolMethod = function(arg0, success, error) {
    exec(success, error, "PanasonicScanner", "coolMethod", [arg0]);
};
exports.getDevices = function(arg0, success, error) {
    exec(success, error, "PanasonicScanner", "getDevices", [arg0]);
};
exports.activate = function(arg0, success, error) {
    exec(success, error, "PanasonicScanner", "activate", [arg0]);
};
exports.deactivate = function(arg0, success, error) {
    exec(success, error, "PanasonicScanner", "deactivate", [arg0]);
}

});
