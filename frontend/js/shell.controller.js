angular.module('warrantyApp').controller('ShellController', function (AuthService, $location) {
    var shell = this;
    shell.role = AuthService.getRole();
    shell.fullName = AuthService.getFullName();
    shell.isLoggedIn = AuthService.isLoggedIn;
    shell.logout = function () {
        AuthService.logout();
        $location.path('/login');
    };
});
