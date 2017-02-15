module.exports.coolMethod = function (success, error, args) {
    if (args.length === 0 || typeof args[0] !== 'string' || args[0].length === 0) {
        error('Invalid arguments');
    } else {
        success(args[0]);
    }
};
