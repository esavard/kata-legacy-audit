angular.module('warrantyApp').controller('LoginController', function (AuthService, $location) {
    var vm = this;
    vm.username = '';
    vm.password = '';
    vm.error = '';

    vm.login = function () {
        AuthService.login(vm.username, vm.password).then(function () {
            // full page reload instead of letting Angular route - a leftover from an
            // early bug where the navbar wouldn't refresh after login. "fixed" this way
            // instead of actually finding the binding issue.
            window.location.href = '#!/claims';
            window.location.reload();
        }, function (err) {
            vm.error = (err.data && err.data) || 'Login failed.';
        });
    };
});
